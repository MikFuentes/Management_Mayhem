using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class HealthBar : NetworkBehaviour
{
    private Transform bar;
    // Start is called before the first frame update
    private void Start()
    {
        bar = transform.Find("Bar");
    }

    [ClientRpc]
    public void RpcSetSize(float size_normalized)
    {
        bar.localScale = new Vector3(size_normalized, 1f);
    }

}
