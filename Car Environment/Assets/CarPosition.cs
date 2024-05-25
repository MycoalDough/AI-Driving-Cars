using UnityEngine;
using System.Collections.Generic;
using System;

public class CarPosition : MonoBehaviour
{
    public float totalDistance = 0f;
    public Vector3 previousPosition;

    void Start()
    {
        previousPosition = transform.position;
        totalDistance = 0f;
    }

    void Update()
    {
        float distanceTraveled = Vector3.Distance(previousPosition, transform.position);
        totalDistance += distanceTraveled;
        previousPosition = transform.position;
    }

    public float GetNormalizedDistance()
    {
        return totalDistance / 400;
    }
}
