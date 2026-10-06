# Autonomous Driving with Deep Reinforcement Learning

A **deep reinforcement learning autonomous-driving experiment** built as a **4-hour development challenge**.

The project combines a custom **Unity/C# driving environment** with a **Python + PyTorch Dueling Double Deep Q-Network (D3QN)** agent. The goal was to build an end-to-end reinforcement-learning system capable of learning vehicle-control behavior through interaction with a simulated environment — from scratch and within a strict time constraint.

## Overview

Rather than explicitly programming a vehicle with rules such as:

```text
If obstacle is left → steer right
If road turns → follow predefined path
```

the agent learns behavior through **trial and error**.

At each timestep:

```text
Observe environment
        ↓
Choose driving action
        ↓
Apply action in Unity
        ↓
Receive new state + reward
        ↓
Store experience
        ↓
Train neural network
```

Over repeated interactions, the agent learns which actions are associated with better long-term outcomes.

The project was intentionally scoped as a rapid prototyping challenge: build the simulation, connect it to a Python learning system, implement the reinforcement-learning algorithm, and produce an autonomous agent within approximately **four hours**.

---

# Architecture

```text
                 ┌───────────────────────────────┐
                 │          Unity / C#           │
                 │                               │
                 │   Driving Environment         │
                 │   • Vehicle simulation        │
                 │   • Environment state         │
                 │   • Collision detection       │
                 │   • Rewards                   │
                 │   • Episode reset             │
                 └───────────────┬───────────────┘
                                 │
                           Socket Interface
                                 │
                 ┌───────────────▼───────────────┐
                 │       Python / PyTorch        │
                 │                               │
                 │         D3QN Agent            │
                 │   • Dueling architecture      │
                 │   • Double Q-learning         │
                 │   • Experience replay         │
                 │   • Target network            │
                 │   • ε-greedy exploration      │
                 └───────────────────────────────┘
```

Unity handles the physical simulation while Python handles the learning algorithm.

The two systems communicate through **sockets**, allowing the game environment and neural-network training code to remain separate.

---

# Reinforcement Learning

The problem is modeled as a sequential decision-making task.

At time `t`, the agent receives a state:

```text
sₜ
```

selects a driving action:

```text
aₜ
```

and receives a reward and new state:

```text
rₜ, sₜ₊₁
```

The objective is to learn a policy that maximizes expected cumulative reward rather than simply choosing the action that appears best immediately.

This distinction matters for driving: an action that seems harmless at one moment may place the vehicle in a much worse position several timesteps later.

---

# Dueling Double DQN

The learning agent uses a **Dueling Double Deep Q-Network (D3QN)**.

D3QN combines two improvements over the original Deep Q-Network algorithm.

## Dueling Network Architecture

Instead of directly predicting every action value using a single output stream, the network separately estimates:

```text
V(s)   — value of the current state
A(s,a) — advantage of taking each action
```

These are then combined to estimate:

```text
Q(s,a)
```

Conceptually:

```text
Q(s,a) = V(s) + A(s,a)
```

This lets the network learn whether the vehicle is currently in a generally good or bad situation separately from determining which specific action is preferable.

That can be useful in control problems because many nearby states may have similar overall value even when the optimal control action differs.

---

## Double DQN

Standard DQN can overestimate Q-values because the same network is involved in both selecting and evaluating future actions.

Double DQN reduces this problem by separating those responsibilities between:

```text
Online Network → selects the action
Target Network → evaluates the action
```

This generally provides more stable value estimates during training.

---

# Experience Replay

Interactions with the environment are stored as transitions:

```text
(state, action, reward, next_state, done)
```

Rather than immediately training only on the newest transition, previous experiences are stored in replay memory and sampled during optimization.

This provides two major advantages:

- training data is reused multiple times
- consecutive highly correlated observations are mixed together

Both help stabilize neural-network training.

---

# Target Network

The agent maintains two neural networks:

```text
Online Q-Network
Target Q-Network
```

The online network is optimized continuously.

The target network changes less frequently, providing a comparatively stable value target for Q-learning updates.

Without this separation, the model would effectively be trying to learn from targets that change every time the model itself changes.

---

# Exploration

During training, the agent uses **epsilon-greedy exploration**.

```text
Random action              probability ε
Best predicted action      probability 1 - ε
```

At the beginning of training, the agent explores more heavily because it has little useful knowledge about the environment.

As training progresses, exploration can be reduced so the agent increasingly relies on behaviors it has learned.

---

# Custom Unity Environment

