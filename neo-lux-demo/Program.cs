using System;
using System.Collections.Generic;
using System.Numerics;
using Neo.Cryptography;
using Neo.Emulator;
using NeoLux;

namespace neo_lux_demo
{
    class Program
    {
        static void Main(string[] args)
        {
            // NOTE - Private keys should not be hardcoded in the code, this is just for demonstration purposes!
            var privateKey = "a9e2b5436cab6ff74be2d5c91b8a67053494ab5b454ac2851f872fb0fd30ba5e";

            Console.WriteLine("*Loading NEO address...");
            var keys = new KeyPair(privateKey.HexToBytes());
            Console.WriteLine("Got :"+keys.address);

            // it is possible to optionally obtain also token balances with this method
            Console.WriteLine("*Syncing balances...");
            var balances = NeoAPI.GetBalance(NeoAPI.Net.Test, keys.address, false);
            foreach (var entry in balances)
            {
                Console.WriteLine(entry.Value + " " + entry.Key);
            }

            // TestInvokeScript let's us call a smart contract method and get back a result
            // NEP5 https://github.com/neo-project/proposals/issues/3
            var contractHash = "ecc6b20d3ccac1ee9ef109af5a7cdb85706b1df9";

            Console.WriteLine("*Querying Symbol from RedPulse contract...");
            var response = NeoAPI.TestInvokeScript(NeoAPI.Net.Main, contractHash, "symbol", new object[] { "" });
            var symbol = System.Text.Encoding.ASCII.GetString((byte[])response.result);
            Console.WriteLine(symbol); // should return "RPX"

            // here we get the RedPulse token balance from an address
            Console.WriteLine("*Querying BalanceOf from RedPulse contract...");
            var balance = NeoAPI.GetTokenBalance("AVQ6jAQ3Prd32BXU5r2Vb3QL1gYzTpFhaf", "RPX");
            Console.WriteLine(balance);

            Console.WriteLine("Press any key to quit...");
            Console.ReadKey();
        }
    }
}
