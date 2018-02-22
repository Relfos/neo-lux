using System.Collections.Generic;

namespace NeoLux
{
    public class NeoEmulator : NeoAPI
    {
        public override Dictionary<string, decimal> GetBalancesOf(string address, bool getTokens = false)
        {
            throw new System.NotImplementedException();
        }

        public override byte[] GetStorage(string scriptHash, string key)
        {
            throw new System.NotImplementedException();
        }

        public override Dictionary<string, List<UnspentEntry>> GetUnspent(string address)
        {
            throw new System.NotImplementedException();
        }

        public override bool SendRawTransaction(string hexTx)
        {
            throw new System.NotImplementedException();
        }

        public override InvokeResult TestInvokeScript(byte[] script)
        {
            throw new System.NotImplementedException();
        }
    }
}
