using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

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

    [Header("Movement")]
    public float moveSpeed;
    private float horizontalMove, verticalMove;
    private bool facingRight = true;
    private Vector3 movement;

    [Header("Debug Info")]
    [SyncVar]
    public GameObject pickup;
    [SyncVar]
    public bool pickUpActive = false;

    private void flipPlayer()
    {
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
        facingRight = !facingRight;
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
        dropButton.gameObject.SetActive(false);
        pickUpButton.gameObject.SetActive(true);
    }

    [Command]
    public void CmdPickUp(Vector3 playerPosition)
    {
        RpcPickUp(playerPosition);
        pickup.transform.position = playerPosition;
    }

    [ClientRpc]
    public void RpcPickUp(Vector3 playerPosition)
    {
        pickup.transform.position = playerPosition;
    }

    [Command]
    void CmdTriggerStay(GameObject temp)
    {
        pickup = temp;
        RpcTriggerStay();
    }

    [ClientRpc]
    void RpcTriggerStay()
    {
        pickUpButton.interactable = true;
    }

    [Command]
    void CmdTriggerExit()
    {
        pickup = null;
        RpcTriggerExit();
    }

    [ClientRpc]
    void RpcTriggerExit()
    {
        pickUpButton.interactable = false;
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
    }

    void FixedUpdate()
    {
        // Don't move other player's models
        if (!isLocalPlayer)
            return;

        movement = new Vector3(horizontalMove, verticalMove, 0f).normalized;
        transform.position += movement * Time.fixedDeltaTime * moveSpeed;

        CmdRun();

        if (pickUpActive)
            CmdPickUp(transform.position);
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
                CmdTriggerStay(pickup);
            }
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
                CmdTriggerExit();
            }
        }
    }
}
