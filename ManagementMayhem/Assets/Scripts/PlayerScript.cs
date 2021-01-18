using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

    [Header("Terrain")]
    public List<GameObject> frontWalls;
    public bool Indoors = false;

    [Header("Time")]
    [SerializeField] private TMP_Text ui_Timer = null;
    private static event Action<float> OnTimeChange;
    [SyncVar] private float currentTime;
    [SyncVar] public bool timerNotStarted = true;

    [Header("Movement")]
    [SyncVar] public Vector3 playerPos;
    public float moveSpeed;
    private float horizontalMove, verticalMove;
    private Vector3 movement;

    [Header("Money")]
    [SerializeField] private GameObject moneyUI = null;
    [SerializeField] private TMP_Text moneyText = null;
    [SyncVar] [SerializeField] public float currentMoney;
    private static event Action<float> OnMoneyChange;

    [Header("ATM_Balance")]
    [SerializeField] private GameObject homePanel = null;
    [SerializeField] private GameObject balanceUI = null;
    [SerializeField] private TMP_Text balanceText = null;
    [SyncVar] [SerializeField] public float currentBalance = 1000;
    private static event Action<float> OnBalanceChange;

    [Header("ATM_Withdraw")]
    [SerializeField] private GameObject withdrawPanel = null;
    [SerializeField] private GameObject withdrawUI = null;
    [SerializeField] private TMP_Text withdrawText = null;
    [SerializeField] private TMP_Text errorText = null;
    private string codeSequence = "0";
    public Button withdrawButton = null;

    [Header("ATM_Processing")]
    [SerializeField] private GameObject processingPanel = null;
    [SerializeField] private TMP_Text processingText = null;


    [Header("Items")]
    public GameObject itemPrefab;
    private string tempName = null;
    private bool NPC_item_match = false;
    private bool Cooldown = false;

    [Header("Debug Info")]
    [SyncVar] public GameObject pickup;
    [SyncVar] public bool pickUpActive = false;
    public GameObject interactable;
    public GameObject NPC;
    [SyncVar] public bool canDeposit;
    [SyncVar] public bool canDelete;
    public bool UI_Active;
    public GameObject[] sceneObjects = null;

    private NetworkManagerLobby room;
    private NetworkManagerLobby Room
    {
        get
        {
            if (room != null)
            {
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
        interactButton.onClick.AddListener(delegate { Activate_Interactable_UI(interactable); });
        withdrawButton.onClick.AddListener(delegate { ConfirmWithdrawal(); });

        // subscribe to events
        OnMoneyChange += HandleMoneyChange;
        OnBalanceChange += HandleBalanceChange;
        OnTimeChange += HandleTimeChange;
        PushTheButton.ButtonPressed += AddDigitToSequence;
    }

    [ClientCallback]
    public void OnDestroy()
    {
        if (!hasAuthority) { return; } // do nothing if we don't have authority
        OnMoneyChange -= HandleMoneyChange;
        OnBalanceChange -= HandleBalanceChange;
        OnTimeChange -= HandleTimeChange;
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
            //face left
            Vector3 theScale = transform.localScale;
            theScale.x = Math.Abs(theScale.x) * -1;
            transform.localScale = theScale;

            Vector3 theNameScale = gameName.transform.localScale;
            theNameScale.x = Math.Abs(theNameScale.x) * -1;
            gameName.transform.localScale = theNameScale;

            horizontalMove = -moveSpeed;
        }
        else if (Input.GetKey(KeyCode.D) || joystick.Horizontal >= .2f)
        {
            //face right
            Vector3 theScale = transform.localScale;
            theScale.x = Math.Abs(theScale.x);
            transform.localScale = theScale;

            Vector3 theNameScale = gameName.transform.localScale;
            theNameScale.x = Math.Abs(theNameScale.x);
            gameName.transform.localScale = theNameScale;

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

        if (!Cooldown)
        {
            // depositing money, buying items
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

            if (canDelete && !pickUpActive && pickup != null) // if you're standing next to a deleter empty-handed with the item on the floor
            {
                if (pickup.GetComponent<PickupProperties>().itemType != "Money" && pickup.GetComponent<PickupProperties>().itemType != "Box")
                {
                    if (NPC_item_match)
                    {
                        Debug.Log("Thank you for " + tempName + "!");

                        CmdChangeSprite(NPC, tempName);

                        NPC_item_match = false;

                        //restart wait timer
                        CmdRestartWaitTimer(10, NPC);
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

                gameObject.GetComponent<BoxCollider2D>().enabled = false;

                //GameObject Health_Bar = NPC.transform.Find("Health_Bar").gameObject;
                Sprite Item_Sprite = NPC.transform.Find("Item_Sprite").GetComponent<SpriteRenderer>().sprite;
                tempName = null;

                if (Item_Sprite == NPC.GetComponent<NPC_Script>().blank_sprite && Room.GamePlayers[0].timerStarted == false)
                {
                    CmdStartWaitTimer(10, NPC);
                }
                else
                {
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

                if (interactable.name == "ATM")
                {
                    CmdUpdateBalance(0); // show the most recent balance

                    //reset the ATM
                    codeSequence = "0";
                    withdrawText.text = codeSequence;
                    errorText.gameObject.SetActive(false);
                    homePanel.SetActive(true);
                    withdrawPanel.SetActive(false);
                }
            }
            else if (collision.gameObject.CompareTag("Depositor"))
                canDeposit = true;
            else if (collision.gameObject.CompareTag("Deleter"))
                canDelete = true;
            else if (collision.gameObject.CompareTag("Indoor"))
            {
                if (!Indoors)
                {
                    StartCoroutine(FadeOut());
                    Indoors = true;
                }

            }
            else if (collision.gameObject.CompareTag("Hidable"))
            {
                if (frontWalls.Count < 8)
                {
                    if (!frontWalls.Contains(collision.gameObject))
                    {
                        frontWalls.Add(collision.gameObject);
                    }
                }
                gameObject.GetComponent<BoxCollider2D>().enabled = false;
            }
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
            if (collision.gameObject.CompareTag("Pickup") && !pickUpActive)
            {
                pickup = null;
                CmdTriggerExitPickup();
            }
            else if (collision.gameObject.CompareTag("NPC"))
            {
                canDelete = false;

                gameObject.GetComponent<BoxCollider2D>().enabled = true;
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
            else if (collision.gameObject.CompareTag("Indoor"))
            {
                StartCoroutine(FadeIn());
                Indoors = false;
            }
            else if (collision.gameObject.CompareTag("Hidable"))
            {
                gameObject.GetComponent<BoxCollider2D>().enabled = true;
            }
        }
    }
    #region Items
    private GameObject FindItem(string name)
    {
        string temp = "";

        if (name.StartsWith("P"))
        {
            temp = name;
        }
        else
        {
            temp = "Item (" + name + ")";
        }

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
        Current_Interactable_UI = gameObject.transform.Find("CameraPlayer/HUD_Canvas/" + name + "_UI").gameObject; // look for coressponding UI using name

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
        pickup.transform.Find("Shadow").gameObject.SetActive(false); //disable shadow when picking up
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
        pickup.transform.Find("Shadow").gameObject.SetActive(true); //enable shadow when picking up
    }

    [Command]
    void CmdChangeSprite(GameObject go, string ItemName)
    {
        ChangeSprite(ItemName, go);
    }

    [Server]
    void ChangeSprite(string ItemName, GameObject go)
    {
        NPC = go;

        if (Room.ItemsRemaining == 0)
        {
            RpcChangeItemSprite(-1, go);
        }
        else if (ItemName != null)
        {
            Room.ItemsRemaining--;
            RpcRemoveFromItemArray(ItemName, go);
            ChangeSprite(null, go);
        }
        else if (ItemName == null)
        {
            int rand = UnityEngine.Random.Range(0, Room.ItemsRemaining); //0, 1 --> 0

            if (Room.ItemsRemaining != 1) //prevents infinite while loop when there is only 1 item left
            {
                while (rand == Room.prev_rand)
                {
                    rand = UnityEngine.Random.Range(0, Room.ItemsRemaining); //0, 1 --> 0
                }
            }

            RpcChangeItemSprite(rand, go);

            Room.prev_rand = rand;
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

    private IEnumerator FadeOut()
    {
        int opacity = 10;
        while (opacity > 0)
        {
            opacity--;
            foreach (GameObject go in frontWalls)
            {
                go.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, opacity / 10f);
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    private IEnumerator FadeIn()
    {
        int opacity = 0;
        while (opacity < 10f)
        {
            opacity++;
            foreach (GameObject go in frontWalls)
            {
                go.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, opacity / 10f);
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    [ClientRpc]
    private void RpcChangeItemSprite(int rand, GameObject go)
    {
        NPC = go;
        NPC.GetComponent<NPC_Script>().ChangeSprite(rand);
    }

    [ClientRpc]
    private void RpcRemoveFromItemArray(string ItemName, GameObject go)
    {
        NPC = go;
        NPC.GetComponent<NPC_Script>().RemoveFromArray(ItemName);
    }
    #endregion

    #region Movement

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

    #region ATM_Balance
    private void HandleBalanceChange(float value)
    {
        currentBalance += value;
        balanceText.text = currentBalance.ToString();

        codeSequence = "0";
    }

    [Command]
    private void CmdUpdateBalance(float value)
    {
        RpcUpdateBalance(value);
    }

    [ClientRpc]
    private void RpcUpdateBalance(float value)
    {
        OnBalanceChange?.Invoke(value);
    }

    private void AddDigitToSequence(string digitEntered)
    {
        if (digitEntered == "Clear" || digitEntered == "Back")
        {
            codeSequence = "0";
        }
        else
        {
            if (codeSequence.Length < 4)
            {
                if (codeSequence == "0")
                {
                    codeSequence = digitEntered;
                }
                else
                {
                    codeSequence += digitEntered;
                }
            }
        }
        withdrawText.text = codeSequence;
    }

    private void ConfirmWithdrawal()
    {
        int amount = Convert.ToInt32(codeSequence);

        if (IsValidWithdrawAmount(amount))
        {
            StartCoroutine(ActivateATMLoadScreen());

            DispenseCoins(amount);

            errorText.gameObject.SetActive(false);

            CmdUpdateBalance(-amount);
        }
        else
        {
            codeSequence = "0";
            withdrawText.text = "ERROR";

            errorText.gameObject.SetActive(true);
        }
    }

    private bool IsValidWithdrawAmount(int amount)
    {
        if (amount > currentBalance || amount < 25 || amount % 25 != 0) //max, min, divisible by 25 (smallest denomination)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    private void DispenseCoins(int amount)
    {
        int hundredcoins = 0;
        int fiftycoins = 0;
        int twofivecoins = 0;

        while (amount >= 100)
        {
            amount -= 100;
            hundredcoins++;
            CmdSpawn("P100");
        }
        while (amount >= 50)
        {
            amount -= 50;
            fiftycoins++;
            CmdSpawn("P50");
        }
        while (amount >= 25)
        {
            amount -= 25;
            twofivecoins++;
            CmdSpawn("P25");
        }
    }

    private IEnumerator ActivateATMLoadScreen()
    {
        processingText.text = "Withdrawing";
        processingPanel.SetActive(true);
        withdrawPanel.SetActive(false);
        yield return new WaitForSeconds(1);

        //wait some time
        processingText.text += ".";
        yield return new WaitForSeconds(1);

        processingText.text += ".";
        yield return new WaitForSeconds(1);

        processingText.text += ".";
        yield return new WaitForSeconds(1);

        processingText.text = "Withdrawal Complete!";
        yield return new WaitForSeconds(1);

        processingPanel.SetActive(false);
        homePanel.SetActive(true);
        codeSequence = "0";
        withdrawText.text = codeSequence;
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

    //Tell the server to start the wait timer
    [Command]
    private void CmdStartWaitTimer(int value, GameObject NPC)
    {
        RpcSynchTimers(NPC);
        Room.waitTimerCoroutine = StartCoroutine(StartWaiting(value, NPC));
    }

    //Tell the server to restart the wait timer
    [Command]
    private void CmdRestartWaitTimer(int value, GameObject NPC)
    {
        StopCoroutine(Room.waitTimerCoroutine);
        Room.waitTimerCoroutine = StartCoroutine(StartWaiting(value, NPC));
    }

    [ClientRpc]
    private void RpcSynchTimers(GameObject go)
    {
        for (int i = 0; i < Room.GamePlayers.Count; i++)
        {
            Room.GamePlayers[i].timerStarted = true;
            Room.GamePlayers[i].GetComponent<PlayerScript>().NPC = go;

            Room.GamePlayers[i].GetComponent<PlayerScript>().NPC.transform.Find("Health_Bar").gameObject.SetActive(true);
            Room.GamePlayers[i].GetComponent<PlayerScript>().NPC.transform.Find("Speech_Bubble_Sprite").gameObject.SetActive(true);
            Room.GamePlayers[i].GetComponent<PlayerScript>().NPC.transform.Find("Item_Sprite").gameObject.SetActive(true);
        }
    }

    [Server]
    private IEnumerator StartWaiting(int timeToWait, GameObject NPC)
    {
        ChangeSprite(null, NPC);

        for (int i = timeToWait; i >= 0; --i)
        {
            Room.currentWaitTime = i;

            RpcSetTimerSize(Room.currentWaitTime / timeToWait, NPC);
            RpcSetTimerColor(Room.currentWaitTime / timeToWait, NPC);

            yield return new WaitForSeconds(1);
        }

        //restart countdown
        Room.waitTimerCoroutine = StartCoroutine(StartWaiting(timeToWait, NPC));
    }

    [ClientRpc]
    private void RpcSetTimerSize(float f, GameObject go)
    {
        NPC = go;
        float size = f;
        NPC.GetComponent<NPC_Script>().SetSize(size);
    }

    [ClientRpc]
    private void RpcSetTimerColor(float f, GameObject go)
    {
        NPC = go;
        float size = f;

        if (size >= 0.5)
            NPC.GetComponent<NPC_Script>().SetColor(Color.green);
        else if (size >= 0.2)
            NPC.GetComponent<NPC_Script>().SetColor(Color.yellow);
        else
            NPC.GetComponent<NPC_Script>().SetColor(Color.red);
    }

    private IEnumerator CooldownTimer(float time)
    {
        yield return new WaitForSeconds(time);
        Cooldown = false;
    }

    #endregion
}