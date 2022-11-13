using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotatable : MonoBehaviour // interfaces?
{
    public float x;
    public float y;
    public float z;
    public float w;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        x = transform.rotation.x;
        y = transform.rotation.y;
        z = transform.rotation.z;
        w = transform.rotation.w;
    }
}

// Use this to record quaternion rotations for different facings (for the quad)!