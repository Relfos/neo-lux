using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using NeoLux;
using Neo.Cryptography;

/* about wallet.db3 
 * password is test
 * address is AH78bx9sfVKR4cPaC527ktaDRjtwKNur8B
 * private key is a9e2b5436cab6ff74be2d5c91b8a67053494ab5b454ac2851f872fb0fd30ba5e
 */

public class NeoDemo : MonoBehaviour {

    private enum WalletState
    {
        Init,
        Sync,
        Update,
        Ready
    }

    private KeyPair keys;

    public Text addressLabel;
    public Text balanceLabel;

    public Button startBtn;

    private WalletState state = WalletState.Init;
    private decimal balance;

    private const string assetSymbol = "GAS";

    void Start () {
        this.keys = new KeyPair("a9e2b5436cab6ff74be2d5c91b8a67053494ab5b454ac2851f872fb0fd30ba5e".HexToBytes());

        this.addressLabel.text = keys.address;
        this.balanceLabel.text = "Please wait, syncing balance...";        
    }


    IEnumerator SyncBalance()
    {
        yield return new WaitForSeconds(2);
        var balances = NeoAPI.GetBalance(NeoAPI.Net.Test, this.keys.address);
        this.balance = balances.ContainsKey(assetSymbol) ? balances[assetSymbol] : 0;
        this.state = WalletState.Update;
    }

    void Update () {

        switch (state)
        {
            case WalletState.Init:
                {
                    state = WalletState.Sync;
                    StartCoroutine(SyncBalance());
                    break;
                }

            case WalletState.Update:
                {
                    this.state = WalletState.Ready;
                    this.balanceLabel.text = balance.ToString() + " "+ assetSymbol;
                    this.startBtn.interactable = true;
                    break;
                }
        }		
	}
}
