using System;
using Unity.Collections;
using Unity.Netcode;

public struct PlayerData : IEquatable<PlayerData>, INetworkSerializable
{
    public ulong clientId;
    
    public CharacterSelectUI.CharacterId characterSelection;

    public FixedString64Bytes lobbyPlayerId;

    public bool Equals(PlayerData other)
    {
        return clientId == other.clientId &&
               characterSelection == other.characterSelection &&
               lobbyPlayerId.Equals(other.lobbyPlayerId);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref characterSelection);
        serializer.SerializeValue(ref lobbyPlayerId);
    }
}
