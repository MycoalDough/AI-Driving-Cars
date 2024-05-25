import torch
import torch.nn as nn
import torch.nn.functional as F
import torch.optim as optim
import numpy as np
from collections import deque
import random
import os

class ReplayBuffer():
    def __init__(self, mem_size, input_shape):
        self.mem_size = mem_size
        self.buffer = deque(maxlen=mem_size)
        self.input_size = input_shape
        self.mem_cntr = 0

    def add(self, state, action, reward, state_, done):
        experience = (state, action, reward, state_, done)
        self.buffer.append(experience)
        self.mem_cntr += 1

    def sample(self, batch):
        batch = random.sample(self.buffer, batch)
        states, actions, rewards, states_, dones = zip(*batch)

        return(np.array(states),
               np.array(actions),
               np.array(rewards),
               np.array(states_),
               np.array(dones))
    

    def __len__(self):
        return len(self.buffer)

        

class D3QN(nn.Module):
    def __init__(self, lr, input_dims, n_actions, checkpoint_dir, name):
        super(D3QN, self).__init__()
        self.checkpoint_dir = checkpoint_dir
        self.checkpoint_file = os.path.join(self.checkpoint_dir, name)

        self.input_dims = input_dims
        self.n_actions = n_actions

        self.fc1 = nn.Linear(*self.input_dims, 32)
        self.V = nn.Linear(32, 1)
        self.A = nn.Linear(32, self.n_actions)

        self.optim = optim.Adam(self.parameters(), lr=lr)
        self.loss = nn.MSELoss()
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        self.to(device=self.device)

    def forward(self, state):
        flat1 = F.relu(self.fc1(state))
        V = self.V(flat1)
        A = self.A(flat1)

        return V,A
    
    def save_checkpoint(self):
        print("-- saving checkpoint --")
        torch.save(self.state_dict(), self.checkpoint_file)

    def load_checkpoint(self):
        print("-- loading checkpoint --")
        self.load_state_dict(torch.load(self.checkpoint_file))


class Agent():
    def __init__(self, gamma, epsilon, lr, n_actions, input_dims, mem_size, batch_size,checkpoint_name, eps_min = 0.01, eps_dec=5e-7, replace=1000, checkpoint_dir='models'):
        self.gamma = gamma
        self.epsilon = epsilon
        self.lr = lr
        self.n_actions = n_actions
        self.input_dims = input_dims
        self.mem_size = mem_size
        self.batch_size = batch_size
        self.eps_min = eps_min
        self.eps_dec = eps_dec
        self.replace_target_count = replace
        self.checkpoint_dir = checkpoint_dir
        self.learn_step_counter = 0
        self.action_space = [i for i in range(self.n_actions)]
        self.checkpoint_name = checkpoint_name

        self.memory = ReplayBuffer(50_000, self.input_dims)

        self.q_eval = D3QN(self.lr, input_dims=self.input_dims, n_actions=self.n_actions, checkpoint_dir=self.checkpoint_dir, name=self.checkpoint_name+ "_eval")
        self.q_next = D3QN(self.lr, input_dims=self.input_dims, n_actions=self.n_actions, checkpoint_dir=self.checkpoint_dir, name=self.checkpoint_name + "_next")

    def choose_action(self, observation):
        if(np.random.random() > self.epsilon):
            state = torch.tensor([observation], dtype=torch.float).to(self.q_eval.device)
            _, advantage = self.q_eval.forward(state)
            action = torch.argmax(advantage).item()
            randomness = "AI"
        else:
            action = np.random.choice(self.action_space)
            randomness = "Epsilon"

        return action, randomness
    
    def store_transition(self, state, action, reward, state_, done):
        self.memory.add(state, action, reward, state_, done)

    def replace_target_network(self):
        if self.learn_step_counter % self.replace_target_count == 0:
            self.q_next.load_state_dict(self.q_eval.state_dict())

    def decrease_epsilon(self):
        self.epsilon = self.epsilon - self.eps_dec \
            if self.epsilon > self.eps_min else self.eps_min

    def save_models(self):
        self.q_eval.save_checkpoint()
        self.q_next.save_checkpoint()

    def load_models(self):
        self.q_eval.load_checkpoint()
        self.q_next.load_checkpoint()

    def learn(self):
        if self.memory.mem_cntr < self.batch_size:
            return

        self.q_eval.optim.zero_grad()

        self.replace_target_network()

        states, actions, rewards, states_, dones = self.memory.sample(self.batch_size)

        states = torch.tensor(states, dtype=torch.float).to(self.q_eval.device)
        actions = torch.tensor(actions, dtype=torch.long).to(self.q_eval.device)
        rewards = torch.tensor(rewards, dtype=torch.float).to(self.q_eval.device)
        states_ = torch.tensor(states_, dtype=torch.float).to(self.q_eval.device)
        dones = torch.tensor(dones, dtype=torch.bool).to(self.q_eval.device)

        indices = np.arange(self.batch_size)
        V_s, A_s = self.q_eval.forward(states)
        V_s_, A_s_ = self.q_next.forward(states_)
        
        q_pred = V_s + (A_s - A_s.mean(dim=1, keepdim=True))
        q_next = V_s_ + (A_s_ - A_s_.mean(dim=1, keepdim=True))

        q_target = rewards + self.gamma * torch.max(q_next, dim=1)[0] * (~dones)
        
        q_pred = q_pred[indices, actions]
        loss = self.q_eval.loss(q_pred, q_target).to(self.q_eval.device)

        loss.backward()
        self.q_eval.optim.step()
        self.learn_step_counter += 1
        self.decrease_epsilon() 