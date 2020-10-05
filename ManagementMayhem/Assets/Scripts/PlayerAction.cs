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
    public Collider2D pickupTrigger;
    public bool pickUpActive = false;

    private void Start()
    {
        if (!isLocalPlayer)
            return;

        holdPoint.gameObject.SetActive(true);
        dropPoint.gameObject.SetActive(true);

        pickUpButton.onClick.AddListener(PickUpOnClick);
        dropButton.onClick.AddListener(DropOnClick);
    }

    void PickUpOnClick()
    {
        if (!isLocalPlayer)
            return;

        pickUpActive = true;
        pickupTrigger.gameObject.transform.position = holdPoint.position;
        pickUpButton.gameObject.SetActive(false);
        dropButton.gameObject.SetActive(true);
    }

    void DropOnClick()
    {
        if (!isLocalPlayer)
            return;

        pickUpActive = false;        
        pickupTrigger.gameObject.transform.position = dropPoint.position; //This doesn't always drop at the drop point
        dropButton.gameObject.SetActive(false);
        pickUpButton.gameObject.SetActive(true);

    }

    [Command]
    public void CmdPickUp()
    {
        RpcPickUp();
    }

    [ClientRpc]
    void RpcPickUp()
    {
        if (!isLocalPlayer)
            return;

        pickupTrigger.gameObject.transform.position = holdPoint.position;
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
                pickupTrigger = collision;
                pickUpButton.interactable = true;
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
                pickupTrigger = null;
                pickUpButton.interactable = false;
            }
        }
    }


    private void FixedUpdate()
    {
        //if (!isLocalPlayer)
        //    return;

        if (pickUpActive)
            CmdPickUp();


    }
}
