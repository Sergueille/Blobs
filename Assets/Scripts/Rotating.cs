using System.Collections.Generic;
using UnityEngine;

public class Rotating : MonoBehaviour
{
    [Tooltip("Degrees / sec")]
    public float rotatingSpeed;

    private float initialAngle;
    private float initialTime;

    private void Start()
    {
        initialAngle = gameObject.transform.localEulerAngles.z;
        initialTime = Time.time;
    }

    private void Update()
    {
        gameObject.transform.localEulerAngles = new Vector3(0, 0, initialAngle + rotatingSpeed * (Time.time - initialTime));
    }
}
