using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System.ComponentModel;

/*
 * https://answers.unity.com/questions/1271861/how-to-destroy-an-object-on-all-the-network.html
 */
public class PlayerScript : NetworkBehaviour
{
    [Header("Components")]
    public Joystick joystick;
    public Animator animator;
    public Button interactButton;
    public Button pickUpButton;
    public Button dropButton;
    public Transform holdPoint;
    public Transform dropPoint;
    public CapsuleCollider2D playerTrigger;
    [SerializeField] private GameObject gameName;
    //public TMP_Text playerName = null;
    private const string PlayerPrefsNameKey = "PlayerName";

    [Header("Movement")]
    public float moveSpeed;
    private float horizontalMove, verticalMove;
    private bool facingRight = true;
    private Vector3 movement;

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
    [SyncVar]
    public GameObject deleter;
    [SyncVar]
    public bool canDeposit;

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
    void CmdDestroy(GameObject gameObject)
    {
        NetworkServer.Destroy(gameObject);
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

    // Start is called before the first frame update
    void Start()
    {
        if (!isLocalPlayer)
            return;

        holdPoint.gameObject.SetActive(true);
        dropPoint.gameObject.SetActive(true);

        pickUpButton.onClick.AddListener(CmdPickUpOnClick);
        dropButton.onClick.AddListener(CmdDropOnClick);
        interactButton.onClick.AddListener(CmdInteractOnClick);


        playerId = ClientScene.localPlayer.netId.ToString();
    }

    // Update is called once per frame
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
        {
            CmdPickUpOnClick();
        }

        if (Input.GetKey(KeyCode.K))
        {
            CmdDropOnClick();
        }
        if (Input.GetKey(KeyCode.I))
        {
            CmdInteractOnClick();
        }
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

        if (pickUpActive)
        {
            //pickup.transform.position = holdPoint.position;
            CmdPickUp();
        }

        if (canDeposit && !pickUpActive)
        {
            CmdDestroy(pickup);
        }
            
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        //Don't check collision of other player's models
        if (!isLocalPlayer)
            return;

        if (collision.gameObject.CompareTag("Pickup") && collision.IsTouching(playerTrigger))
        {
            if (!pickUpActive)
            {
                pickup = collision.gameObject;
                CmdTriggerStayPickup(pickup);
            }
        }
        else if (collision.gameObject.CompareTag("Interactable") && collision.IsTouching(playerTrigger))
        {
            interactable = collision.gameObject;
            CmdTriggerStayInteractable(interactable);
        }
        else if (collision.gameObject.CompareTag("Deleter") && collision.IsTouching(playerTrigger))
        {
            //deleter = collision.gameObject;
            //CmdTriggerStayDeleter(deleter);
            canDeposit = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        //Don't check collision of other player's models
        if (!isLocalPlayer)
            return;

        if (collision.gameObject.CompareTag("Pickup") && !collision.IsTouching(playerTrigger))
        {
            if (!pickUpActive)
            {
                CmdTriggerExitPickup();
            }
        }
        else if (collision.gameObject.CompareTag("Interactable") && !collision.IsTouching(playerTrigger))
        {
            CmdTriggerExitInteractable();
        }
        else if (collision.gameObject.CompareTag("Deleter") && !collision.IsTouching(playerTrigger))
        {
            //deleter = collision.gameObject;
            //CmdTriggerStayDeleter(deleter);
            canDeposit = false;
        }
    }
}
