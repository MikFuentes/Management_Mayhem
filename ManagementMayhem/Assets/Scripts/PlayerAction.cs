using System.Collections;
using System.Collections.Generic;
using UnityEditor;
//using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

/**Sources
 * https://www.youtube.com/watch?v=ZoZcBgRR9ns
 * https://www.youtube.com/watch?v=Bc9lmHjqLZc
 * https://www.youtube.com/watch?v=SoBdvBTZqhw
 **/

public class PlayerAction : NetworkBehaviour
{
    [Header("Components")]
    public Button interactButton;
    public Button pickUpButton;
    public Button dropButton;
    public Transform holdPoint;
    public Transform dropPoint;
    public CapsuleCollider2D playerTrigger;

    [Header("Debug Info")]
    [SyncVar]
    public GameObject pickup;
    [SyncVar]
    public bool pickUpActive = false;

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
    public void CmdPickUp(float time)
    {   
        RpcPickUp(holdPoint.position);   
        pickup.transform.position = holdPoint.position;
        //Debug.Log("SERVER: " + pickup.transform.position);
    }

    [ClientRpc]
    public void RpcPickUp(Vector3 temp)
    {
        pickup.transform.position += temp * Time.fixedDeltaTime;
        //Debug.Log("CLIENT:  " + pickup.transform.position);
    }

    private void Start()
    {
        if (!isLocalPlayer)
            return;

        holdPoint.gameObject.SetActive(true);
        dropPoint.gameObject.SetActive(true);

        pickUpButton.onClick.AddListener(CmdPickUpOnClick);
        dropButton.onClick.AddListener(CmdDropOnClick);
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

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        if (Input.GetKey(KeyCode.J))
        {
            CmdPickUpOnClick();
        }

        if (Input.GetKey(KeyCode.K))
        {
            CmdDropOnClick();
        }
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer)
            return;

        if (pickUpActive)
            CmdPickUp(Time.fixedDeltaTime);
    }
}
