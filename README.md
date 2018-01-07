# NEO Lux
NEO lightwallet / blockchain API for C#.

NEO Lux was developed to provide an easy way to interact with smart contracts in the NEO blockchain using C#. 

A full node is not necessary but you can also have one running locally and connect to it using NEO Lux.

## Compatibility

Platform | Status
------------ | -------------
.NET framework | Working
UWP | Working
Mono | Working
Xamarin / Mobile | Untested
Unity | Working


## Installation

    PM> Install-Package NeoLux

# Usage

Import the package:

```c#
using NeoLux;
```

For invoking a smart contract, eg:

```c#
	var privKey = "XXXXXXXXXXXXXXXXprivatekeyhereXXXXXXXXXXX".HexToBytes();	 // can be any valid private key
	var key = new KeyPair(privKey);
	var scriptHash = "de1a53be359e8be9f3d11627bcca40548a2d5bc1"; // the scriptHash of the smart contract you want to use	
	// for now, contracts must be in the format Main(string operation, object[] args)
	var result = NeoAPI.CallContract(NeoAPI.Net.Test, key, scriptHash, "registerMailbox", new object[] { "ABCDE", "demo@phantasma.io" });
```

For getting balance of an address:

```c#
	var balances = NeoAPI.GetBalance(NeoAPI.Net.Test, "AYpY8MKiJ9q5Fpt4EeQQmoYRHxdNHzwWHk");
	foreach (var entry in balances)
	{
		Console.WriteLine(entry.Key + " => " + entry.Value);
	}
```

# Unity support

NEOLux can be used together with Unity to make games that interact with the NEO blockchain.
A Unity demo showcasing loading a NEO wallet and querying the balance is included.

Careful as most NEOLux methods are blocking calls, in Unity the proper way to call them is using [Coroutines](https://docs.unity3d.com/Manual/Coroutines.html).
```c#
    IEnumerator SyncBalance()
    {
        var balances = NeoAPI.GetBalance(NeoAPI.Net.Test, this.keys.address);
        this.balance = balances["NEO"];
    }
	
	// then you call the method like this
	StartCoroutine(SyncBalance());
```

![Inputs Screenshot](images/neo_unity.jpg)

# TODO

* Sending assets to address (GAS and NEO)
* NEP5 token support

# Credits and License

Created by Sérgio Flores (<http://lunarlabs.pt/>).

Credits also go to the other devs from City Of Zion(<http://cityofzion.io/>), as this projected started as a port of code from their [NEON wallet](https://github.com/CityOfZion/neon-wallet) from Javascript to C#.
And of course, to the NEO team(<http://neo.org>), as I also used some code from their [NEO source](https://github.com/neo-project/neo).

This project is released under the MIT license, see `LICENSE.md` for more details.