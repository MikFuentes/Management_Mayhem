using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Sources
 * https://www.youtube.com/watch?v=oBRt9OifJvE
 */
public class Player : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    private float horizontalMove = 0f;
    private float verticalMove = 0f;
    private Vector3 movement;

    [Header("Joystick")]
    [SerializeField] private Joystick joystick;

    [Client]
    private void Update()
    {
        if (!hasAuthority) { return; }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            //CmdJump();
        }

        //Controls
        if (Input.GetKey(KeyCode.W) || joystick.Vertical >= .2f)
        {
            verticalMove = moveSpeed;
        }
        else if (Input.GetKey(KeyCode.S) || joystick.Vertical <= -.2f)
        {
            verticalMove = -moveSpeed;
        }
        else
        {
            verticalMove = 0;
        }

        if (Input.GetKey(KeyCode.A) || joystick.Horizontal <= -.2f)
        {
            horizontalMove = -moveSpeed;
        }
        else if (Input.GetKey(KeyCode.D) || joystick.Horizontal >= .2f)
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
