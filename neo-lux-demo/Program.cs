using System;
using System.Collections.Generic;
using System.Numerics;
using Neo.Cryptography;
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

            Console.WriteLine("*Syncing balances...");
            var balances = NeoAPI.GetBalance(NeoAPI.Net.Test, keys.address);
            foreach (var entry in balances)
            {
                Console.WriteLine(entry.Value + " " + entry.Key);
            }

            // NEP5 https://github.com/neo-project/proposals/issues/3
            var contractHash = "ecc6b20d3ccac1ee9ef109af5a7cdb85706b1df9";

            Console.WriteLine("*Querying Symbol from RedPulse contract...");
            var response = NeoAPI.TestInvokeScript(NeoAPI.Net.Main, contractHash, "symbol", new object[] { "" });
            var symbol = System.Text.Encoding.ASCII.GetString((byte[])response.result);
            Console.WriteLine(symbol);

            Console.WriteLine("*Querying TotalSupply from RedPulse contract...");
            response = NeoAPI.TestInvokeScript(NeoAPI.Net.Main, contractHash, "totalSupply", new object[] { "" });
            var supply = new BigInteger((byte[])response.result);
            Console.WriteLine(supply);

            Console.WriteLine("Press any key to quit...");
            Console.ReadKey();
        }
    }
}
