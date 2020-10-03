using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* SOURCES
 * 
 * Multiplayer
 * https://www.youtube.com/watch?v=oBRt9OifJvE
 * 
 * Movement
 * https://www.youtube.com/watch?v=dwcT-Dch0bA
 * https://www.youtube.com/watch?v=L6Q6VHueWnU
 * 
 * Diagonal Movement
 * https://forum.unity.com/threads/how-do-i-used-normalized-my-diagonal-movement.301696/
 * 
 */
public class Player : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed;    
    private float horizontalMove, verticalMove;
    private float horizontalSpeed, verticalSpeed;
    private Vector3 movement;
    private bool facingRight = true;

    [Header("Joystick")]
    [SerializeField] private Joystick joystick;

    [Header("Animator")]
    [SerializeField] public Animator animator;

    private void flipPlayer()
    {
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    private void Update()
    {
        // Will update for the local player
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
            //If moving left while facing right...
            if (facingRight)
            {
                flipPlayer();
                facingRight = false;
            }
            horizontalMove = -moveSpeed;
        }
        else if (Input.GetKey(KeyCode.D) || joystick.Horizontal >= .2f)
        {
            //If moving right while facing left...
            if (!facingRight)
            {
                flipPlayer();
                facingRight = true;
            }
            horizontalMove = moveSpeed;
        }
        else
        {
            horizontalMove = 0;
        }
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer)
            return;

        horizontalSpeed = Mathf.Abs(horizontalMove);
        verticalSpeed = Mathf.Abs(verticalMove);

        //if (horizontalSpeed < 0.01 && verticalSpeed < 0.01)
        //{        
        //    animator.SetBool("Running", false);
        //}
        //else
        //{
        //    animator.SetBool("Running", true);
        //}

        movement = new Vector3(horizontalMove, verticalMove, 0f).normalized;
        transform.position += movement * Time.fixedDeltaTime * moveSpeed;

        CmdRun();
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

        animator.SetBool("Running", !(horizontalSpeed < 0.01 && verticalSpeed < 0.01));
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
