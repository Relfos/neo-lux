using Neo.Cryptography;
using NeoLux;
using System;

namespace Neo.Sender
{
    class Sender
    {
        static void Main(string[] args)
        {
            if (args.Length<5)
            {
                Console.WriteLine("neo-sender <Net> <PrivateKey> <DestAddress> <Symbol> <Amount>");
                Console.WriteLine("Net          Can be Main, Test or custom URL");
                Console.WriteLine("PrivateKey   Can be a full hex private key or a WIF private key");
                Console.WriteLine("DestAddress  Must be a valid Neo address");
                Console.WriteLine("Symbol       Must be Neo, Gas or any support token (eg: RPX, DBC)");
                return;
            }

            var keyStr = args[1];
            var outputAddress = args[2];

            var symbol = args[3];   //"GAS"
            var amount = decimal.Parse(args[4]);

            var fromKey = keyStr.Length == 52 ? KeyPair.FromWIF(keyStr) : new KeyPair(keyStr.HexToBytes());

            Console.WriteLine($"Sending {amount} {symbol} from {fromKey.address} to {outputAddress}");

            var net = args[0].ToLowerInvariant();
            NeoAPI api;

            switch (net)
            {
                case "main": api = NeoRPC.ForMainNet(); break;
                case "test": api = NeoRPC.ForTestNet(); break;
                default: api = new NeoRPC(net); break;
            }

            bool result = false;

            try
            {
                if (api.IsToken(symbol))
                {
                    var token = api.GetToken(symbol);
                    result = token.Transfer(fromKey, outputAddress, amount);
                }
                else
                if (api.IsAsset(symbol))
                {
                    result = api.SendAsset(fromKey, outputAddress, symbol, amount);
                }
                else
                {
                    Console.WriteLine("Unknown symbol.");
                    Environment.Exit(-1);
                }
            }
            catch 
            {
                Console.WriteLine("Error executing transaction.");
                Environment.Exit(-1);
            }

            Console.WriteLine("Transaction result: " +result);
        }
    }
}
