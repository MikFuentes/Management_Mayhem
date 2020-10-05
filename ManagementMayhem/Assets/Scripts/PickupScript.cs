using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PickupScript : NetworkBehaviour
{
    public Collider2D box;
    public CapsuleCollider2D trigger;
    public bool triggered;

    // Update is called once per frame
    void FixedUpdate()
    {
        //If a player touches the trigger collider, disable box collider 
        if (triggered)
        {
            box.enabled = false; 
        }
        else
        {
            box.enabled = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && collision.IsTouching(trigger))
            triggered = true;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && collision.IsTouching(trigger))
            triggered = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !collision.IsTouching(trigger))
        {
            triggered = false;
        }
    }
}