The driving simulation was built in **Unity** rather than using a pre-existing reinforcement-learning environment.

This required implementing the environment-side infrastructure needed by the agent, including:

- vehicle simulation
- state generation
- actions
- rewards
- collision / failure handling
- episode termination
- environment reset
- communication with the Python process

Building the environment alongside the agent provides direct control over what information the neural network receives and how successful behavior is rewarded.

---

# Unity ↔ Python Communication

The simulator and reinforcement-learning model run in different environments:

```text
Unity / C#
     ↕
Sockets
     ↕
Python / PyTorch
```

During training, Unity sends observations describing the current simulation state.

Python feeds those observations into the Q-network and selects an action.

The action is returned to Unity, executed by the vehicle, and the resulting reward and new observation are sent back to the learning process.

This creates a continuous control loop:

```text
Unity
  │
  │ state
  ▼
Python Agent
  │
  │ action
  ▼
Unity
  │
  │ reward + next state
  ▼
Replay Buffer
  │
  ▼
Neural Network Update
```

---

# The 4-Hour Challenge

This project was deliberately built under a strict constraint:

> **How much of a complete deep-reinforcement-learning system could I build in four hours?**

That meant prioritizing the fundamental components required for an end-to-end working system:

1. Create a controllable simulation
2. Expose the environment state
3. Define actions and rewards
4. Connect Unity and Python
5. Implement the D3QN agent
6. Collect experience
7. Train the network
8. Observe learned driving behavior

The objective was not to construct a production-grade autonomous-driving system.

It was an exercise in **rapid experimentation, system integration, and reinforcement-learning implementation**.

---

# Tech Stack

| Component | Technology |
|---|---|
| Reinforcement Learning | Dueling Double DQN |
| Deep Learning | PyTorch |
| Training | Python |
| Simulation | Unity 2019.4 |
| Environment Logic | C# |
| Numerical Computing | NumPy |
| Communication | Python sockets |

---

# Repository Structure

```text
AI-Driving-Cars/
│
├── Car AI/
│   └── Python reinforcement-learning agent
│
├── Car Environment/
│   └── Unity autonomous-driving environment
│
└── README.md
```

---

# Running the Project

## Requirements

The project was developed using:

```text
Unity 2019.4
Python
PyTorch
NumPy
```

Python's built-in socket functionality is used to communicate with Unity.

## Clone

```bash
git clone https://github.com/MycoalDough/AI-Driving-Cars.git
cd AI-Driving-Cars
```

Open the Unity project contained in:

```text
Car Environment/
```

and run the Python reinforcement-learning code from:

```text
Car AI/
```

The two processes communicate through sockets during environment interaction.

---

# Key Challenges

## Building Under a Time Constraint

The four-hour limit forced me to prioritize functionality and quickly make design decisions about the environment, observations, rewards, training architecture, and communication layer.

This required treating the project as an integrated system rather than focusing exclusively on the neural network.

## Environment Design

Unlike supervised learning, there is no static dataset telling the model the correct driving action.

The training data is generated by the agent's own interactions.

The quality of the state representation and reward function therefore directly affects what behavior the model learns.

## Delayed Consequences

Driving actions can affect the trajectory of the vehicle several timesteps later.

The agent needs to learn not only which actions produce immediate reward, but which decisions place it in states that lead to better long-term outcomes.

## Training Stability

Q-learning with neural networks can become unstable.

The project uses techniques including:

- experience replay
- a target network
- Double DQN
- epsilon-greedy exploration

to make training more stable.

## Cross-Language Integration

The learning algorithm and simulator run in separate languages and runtimes.

Maintaining synchronized:

```text
State → Action → Simulation → Reward → State
```

communication was therefore part of the engineering problem alongside the RL implementation itself.

---

# What I Learned

Despite its intentionally short development time, this project required building nearly every major component of a custom deep-RL pipeline.

It gave me additional experience with:

- implementing Deep Q-Learning in PyTorch
- Dueling DQN architectures
- Double DQN
- target networks
- experience replay
- exploration strategies
- state-space design
- reward design
- custom Unity RL environments
- Unity/Python communication
- sequential control problems
- rapid technical prototyping

More importantly, it reinforced that an RL system is much more than its neural network.

The simulator, observations, reward function, training loop, communication infrastructure, and model all have to work together before useful behavior can emerge.

---

## Disclaimer

This project is a small-scale reinforcement-learning experiment created for educational purposes.

It is **not** intended to model or provide software for real-world autonomous vehicle operation.
