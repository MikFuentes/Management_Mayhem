using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/**Sources
 * https://www.youtube.com/watch?v=zc8ac_qUXQY
 * https://youtu.be/Fx8efi2MNz0?list=PLS6sInD7ThM1aUDj8lZrF4b4lpvejB2uB
 **/
public class MainMenu : MonoBehaviour
{
    [SerializeField] private NetworkManagerLobby NetworkManager = null;

    [Header("UI")]
    [SerializeField] private GameObject landingPagePanel = null;

    public void HostLobby()
    {
        NetworkManager.StartHost();

        landingPagePanel.SetActive(false);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        FindObjectOfType<AudioManager>().Play("MenuMusic", false);
    }
}
