using NeoLux;
using Neo.Cryptography;
using System;
using System.Windows.Forms;

namespace neo_lux_light_wallet
{
    public partial class WalletForm : Form
    {
        private KeyPair keyPair;
        private NeoAPI api = NeoRPC.ForTestNet();


        public WalletForm()
        {
            InitializeComponent();

            dataGridView1.Columns.Add("Property", "Property");
            dataGridView1.Columns.Add("Value", "Value");

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.Columns[0].ReadOnly = true;
            dataGridView1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Columns[0].FillWeight = 3;

            dataGridView1.Columns[1].ReadOnly = true;
            dataGridView1.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Columns[1].FillWeight = 4;

            assetComboBox.Items.Clear();
            foreach (var symbol in NeoAPI.AssetSymbols)
            {
                assetComboBox.Items.Add(symbol);
            }
            assetComboBox.SelectedIndex = 0;

            fromAddressBox.ReadOnly = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            tabs.TabPages.Remove(balancePage);
            tabs.TabPages.Remove(transferPage);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var keyBytes = privateKeyInput.Text.HexToBytes();
            if (keyBytes.Length == 52)
            {
                keyPair = KeyPair.FromWIF(privateKeyInput.Text);
            }
            else
            if (keyBytes.Length == 32)
            {
                keyPair = new KeyPair(keyBytes);
            }
            else
            {
                MessageBox.Show("Invalid key input, must be 104 or 64 hexdecimal characters.");
                return;
            }


            tabs.TabPages.Add(balancePage);
            tabs.TabPages.Add(transferPage);
            tabs.TabPages.Remove(loginPage);

            dataGridView1.Rows.Clear();
            dataGridView1.Rows.Add("Address", keyPair.address);

            var balances = api.GetBalancesOf(keyPair, false);

            foreach (var symbol in NeoAPI.AssetSymbols)
            {
                var amount = balances.ContainsKey(symbol) ? balances[symbol] : 0;
                dataGridView1.Rows.Add(symbol, amount);
            }

            fromAddressBox.Text = keyPair.address;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(toAddressBox.Text))
            {
                MessageBox.Show("Please insert destination address");
                return;
            }

            var symbol = assetComboBox.SelectedItem.ToString();

            int amount = int.Parse(amountBox.Text);
            if (amount<=0)
            {
                MessageBox.Show("Please insert a valid amount of "+symbol);
                return;
            }

            api.SendAsset(keyPair, toAddressBox.Text, symbol, amount);
        }
    }
}
