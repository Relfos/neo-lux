using LunarParser;
using Neo.Cryptography;
using NeoLux.Core;
using System.Collections.Generic;

namespace NeoLux
{
    public class NeoRPC : NeoAPI
    {
        public readonly string apiEndpoint;

        public NeoRPC(string apiEndpoint)
        {
            this.apiEndpoint = apiEndpoint;
        }

        public static NeoRPC ForMainNet()
        {
            return new NeoRPC("http://api.wallet.cityofzion.io");
        }

        public static NeoRPC ForTestNet()
        {
            return new NeoRPC("http://testnet-api.wallet.cityofzion.io");
        }

        public override InvokeResult TestInvokeScript(byte[] script)
        {
            var invoke = new InvokeResult();
            invoke.state = null;

            var response = QueryRPC("invokescript", new object[] { script.ByteToHex() });
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

        public override bool SendRawTransaction(string hexTx)
        {
            var response = QueryRPC("sendrawtransaction", new object[] { hexTx });
            return response.GetBool("result");
        }

        public override byte[] GetStorage(string scriptHash, string key)
        {
            var result = QueryRPC("getstorage", new object[] { scriptHash, key });
            if (result == null)
            {
                return null;
            }

            var hex = result.GetString("result");
            return hex.HexToBytes();
        }

        public override Dictionary<string, decimal> GetBalance(string address, bool getTokens = false)
        {
            var url = apiEndpoint + "/v2/address/balance/" + address;
            var response = RequestUtils.Request(RequestType.GET, url);

            var result = new Dictionary<string, decimal>();
            foreach (var node in response.Children)
            {
                if (node.HasNode("balance"))
                {
                    var balance = node.GetDecimal("balance");
                    if (balance > 0)
                    {
                        result[node.Name] = balance;
                    }
                }
            }

            if (getTokens)
            {
                var info = GetTokenInfo();
                foreach (var symbol in info.Keys)
                {
                    var balance = GetTokenBalance(address, symbol);
                    if (balance > 0)
                    {
                        result[symbol] = balance;
                    }
                }
            }

            return result;
        }

        public override Dictionary<string, List<UnspentEntry>> GetUnspent(string address)
        {
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

        public DataNode QueryRPC(string method, object[] _params, int id = 1)
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

            DataNode response;
            
            response = RequestUtils.Request(RequestType.GET, apiEndpoint + "/v2/network/best_node");
            var rpcEndpoint = response.GetString("node");

            response = RequestUtils.Request(RequestType.POST, rpcEndpoint, jsonRpcData);

            return response;
        }
    }
}
