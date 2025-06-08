using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button quickJoinButton;
    [SerializeField] private Button joinCodeButton;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private LobbyCreateUI lobbyCreateUI;
    [SerializeField] private Transform lobbyContainer;
    [SerializeField] private Transform lobbyTemplate;
    // [SerializeField] private TMP_InputField playerNameInputField;


    private void Awake()
    {
        //Use Load instead of LoadNetwork because we still don't have an active connection
        mainMenuButton.onClick.AddListener(() =>
        {
            Loader.Load(Loader.Scene.MainMenuScene);
        });
        createLobbyButton.onClick.AddListener(() =>
        {
            lobbyCreateUI.Show();
        });
        quickJoinButton.onClick.AddListener(() =>
        {
            KitchenGameLobby.Instance.QuickJoin();
        });
        joinCodeButton.onClick.AddListener(() =>
        {
            KitchenGameLobby.Instance.JoinWithCode(inputField.text);
        });
        lobbyTemplate.gameObject.SetActive(false);
        
    }

    private void KitchenGameLobby_OnLobbyListChanged(object sender, KitchenGameLobby.OnLobbyListChangedEvenArgs e)
    {
        UdpdateLobbyList(e.lobbyList);
    }

    private void UdpdateLobbyList(List<Lobby> lobbies)
    {
        foreach (Transform child in lobbyContainer)
        {
            if (child == lobbyTemplate) continue;
            Destroy(child.gameObject);
        }
        foreach (Lobby lobby in lobbies)
        {
            Transform lobbyTransform = Instantiate(lobbyTemplate,lobbyContainer);
            lobbyTransform.gameObject.SetActive(true);
            lobbyTransform.GetComponent<LobbyListSingleUI>().SetLobby(lobby);
        }

    }
    private void Start()
    {
        // playerNameInputField.text = KitchenGameMultiplayer.Instance.GetPlayerName();
        // playerNameInputField.onValueChanged.AddListener((string newText) =>
        // {
        //     KitchenGameMultiplayer.Instance.SetPlayerName(newText);
        // });
        KitchenGameLobby.Instance.OnLobbyListChanged += KitchenGameLobby_OnLobbyListChanged;
        //Start off with an empty list:
        UdpdateLobbyList(new List<Lobby>());
    }


    private void OnDestroy()
    {
        KitchenGameLobby.Instance.OnLobbyListChanged -= KitchenGameLobby_OnLobbyListChanged;
        
    }
    
}
