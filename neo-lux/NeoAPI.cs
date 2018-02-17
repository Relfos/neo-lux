using LunarParser;
using NeoLux.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Neo.Cryptography;
using Neo.VM;
using Neo.Emulator.Utils;
using System.Numerics;

namespace NeoLux
{
    public class NeoException : Exception
    {
        public NeoException(string msg) : base (msg)
        {

        }
    }

    public struct InvokeResult
    {
        public string state;
        public decimal gasSpent;
        public object result;
    }

    public abstract class NeoAPI
    {
        // hard-code asset ids for NEO and GAS
        private const string neoId = "c56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b";
        private const string gasId = "602c79718b16e442de58778e148d0b1084e3b2dffd5de6b7b16cee7969282de7";

        public struct TokenInfo
        {
            public string scriptHash;
            public int decimals;
        }

        private static Dictionary<string, TokenInfo> _tokens = null;

        internal static Dictionary<string, TokenInfo> GetTokenInfo()
        {
            if (_tokens == null)
            {
                _tokens = new Dictionary<string, TokenInfo>();
                AddToken("RPX", "ecc6b20d3ccac1ee9ef109af5a7cdb85706b1df9", 8);
                AddToken("DBC", "b951ecbbc5fe37a9c280a76cb0ce0014827294cf", 8);
                AddToken("QLC", "0d821bd7b6d53f5c2b40e217c6defc8bbe896cf5", 8);
                AddToken("APH", "a0777c3ce2b169d4a23bcba4565e3225a0122d95", 8);
            }

            return _tokens;
        }

        private static void AddToken(string symbol, string hash, int decimals)
        {
            _tokens[symbol] = new TokenInfo() { scriptHash = hash, decimals = decimals };
        }

        protected static object ParseStack(DataNode stack)
        {
            if (stack != null)
            {
                var item = stack.Children.FirstOrDefault();
                if (item != null)
                {
                    var type = item.GetString("type");
                    var val = item.GetString("value");

                    switch (type)
                    {
                        case "ByteArray":
                            {
                                return val.HexToBytes();
                            }

                        case "Integer":
                            {
                                BigInteger intVal;
                                BigInteger.TryParse(val, out intVal);
                                return intVal;
                            }

                    }

                    return val;
                }
            }

            return null;
        }

        public abstract InvokeResult TestInvokeScript(byte[] script);

        public InvokeResult TestInvokeScript(string scriptHash, string operation, object[] args)
        {
            var bytes = GenerateScript(scriptHash, operation, args);
            return TestInvokeScript(bytes);
        }

        public static byte[] GenerateScript(string scriptHash, string operation, object[] args)
        {
            using (var sb = new ScriptBuilder())
            {
                for (int i = args.Length - 1; i >= 0; i--)
                {
                    sb.EmitPush(args[i]);
                }

                var argCount = args.Length;
                sb.Emit((OpCode)((int)OpCode.PUSHT + argCount - 1));

                sb.Emit(OpCode.PACK);

                sb.EmitPush(operation);

                var temp = scriptHash.HexToBytes().Reverse().ToArray();
                sb.EmitAppCall(temp, false);

                var bytes = sb.ToArray();
                return bytes;
            }
        }

        public bool CallContract(KeyPair key, string scriptHash, string operation, object[] args)
        {
            var bytes = GenerateScript(scriptHash, operation, args);
            return CallContract(key, scriptHash, bytes);
        }

        private void GenerateInputsOutputs(KeyPair key, string outputHash, Dictionary<string, decimal> ammounts, out List<Transaction.Input> inputs, out List<Transaction.Output> outputs)
        {
            if (ammounts == null || ammounts.Count == 0)
            {
                throw new NeoException("Invalid amount list");
            }

            //var outputHash = toAddress.GetScriptHashFromAddress().ToHexString();
            
            var unspent = GetUnspent(key.address);

            inputs = new List<Transaction.Input>();
            outputs = new List<Transaction.Output>();

            foreach (var entry in ammounts)
            {
                var symbol = entry.Key;

                if (!unspent.ContainsKey(symbol))
                {
                    throw new NeoException($"Not enough {symbol} in address {key.address}");
                }
            }

            foreach (var entry in ammounts)
            {
                var symbol = entry.Key;
                var cost = entry.Value;

                string assetID;

                switch (symbol)
                {
                    case "GAS": assetID = gasId; break;
                    case "NEO": assetID = neoId; break;
                    default:
                        {
                            throw new NeoException($"{symbol} is not a valid blockchain asset.");
                        }
                }

                var sources = unspent[symbol];

                decimal selected = 0;
                foreach (var src in sources)
                {
                    selected += src.value;

                    var input = new Transaction.Input()
                    {
                        prevHash = src.txid,
                        prevIndex = src.index,
                    };

                    inputs.Add(input);

                    if (selected >= cost)
                    {
                        break;
                    }
                }

                if (selected < cost)
                {
                    throw new NeoException($"Not enough {symbol}");
                }

                var output = new Transaction.Output()
                {
                    assetID = assetID,
                    scriptHash = outputHash,
                    value = cost
                };
                outputs.Add(output);

                if (selected > cost)
                {
                    var left = selected - cost;

                    var change = new Transaction.Output()
                    {
                        assetID = assetID,
                        scriptHash = LuxUtils.reverseHex(key.signatureHash.ToArray().ByteToHex()),
                        value = left
                    };
                    outputs.Add(change);
                }
            }
        }

