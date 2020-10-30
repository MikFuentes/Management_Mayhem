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

    public void JoinLobby()
    {
        string ipAddress = ipAddressInputField.text;

        networkManager.networkAddress = ipAddress; //set network address to specified ip address
        networkManager.StartClient(); //start as a client
         
        joinButton.interactable = false;
    }

    //Called when the player successfully connects to the server
    private void HandleClientConnected()
    {
        joinButton.interactable = true;

        gameObject.SetActive(false); 
        //landingPagePanel.SetActive(false);
    }

    //Called when the player fails to connect to the server
    private void HandleClientDisconnected()
    {
        joinButton.interactable = true;
    }
}
