using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreamActivator : MonoBehaviour
{
    private void OnEnable()
    {
        switch (name)
        {
            case "baked_mesh":
                transform.position = Vector3.left * 3.0f;
                transform.localScale = Vector3.one * 3.0f;
                break;
            case "testa_mesh":
                transform.position = Vector3.right * 3.0f;
                transform.localScale = Vector3.one * 2.0f;
                break;
        }
        
        Destroy(this);
    }
}
