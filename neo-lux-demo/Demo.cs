using System;
using Neo.Cryptography;

namespace NeoLux.Demo
{
    class Demo
    {
        static void Main(string[] args)
        {
            // NOTE - You can also create an API instance for a specific private net
            var api = NeoRPC.ForTestNet();

            // NOTE - Private keys should not be hardcoded in the code, this is just for demonstration purposes!
            var privateKey = "a9e2b5436cab6ff74be2d5c91b8a67053494ab5b454ac2851f872fb0fd30ba5e";

            Console.WriteLine("*Loading NEO address...");
            var keys = new KeyPair(privateKey.HexToBytes());
            Console.WriteLine("Got :"+keys.address);

            // it is possible to optionally obtain also token balances with this method
            Console.WriteLine("*Syncing balances...");
            var balances = api.GetBalancesOf(keys.address, false);
            foreach (var entry in balances)
            {
                Console.WriteLine(entry.Value + " " + entry.Key);
            }

            // TestInvokeScript let's us call a smart contract method and get back a result
            // NEP5 https://github.com/neo-project/proposals/issues/3

            api = NeoRPC.ForMainNet();
            var redPulse = api.GetToken("RPX");

            // you could also create a NEP5 from a contract script hash
            //var redPulse_contractHash = "ecc6b20d3ccac1ee9ef109af5a7cdb85706b1df9";
            //var redPulse = new NEP5(api, redPulse_contractHash);

            Console.WriteLine("*Querying Symbol from RedPulse contract...");
            //var response = api.TestInvokeScript(redPulse_contractHash, "symbol", new object[] { "" });
            //var symbol = System.Text.Encoding.ASCII.GetString((byte[])response.result);
            var symbol = redPulse.Symbol;
            Console.WriteLine(symbol); // should print "RPX"

            // here we get the RedPulse token balance from an address
            Console.WriteLine("*Querying BalanceOf from RedPulse contract...");
            //var balance = api.GetTokenBalance("AVQ6jAQ3Prd32BXU5r2Vb3QL1gYzTpFhaf", "RPX");
            var balance = redPulse.BalanceOf("AVQ6jAQ3Prd32BXU5r2Vb3QL1gYzTpFhaf");
            Console.WriteLine(balance);

            Console.WriteLine("Press any key to quit...");
            Console.ReadKey();
        }
    }
}
