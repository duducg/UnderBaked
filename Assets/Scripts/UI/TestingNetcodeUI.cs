using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TestingNetcodeUI : MonoBehaviour
{
    [SerializeField] private Button _startHostButton;
    [SerializeField] private Button _startClientButton;

    void Awake()
    {
        _startHostButton.onClick.AddListener(() =>
        {
            Debug.Log("HOST STARTED");
            NetworkManager.Singleton.StartHost();
            Hide();
        });
        _startClientButton.onClick.AddListener(() =>
        {
            Debug.Log("CLIENT STARTED");
            NetworkManager.Singleton.StartClient();
            Hide();
        });
    }
    private void Hide() => gameObject.SetActive(false);
}
