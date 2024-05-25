using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System;
using TMPro;


public class CarController : MonoBehaviour
{
    [Header("Car Movement")]
    public float minAccel;
    public float maxAccel;
    public float currAccel;
    public float turnSpeed;
    public float driftFactor = 0.95f;
    private Rigidbody2D rb;
    public bool hasCollidedWall;
    public bool hasWon;

    [Header("AI")]
    public float raycastDistance = 3f;
    public float time = 0;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        time += Time.deltaTime;
        float rotationZ = transform.eulerAngles.z;

        float angleRad = rotationZ * Mathf.Deg2Rad;

        float directionX = Mathf.Cos(angleRad);
        float directionY = Mathf.Sin(angleRad);

        Vector2 velocity = new Vector2(directionX, directionY) * currAccel * Time.deltaTime;
        transform.position = new Vector3(transform.position.x + velocity.x, transform.position.y + velocity.y, transform.position.z);
    }

    public string sendInput()
    {
        //5 raycasts, front, 2 left 2 right
        //accel
        //z value

        string toReturn = "";
        toReturn += PerformRaycasts();
        toReturn += ", " + currAccel;
        toReturn += ", " + transform.eulerAngles.z;
        toReturn += ", " + Math.Round(time, 1);

        return toReturn;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.name.Contains("Wall"))
        {
            hasCollidedWall = true;
        }
        else if (collision.gameObject.name.Contains("Win"))
        {
            hasWon = true;
        }
    }

    public string checkWinOrLose(int action)
    {
        if (time > 61)
        {
            return "-1:True:";
        }
        if (!hasCollidedWall && !hasWon)
        {
            float reward = Math.Min((float)Math.Round(time / 60, 2), 1);

            if (action == 0 && closer().Contains("RIGHT"))
            {
                reward += 0.3f;
            }else if(action == 0 && closer().Contains("LEFT"))
            {
                reward -= 0.3f;
            }

            if (action == 1 && closer().Contains("LEFT"))
            {
                reward += 0.3f;
            }
            else if (action == 1 && closer().Contains("RIGHT"))
            {
                reward -= 0.3f;
            }


            if (action == 0 && closer2().Contains("RIGHT"))
            {
                reward += 0.3f;
            }
            else if (action == 0 && closer2().Contains("LEFT"))
            {
                reward -= 0.3f;
            }

            if (action == 1 && closer2().Contains("LEFT"))
            {
                reward += 0.3f;
            }
            else if (action == 1 && closer2().Contains("RIGHT"))
            {
                reward -= 0.3f;
            }

            Debug.Log(reward);
            return reward + ":False:";
        }

        if (hasCollidedWall)
        {
            return "-1:True:";
        }

        if (hasWon)
        {
            int reward = Math.Max(10, 60 - (int)Math.Round(time));
            return reward + ":True:";
        }
        return "";
    }

    public string PerformRaycasts()
    {
        List<float> s = new List<float>();

        Vector2[] directions = new Vector2[5];
        directions[0] = transform.right;
        directions[1] = (Quaternion.Euler(0, 0, 40) * transform.right).normalized;
        directions[2] = (Quaternion.Euler(0, 0, -40) * transform.right).normalized;
        directions[3] = -transform.up; //left
        directions[4] = transform.up; //right



        foreach (var dir in directions)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, raycastDistance);
            if (hit.collider != null)
            {
                s.Add((float)Math.Round(hit.distance, 1));
            }
            else
            {
                s.Add(-1);
            }
        }

        return string.Join(", ", s.Select(f => f.ToString("0.##")).ToArray());

    }

    public string closer()
    {
        List<float> s = new List<float>();

        Vector2[] directions = new Vector2[4];
        directions[1] = (Quaternion.Euler(0, 0, 40) * transform.right).normalized;
        directions[0] = (Quaternion.Euler(0, 0, -40) * transform.right).normalized;



        foreach (var dir in directions)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, raycastDistance);
            if (hit.collider != null)
            {
                s.Add((float)Math.Abs(Math.Round(hit.distance, 1)));
            }
            else
            {
                s.Add(100);
            }
        }

        if (s[0] > s[1])
        {
            return "TURN LEFT";
        }
        else if (s[1] > s[0])
        {
            return "TURN RIGHT";
        }
        else
        {
            return "NO TURN";
        }
    }


    public string closer2()
    {
        List<float> s = new List<float>();

        Vector2[] directions = new Vector2[4];
        directions[0] = -transform.up;
        directions[1] = transform.up;



        foreach (var dir in directions)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, raycastDistance);
            if (hit.collider != null)
            {
                s.Add((float)Math.Abs(Math.Round(hit.distance, 1)));
            }
            else
            {
                s.Add(100);
            }
        }

        if (s[0] > s[1])
        {
            return "TURN LEFT";
        }
        else if (s[1] > s[0])
        {
            return "TURN RIGHT";
        }
        else
        {
            return "NO TURN";
        }
    }
    void OnDrawGizmos()
    {
        if (rb != null)
        {
            Vector2[] directions = new Vector2[5];
            directions[0] = transform.right;
            directions[1] = (Quaternion.Euler(0, 0, 40) * transform.right).normalized;
            directions[2] = (Quaternion.Euler(0, 0, -40) * transform.right).normalized;
            directions[3] = -transform.up;
            directions[4] = transform.up;

            Gizmos.color = Color.blue;
            foreach (var dir in directions)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, raycastDistance);
                Vector2 endPosition;

                if (hit.collider != null)
                {
                    endPosition = hit.point;
                }
                else
                {
                    endPosition = (Vector2)transform.position + dir * raycastDistance;
                }

                // Draw an X at the endPosition
                float xSize = 0.5f; // Size of the X mark
                Gizmos.DrawLine(endPosition - Vector2.right * xSize, endPosition + Vector2.right * xSize);
                Gizmos.DrawLine(endPosition - Vector2.up * xSize, endPosition + Vector2.up * xSize);
            }
        }
    }


    void FixedUpdate()
    {
        Vector2 forwardVelocity = transform.up * Vector2.Dot(rb.velocity, transform.up);
        Vector2 rightVelocity = transform.right * Vector2.Dot(rb.velocity, transform.right);

        rb.velocity = forwardVelocity + rightVelocity * driftFactor;
    }
}
