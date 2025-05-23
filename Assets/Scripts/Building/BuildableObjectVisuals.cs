using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildableObjectVisuals : MonoBehaviour
{
    [SerializeField] private GameObject buildableObjectPreview;

    public void HidePreview()
    {
        buildableObjectPreview.SetActive(false);
    }

    public void ShowPreview()
    {
        buildableObjectPreview.SetActive(true);
    }
    
    public void ShowPreview(Vector3 position)
    {
        transform.position = position;
        
        buildableObjectPreview.SetActive(true);
    }
}
