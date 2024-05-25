using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Threading;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SocketConnection : MonoBehaviour
{
    [Header("Socket")]
    private const string host = "127.0.0.1"; // localhost
    private const int port = 12345;
    TcpClient client;
    NetworkStream stream;
    private Thread receiveThread;
    private bool isRunning = true;
    static SocketConnection instance;
    public UnityMainThreadDispatcher umtd;

    [Header("Environment")]
    public Transform car;
    public Transform carSpawn;

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 30;
        Time.timeScale = 1;
        resetEnv();
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            ConnectToServer();

        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ConnectToServer()
    {
        try
        {
            client = new TcpClient(host, port);
            stream = client.GetStream();

            // Start the receive thread
            receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception: {e.Message}");
        }
    }

    void ReceiveData()
    {
        Debug.Log("Thread started!");
        byte[] data = new byte[1024];
        while (isRunning)
        {
            try
            {
                int bytesRead = stream.Read(data, 0, data.Length);
                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(data, 0, bytesRead);
                    if (message == "get_state")
                    {
                        // Enqueue the getItems call to be executed on the main thread
                        umtd.Enqueue(() => {
                            string toSend = "";
                            toSend = car.gameObject.GetComponent<CarController>().sendInput();
                            byte[] dataToSend = Encoding.UTF8.GetBytes(toSend);
                            stream.Write(dataToSend, 0, dataToSend.Length);
                        });
                    }
                    else if (message.Contains("play_step")) //recieve: play_step:STEP
                        //send: REWARD:DONE:OBS_
                    {
                        string[] step = message.Split(':');
                        umtd.Enqueue(() => {
                            string s = "";
                            s = playStep(int.Parse(step[1]));
                            s += car.gameObject.GetComponent<CarController>().sendInput();
                            byte[] dataToSend = Encoding.UTF8.GetBytes(s);
                            stream.Write(dataToSend, 0, dataToSend.Length);

                        });
                    }
                    if (message == "reset")
                    {
                        umtd.Enqueue(() =>
                        {
                            resetEnv();
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception: {e.Message}");
            }
        }
    }

    public void resetEnv()
    {
        car.eulerAngles = new Vector3(0, 0, -90);
        car.transform.position = carSpawn.transform.position;
        car.gameObject.GetComponent<CarController>().time = 0;
        car.gameObject.GetComponent<CarController>().currAccel = 5;
        car.gameObject.GetComponent<CarController>().hasCollidedWall = false;
        car.gameObject.GetComponent<CarController>().hasWon = false;
        car.GetComponent<CarPosition>().previousPosition = car.transform.position;
        car.GetComponent<CarPosition>().totalDistance = 0;
    }

    // Update is called once per frame
    public string sendInput()
    {
        string toSend = car.gameObject.GetComponent<CarController>().sendInput();


        return toSend;
    }

    public string playStep(int action)
    {
        CarController cc = car.gameObject.GetComponent<CarController>();
        //actions: 
        //turn car by 2.5 deg left or right (0,1)
        //increase accel by 0.5 (2,3)

        if(action == 0)
        {
            car.Rotate(0,0, 5f);
        }
        else if(action == 1)
        {
            car.Rotate(0, 0, -5f);
        }else if(action == 2)
        {
            cc.currAccel = Mathf.Clamp(cc.currAccel + 0.5f, cc.minAccel, cc.maxAccel);
        }
        else if(action == 3)
        {
            cc.currAccel = Mathf.Clamp(cc.currAccel - 0.5f, cc.minAccel, cc.maxAccel);
        }


        return cc.checkWinOrLose(action);

    }
}
