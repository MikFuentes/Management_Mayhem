using Cinemachine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Sources
 * https://www.youtube.com/watch?v=B4vNWUTQues
 */
public class PlayerCameraController : NetworkBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform playerTransform = null;
    [SerializeField] private CinemachineVirtualCamera virtualCamera = null;

    //private CinemachineTransposer transposer;

    public override void OnStartAuthority()
    {
        //transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();

        virtualCamera.gameObject.SetActive(true);

        enabled = true;
    }
}
