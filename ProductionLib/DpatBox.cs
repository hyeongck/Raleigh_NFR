using System;
using System.Windows.Forms;
using Passkey_NFR;

namespace ProductionLib2
{
    public partial class DpatBox : Form
    {
        public DpatBox()
        {
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            InitializeComponent();
        }

        //private string Password = "pinot";
        public static string Password = "pinot";

        public static int PasskeyT = 18091977;

        public static DateTime date1 = DateTime.Now;

        public static int Date2 = Convert.ToInt32(date1.ToString("yyyyMMdd"));

        public static string Date3 = date1.ToString("yyyy");

        public static int Date4 = Convert.ToInt32(date1.ToString("MM"));

        public static int Date5 = Convert.ToInt32(date1.ToString("dd"));

        //Ok
        private void button1_Click(object sender, EventArgs e)
        {
            int num1 = Convert.ToInt32(Date3.Substring(3)) + Date4 + Date5;
            string num2 = Convert.ToString(num1);
            string animal = "dragon";

            Passkey_NFR.Passkey_NFR Passkeys = new Passkey_NFR.Passkey_NFR();

            if (num2.Length > 1)
            {
                num2 = num2.Substring(1);
            }

            if (num2 == "0")
            {
                animal = "dog";
                PasskeyT = Passkeys.PasskeyT1;

            }
            else if (num2 == "1")
            {
                animal = "snake";
                PasskeyT = Passkeys.PasskeyT2;
            }
            else if (num2 == "2")
            {
                animal = "cat";
                PasskeyT = Passkeys.PasskeyT3;
            }
            else if (num2 == "3")
            {
                animal = "pig";
                PasskeyT = Passkeys.PasskeyT4;
            }
            else if (num2 == "4")
            {
                animal = "rabbit";
                PasskeyT = Passkeys.PasskeyT5;
            }
            else if (num2 == "5")
            {
                animal = "horse";
                PasskeyT = Passkeys.PasskeyT6;
            }
            else if (num2 == "6")
            {
                animal = "rat";
                PasskeyT = Passkeys.PasskeyT7;
            }
            else if (num2 == "7")
            {
                animal = "cow";
                PasskeyT = Passkeys.PasskeyT8;
            }
            else if (num2 == "8")
            {
                animal = "goat";
                PasskeyT = Passkeys.PasskeyT9;
            }
            else if (num2 == "9")
            {
                animal = "roaster";
                PasskeyT = Passkeys.PasskeyT10;
            }
            else
            {
                animal = "avenger";
                PasskeyT = 18091977;
            }

            Password = animal + "@" + (Date2 + PasskeyT);

            if (textBox1.Text == Password)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                this.DialogResult = DialogResult.Retry;
                MessageBox.Show("Password incorrect, please try again or press cancel to unload program...");
            }
        }

        //Cancel
        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}