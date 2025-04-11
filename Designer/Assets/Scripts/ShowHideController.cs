using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RuntimeInspectorNamespace;

public class ShowHideController : MonoBehaviour
{
    public static ShowHideController instance;
    
    [SerializeField] private RectTransform inspector;
    [SerializeField] private RectTransform hierarchy;
    [SerializeField] private GameObject colorPickerObj;

    private void Awake()
    {
        instance = this;
    }

    public void ShowColorPicker(bool show)
    {
        colorPickerObj.transform.localScale = show ? Vector3.one : Vector3.zero;
    }

    public void HideInspectorAndHierarchy()
    {
        inspector.anchoredPosition = new Vector2(inspector.sizeDelta.x, 0);
        hierarchy.anchoredPosition = new Vector2(hierarchy.sizeDelta.x, 0);
    }

    public void ShowInspectorAndHierarchy()
    {
        inspector.anchoredPosition = Vector2.zero;
        hierarchy.anchoredPosition = Vector2.zero;
    }
}   
