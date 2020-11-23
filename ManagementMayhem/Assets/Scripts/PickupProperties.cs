using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PickupProperties : NetworkBehaviour
{
    public string itemType; //box or money
    public string itemName;
    public float value; //cost = -value

    [SerializeField] private TMP_Text playerName = null;

    public void Start()
    {
        if(itemType == "Box")
            playerName.text = "₱" + value.ToString();
    }
}
