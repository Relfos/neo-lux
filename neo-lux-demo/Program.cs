using System;
using System.Collections.Generic;

using NeoLux;
using NeoLux.Core;
using Neo.VM;
using System.Numerics;
using System.Globalization;
using System.IO;
using System.Linq;
using Neo.Cryptography;

namespace NeoLuxDemo
{
    class Program
    {

        static void Main(string[] args)
        {

            /*var keys = new KeyPair("1480ac44ae8081f67242b9aaef8349f57d25e144d1ec1609a5f0eea4bc97d014".HexToBytes());
            Console.WriteLine(keys.PublicKey.ToHexString());
            return;*/

            var scriptHash = "de1a53be359e8be9f3d11627bcca40548a2d5bc1".HexToBytes();

/*            {
                var script = NeoAPI.GenerateScript("de1a53be359e8be9f3d11627bcca40548a2d5bc1", "getMailCount", new object[] { "bambi@phantasma.io" });
                File.WriteAllBytes("output4.avm", script);

                var response = NeoAPI.TestInvokeScript(NeoAPI.Net.Test, script);

                Console.WriteLine(response.result);

                if (response.result is byte[])
                {
                    var bytes = (byte[])response.result;
                    var text = System.Text.Encoding.ASCII.GetString(bytes);
                    Console.WriteLine(bytes.Length > 0 ? text : "{Null}");
                }

                return;
            }       */

            //File.WriteAllBytes("output3.avm", HexToByte("1268656c6c6f407068616e7461736d612e696f21029ada24a94e753729768b90edee9d24ec9027cb64cea406f8ab296fce264597f452c10f72656769737465724d61696c626f7867c15b2d8a5440cabc2716d1f3e98b9e35be531ade"));

            //var s = System.Text.Encoding.ASCII.GetString("68656c6c6f407068616e7461736d612e696f".HexToBytes());
            //Console.WriteLine(s);

            {
                var keys = new KeyPair("f0dcac98c86241c165794f33d21b85d270e4efb2de9d86913ca980c1dea03217".HexToBytes());

                var pubKey = keys.CompressedPublicKey;
                //var pubKey = "029ada24a94e753729768b90edee9d24ec9027cb64cea406f8ab296fce264597f4".HexToBytes();
                //var pubKey = "0381adc5e205f0d4c86efa7a2f57032dbaed907098f377e343b864a57567de862e".HexToBytes();

                var operation = "getMailboxFromAddress";

                using (var sb = new ScriptBuilder())
                {
                    sb.EmitPush(pubKey);

                    var argCount = 1;
                    sb.Emit((OpCode)((int)OpCode.PUSHT + argCount - 1));

                    sb.Emit(OpCode.PACK);

                    sb.EmitPush(operation);

                    sb.EmitAppCall(scriptHash.Reverse().ToArray(), false);

                    var bytes = sb.ToArray();
                    File.WriteAllBytes("output2.avm", bytes);

                    var response = NeoAPI.TestInvokeScript(NeoAPI.Net.Test, bytes);
                    Console.WriteLine(response.result);

                    if (response.result is byte[])
                    {
                        bytes = (byte[])response.result;
                        var text = System.Text.Encoding.ASCII.GetString(bytes);
                        Console.WriteLine(bytes.Length > 0 ? text : "{Null}");
                    }

                }
                return;
            }

            {
                var balances = NeoAPI.GetBalance(NeoAPI.Net.Test, "AYpY8MKiJ9q5Fpt4EeQQmoYRHxdNHzwWHk");
                foreach (var entry in balances)
                {
                    Console.WriteLine(entry.Key + " => " + entry.Value);
                }
            }

            {
                //var pubKey = "029ada24a94e753729768b90edee9d24ec9027cb64cea406f8ab296fce264597f4".HexToBytes();
                var pubKey = "0381adc5e205f0d4c86efa7a2f57032dbaed907098f377e343b864a57567de862e".HexToBytes();

                //var privKey = "1480ac44ae8081f67242b9aaef8349f57d25e144d1ec1609a5f0eea4bc97d014".HexToBytes();
                var privKey = "fc1fa7c0d83426486373d9ce6eaca8adb506fc4ca25e89887c8eb5567f317a53".HexToBytes();

                var key = new KeyPair(privKey);
                var addr = key.address;
                Console.WriteLine(addr);
                var result = NeoAPI.CallContract(NeoAPI.Net.Test, key, "de1a53be359e8be9f3d11627bcca40548a2d5bc1", "registerMailbox", new object[] { pubKey, "demo@phantasma.io" });
                Console.WriteLine(result);
            }
            return;



            //var wallet = new Wallet(Wallet.Net.Main);
            //var balances = wallet.getBalance("Ac3z2CCmQBY6MmmnqsJJPxoDX6ydYiPD4w");
            /*var balances = wallet.getBalance("AemdeDMPATzTdcMwaxnfqdiUTuQQgD9SSX");
            


            foreach (var balance in balances)
            {
                Console.WriteLine(balance.Key + ": " + balance.Value);
            }*/

        }
    }
}
