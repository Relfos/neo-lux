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

    public static class NeoAPI
    {
        public enum Net
        {
            Main,
            Test
        }

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

        // hard-code asset ids for NEO and GAS
        public const string neoId = "c56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b";
        public const string gasId = "602c79718b16e442de58778e148d0b1084e3b2dffd5de6b7b16cee7969282de7";

        public struct InvokeResult
        {
            public string state;
            public decimal gasSpent;
            public object result;
        }

        private static object ParseStack(DataNode stack)
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

        /**
         * Sends an invokescript RPC request and returns a parsed result.
         * @param {string} net - 'MainNet' or 'TestNet' or custom URL
         * @param {string} script - script to run on VM.
         * @param {boolean} parse - If method should parse response
         * @return {{state: string, gas_consumed: string|number, stack: []}} VM Response.
         */
        public static InvokeResult TestInvokeScript(Net net, byte[] script)
        {
            var invoke = new InvokeResult();
            invoke.state = null;

            var response = QueryRPC(net, "invokescript", new object[] { script.ByteToHex() });
            if (response != null)
            {
                var root = response["result"];
                if (root != null)
                {
                    var stack = root["stack"];
                    invoke.result = ParseStack(stack);

                    invoke.gasSpent = root.GetDecimal("gas_consumed");
                    invoke.state = root.GetString("state");
                }
            }

            return invoke;
        }

        public static InvokeResult TestInvokeScript(Net net, string scriptHash, string operation, object[] args)
        {
            var bytes = GenerateScript(scriptHash, operation, args);
            return TestInvokeScript(net, bytes);
        }

        private static Transaction Sign(this Transaction transaction, KeyPair key)
        {
            var txdata = SerializeTransaction(transaction, false);
            var txstr = txdata.HexToBytes();

            var privkey = key.PrivateKey;
            var pubkey = key.PublicKey;
            var signature = Crypto.Default.Sign(txstr, privkey, pubkey);

            var invocationScript = "40" + signature.ByteToHex();
            var verificationScript = key.signatureScript;
            transaction.witnesses = new Transaction.Witness[] { new Transaction.Witness() { invocationScript = invocationScript, verificationScript = verificationScript } };

            return transaction;
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

        public static bool CallContract(Net net, KeyPair key, string scriptHash, string operation, object[] args)
        {
            var bytes = GenerateScript(scriptHash, operation, args);
            return CallContract(net, key, scriptHash, bytes);
        }

        private static void GenerateInputsOutputs(Net net, KeyPair key, string outputHash, Dictionary<string, decimal> ammounts, out List<Transaction.Input> inputs, out List<Transaction.Output> outputs)
        {
            if (ammounts == null || ammounts.Count == 0)
            {
                throw new NeoException("Invalid amount list");
            }

            //var outputHash = toAddress.GetScriptHashFromAddress().ToHexString();
            
            var unspent = GetUnspent(net, key.address);

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
                        scriptHash = reverseHex(key.signatureHash.ToArray().ByteToHex()),
                        value = left
                    };
                    outputs.Add(change);
                }
            }
        }

        public static bool CallContract(Net net, KeyPair key, string scriptHash, byte[] bytes)
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

            GenerateInputsOutputs(net, key, scriptHash, new Dictionary<string, decimal>() { { "GAS", gasCost } }, out inputs, out outputs);

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

            var hexTx = SerializeTransaction(tx, true);

            var response = QueryRPC(net, "sendrawtransaction", new object[] { hexTx });

            return response.GetBool("result");
        }

        #region NEEDS CLEANUP
        public struct Transaction
        {
            public struct Witness
            {
                public string invocationScript;
                public string verificationScript;
            }

            public struct Input
            {
                public string prevHash;
                public uint prevIndex;
            }

            public struct Output
            {
                public string scriptHash;
                public string assetID;
                public decimal value;
            }

            public byte type;
            public byte version;
            public byte[] script;
            public decimal gas;

            public Input[] inputs;
            public Output[] outputs;
            public Witness[] witnesses;
        }

        private static string num2hexstring(long num, int size = 2)
        {
            return num.ToString("X" + size);
        }

        private static string num2VarInt(long num)
        {
            if (num < 0xfd)
            {
                return num2hexstring(num);
            }

            if (num <= 0xffff)
            {
                return "fd" + num2hexstring(num, 4);
            }

            if (num <= 0xffffffff)
            {
                return "fe" + num2hexstring(num, 8);
            }

            return "ff" + num2hexstring(num, 8) + num2hexstring(num / (int)Math.Pow(2, 32), 8);
        }

        private static string reverseHex(string hex)
        {

            string result = "";
            for (var i = hex.Length - 2; i >= 0; i -= 2)
            {
                result += hex.Substring(i, 2);
            }
            return result;
        }

        private static string num2fixed8(decimal num)
        {
            long val = (long)Math.Round(num * 100000000);
            var hexValue = val.ToString("X16");
            return reverseHex(("0000000000000000" + hexValue).Substring(hexValue.Length));
        }

        private static string SerializeWitness(Transaction.Witness witness)
        {
            var invoLength = num2hexstring((witness.invocationScript.Length / 2));
            var veriLength = num2hexstring(witness.verificationScript.Length / 2);
            return invoLength + witness.invocationScript + veriLength + witness.verificationScript;
        }

        private static string SerializeTransactionInput(Transaction.Input input)
        {
            return reverseHex(input.prevHash) + reverseHex(num2hexstring(input.prevIndex, 4));
        }

        private static string SerializeTransactionOutput(Transaction.Output output)
        {
            var value = num2fixed8(output.value);
            return reverseHex(output.assetID) + value + reverseHex(output.scriptHash);
        }

        private static string SerializeTransaction(Transaction tx, bool signed = true)
        {
            var result = new StringBuilder();
            result.Append(num2hexstring(tx.type));
            result.Append(num2hexstring(tx.version));

            // excluusive data
            if (tx.version == 0xd1)
            {
                result.Append(num2VarInt(tx.script.Length));
                result.Append(tx.script.ToHexString());
                if (tx.version >= 1)
                {
                    result.Append(num2fixed8(tx.gas));
                }
            }

            // Don't need any attributes
            result.Append("00");

            result.Append(num2VarInt(tx.inputs.Length));
            foreach (var input in tx.inputs)
            {
                result.Append(SerializeTransactionInput(input));
            }

            result.Append(num2VarInt(tx.outputs.Length));
            foreach (var output in tx.outputs)
            {
                result.Append(SerializeTransactionOutput(output));
            }


            if (signed && tx.witnesses != null && tx.witnesses.Length > 0)
            {
                result.Append(num2VarInt(tx.witnesses.Length));
                foreach (var script in tx.witnesses)
                {
                    result.Append(SerializeWitness(script));
                }
            }

            return result.ToString().ToLowerInvariant();
        }

        #endregion


        /**
         * Lookup key in SC storage
         * @param {string} net - 'MainNet' or 'TestNet'.
         * @param {string} scriptHash of SC
         * @return {Promise<Response>} RPC response looking up key from storage
         */
        public static DataNode getStorage(Net net, string scriptHash, string key)
        {
            return QueryRPC(net, "getstorage", new object[] { scriptHash, key });
        }

        public static bool SendAsset(Net net, string toAddress, string symbol, decimal amount, KeyPair key)
        {
            return SendAsset(net, toAddress, new Dictionary<string, decimal>() { {symbol, amount }}, key);
        }

        public static bool SendAsset(Net net, string toAddress, Dictionary<string, decimal> amounts, KeyPair key)
        {
            List<Transaction.Input> inputs;
            List<Transaction.Output> outputs;

            var scriptHash = reverseHex(toAddress.GetScriptHashFromAddress().ToHexString());
            GenerateInputsOutputs(net, key, scriptHash, amounts, out inputs, out outputs);

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

            var hexTx = SerializeTransaction(tx, true);

            var response = QueryRPC(net, "sendrawtransaction", new object[] { hexTx });

            return response.GetBool("result");

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
         * API Switch for MainNet and TestNet
         * @param {string} net - 'MainNet' or 'TestNet'.
         * @return {string} URL of API endpoint.
         */
        private static string getAPIEndpoint(Net net)
        {
            if (net == Net.Main)
            {
                return "http://api.wallet.cityofzion.io";
            }
            else
            {
                return "http://testnet-api.wallet.cityofzion.io";
            }
        }

        /**
         * Get balances of NEO and GAS for an address
         * @param {string} net - 'MainNet' or 'TestNet'.
         * @param {string} address - Address to check.
         * @return {Promise<Balance>} Balance of address
         */
        public static Dictionary<string, decimal> GetBalance(Net net, string address, bool getTokens = false)
        {
            var apiEndpoint = getAPIEndpoint(net);
            var url = apiEndpoint + "/v2/address/balance/" + address;
            var response = RequestUtils.Request(RequestType.GET, url);

            var result = new Dictionary<string, decimal>();
            foreach (var node in response.Children)
            {
                if (node.HasNode("balance"))
                {
                    var balance = node.GetDecimal("balance");
                    if (balance>0)
                    {
                        result[node.Name] = balance;
                    }
                }
            }

            if (net == Net.Main && getTokens)
            {
                var info = GetTokenInfo();
                foreach (var symbol in info.Keys)
                {
                    var balance = GetTokenBalance(address, symbol);
                    if (balance>0)
                    {
                        result[symbol] = balance;
                    }
                }
            }

            return result;
        }

        // FIXME - I'm almost sure that this code won't return non-integer balances correctly...
        public static decimal GetTokenBalance(string address, string symbol)
        {
            var info = GetTokenInfo();
            if (info.ContainsKey(symbol))
            {
                var token = info[symbol];
                var response = NeoAPI.TestInvokeScript(NeoAPI.Net.Main, token.scriptHash, "balanceOf", new object[] { address.GetScriptHashFromAddress() });
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

        internal static Dictionary<string, List<UnspentEntry>> GetUnspent(Net net, string address)
        {
            var apiEndpoint = getAPIEndpoint(net);
            var url = apiEndpoint + "/v2/address/balance/" + address;
            var response = RequestUtils.Request(RequestType.GET, url);

            var result = new Dictionary<string, List<UnspentEntry>>();
            foreach (var node in response.Children)
            {
                var child = node.GetNode("unspent");
                if (child != null)
                {
                    List<UnspentEntry> list;
                    if (result.ContainsKey(node.Name))
                    {
                        list = result[node.Name];
                    }
                    else
                    {
                        list = new List<UnspentEntry>();
                        result[node.Name] = list;
                    }

                    foreach (var data in child.Children)
                    {
                        var input = new UnspentEntry()
                        {
                            txid = data.GetString("txid"),
                            index = data.GetUInt32("index"),
                            value = data.GetDecimal("value")
                        };

                        list.Add(input);
                    }
                }
            }
            return result;
        }

        /**
         * Get amounts of available (spent) and unavailable claims
         * @param {string} net - 'MainNet' or 'TestNet'.
         * @param {string} address - Address to check.
         * @return {Promise<{available: number, unavailable: number}>} An Object with available and unavailable GAS amounts.
         */
        public static decimal getClaimAmounts(Net net, string address)
        {
            var apiEndpoint = getAPIEndpoint(net);
            var response = RequestUtils.Request(RequestType.GET, apiEndpoint + "/v2/address/claims/" + address);
            return response.GetDecimal("total_claim");
            //return (available: parseInt(res.data.total_claim), unavailable: parseInt(res.data.total_unspent_claim));
        }


        /**
         * Returns the best performing (highest block + fastest) node RPC
         * @param {string} net - 'MainNet' or 'TestNet' or a custom URL.
         * @return {Promise<string>} The URL of the best performing node or the custom URL provided.
         */
        public static string getRPCEndpoint(Net net)
        {
            var apiEndpoint = getAPIEndpoint(net);
            var response = RequestUtils.Request(RequestType.GET, apiEndpoint + "/v2/network/best_node");
            return response.GetString("node");
        }


        /**
         * Get transaction history for an account
         * @param {string} net - 'MainNet' or 'TestNet'.
         * @param {string} address - Address to check.
         * @return {Promise<History>} History
         */
        public static DataNode getTransactionHistory(Net net, string address)
        {
            var apiEndpoint = getAPIEndpoint(net);
            var response = RequestUtils.Request(RequestType.GET, apiEndpoint + "/v2/address/history/" + address);
            return response["history"];
        }

        /**
         * Get the current height of the light wallet DB
         * @param {string} net - 'MainNet' or 'TestNet'.
         * @return {Promise<number>} Current height.
         */
        public static int getWalletDBHeight(Net net)
        {
            var apiEndpoint = getAPIEndpoint(net);
            var response = RequestUtils.Request(RequestType.GET, apiEndpoint + "/v2/block/height");
            return response.GetInt32("block_height");
        }

        /**
         * Wrapper for querying node RPC
         * @param {string} net - 'MainNet' or 'TestNet'.
         * @param {string} method - RPC Method name.
         * @param {Array} params - Array of parameters to send.
         * @param {number} id - Unique id to identity yourself. RPC should reply with same id.
         * @returns {Promise<Response>} RPC Response
         */
        public static DataNode QueryRPC(Net net, string method, object[] _params, int id = 1)
        {
            var paramData = DataNode.CreateArray("params");
            foreach (var entry in _params)
            {
                paramData.AddField(null, entry);
            }

            var jsonRpcData = DataNode.CreateObject(null);
            jsonRpcData.AddField("method", method);
            jsonRpcData.AddNode(paramData);
            jsonRpcData.AddField("id", id);
            jsonRpcData.AddField("jsonrpc", "2.0");

            var rpcEndpoint = getRPCEndpoint(net);
            var response = RequestUtils.Request(RequestType.POST, rpcEndpoint, jsonRpcData);

            return response;
        }

    }

}
