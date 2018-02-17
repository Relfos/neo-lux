using Neo.Cryptography;
using NeoLux;
using System;

namespace neo_sender
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length<5)
            {
                Console.WriteLine("neo-sender <Net> <PrivateKey> <DestAddress> <Symbol> <Amount>");
                return;
            }

            var keyStr = args[1];
            //fc1fa7c0d83426486373d9ce6eaca8adb506fc4ca25e89887c8eb5567f317a53"
            var outputAddress = args[2];
            //"AanTL6pTTEdnphXpyPMgb7PSE8ifSWpcXU"

            var symbol = args[3];   //"GAS"
            var amount = decimal.Parse(args[4]);

            var myKey = keyStr.Length == 52 ? KeyPair.FromWIF(keyStr) : new KeyPair(keyStr.HexToBytes());

            Console.WriteLine($"Sending {amount} {symbol} from {myKey.address} to {outputAddress}");

            var net = args[0].ToLowerInvariant();
            NeoAPI api;

            switch (net)
            {
                case "main": api = NeoRPC.ForMainNet(); break;
                case "test": api = NeoRPC.ForTestNet(); break;
                default: api = new NeoRPC(net); break;
            }

            var result = api.SendAsset(outputAddress, symbol, amount, myKey);
            Console.WriteLine(result);
        }
    }
}
