using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinLobbyMenu : MonoBehaviour
{
    [SerializeField] private NetworkManagerLobby networkManager = null;

    [Header("UI")]
    [SerializeField] private GameObject landingPagePanel = null;
    [SerializeField] private TMP_InputField ipAddressInputField = null;
    [SerializeField] private Button joinButton = null;
    [SerializeField] private Button backButton = null;
    [SerializeField] private GameObject LoadingIcon = null;
    [SerializeField] private GameObject ErrorIcon = null;

    private void OnEnable()
    {
        NetworkManagerLobby.OnClientConnected += HandleClientConnected;
        NetworkManagerLobby.OnClientDisconnected += HandleClientDisconnected;
    }

    private void OnDisable()
    {
        NetworkManagerLobby.OnClientConnected -= HandleClientConnected;
        NetworkManagerLobby.OnClientDisconnected -= HandleClientDisconnected;
    }

    public void Start()
    {
        setDefaultip();
        joinButton.interactable = false;
        joinButton.transform.Find("Text").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 0.5f);
    }

    public void SetIPAddress(string name)
    {
        joinButton.interactable = !string.IsNullOrEmpty(name);

        if (string.IsNullOrEmpty(name))
        {
            joinButton.transform.Find("Text").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 0.5f);
        }
        else
        {
            joinButton.transform.Find("Text").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 1);
        }

    }

    public void JoinLobby()
    {
        if(ipAddressInputField.text.Length == 0) { return; }

        string ipAddress = ipAddressInputField.text;

        PlayerPrefs.SetString("ipAddress", ipAddress);

        networkManager.networkAddress = ipAddress; //set network address to specified ip address
        networkManager.StartClient(); //start as a client

        LoadingIcon.SetActive(true);
        ErrorIcon.SetActive(false);

        backButton.interactable = false;

        joinButton.interactable = false;
        joinButton.transform.Find("Text").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 0.5f);
    }

    //Called when the player successfully connects to the server
    private void HandleClientConnected()
    {
        LoadingIcon.SetActive(false);
        ErrorIcon.SetActive(false);

        backButton.interactable = true;

        joinButton.interactable = true;
        joinButton.transform.Find("Text").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 1);

        landingPagePanel.SetActive(true);
        gameObject.SetActive(false); 
    }

    //Called when the player fails to connect to the server
    private void HandleClientDisconnected()
    {
        LoadingIcon.SetActive(false);
        ErrorIcon.SetActive(true);

        backButton.interactable = true;

        ipAddressInputField.text = PlayerPrefs.GetString("ipAddress", "");
        joinButton.interactable = true;
        joinButton.transform.Find("Text").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 1);
    }
    public void setDefaultip()
    {
        ipAddressInputField.text = PlayerPrefs.GetString("ipAddress", "");
    }
}
