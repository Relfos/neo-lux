using System.Numerics;

namespace NeoLux
{
    public class NEP5
    {
        private readonly string contractHash;
        private readonly NeoAPI api;

        public NEP5(NeoAPI api, string contractHash)
        {
            this.api = api;
            this.contractHash = contractHash;
        }

        private string _name = null;
        public string Name
        {
            get
            {
                if (_name == null)
                {
                    var response = api.TestInvokeScript(contractHash, "name", new object[] { "" });
                    _name = System.Text.Encoding.ASCII.GetString((byte[])response.result);
                }

                return _name;
            }
        }

        private string _symbol = null;
        public string Symbol
        {
            get
            {
                if (_symbol == null)
                {
                    var response = api.TestInvokeScript(contractHash, "symbol", new object[] { "" });
                    _symbol = System.Text.Encoding.ASCII.GetString((byte[])response.result);
                }

                return _symbol;
            }
        }

        private BigInteger _decimals = -1;
        public BigInteger Decimals
        {
            get
            {
                if (_decimals < 0)
                {
                    var response = api.TestInvokeScript(contractHash, "decimals", new object[] { "" });
                    _decimals = (BigInteger)response.result;
                }

                return _decimals;
            }
        }

        private BigInteger _totalSupply = -1;
        public BigInteger TotalSupply
        {
            get
            {
                if (_totalSupply < 0)
                {
                    var response = api.TestInvokeScript(contractHash, "totalSupply", new object[] { "" });
                    _totalSupply = new BigInteger((byte[])response.result);

                    var decs = Decimals;
                    while (decs>0)
                    {
                        _totalSupply /= 10;
                        decs--;
                    }
                }

                return _totalSupply;
            }
        }

        public decimal BalanceOf(string address)
        {
            return BalanceOf(address.GetScriptHashFromAddress());
        }

        // FIXME - I'm almost sure that this code won't return non-integer balances correctly...
        public decimal BalanceOf(byte[] addressHash)
        {
            var response = api.TestInvokeScript(contractHash, "balanceOf", new object[] { addressHash });
            var balance = new BigInteger((byte[])response.result);
            var decs = this.Decimals;
            while (decs > 0)
            {
                balance /= 10;
                decs--;
            }
            return (decimal)balance;
        }

        public bool Transfer(KeyPair from_key, string to_address, decimal value)
        {
            return Transfer(from_key, to_address.GetScriptHashFromAddress(), value);
        }

        public bool Transfer(KeyPair from_key, byte[] to_address_hash, decimal value)
        {
            var decs = this.Decimals;
            while (decs > 0)
            {
                value *= 10;
                decs--;
            }

            BigInteger amount = new BigInteger((ulong)value);

            var sender_address_hash = from_key.address.GetScriptHashFromAddress();
            var response = api.CallContract(from_key, contractHash, "balanceOf", new object[] { sender_address_hash, to_address_hash, amount });
            return response;
        }
    }
}
