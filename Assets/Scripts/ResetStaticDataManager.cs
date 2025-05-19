using UnityEngine;

public class ResetStaticDataManager : MonoBehaviour
{
    private void Awake()
    {
        CharacterSelectUI.ResetStaticData();
        SingleBuildableObjectSelectUI.ResetStaticData();
    }
}
