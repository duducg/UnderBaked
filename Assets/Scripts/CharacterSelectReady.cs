using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CharacterSelectReady : NetworkBehaviour
{
    public static CharacterSelectReady Instance{ get; private set; }
    private Dictionary<ulong, bool> playerReadyDictionary;
    private void Awake()
    {
        Instance = this;
        
        playerReadyDictionary = new Dictionary<ulong, bool>();
    }

    public void SetPlayerReady()
    {
        SetPlayerReadyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        
        playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = true;
        bool allPlayersReady = true;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if(!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId])
            {
                //if even one of them isin't ready then set to false
                allPlayersReady = false;
                //save time by breaking at earlyest not ready
                break;
            }
        }
        if (allPlayersReady)
        {
            Loader.LoadNetwork(Loader.Scene.GameScene);
        }        

    }
}