        public bool CallContract(KeyPair key, string scriptHash, byte[] bytes)
        {
            /*var invoke = TestInvokeScript(net, bytes);
            if (invoke.state == null)
            {
                throw new Exception("Invalid script invoke");
            }

            decimal gasCost = invoke.gasSpent;*/


            List<Transaction.Input> inputs;
            List<Transaction.Output> outputs;
            var gasCost = 0;

            GenerateInputsOutputs(key, scriptHash, new Dictionary<string, decimal>() { { "GAS", gasCost } }, out inputs, out outputs);

            Transaction tx = new Transaction()
            {
                type = 0xd1,
                version = 0,
                script = bytes,
                gas = gasCost,
                inputs = inputs.ToArray(),
                outputs = outputs.ToArray()
            };

            //File.WriteAllBytes("output2.avm", bytes);

            tx = tx.Sign(key);

            var hexTx = tx.SerializeTransaction(true);

            return SendRawTransaction(hexTx);
        }

        public abstract bool SendRawTransaction(string hexTx);

        public abstract byte[] GetStorage(string scriptHash, string key);

        public bool SendAsset(string toAddress, string symbol, decimal amount, KeyPair key)
        {
            return SendAsset(toAddress, new Dictionary<string, decimal>() { {symbol, amount }}, key);
        }

        public bool SendAsset(string toAddress, Dictionary<string, decimal> amounts, KeyPair key)
        {
            List<Transaction.Input> inputs;
            List<Transaction.Output> outputs;

            var scriptHash = LuxUtils.reverseHex(toAddress.GetScriptHashFromAddress().ToHexString());
            GenerateInputsOutputs(key, scriptHash, amounts, out inputs, out outputs);

            Transaction tx = new Transaction()
            {
                type = 128,
                version = 0,
                script = null,
                gas = -1,
                inputs = inputs.ToArray(),
                outputs = outputs.ToArray()
            };
            
            tx = tx.Sign(key);

            var hexTx = tx.SerializeTransaction(true);

            return SendRawTransaction(hexTx);

            /*
            var account = getAccountFromWIFKey(fromWif);
            var toScriptHash = getScriptHashFromAddress(toAddress);
            var balances = getBalance(net, account.address);
            // TODO: maybe have transactions handle this construction?
            var intents = _.map(assetAmounts, (v, k) =>
            {
                return { assetId: tx.ASSETS[k], value: v, scriptHash: toScriptHash }
            });
            var unsignedTx = tx.create.contract(account.publicKeyEncoded, balances, intents);
            var signedTx = tx.signTransaction(unsignedTx, account.privateKey);
            var hexTx = tx.serializeTransaction(signedTx);
            return queryRPC(net, "sendrawtransaction", new object[] { hexTx }, 4);*/
        }

        /**
         * Get balances of NEO and GAS for an address
         * @param {string} net - 'MainNet' or 'TestNet'.
         * @param {string} address - Address to check.
         * @return {Promise<Balance>} Balance of address
         */
        public abstract Dictionary<string, decimal> GetBalance(string address, bool getTokens = false);

        // FIXME - I'm almost sure that this code won't return non-integer balances correctly...
        public decimal GetTokenBalance(string address, string symbol)
        {
            var info = GetTokenInfo();
            if (info.ContainsKey(symbol))
            {
                var token = info[symbol];
                var response = TestInvokeScript(token.scriptHash, "balanceOf", new object[] { address.GetScriptHashFromAddress() });
                var balance = new BigInteger((byte[])response.result);
                var decimals = token.decimals;
                while (decimals > 0)
                {
                    balance /= 10;
                    decimals--;
                }
                return (decimal) balance;
            }

            throw new NeoException("Invalid token symbol");
        }

        public struct UnspentEntry
        {
            public string txid;
            public uint index;
            public decimal value;
        }

        public abstract Dictionary<string, List<UnspentEntry>> GetUnspent(string address);

        /*public static decimal getClaimAmounts(Net net, string address)
        {
            var apiEndpoint = getAPIEndpoint(net);
            var response = RequestUtils.Request(RequestType.GET, apiEndpoint + "/v2/address/claims/" + address);
            return response.GetDecimal("total_claim");
            return (available: parseInt(res.data.total_claim), unavailable: parseInt(res.data.total_unspent_claim));
        }
        */

        /*public static DataNode getTransactionHistory(Net net, string address)
        {
            var apiEndpoint = getAPIEndpoint(net);
            var response = RequestUtils.Request(RequestType.GET, apiEndpoint + "/v2/address/history/" + address);
            return response["history"];
        }*/

        /*public static int getWalletDBHeight(Net net)
        {
            var apiEndpoint = getAPIEndpoint(net);
            var response = RequestUtils.Request(RequestType.GET, apiEndpoint + "/v2/block/height");
            return response.GetInt32("block_height");
        }*/


    }

}
