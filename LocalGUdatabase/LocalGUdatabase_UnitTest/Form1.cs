using LocalGUdatabase;
using Newtonsoft.Json;
using System;
using System.Windows.Forms;
using WSD.Data.WUDAS;

namespace LocalGUdatabase_UnitTest
{
    public partial class Form1 : Form
    {
        private GUsqlite gudb = GUsqlite.Instance;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //gudb.GUwriter.GenerateNewGUattempt("AFEM-8010-AP1");
            //ReadWUDAS();
        }

        private void ReadWUDAS()
        {
            try
            {
                // Update region and username accordingly.
                string database = "zdb";
                string region = "sg";
                string username = "xs123456";

                string connectionString = $"database={database};region={region};username={username}";

                WudasClient wudasClient = new WudasClient(connectionString);

                Console.WriteLine("--- top table test ---");

                string sql = "SELECT * FROM Product";

                // returned download URL.

                string result = wudasClient.QueryData(sql);

                Console.WriteLine(result);

                Console.WriteLine("--- summary table test ---");

                sql = "SELECT * FROM topparameter WHERE productname = 'AFEM-8215-AP1-RF1' and lotid = 'PT1302108150'";

                // returned download URL.

                result = wudasClient.QueryData(sql);

                Console.WriteLine(result);

                Console.WriteLine("--- detail table test ---");

                sql = "SELECT dieclassname, waferid, X, Y, highlimit, lowlimit, BIN, 571_CAT11_I_VDDA_TERMINATION-ANT3D_I-PDD FROM waferdetail2 WHERE Filename in ('4GP2-2801T-BC_T979716.1-#1_T979716-01A0_AJKM_REV09-02_3_T979716.1-01_TJ_CB-2')";

                // returned download URL.

                result = wudasClient.QueryData(sql);

                Console.WriteLine(result);

                Console.WriteLine("--- detail table test ---");

                dynamic json = new { name = "waferdetail2", region = region, token = username, args = new { FileName = "4GP2-2801T-BC_T979716.1-#1_T979716-01A0_AJKM_REV09-02_3_T979716.1-01_TJ_CB-2", Parameters = "X,Y,highlimit,lowlimit,BIN,571_CAT11_I_VDDA_TERMINATION-ANT3D_I-PDD", Headers = "dieclassname,waferid" } };

                // returned download URL.

                result = wudasClient.QueryData(JsonConvert.SerializeObject(json));

                Console.WriteLine(result);
            }
            catch (Exception ex)

            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}