using System;
using UnityEngine;

public class CanvasPosition : MonoBehaviour
{
    private Camera _cam;

    private void Start()
    {
        _cam = Camera.main;
    }

    void LateUpdate()
    {
        transform.LookAt(_cam.transform); 
        transform.Rotate(0, 180, 0);
    }
}
