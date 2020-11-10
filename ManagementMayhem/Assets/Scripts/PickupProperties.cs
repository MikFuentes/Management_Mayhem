using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupProperties : NetworkBehaviour
{
    public string itemType; //box or money
    public string itemName;
    public float value; //cost = -value
}
