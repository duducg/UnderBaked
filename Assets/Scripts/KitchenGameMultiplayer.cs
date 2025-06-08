using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KitchenGameMultiplayer : NetworkBehaviour
{
    public const int MAX_PLAYER_AMOUNT = 4;
    private const string PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER = "PlayerNameMulitplayer";
    public static KitchenGameMultiplayer Instance { get; private set;}
    public event EventHandler OnTryingToJoinGame;
    public event EventHandler OnFailedToJoinGame;
    private string playerName;

    [SerializeField] private KitchenObjectListSO kitchenObjectListSO;

    private void Awake() 
    {
        Instance  = this;
        DontDestroyOnLoad(this);
        playerName = PlayerPrefs.GetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER,"PlayerName" + UnityEngine.Random.Range(100,1000));
    }
    public string GetPlayerName()
    {
        return playerName;
    }
    public void SetPlayerName(string newPlayerName)
    {
       playerName = newPlayerName;
        PlayerPrefs.SetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, playerName);
    }
    public void StartHost()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
        NetworkManager.Singleton.StartHost();
    }

    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        //If scene is incorrect one:
        if(SceneManager.GetActiveScene().name != Loader.Scene.CharacterSelectScene.ToString())
        {
            response.Approved = false;
            response.Reason = "The Game has already started";
            Debug.Log("The game has already started");
            return;
        }
        //If maxNumber of players is already here
        if (NetworkManager.Singleton.ConnectedClientsIds.Count >= MAX_PLAYER_AMOUNT)
        {
            response.Approved = false;
            response.Reason = "The Game is full";
            Debug.Log("MaxPlayersReached");
            return;
            
        }

        response.Approved = true;
        
        
    }

    public void StartClient()
    {
        OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        NetworkManager.Singleton.StartClient();
    }

    


    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
    }

    public void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent) 
    {
        int kitchenObjectSOIndex = GetKitchenObjectSOIndex(kitchenObjectSO);
        SpawnKitchenObjectServerRpc(kitchenObjectSOIndex, kitchenObjectParent.GetNetworkObject());
        
    }
    [ServerRpc(RequireOwnership = false)]
    private void SpawnKitchenObjectServerRpc(int kitchenObjectSOIndex, NetworkObjectReference kitchenObjectParentNetworkObjectReference)
    {
        KitchenObjectSO kitchenObjectSO = GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);
        //Spawns object in server or if in Host mode, there too
        Transform kitchenObjectTransform = Instantiate(kitchenObjectSO.prefab);

        //Actually spawning it on the network:
        NetworkObject kitchenNetworkObject = kitchenObjectTransform.GetComponent<NetworkObject>();
        kitchenNetworkObject.Spawn(true); //destroid when scene is changed.

        KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();
        
        //Convert it back to a IKitchenObjectParent:

        //First convert the NetworkObjectReference back to a NetworkObject
        kitchenObjectParentNetworkObjectReference.TryGet(out NetworkObject kitchenObjectParentNetworkObject);

        //Access the IKitchenObjectParent component that was originally there:
        IKitchenObjectParent kitchenObjectParent = kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();

        //This code is only running on the server so only the Host will pick up objects:
        //The parenting code wont run on clients 
        kitchenObject.SetKitchenObjectParent(kitchenObjectParent);


    }
    public int GetKitchenObjectSOIndex(KitchenObjectSO kitchenObjectSO)
    {
        return kitchenObjectListSO.kitchenObjectSOList.IndexOf(kitchenObjectSO);       
    }
    public KitchenObjectSO GetKitchenObjectSOFromIndex(int kitchenObjectSOIndex)
    {
        return kitchenObjectListSO.kitchenObjectSOList[kitchenObjectSOIndex];
    }

    public void DestroyKitchenObject(KitchenObject kitchenObject)
    {
        DestroyKitchenObjectServerRpc(kitchenObject.NetworkObject);
    }
    //Only the server can have the authority to destroy NetworkObjects
    //When a network object is destroyed on the server all clients get this even synched. 
    [ServerRpc (RequireOwnership = false)]
    public void DestroyKitchenObjectServerRpc(NetworkObjectReference kitchenObjectNetworkObjectReference)    {
        
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);
        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();
        //Also clear parenting on the server:
        ClearKitchenObjectOnParentClientRpc(kitchenObjectNetworkObjectReference);
        
        kitchenObject.DestroySelf();
        
    }
    //But the Unparenting has to happen for each individual client. (since they each have a clone of the parented object)
    [ClientRpc]
    private void ClearKitchenObjectOnParentClientRpc(NetworkObjectReference kitchenObjectNetworkObjectReference)
    {
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);
        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();

        kitchenObject.ClearKitchenObjectOnParent();
    }
}
