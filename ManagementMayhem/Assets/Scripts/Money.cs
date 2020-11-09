using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Money : NetworkBehaviour
{
    private float moneyAdded;

    [SyncVar]
    private float currentMoney;

    //define the event
    public delegate void MoneyChangedDelegate(float currentMoney);
    [SyncEvent]
    public event MoneyChangedDelegate EventMoneyChanged;

    #region Server
    
    [Server]
    private void SetMoney(float value)
    {
        currentMoney = value;
        EventMoneyChanged?.Invoke(currentMoney);
    }

    [Server]
    private void AddMoney(float value)
    {
        currentMoney += value;
        EventMoneyChanged?.Invoke(currentMoney); //updating the money will raise this event
    }

    public override void OnStartServer() => SetMoney(0);

    [Command]
    private void CmdAddMoney() => AddMoney(0);
    #endregion

    #region Client

    #endregion
}
