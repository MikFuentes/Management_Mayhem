using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

/**Sources
 * https://www.youtube.com/watch?v=zc8ac_qUXQY
 * https://youtu.be/Fx8efi2MNz0?list=PLS6sInD7ThM1aUDj8lZrF4b4lpvejB2uB
 **/
public class MainMenu : NetworkBehaviour
{
    [SerializeField] private NetworkManagerLobby NetworkManager = null;

    [Header("UI")]
    [SerializeField] private GameObject landingPagePanel = null;

    [SerializeField] private GameObject home = null;
    [SerializeField] private GameObject stageNegativeOne = null;
    [SerializeField] private List<GameObject> pages = null;
    [SerializeField] private GameObject nameInput = null;

    void Start()
    {
        setDefaultFirstTime();
    }

    //private NetworkManagerLobby room;
    //private NetworkManagerLobby Room
    //{
    //    get
    //    {
    //        if (room != null) { return room; }
    //        return room = NetworkManager.singleton as NetworkManagerLobby;
    //    }
    //}


    public void HostLobby()
    {
        Debug.Log("StartHost()");
        NetworkManager.StartHost();

        //landingPagePanel.SetActive(false);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        FindObjectOfType<AudioManager>().Play("MenuMusic", false);
    }

    public void StartButton()
    {
        home.SetActive(false);
        stageNegativeOne.SetActive(false);
        pages[0].SetActive(true);
        pages[1].SetActive(false);
        pages[2].SetActive(false);
        //nameInput.SetActive(false);

        if (PlayerPrefs.GetInt("firstTime", 1) == 1)
        {
            //load stage -1
            stageNegativeOne.SetActive(true);
        }
        else
        {
            //get name input
            landingPagePanel.SetActive(true);
        }
    }

    public void setDefaultFirstTime()
    {
        PlayerPrefs.GetInt("firstTime", 1);
    }
    public void NotFirstTime()
    {
        PlayerPrefs.SetInt("firstTime", 0);
    }
}
