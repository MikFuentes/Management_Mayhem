﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PickupScript : NetworkBehaviour
{
    public CapsuleCollider2D pickup_collider;
    public bool triggered = false;
    public Vector3 position;

    void FixedUpdate()
    {
        //if (!triggered)
        //    pickup_collider.enabled = true;
    }

    //private void OnTriggerEnter2D(Collider2D collision)
    //{
    //    if (collision.CompareTag("Player") && collision.IsTouching(pickup_collider))
    //    {
    //        triggered = true;
    //    }
    //}

    //private void OnTriggerExit2D(Collider2D collision)
    //{
    //    if (collision.CompareTag("Player") && !collision.IsTouching(pickup_collider) && !collision.gameObject.GetComponent<PlayerScript>().pickUpActive)
    //    {
    //        triggered = false;
    //    }
    //}

    [ClientRpc]
    public void RpcEnableTrigger(bool b)
    {
        triggered = !b;
        pickup_collider.enabled = b;
    }


}
