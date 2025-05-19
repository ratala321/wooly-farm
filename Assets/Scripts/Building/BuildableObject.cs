using UnityEngine;

public abstract class BuildableObject : MonoBehaviour, IBuildable
{
    public abstract int Cost { get; set; }
    [field: Header("Buildable Object")]
    [SerializeField] protected BuildableObjectSO buildableObjectSO;

    public abstract void Build(Vector2Int positionToBuild);

    public  BuildableObjectSO GetBuildableObjectSO()
    {
        return buildableObjectSO;
    }

    public abstract void HidePreview();
    
    /// <summary>
    /// Usually called from a client rpc in order to sync the build of an object locally.
    /// </summary>
    public void SynchBuild()
    {
        HidePreview();
        // TODO Move object into specific location
    }

    public GameObject ToGameObject()
    {
        return gameObject;
    }

    public virtual bool IsWalkable()
    {
        return buildableObjectSO.type == BuildableObjectSO.TypeOfBuildableObject.Trap;
    }
}
