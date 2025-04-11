using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplyColorController : MonoBehaviour
{
    public FlexibleColorPicker colorPicker;
    public MeshRenderer meshRenderer;
    private bool isMeshAllocated = false;

    public void SetOffColorPicker()
    {
        if (colorPicker.transform.localScale == Vector3.one)
        {
            colorPicker.transform.localScale = Vector3.zero;
            meshRenderer = null;
            isMeshAllocated = false;
        }
    }

    private void Update()
    {
        if(colorPicker.transform.localScale == Vector3.zero) return;
        if (!meshRenderer) return;

        if (meshRenderer && !isMeshAllocated)
        {
            isMeshAllocated = true;
            colorPicker.color = meshRenderer.material.color;
        }
        
        foreach (Material material in meshRenderer.materials)
            material.color = colorPicker.color;
    }
}
