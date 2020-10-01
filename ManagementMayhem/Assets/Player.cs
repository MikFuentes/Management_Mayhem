using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField] private Vector3 movement;
    [SerializeField] private Vector3 jump = new Vector3();

    [SerializeField] private float moveSpeed;
    float horizontalMove = 0f;
    float verticalMove = 0f;

    [Client]
    private void Update()
    {
        if (!hasAuthority) { return; }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            //CmdJump();
        }

        //Controls
        if (Input.GetKey(KeyCode.W))
        {
            verticalMove = moveSpeed;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            verticalMove = -moveSpeed;
        }
        else
        {
            verticalMove = 0;
        }

        if (Input.GetKey(KeyCode.A))
        {
            horizontalMove = -moveSpeed;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            horizontalMove = moveSpeed;
        }
        else 
        {
            horizontalMove = 0;
        }

        //CmdMove();

    }

    [Client]

    private void FixedUpdate()
    {
        movement = new Vector3(horizontalMove, verticalMove, 0f).normalized;
        transform.position += movement * Time.fixedDeltaTime * moveSpeed;
    }

    //[Command]
    //private void CmdMove()
    //{
    //    //Validate logic here

    //    RpcMove();
    //}

    //[Command]
    //private void CmdJump()
    //{
    //    //Validate logic here

    //    RpcJump();
    //}

    //[ClientRpc]
    //private void RpcMove() 
    //{
    //    //Debug.Log(horizontalMove + " ," + verticalMove + " , 0");
    //    movement = new Vector3(horizontalMove, verticalMove, 0f).normalized;
    //    transform.position += movement * Time.fixedDeltaTime * moveSpeed;
    //}

    //[ClientRpc]
    //private void RpcJump()
    //{
    //    transform.Translate(jump);
    //}
}
