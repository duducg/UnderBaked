using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KitchenGameLobby : MonoBehaviour
{

    private const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";
    public static KitchenGameLobby Instance { get; private set; }

    public event EventHandler<OnLobbyListChangedEvenArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEvenArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }
    private Lobby joinedLobby;
    private float hearthbeatTimer;
    private float ListLobbiesTimer;
    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this);
        InitializeUnityAuthentication();
    }
    //This can only be initialized once, otherwise we get an error
    private async void InitializeUnityAuthentication()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            //Since we are using the same machine for testing it will always send the same id. 
            //To properly test, we setup initoptions so it creates a new ID per isntance:
            InitializationOptions initializationOptions = new InitializationOptions();
            initializationOptions.SetProfile(UnityEngine.Random.Range(0, 1000).ToString());

            await UnityServices.InitializeAsync();
            //Anonymous means there's no need for sign in credentials like google or your steam id, you just join
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
    private void Update()
    {
        HandleHearthbeat();
        HandlePeriodicListLobbies();
    }
    private void HandlePeriodicListLobbies()
    {
        if (joinedLobby == null &&
          AuthenticationService.Instance.IsSignedIn &&
         SceneManager.GetActiveScene().name == Loader.Scene.LobbyScene.ToString())
        {
            ListLobbiesTimer -= Time.deltaTime;
            if (ListLobbiesTimer <= 0f)
            {
                float ListLobbiesTimerMax = 3f;
                ListLobbiesTimer = ListLobbiesTimerMax;
                ListLobbies();
            }

        }

    }
    private void HandleHearthbeat()
    {
        if (IsLobbyHost())
        {
            hearthbeatTimer -= Time.deltaTime;
            if (hearthbeatTimer <= 0f)
            {
                float hearthbeatTimerMax = 15f;
                hearthbeatTimer = hearthbeatTimerMax;

                LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }

        }

    }
    private bool IsLobbyHost()
    {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;

    }
    private async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter>
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            }
            };
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);
            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEvenArgs
            {
                lobbyList = queryResponse.Results
            });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }
    
    private async Task<Allocation> AllocateRelay()
    {
        //Allocate a relay with maxplayercount - host (1)
        //Returns an "Allocation" instance:
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(KitchenGameMultiplayer.MAX_PLAYER_AMOUNT - 1);
            return allocation;
        }catch(RelayServiceException e)
        {
            Debug.Log(e);
            return default;
        }
    }
    private async Task<string> GetRelayJoinCode( Allocation allocation)
    {
        try
        {
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return relayJoinCode;
        }catch(RelayServiceException e)
        {
            Debug.Log(e);
            return default;
        }
    }
    //Join Relay using code:
    private async Task<JoinAllocation> JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            return joinAllocation;
        }catch(RelayServiceException e)
        {
            Debug.Log(e);
            return default;
        }
    }

    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        try
        {
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, KitchenGameMultiplayer.MAX_PLAYER_AMOUNT, new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
            });
            //We alocate the relay before starting a new host instance:

            //Get allocation:
            Allocation allocation = await AllocateRelay(); 
            
            //Now we need the code to join it.
            string relayJoinCode = await GetRelayJoinCode(allocation);

            //Share joinkey code with lobby
            await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> {
                    {KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member,relayJoinCode)}
                }
            });

            //And to actually set it up. The connection now goes trough this relay
            NetworkManager.Singleton.GetComponent<UnityTransport>().
                SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

            
            KitchenGameMultiplayer.Instance.StartHost();
            Loader.LoadNetwork(Loader.Scene.CharacterSelectScene);

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }
    public async void LeaveLobby()
    {
        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);

                joinedLobby = null;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }
    public async void QuickJoin()
    {
        try
        {
            //Join Lobby:
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();

            //Access data that was shared previously to get joincode:
            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;

            //Get the joinRelay 
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
            
            //Join relay with Unity Transport:
            NetworkManager.Singleton.GetComponent<UnityTransport>().
                SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));

            KitchenGameMultiplayer.Instance.StartClient();

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }
    public async void JoinWithCode(string lobbyCode)
    {
        try
        {
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            
            //Access data that was shared previously to get joincode:
            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;

            //Get the joinRelay 
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
            
            //Join relay with Unity Transport:
            NetworkManager.Singleton.GetComponent<UnityTransport>().
                SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));


            KitchenGameMultiplayer.Instance.StartClient();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }
    public async void JoinWithId(string lobbyId)
    {
        try
        {
            
            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

            //Access data that was shared previously to get joincode:
            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;

            //Get the joinRelay 
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
            
            //Join relay with Unity Transport:
            NetworkManager.Singleton.GetComponent<UnityTransport>().
                SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));


            KitchenGameMultiplayer.Instance.StartClient();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }
    public Lobby GetLobby()
    {
        return joinedLobby;
    }
}
