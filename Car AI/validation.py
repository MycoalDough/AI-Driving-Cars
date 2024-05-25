from model import Agent
import data

if(__name__ == "__main__"):
    load_checkpoint = True

    agent = Agent(gamma=0.99, epsilon=0.01, lr=1e-4, input_dims=[7], n_actions=5, mem_size=75_000, eps_min=0.01, batch_size=32, checkpoint_name="car", eps_dec=2e-4, replace=100)


    if load_checkpoint:
        agent.load_models()

    num_iter = 10000


    def on_connection_established(client_socket):
        print("Connection established")

        min_reward = float('inf')
        max_reward = -float('inf')
    
        for i in range(1, num_iter + 1):
            done = False
            while not(done):
                observation = data.get_state()
                action, was_random = agent.choose_action(observation=observation)
                reward, done,observation_ = data.play_step(action)

                if reward > max_reward:
                    max_reward = reward
                if reward < min_reward:
                    min_reward = reward
                    
            data.reset()
            print('episode', i, 'epsilon %.2f ' % agent.epsilon, "| MIN REWARD: ", min_reward, " | MAX REWARD: ", max_reward)


        print("- training process over -")
        print("- creating model graph -")
        #x = [i+1 for i in range(num_games)]
        #plotLearning(x,scores,eps_history,filename)


        


data.create_host(on_connection_established)
