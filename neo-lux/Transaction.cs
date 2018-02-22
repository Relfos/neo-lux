using Neo.Cryptography;
using System;
using System.Text;

namespace NeoLux
{
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

        private static string SerializeWitness(Witness witness)
        {
            var invoLength = num2hexstring((witness.invocationScript.Length / 2));
            var veriLength = num2hexstring(witness.verificationScript.Length / 2);
            return invoLength + witness.invocationScript + veriLength + witness.verificationScript;
        }

        private static string SerializeTransactionInput(Input input)
        {
            return LuxUtils.reverseHex(input.prevHash) + LuxUtils.reverseHex(num2hexstring(input.prevIndex, 4));
        }

        private static string SerializeTransactionOutput(Output output)
        {
            var value = LuxUtils.num2fixed8(output.value);
            return LuxUtils.reverseHex(output.assetID) + value + LuxUtils.reverseHex(output.scriptHash);
        }

        public string SerializeTransaction(bool signed = true)
        {
            var tx = this;
            var result = new StringBuilder();
            result.Append(num2hexstring(tx.type));
            result.Append(num2hexstring(tx.version));

            // excluusive data
            if (tx.type == 0xd1)
            {
                result.Append(num2VarInt(tx.script.Length));
                result.Append(tx.script.ToHexString());
                if (tx.version >= 1)
                {
                    result.Append(LuxUtils.num2fixed8(tx.gas));
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

        public Transaction Sign(KeyPair key)
        {
            var tx = this;
            var txdata = tx.SerializeTransaction(false);
            var txstr = txdata.HexToBytes();

            var privkey = key.PrivateKey;
            var pubkey = key.PublicKey;
            var signature = Crypto.Default.Sign(txstr, privkey, pubkey);

            var invocationScript = "40" + signature.ByteToHex();
            var verificationScript = key.signatureScript;
            tx.witnesses = new Transaction.Witness[] { new Transaction.Witness() { invocationScript = invocationScript, verificationScript = verificationScript } };

            return tx;
        }

    }

}
