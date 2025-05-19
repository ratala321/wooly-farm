using TMPro;
using UI;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class BuildableObjectInfoDisplayUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descriptionText;
    
    private void Start()
    {
        SingleBuildableObjectSelectUI.OnAnySelectUI +=
            SingleBuildableObjectSelectUI_OnAnySelectUI;
        SingleBuildableObjectSelectUI.OnAnyDeselectUI +=
            SingleBuildableObjectSelectUI_OnAnyDeselectUI;
        SingleBuildableObjectSelectUI.OnAnySingleBuildableObjectSelectUISelected +=
            SingleBuildableObjectSelectUI_OnAnySingleBuildableObjectSelectUISelected;

        BasicShowHide.Hide(gameObject);
    }

    private GameObject _preview;
    private const float PREVIEW_DISTANCE = 10f;
    
    private void SingleBuildableObjectSelectUI_OnAnySelectUI
        (object sender, SingleBuildableObjectSelectUI.BuildableObjectData e)
    {
        ClearOldPreview();
        
        ClearOldMaterialCostUI();
        
        BasicShowHide.Show(gameObject);
        
        descriptionText.text = e.buildableObjectInfos.description;
        
        ShowPreviewInFrontOfCamera(e);
        
        ShowMaterialCost(e);
    }

    private void ShowPreviewInFrontOfCamera(SingleBuildableObjectSelectUI.BuildableObjectData buildableObjectData)
    {
        _preview = Instantiate(buildableObjectData.buildableObjectInfos.visuals);

        GameObject toFollow = CinemachineBrain.GetActiveBrain(0).OutputCamera.GameObject(); // Fix during import, might need rework.
        _preview.AddComponent<FollowTransform>().SetFollowParameters(toFollow, new Vector3(0, 0,PREVIEW_DISTANCE), FollowTransform.DirectionOfFollow.Front);
            
        _preview.GetComponent<BuildableObjectVisuals>().ShowPreview();
    }

    private void ShowMaterialCost(SingleBuildableObjectSelectUI.BuildableObjectData buildableObjectData)
    {
        CentralizedInventory.Instance.ShowCostForBuildableObject(buildableObjectData.buildableObjectInfos);
    }

    private void ClearOldPreview()
    {
        if (_preview != null)
        {
            Destroy(_preview);
        }
    }
    
    private void ClearOldMaterialCostUI()
    {
        CentralizedInventory.Instance.ClearAllMaterialsCostUI();
    }
    
    private void SingleBuildableObjectSelectUI_OnAnyDeselectUI
        (object sender, SingleBuildableObjectSelectUI.BuildableObjectData e)
    {
        ClearDisplay();
    }

    private void SingleBuildableObjectSelectUI_OnAnySingleBuildableObjectSelectUISelected
        (object sender, SingleBuildableObjectSelectUI.BuildableObjectData e)
    {
        ClearDisplay();
    }

    private void ClearDisplay()
    {
        BasicShowHide.Hide(gameObject);
        
        Destroy(_preview);
        _preview = null;
    }

}
