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

            var net = Enum.Parse(typeof(NeoAPI.Net), args[0], true);

            var privKey = args[1].HexToBytes();
            //fc1fa7c0d83426486373d9ce6eaca8adb506fc4ca25e89887c8eb5567f317a53"
            var outputAddress = args[2];
            //"AanTL6pTTEdnphXpyPMgb7PSE8ifSWpcXU"

            var symbol = args[3];   //"GAS"
            var amount = decimal.Parse(args[4]);

            var myKey = new KeyPair(privKey);
            NeoAPI.SendAsset(NeoAPI.Net.Test, outputAddress, symbol, amount, myKey);
            Console.ReadKey();
        }
    }
}
