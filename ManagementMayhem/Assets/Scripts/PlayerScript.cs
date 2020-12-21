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
    public GameObject Game_UI;
    public GameObject Current_Interactable_UI;
    public GameObject Faded_Background;
    [SerializeField] private GameObject gameName;


    [Header("Time")]
    [SerializeField] private TMP_Text ui_Timer = null;
    private static event Action<float> OnTimeChange;
    [SyncVar] private float currentTime;
    private static event Action<float> OnWaitChange;
    [SyncVar] public float current_wait_time;

    [Header("Movement")]
    [SyncVar] public Vector3 playerPos;
    public float moveSpeed;
    private float horizontalMove, verticalMove;
    private bool facingRight = true;
    private Vector3 movement;

    [Header("Money")]
    [SerializeField] private GameObject moneyUI = null;
    [SerializeField] private TMP_Text moneyText = null;
    [SyncVar] [SerializeField] public float currentMoney;
    private static event Action<float> OnMoneyChange;

    [Header("Items")]
    public GameObject itemPrefab;
    [SyncVar] private int rand = 0;
    [SyncVar] private int prev_rand = -1;
    private String tempName = null;
    private bool NPC_item_match = false;
    private bool Cooldown = false;

    [Header("Debug Info")]
    [SyncVar] public GameObject pickup;
    [SyncVar] public bool pickUpActive = false;
    public GameObject interactable;
    [SyncVar] public GameObject NPC;
    [SyncVar] public bool canDeposit;
    [SyncVar] public bool canDelete;
    public bool UI_Active;


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
    public override void OnStartAuthority()
    {
        // ui elements
        moneyUI.SetActive(true);
        holdPoint.gameObject.SetActive(true);
        dropPoint.gameObject.SetActive(true);

        // buttons
        pickUpButton.onClick.AddListener(CmdPickUpOnClick);
        dropButton.onClick.AddListener(CmdDropOnClick);
        interactButton.onClick.AddListener(delegate{Activate_Interactable_UI(interactable);});
        
        // subscribe to events
        OnMoneyChange += HandleMoneyChange;
        OnTimeChange += HandleTimeChange;
        OnWaitChange += HandleWaitChange;

        NPC = GameObject.Find("NPC_P (1)");
    }

    [ClientCallback]
    public void OnDestroy()
    {
        if (!hasAuthority) { return; } // do nothing if we don't have authority
        OnMoneyChange -= HandleMoneyChange;
        OnTimeChange -= HandleTimeChange;
        OnWaitChange -= HandleWaitChange;
    }

    void Update()
    {
        // don't get input from other player's
        if (!isLocalPlayer)
            return;

        // up and down
        if (Input.GetKey(KeyCode.W) || joystick.Vertical >= .2f)
            verticalMove = moveSpeed;
        else if (Input.GetKey(KeyCode.S) || joystick.Vertical <= -.2f)
            verticalMove = -moveSpeed;
        else
            verticalMove = 0;

        // left and right
        if (Input.GetKey(KeyCode.A) || joystick.Horizontal <= -.2f)
        {
            if (facingRight) { flipPlayer(); } // if moving left while facing right...
            horizontalMove = -moveSpeed;
        }
        else if (Input.GetKey(KeyCode.D) || joystick.Horizontal >= .2f)
        {
            if (!facingRight) { flipPlayer(); } // if moving right while facing left...
            horizontalMove = moveSpeed;
        }
        else
            horizontalMove = 0;

        if (Input.GetKey(KeyCode.J) && pickup != null) // pick up
            CmdPickUpOnClick();
        if (Input.GetKey(KeyCode.K) && pickup != null) // drop
            CmdDropOnClick();
        if (Input.GetKey(KeyCode.I) && interactable != null) // interact
            Activate_Interactable_UI(interactable);
    }

    void FixedUpdate()
    {
        // don't affect other players
        if (!isLocalPlayer)
            return;

        // update match time
        CmdUpdateTime();

        // update movement
        movement = new Vector3(horizontalMove, verticalMove, 0f).normalized;
        transform.position += movement * Time.fixedDeltaTime * moveSpeed;
        playerPos = transform.position;
        CmdRun();

        // update pickup position
        if (pickUpActive)
        {
            pickup.transform.position = holdPoint.position;
            CmdHold();
        }
        //else if (pickup != null && !pickup.GetComponent<PickupScript>().triggered)
        //{
        //    Debug.Log(1);
        //}

        // 
        if (!Cooldown)
        {
            if (canDeposit && !pickUpActive && pickup != null) // if you're standing next to a depositor empty-handed with the item on the floor
            {
                if (pickup.GetComponent<PickupProperties>().itemType == "Money")
                {
                    float value = pickup.GetComponent<PickupProperties>().value;
                    CmdDestroy(pickup);
                    CmdTriggerExitPickup();
                    CmdUpdateMoney(value);
                }
                else if (pickup.GetComponent<PickupProperties>().itemType == "Box")
                {
                    float cost = pickup.GetComponent<PickupProperties>().value;
                    string itemName = pickup.GetComponent<PickupProperties>().itemName;

                    if (currentMoney != 0 && currentMoney - cost >= 0 && itemName != null)
                    {
                        Debug.Log("Purchasing...");
                        CmdDestroy(pickup);
                        CmdTriggerExitPickup();
                        CmdUpdateMoney(-cost);
                        CmdSpawn(itemName);
                    }
                    else
                    {
                        // not enough funds
                        Debug.Log("Not enough funds.");
                    }
                }
                Cooldown = true;
                StartCoroutine(CooldownTimer(0.5f));
            }

            if (canDelete && !pickUpActive && pickup != null)
            {
                if (pickup.GetComponent<PickupProperties>().itemType != "Money" && pickup.GetComponent<PickupProperties>().itemType != "Box")
                {
                    if (NPC_item_match)
                    {
                        Debug.Log("Thank you for " + tempName + "!");

                        CmdChangeSprite(NPC, tempName);


                        NPC_item_match = false;

                        //restart wait timer
                        CmdUpdateWaitTime(1f);
                    }

                    string itemName = pickup.GetComponent<PickupProperties>().itemName;

                    Debug.Log(itemName + " deposited.");
                    CmdDestroy(pickup);
                    CmdTriggerExitPickup();
                }
                Cooldown = true;
                StartCoroutine(CooldownTimer(0.5f));
            }
        }
        //else
        //    Debug.Log("On Cooldown!");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isLocalPlayer)
            return; // don't check collision of other player's models

        if (collision.IsTouching(playerTrigger))
        {
            if (collision.gameObject.CompareTag("Pickup") && !pickUpActive)
            {
                pickup = collision.gameObject;
                CmdTriggerEnterPickup(pickup);
            }
            else if (collision.gameObject.CompareTag("NPC"))
            {
                NPC = collision.gameObject;
                NPC.transform.Find("Health_Bar").gameObject.SetActive(true);
                NPC.transform.Find("Speech_Bubble_Sprite").gameObject.SetActive(true);
                NPC.transform.Find("Item_Sprite").gameObject.SetActive(true);

                //GameObject Health_Bar = NPC.transform.Find("Health_Bar").gameObject;
                Sprite Item_Sprite = NPC.transform.Find("Item_Sprite").GetComponent<SpriteRenderer>().sprite;
                tempName = null;

                if (Item_Sprite == NPC.GetComponent<NPC_Script>().blank_sprite)
                {
                    CmdChangeSprite(NPC, null);

                    //restart wait timer
                    CmdUpdateWaitTime(1f);
                }
                else
                {
                    CmdUpdateWaitTime(-0.1f);
                    switch (Item_Sprite.name)
                    {
                        case "Chair":
                            tempName = "Chair";
                            break;
                        case "Drinks":
                            tempName = "Drinks";
                            break;
                        case "Food":
                            tempName = "Food";
                            break;
                        case "Microphone":
                            tempName = "Microphone";
                            break;
                        case "Speaker":
                            tempName = "Speaker";
                            break;
                    }

                    if (pickup != null && pickup.GetComponent<PickupProperties>().itemType == "Item" && pickup.GetComponent<PickupProperties>().itemName == tempName)
                    {
                        Debug.Log("You have what I want!");
                        canDelete = true;
                        NPC_item_match = true;
                    }
                    else
                    {
                        Debug.Log("I want " + tempName);
                    }
                }
                
            }
            else if (collision.gameObject.CompareTag("Interactable"))
            {
                interactable = collision.gameObject;
                interactButton.interactable = true;
            }
            else if (collision.gameObject.CompareTag("Depositor"))
                canDeposit = true;
            else if (collision.gameObject.CompareTag("Deleter"))
                canDelete = true;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isLocalPlayer)
            return; // don't check collision of other player's models

        if (collision.IsTouching(playerTrigger))
        {
            if (collision.gameObject.CompareTag("Pickup") && !pickUpActive)
            {
                pickup = collision.gameObject;
                CmdTriggerEnterPickup(pickup);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!isLocalPlayer)
            return; // don't check collision of other player's models

        if (!collision.IsTouching(playerTrigger))
        {
            if (collision.gameObject.CompareTag("Pickup") && !pickUpActive) {
                pickup = null;
                CmdTriggerExitPickup();
            }
            else if (collision.gameObject.CompareTag("NPC"))
            {
                //NPC.transform.Find("Health_Bar").gameObject.SetActive(false);
                //NPC.transform.Find("Speech_Bubble_Sprite").gameObject.SetActive(false);
                //NPC.transform.Find("Item_Sprite").gameObject.SetActive(false);
                canDelete = false;
            }
            else if (collision.gameObject.CompareTag("Interactable"))
            {
                interactable = null;
                interactButton.interactable = false;
                Deactivate_Interactable_UI();
            }
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
                return prefab; // return the gameObject
            }
        }
        return null;
    }

    [Command]
    void CmdSpawn(string itemName)
    {
        itemPrefab = FindItem(itemName);
        GameObject item = (GameObject)Instantiate(itemPrefab, dropPoint.transform.position, Quaternion.identity);
        NetworkServer.Spawn(item);
    }

    [Command]
    void CmdDestroy(GameObject gameObject)
    {
        NetworkServer.Destroy(gameObject);
    }

    #endregion

    #region Actions

    void Activate_Interactable_UI(GameObject obj)
    {
        string name = obj.name; // get name of interactable 
        Current_Interactable_UI = gameObject.transform.Find("CameraPlayer/HUD/" + name + "_UI").gameObject; // look for coressponding UI using name

        Current_Interactable_UI.SetActive(true); // activate UI
        Faded_Background.SetActive(true);
        UI_Active = true; // set bool
        Game_UI.SetActive(false); // deactivate game_UI
    }

    void Deactivate_Interactable_UI()
    {
        if (UI_Active)
        {
            Current_Interactable_UI.SetActive(false); // deactivate UI
            Faded_Background.SetActive(false);
            UI_Active = false; // set bool
        }
        Game_UI.SetActive(true); // activate game_UI
    }

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
    public void CmdHold()
    {
        pickup.GetComponent<PickupScript>().RpcEnableTrigger(!pickUpActive); // BUG: exit trigger is triggered when it's not supposed to | WORKAROUND: place here instead of OnClick (but terrible for performance)
        pickup.transform.position = holdPoint.position;
    }

    [Command]
    void CmdDropOnClick()
    {
        pickUpActive = false;
        pickup.GetComponent<PickupScript>().RpcEnableTrigger(!pickUpActive);
        pickup.transform.position = dropPoint.position; // BUG: doesn't always drop at the drop point...
        RpcDropOnClick();

        //pickup = null; // (1/2) BUG: exit trigger doesn't always trigger | WORKAROUND: force the exit trigger every time by making pickup null 
        //commented the above due to null error message when dropping items
    }

    [ClientRpc]
    void RpcDropOnClick()
    {
        pickUpButton.interactable = false; // (2/2) and making pickUpButton non-interactable
        pickUpButton.gameObject.SetActive(true);
        dropButton.gameObject.SetActive(false);
    }

    [Command]
    void CmdChangeSprite(GameObject go, string Item)
    {
        NPC = go;
        int count = NPC.GetComponent<NPC_Script>().item_sprite_array.Count;

        if (Item != null)
        {
            NPC.GetComponent<NPC_Script>().RpcRemoveFromArray(Item);
            count--;
        }

        if(count == 0)
        {
            NPC.GetComponent<NPC_Script>().RpcChangeSprite(-1);
        }
        else
        {
            rand = UnityEngine.Random.Range(0, count);

            while (rand == prev_rand)
            {
                rand = UnityEngine.Random.Range(0, count);
            }

            NPC.GetComponent<NPC_Script>().RpcChangeSprite(rand);

            prev_rand = rand;

        }
    }

    #endregion

    #region Triggers

    [Command]
    void CmdTriggerEnterPickup(GameObject go)
    {
        pickup = go;
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
    #endregion

    #region Movement
    private void flipPlayer()
    {
        Vector3 temp = gameName.transform.localScale;
        Vector3 theScale = transform.localScale;

        theScale.x *= -1;
        transform.localScale = theScale;
        facingRight = !facingRight;

        if (theScale.x < 0)
        {
            temp.x *= -1; // facing left
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

    private void HandleWaitChange(float value)
    {
        if ((current_wait_time + value) >= 1)
            current_wait_time = 1;
        else if((current_wait_time + value) <= 0)
            current_wait_time = 0;
        else
            current_wait_time += value;

        NPC.GetComponent<NPC_Script>().SetSize(current_wait_time);
        
        if(current_wait_time >= 0.5)
            NPC.GetComponent<NPC_Script>().SetColor(Color.green);
        else if (current_wait_time >= 0.2) 
            NPC.GetComponent<NPC_Script>().SetColor(Color.yellow);
        else
            NPC.GetComponent<NPC_Script>().SetColor(Color.red);

        if (current_wait_time == 0)
        {
            CmdUpdateWaitTime(1);
            CmdChangeSprite(NPC, null);
        }

        //CmdSetSize(NPC, current_wait_time);
    }

    [Command]
    private void CmdUpdateWaitTime(float value)
    {
        RpcUpdateWaitTime(value);
    }

    [ClientRpc]
    private void RpcUpdateWaitTime(float value)
    {
        OnWaitChange?.Invoke(value);
    }

    [Command]
    void CmdSetSize(GameObject go, float f)
    {
        NPC = go;
        float size = f;
        NPC.GetComponent<NPC_Script>().RpcSetSize(size);
    }


    private IEnumerator CooldownTimer(float time)
    {
        yield return new WaitForSeconds(time);
        Cooldown = false;
    }

    #endregion
}