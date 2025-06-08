using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;

public class KitchenGameLobby : MonoBehaviour
{
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
        if (joinedLobby == null &&  AuthenticationService.Instance.IsSignedIn)
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
    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        try
        {
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, KitchenGameMultiplayer.MAX_PLAYER_AMOUNT, new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
            });

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
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
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
