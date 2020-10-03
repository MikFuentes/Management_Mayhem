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
    [Header("Components")]
    public Joystick joystick;
    public Animator animator;

    [Header("Movement")]
    public float moveSpeed;
    private float horizontalMove, verticalMove;
    private bool facingRight = true;    
    private Vector3 movement;

    private void flipPlayer()
    {
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
        facingRight = !facingRight;
    }

    private void Update()
    {
        // Will update for the local player only
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
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer)
            return;

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

        animator.SetBool("Running", !(Mathf.Abs(horizontalMove) < 0.01 && Mathf.Abs(verticalMove) < 0.01));
    }

}
