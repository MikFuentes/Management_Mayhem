using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
//using System.Security.Policy;
/*
* https://answers.unity.com/questions/1271861/how-to-destroy-an-object-on-all-the-network.html
*/
public class PlayerScript : NetworkBehaviour
{
    [Header("Components")]
    public Joystick joystick;
    public Animator animator;

    [Header("UI Elements")]
    public Button interactButton;
    public Button pickUpButton;
    public Button dropButton;
    public Transform holdPoint;
    public Transform dropPoint;
    public CapsuleCollider2D playerTrigger;
    [SerializeField] private GameObject gameName;
    [SerializeField] private TMP_Text ui_Timer = null;
    private static event Action<float> OnTimeChange;
    [SyncVar]
    private float currentTime;

    [Header("Movement")]
    public float moveSpeed;
    private float horizontalMove, verticalMove;
    private bool facingRight = true;
    private Vector3 movement;

    [Header("Money")]
    [SerializeField] private GameObject moneyUI = null;
    [SerializeField] private TMP_Text moneyText = null;
    [SyncVar]
    [SerializeField] private float currentMoney;
    private static event Action<float> OnMoneyChange;
    private bool Cooldown = false;

    [Header("Items")]
    public GameObject itemPrefab;
    //public GameObject[] clientPrefabs;
    //public List<GameObject> serverPrefabs;

    [Header("Debug Info")]
    [SyncVar]
    public string playerId;
    [SyncVar]
    public Vector3 playerPos;
    [SyncVar]
    public GameObject pickup;
    [SyncVar]
    public bool pickUpActive = false;
    [SyncVar]
    public GameObject interactable;
    //[SyncVar]
    //public GameObject deleter;
    [SyncVar]
    public bool canDeposit;
    [SyncVar]
    public bool canDelete;


    private NetworkManagerLobby room;
    private NetworkManagerLobby Room
    {
        get
        {
            if (room != null) {
                return room;
            }
            return room = NetworkManager.singleton as NetworkManagerLobby;
        }
    }

    public void Start()
    {
        //clientPrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs");
        //serverPrefabs = Room.spawnPrefabs;

    }
    public override void OnStartAuthority()
    {
        moneyUI.SetActive(true);

        holdPoint.gameObject.SetActive(true);
        dropPoint.gameObject.SetActive(true);

        pickUpButton.onClick.AddListener(CmdPickUpOnClick);
        dropButton.onClick.AddListener(CmdDropOnClick);
        interactButton.onClick.AddListener(CmdInteractOnClick);

        playerId = ClientScene.localPlayer.netId.ToString();

        OnMoneyChange += HandleMoneyChange; //subcribe to the money event
        OnTimeChange += HandleTimeChange;

        //spawnablePrefabs = Room.spawnPrefabs;

    }

    [ClientCallback]
    public void OnDestroy()
    {
        if (!hasAuthority) { return; } //do nothing if we don't have authority
        OnMoneyChange -= HandleMoneyChange;
        OnTimeChange -= HandleTimeChange;
    }

    void Update()
    {
        // Don't control other player's models
        if (!isLocalPlayer)
            return;

        //Controls
        if (Input.GetKey(KeyCode.W) || joystick.Vertical >= .2f)
            verticalMove = moveSpeed;
        else if (Input.GetKey(KeyCode.S) || joystick.Vertical <= -.2f)
            verticalMove = -moveSpeed;
        else
            verticalMove = 0;

        if (Input.GetKey(KeyCode.A) || joystick.Horizontal <= -.2f)
        {
            if (facingRight) //If moving left while facing right...
                flipPlayer();
            horizontalMove = -moveSpeed;
        }
        else if (Input.GetKey(KeyCode.D) || joystick.Horizontal >= .2f)
        {
            if (!facingRight) //If moving right while facing left...
                flipPlayer();
            horizontalMove = moveSpeed;
        }
        else
            horizontalMove = 0;

        if (Input.GetKey(KeyCode.J))
            CmdPickUpOnClick();
        if (Input.GetKey(KeyCode.K))
            CmdDropOnClick();
        if (Input.GetKey(KeyCode.I))
            CmdInteractOnClick();
    }

