import socket
import time
import json
import numpy as np
import threading
from typing import Optional

host = "127.0.0.1"  
port = 12345
current_data = []
server_socket: Optional[socket.socket] = None
client_socket: Optional[socket.socket] = None

max_distance = 100.0  # Maximum distance for raycasts, adjust this based on your environment
min_acceleration = 3.0  # Minimum acceleration
max_acceleration = 10.0  # Maximum acceleration
max_rotation = 360.0  # Maximum rotation value (assuming a full circle)
max_time = 60.0  # Maximum time for an iteration


def create_host(callback):
    global server_socket, client_socket
    if server_socket:
        server_socket.close()
        
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(("localhost", 12345))  
    server_socket.listen() 

    print("Listening for Unity connection")

    while True:
        client_socket, client_address = server_socket.accept()
        print(f"Accepted connection from {client_address}")

        callback(client_socket)



def get_state():
    try:
        client_socket.send("get_state".encode("utf-8"))

        data = client_socket.recv(5000).decode("utf-8")
        if data:
            data_list = [float(number) for number in data.split(",")]
            normalized_data = normalize_inputs(data_list)
            return normalized_data
    except Exception as e:
        print("Error in get_state:", e)
        return None

def normalize_inputs(data_list):
    # Extract individual values
    raycasts = data_list[:5]
    acceleration = data_list[5]
    z_rotation = data_list[6]
    
    normalized_raycasts = np.array(raycasts) / max_distance
    
    normalized_acceleration = (acceleration - min_acceleration) / (max_acceleration - min_acceleration)
    
    normalized_z_rotation = z_rotation / max_rotation
    
    
    normalized_inputs = np.concatenate((normalized_raycasts, [normalized_acceleration, normalized_z_rotation]))
    
    return normalized_inputs


def convert_list(string):
    l = string.split(",")
    l = [float(number) for number in l]
    return l

def play_step(step):
    try:
        to_send = "play_step:" + str(step)
        client_socket.send(to_send.encode("utf-8"))
        play_data = client_socket.recv(5000).decode("utf-8")
        if play_data:
            elements = play_data.split(':')

            result_list = []

            for element in elements:
                if is_float(element):
                    result_list.append(float(element))
                elif element.replace('.', '', 1).replace('-', '', 1).isdigit():
                    result_list.append(float(element))
                elif element.lower() == 'true' or element.lower() == 'false':
                    result_list.append(element.lower() == 'true')
                else:
                    result_list.append(element)
                    
            return result_list[0], result_list[1], normalize_inputs([float(number) for number in elements[2].split(",")])
    except Exception as e:
        print("Error in get_state:", e)
        return None


def is_float(s):
    try:
        float_value = float(s)
        return True
    except ValueError:
        return False

def reset():
    client_socket.send("reset".encode("utf-8"))
#client_socket.close()
