using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public static class MultiplayerRelay
{
    public const string RELAY_JOIN_CODE_KEY = "RelayJoinCodeKey";

    private const int HOST_COUNT = 1;
    
    /**
     * Throws RelayServiceException.
     */
    public static async Task<Allocation> AllocateRelay()
    {
        // Need to exclude the host for the max number of player. (-1)
        Allocation allocation =
            await RelayService.Instance.CreateAllocationAsync(GameMultiplayerManager.MAX_NUMBER_OF_PLAYERS - HOST_COUNT);

        return allocation;
    }

    /*
     * Throws RelayServiceException.
     */
    public static async Task<string> GetRelayJoinCode(Allocation allocation)
    {
        string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
        return relayJoinCode;
    }

    /*
     * Throws RelayServiceException.
     */
    public static async Task<JoinAllocation> JoinRelay(string joinCode)
    {
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        
        return joinAllocation;
    }
    
    public static void SetNetworkManagerRelayServer(ref Allocation allocation, ref NetworkEndpoint endpoint, bool isSecure)
    {
        // dtls is the default recommendation by unity

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        
        transport.SetHostRelayData(
            endpoint.Address, endpoint.Port, allocation.AllocationIdBytes, allocation.Key,
            allocation.ConnectionData, isSecure
        );
    }
    
    public static void SetNetworkManagerRelayServer(ref JoinAllocation allocation, ref NetworkEndpoint endpoint, bool isSecure)
    {
        // dtls is the default recommendation by unity
        
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        
        transport.SetClientRelayData(endpoint.Address, endpoint.Port, allocation.AllocationIdBytes, allocation.Key, 
            allocation.ConnectionData, allocation.HostConnectionData, isSecure);
    }
    

}
