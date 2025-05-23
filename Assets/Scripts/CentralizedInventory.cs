using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CentralizedInventory : NetworkBehaviour
{
    public static CentralizedInventory Instance { get; private set; }

    public static int StartingMoney; 
    [SerializeField] private CentralizedInventoryUI correspondingUI;

    [SerializeField] private List<BuildingMaterialSO> allBuildingMaterialSO;
    
    public NetworkVariable<int> NumberOfGreyResources { get; private set; } = new NetworkVariable<int>(0);

    public event EventHandler<OnNumberResourceChangedEventArgs> OnNumberResourceChanged;

    private int _collectedBonus = 0;
    private int _ressourceAddedThisTurn = 0;
    public class OnNumberResourceChangedEventArgs
    {
        public int NewValue;
        public BuildingMaterialSO ResourceChanged;
    }

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        NumberOfGreyResources.OnValueChanged += EmitGreyResourceChangedEvent;
        
        base.OnNetworkSpawn();
    }

    public void Initialize()
    {
        if (IsServer)
        {
            Debug.Log(StartingMoney);
            NumberOfGreyResources.Value = StartingMoney;
        }
    }

    public void DecreaseResourceForBuilding(BuildableObjectSO builtObjectSO)
    {
        foreach (BuildableObjectSO.BuildingMaterialAndQuantityPair pair in builtObjectSO.materialAndQuantityPairs)
        {
            DecreaseIndividualResource(pair.buildingMaterialSO, pair.quantityOfMaterialRequired);
        }
    }

    public void ShowCostForBuildableObject(BuildableObjectSO toShowResourcesCost)
    {
        foreach (BuildableObjectSO.BuildingMaterialAndQuantityPair pair in toShowResourcesCost.materialAndQuantityPairs)
        {
            ShowCostForResource(pair.buildingMaterialSO, pair.quantityOfMaterialRequired);
        }
    }
    
    private void ShowCostForResource(BuildingMaterialSO resourceData, int cost)
    {
        try
        {
            correspondingUI.ShowCostForResource(resourceData, GetNumberAvailableResource(resourceData), cost);
        }
        catch (NoMatchingBuildingMaterialSOException e)
        {
            Debug.LogError(e);
        }
    }
    
    public bool HasResourcesForBuilding(BuildableObjectSO buildableObjectSo)
    {
        foreach (BuildableObjectSO.BuildingMaterialAndQuantityPair pair in buildableObjectSo.materialAndQuantityPairs)
        {
            int numberRequired = GetNumberAvailableResource(pair.buildingMaterialSO);
            if (numberRequired < pair.quantityOfMaterialRequired)
            {
                return false;
            }
        }

        return true;
    }

    private void DecreaseIndividualResource(BuildingMaterialSO resourceData, int cost)
    {
        switch (resourceData.type)
        {
            case BuildingMaterialSO.BuildingMaterialType.GreyMaterial:
                NumberOfGreyResources.Value = NumberOfGreyResources.Value - cost;
                break;
        }
    }

    /// <exception cref="NoMatchingBuildingMaterialSOException"></exception>
    private NetworkVariable<int> GetNetworkVariableOfResource(BuildingMaterialSO buildingMaterialSo)
    {
        switch (buildingMaterialSo.type)
        {
            case BuildingMaterialSO.BuildingMaterialType.GreyMaterial:
                return NumberOfGreyResources;
        }

        throw new NoMatchingBuildingMaterialSOException();
    }

    /// Throws NoMatchingBuildingMaterialSOException when the BuildingMaterialSO specified does not exist in inventory.
    private int GetNumberAvailableResource(BuildingMaterialSO resourceData)
    {
        switch (resourceData.type)
        {
            case BuildingMaterialSO.BuildingMaterialType.GreyMaterial:
                return NumberOfGreyResources.Value;
        }

        throw new NoMatchingBuildingMaterialSOException("No match is found, maybe a new building material was" +
                                                        "not coded in the inventory yet !");
    }

    [ClientRpc]
    private void EmitResourceChangedEventClientRpc(int newValue, int indexOfBuildingMaterialSO)
    {
        BuildingMaterialSO resourceChanged = allBuildingMaterialSO[indexOfBuildingMaterialSO];
        
        OnNumberResourceChanged?.Invoke(this, new OnNumberResourceChangedEventArgs
        {
            NewValue = newValue,
            ResourceChanged = resourceChanged,
        });
    }
    
    private void EmitGreyResourceChangedEvent(int previousValue, int newValue)
    {
        // As there is only on resource in the game for now, the index value for the grey resource is 0.
        BuildingMaterialSO resourceChanged = allBuildingMaterialSO[0];
        
        OnNumberResourceChanged?.Invoke(this, new OnNumberResourceChangedEventArgs
        {
            NewValue = newValue,
            ResourceChanged = resourceChanged,
        });
    }

    public void ClearAllMaterialsCostUI()
    {
        correspondingUI.ClearAllMaterialsCostUI();
    }

    

}