    void FixedUpdate()
    {
        // Don't move other player's models
        if (!isLocalPlayer)
            return;

        movement = new Vector3(horizontalMove, verticalMove, 0f).normalized;
        transform.position += movement * Time.fixedDeltaTime * moveSpeed;

        playerPos = transform.position;

        CmdRun();
        StartCoroutine(Timer(1));
        CmdUpdateTime();

        if (pickUpActive)
        {
            CmdPickUp();
        }

        //Debug.Log(Time.fixedDeltaTime);
        if (!Cooldown)
        {
            if (canDeposit && !pickUpActive && pickup != null) //if you're standing next to a depositor empty-handed with the item on the floor
            {
                if (pickup.GetComponent<PickupProperties>().itemType == "Money")
                {
                    float value = pickup.GetComponent<PickupProperties>().value;
                    CmdDestroy(pickup); //better for lag?
                    CmdUpdateMoney(value);

                }
                else if (pickup.GetComponent<PickupProperties>().itemType == "Box")
                {
                    float cost = pickup.GetComponent<PickupProperties>().value;
                    string itemName = pickup.GetComponent<PickupProperties>().itemName;

                    if (currentMoney != 0 && currentMoney - cost >= 0 && itemName != null) //&& itemPrefab != null
                    {
                        Debug.Log("Purchasing...");
                        CmdDestroy(pickup); //better for lag?
                        CmdUpdateMoney(-cost);
                        CmdSpawn(itemName);

                    }
                    else
                    {
                        Debug.Log("Not enough funds.");
                        //not enough funds
                    }
                }
                Cooldown = true;
                StartCoroutine(CooldownTimer(0.5f));
            }

            if (canDelete && !pickUpActive && pickup != null)
            {
                if (pickup.GetComponent<PickupProperties>().itemType != "Money" && pickup.GetComponent<PickupProperties>().itemType != "Box")
                {
                    string itemName = pickup.GetComponent<PickupProperties>().itemName;

                    Debug.Log(itemName + " deposited.");
                    CmdDestroy(pickup);
                }
                Cooldown = true;
                StartCoroutine(CooldownTimer(0.5f));
            }
        }
        else
            Debug.Log("On Cooldown!");


    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        //Don't check collision of other player's models
        if (!isLocalPlayer)
            return;

        if (collision.IsTouching(playerTrigger)) {
            if (collision.gameObject.CompareTag("Pickup") && !pickUpActive)
            {
                pickup = collision.gameObject;
                CmdTriggerStayPickup(pickup);
            }
            else if (collision.gameObject.CompareTag("Interactable"))
            {
                interactable = collision.gameObject;
                CmdTriggerStayInteractable(interactable);
            }
            else if (collision.gameObject.CompareTag("Depositor"))
                canDeposit = true;
            else if (collision.gameObject.CompareTag("Deleter"))
                canDelete = true;

        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        //Don't check collision of other player's models
        if (!isLocalPlayer)
            return;

        if (!collision.IsTouching(playerTrigger))
        {
            if (collision.gameObject.CompareTag("Pickup") & !pickUpActive) {
                pickup = null;
                CmdTriggerExitPickup();
            }
            else if (collision.gameObject.CompareTag("Interactable"))
                CmdTriggerExitInteractable();
            else if (collision.gameObject.CompareTag("Depositor"))
                canDeposit = false;
            else if (collision.gameObject.CompareTag("Deleter"))
                canDelete = false;
        }
    }
    #region Items
    private GameObject FindItem(string name)
    {
        string temp = "Item (" + name + ")";

        foreach (var prefab in Room.spawnPrefabs)
        {
            if (temp == prefab.name)
            {
                Debug.Log(temp + " found with netId " + prefab.GetComponent<NetworkIdentity>().netId);
                return prefab; //return the gameObject
            }
        }
        return null;
    }

    [Command]
    void CmdSpawn(string itemName)
    {
        itemPrefab = FindItem(itemName);

        //Debug.Log("Instantiating " + itemPrefab.name);
        GameObject item = (GameObject)Instantiate(itemPrefab, dropPoint.transform.position, Quaternion.identity);

        //Debug.Log("Spawning " + item.name);
        NetworkServer.Spawn(item);
    }

    [Command]
    void CmdDestroy(GameObject gameObject)
    {
        NetworkServer.Destroy(gameObject);
    }
    #endregion

    #region Actions
    [Command]
    void CmdPickUpOnClick()
    {
        pickUpActive = true;
        pickup.transform.position = holdPoint.position;
        RpcPickUpOnClick();
    }

    [ClientRpc]
    void RpcPickUpOnClick()
    {
        pickUpButton.gameObject.SetActive(false);
        dropButton.gameObject.SetActive(true);
    }

    [Command]
    void CmdDropOnClick()
    {
        pickUpActive = false;
        pickup.transform.position = dropPoint.position; //doesn't always drop at the drop point

        RpcDropOnClick();
    }

    [ClientRpc]
    void RpcDropOnClick()
    {
        pickUpButton.gameObject.SetActive(true);
        dropButton.gameObject.SetActive(false);
    }

    [Command]
    void CmdInteractOnClick()
    {
        //Debug.Log(playerId + " interacted with " + interactable.gameObject.name);
        RpcInteractOnClick();
    }

    [ClientRpc]
    void RpcInteractOnClick()
    {
        Debug.Log(playerId + " interacted with " + interactable.gameObject.name);
        //Debug.Log("I interacted with " + interactable.gameObject.name);
    }

    [Command]
    public void CmdPickUp()
    {
        //playerPosition.x += 0.1f;
        //playerPosition.y += 0.1f;
        //RpcPickUp(playerPosition);
        //pickup.transform.position = playerPosition;
        RpcPickUp();
        pickup.transform.position = holdPoint.position;
    }

    [ClientRpc]
    public void RpcPickUp()
    {
        //pickup.transform.position = playerPosition;
        //pickup.transform.position = holdPoint.position;
    }

    [Command]
    void CmdTriggerStayPickup(GameObject temp)
    {
        pickup = temp;
        RpcTriggerStayPickup();
    }

    [ClientRpc]
    void RpcTriggerStayPickup()
    {
        pickUpButton.interactable = true;
    }

    [Command]
    void CmdTriggerExitPickup()
    {
        pickup = null;
        RpcTriggerExitPickup();
    }

    [ClientRpc]
    void RpcTriggerExitPickup()
    {
        pickUpButton.interactable = false;
    }

    [Command]
    void CmdTriggerStayInteractable(GameObject temp)
    {
        interactable = temp;
        RpcTriggerStayInteractable();
    }

    [ClientRpc]
    void RpcTriggerStayInteractable()
    {
        interactButton.interactable = true;
    }

    [Command]
    void CmdTriggerExitInteractable()
    {
        interactable = null;
        RpcTriggerExitInteractable();
    }

    [ClientRpc]
    void RpcTriggerExitInteractable()
    {
        interactButton.interactable = false;
    }
    #endregion

    #region Movement
    private void flipPlayer()
    {
        Vector3 temp = gameName.transform.localScale; //transform.GetChild(2).GetChild(0).gameObject.transform.localScale;
        Vector3 theScale = transform.localScale;

        theScale.x *= -1;
        transform.localScale = theScale;
        facingRight = !facingRight;

        if (theScale.x < 0)
        {
            //facing left
            temp.x *= -1;
            gameName.transform.localScale = temp;
        }
        else
        {
            temp.x = Mathf.Abs(temp.x);
            gameName.transform.localScale = temp;
        }
    }

    [Command]
    void CmdRun()
    {
        RpcRun();
    }

    [ClientRpc]
    void RpcRun()
    {
        if (!isLocalPlayer)
            return;

        animator.SetBool("Running", !(Mathf.Abs(horizontalMove) < 0.01 && Mathf.Abs(verticalMove) < 0.01));
    }
    #endregion

    #region Money
    private void HandleMoneyChange(float value)
    {
        currentMoney += value;
        moneyText.text = currentMoney.ToString();
    }

    [Command]
    private void CmdUpdateMoney(float value)
    {
        RpcUpdateMoney(value);
    }

    [ClientRpc]
    private void RpcUpdateMoney(float value)
    {
        OnMoneyChange?.Invoke(value);
    }
    #endregion

    #region Time
    private void HandleTimeChange(float currentMatchTime)
    {
        float minutes = Mathf.FloorToInt(currentMatchTime / 60);
        float seconds = Mathf.FloorToInt(currentMatchTime % 60);

        ui_Timer.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    [Command]
    private void CmdUpdateTime()
    {
        currentTime = Room.GetTime();
        RpcUpdateTime(currentTime);
    }
    
    [ClientRpc]
    private void RpcUpdateTime(float value)
    {
        OnTimeChange?.Invoke(value);
    }

    private IEnumerator CooldownTimer(float time)
    {
        yield return new WaitForSeconds(time);

        Cooldown = false;
    }

    private IEnumerator Timer(float time)
    {
        yield return new WaitForSeconds(time);
    }
    #endregion
}

    
