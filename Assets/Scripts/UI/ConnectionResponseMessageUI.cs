using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionResponseMessageUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private Button closeButton;
    void Awake()
    {
        closeButton.onClick.AddListener(Hide);
    }
    private void Start()
    {
        KitchenGameMultiplayer.Instance.OnFailedToJoinGame += KitchenGameMultiplayer_OnFailedToJoinGame;
        Hide();
    }

    private void KitchenGameMultiplayer_OnFailedToJoinGame(object sender, EventArgs e)
    {
        Show();
        text.text = NetworkManager.Singleton.DisconnectReason;
        //If connection times out the Response Reason field will come empty:
        if (text.text == "") text.text = "Failed to connect";
    }

    private void Show() {
        gameObject.SetActive(true);        
    }

    private void Hide() {
        gameObject.SetActive(false);
    }
    private void OnDestroy()
    {
        KitchenGameMultiplayer.Instance.OnFailedToJoinGame -= KitchenGameMultiplayer_OnFailedToJoinGame;
        
    }

}
