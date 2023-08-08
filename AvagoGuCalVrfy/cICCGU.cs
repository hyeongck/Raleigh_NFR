using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace AvagoGU
{
    public static class cDatabase
    {
        static string Database_Path = @"C:\Avago.ATF.Common.x64\Database\";
        static string Database_Name = "tblCal.xml";
        static DataSet DS = new DataSet();
        static bool ErrorFlag = false;
        public static string Set_DB_Path
        {
            set
            {
                Database_Path = value.TrimEnd('\\') + "\\";
            }
        }
        public static string Set_DB_Filename
        {
            set
            {
                Database_Name = value.Trim();
            }
        }
        public static void Load_Database()
        {
            try
            {
                if (!System.IO.File.Exists(Database_Path + Database_Name))
                {
                    System.IO.File.WriteAllText(Database_Path + Database_Name, AvagoGU.Properties.Resources.tblCal);
                }

                DS = new DataSet();
                DS.ReadXml(Database_Path + Database_Name, XmlReadMode.ReadSchema);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show("Error opening Database File : " + Database_Path + Database_Name + "\n\n" + e.ToString(), "Database Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                ErrorFlag = true;
            }
        }
        public static void SaveDB()
        {
            DS.WriteXml(Database_Path + Database_Name, XmlWriteMode.WriteSchema);
        }

        private static int Find_RowNumber(string Device_Name)
        {
            int Found = 0;
            for (int i = 0; i < DS.Tables[0].Rows.Count; i++)
            {
                if ((DS.Tables[0].Rows[i]["ProductID"]).ToString().ToUpper() == Device_Name.Trim().ToUpper())
                {
                    Found = i;
                    break;
                }
            }
            return Found;
        }
        public static bool Get_VrfyBypassAllowed(string Device_Name)
        {
            DataRow[] result = DS.Tables[0].Select("ProductID = '" + Device_Name + "'");
            double Interval = Convert.ToDouble(result[0]["CalInterval"]);
            DateTime DT = Convert.ToDateTime(result[0]["DateCal"]).AddDays(Interval);
            if (DateTime.Now > DT || !Get_Vrfy_PassStatus(Device_Name) || !Get_Corr_PassStatus(Device_Name) || !Get_ICC_PassStatus(Device_Name))
            {
                return false;
            }
            return true;

        }
        public static int Get_Corr_MaxAllowableTries(string Device_Name)
        {
            DataRow[] result = DS.Tables[0].Select("ProductID = '" + Device_Name + "'");
            int val = Convert.ToInt16(result[0]["ValidGU"]);
            return val;
        }
        public static int Get_Vrfy_MaxAllowableTries(string Device_Name)
        {
            DataRow[] result = DS.Tables[0].Select("ProductID = '" + Device_Name + "'");
            int val = Convert.ToInt16(result[0]["ValidGUVerify"]);
            return val;
        }

        public static bool Check_Device_Exist(string Device_Name)
        {
            DataRow[] result = DS.Tables[0].Select("ProductID = '" + Device_Name + "'");
            if (result.Length == 1)
            {
                return true;
            }

            return false;  
        }
        public static void CreateNewDevice(string Device_Name)
        {
            DataRow dat;
            dat = DS.Tables[0].NewRow();

            dat["ID"] = DS.Tables[0].Rows.Count;
            dat["ProductID"] = Device_Name;
            dat["DateCal"] = DateTime.Now.ToString();
            dat["CalInterval"] = 1.25;
            dat["ICC"] = true;
            dat["GU"] = false;
            dat["ValidGU"] = 5;
            dat["TryGU"] = 0;
            dat["GUVerify"] = false;
            dat["ValidGUVerify"] = 5;
            dat["TryGUVerify"] = 0;

            DS.Tables[0].Rows.Add(dat);
            SaveDB();
        }
        
        public static bool Get_ICC_PassStatus(string Device_Name)
        {
            DataRow[] result = DS.Tables[0].Select("ProductID = '" + Device_Name + "'");
            return Convert.ToBoolean(result[0]["ICC"]);
            //return true;
        }
        public static void Set_ICC_PassStatus(string Device_Name, bool Status)
        {
            int RowNumber = Find_RowNumber(Device_Name);

            DS.Tables[0].Rows[RowNumber]["ICC"] = Status.ToString();
            SaveDB();
        }
        
        public static bool Get_Corr_PassStatus(string Device_Name)
        {
            DataRow[] result = DS.Tables[0].Select("ProductID = '" + Device_Name + "'");
            return Convert.ToBoolean(result[0]["GU"]);
        }
        public static void Set_Corr_PassStatus(string Device_Name, bool Status)
        {
            int RowNumber = Find_RowNumber(Device_Name);

            DS.Tables[0].Rows[RowNumber]["GU"] = Status.ToString();
            SaveDB();
        }

        public static bool Get_Vrfy_PassStatus(string Device_Name)
        {
            DataRow[] result = DS.Tables[0].Select("ProductID = '" + Device_Name + "'");
            return Convert.ToBoolean(result[0]["GUVerify"]);
        }
        public static void Set_Vrfy_PassStatus(string Device_Name, bool Status)
        {
            int RowNumber = Find_RowNumber(Device_Name);

            DS.Tables[0].Rows[RowNumber]["GUVerify"] = Status.ToString();
            Set_Vrfy_Date(Device_Name);
            SaveDB();
        }

        public static DateTime Get_Vrfy_Date(string Device_Name)
        {
            DataRow[] result = DS.Tables[0].Select("ProductID = '" + Device_Name + "'");
            DateTime DT = Convert.ToDateTime(result[0]["DateCal"]);
                return DT;
        }
        public static void Set_Vrfy_Date(string Device_Name)
        {
            int RowNumber = Find_RowNumber(Device_Name);

            DS.Tables[0].Rows[RowNumber]["DateCal"] = DateTime.Now;
            SaveDB();
        }
        
        public static int Get_Corr_NumTries(string Device_Name)
        {
            DataRow[] result = DS.Tables[0].Select("ProductID = '" + Device_Name + "'");
            int val = Convert.ToInt16(result[0]["TryGU"]);
            return val;
        }
        public static void Set_Corr_NumTries(string Device_Name, int tryval)
        {
            int RowNumber = Find_RowNumber(Device_Name);
            DS.Tables[0].Rows[RowNumber]["TryGU"] = tryval;
            SaveDB();
        }

        public static int Get_Vrfy_NumTries(string Device_Name)
        {
            DataRow[] result = DS.Tables[0].Select("ProductID = '" + Device_Name + "'");
            int val = Convert.ToInt16(result[0]["TryGUVerify"]);
            return val;
        }
        public static void Set_Vrfy_NumTries(string Device_Name, int tryval)
        {
            int RowNumber = Find_RowNumber(Device_Name);
            DS.Tables[0].Rows[RowNumber]["TryGUVerify"] = tryval;
            SaveDB();
        }
        
        public static void Update_Database(string Device_Name, bool IccPassed, bool CorrPassed, bool VrfyPassed)
        {
            if (VrfyPassed & CorrPassed & IccPassed)
            {
                Set_Vrfy_PassStatus(Device_Name, true);
                Set_Vrfy_NumTries(Device_Name, 0);

                Set_Corr_PassStatus(Device_Name, true);
                Set_Corr_NumTries(Device_Name, 0);

                Set_ICC_PassStatus(Device_Name, GU.previousIccCalFactorsExist);     // force to rerun if there was no original Icc Cal file, so that IccServoTargetCorrection gets set correctly
            }
            else
            {
                Set_Vrfy_PassStatus(Device_Name, false);
                int numVrfyTries = Get_Vrfy_NumTries(Device_Name);
                if (!VrfyPassed) Set_Vrfy_NumTries(Device_Name, ++numVrfyTries);

                if (CorrPassed & IccPassed)
                {
                    Set_Corr_NumTries(Device_Name, 0);
                    Set_Corr_PassStatus(Device_Name, numVrfyTries <= Get_Vrfy_MaxAllowableTries(Device_Name));

                    Set_ICC_PassStatus(Device_Name, true);
                }
                else
                {
                    Set_Corr_PassStatus(Device_Name, false);
                    int numCorrTries = Get_Corr_NumTries(Device_Name);
                    if (!CorrPassed) Set_Corr_NumTries(Device_Name, ++numCorrTries);

                    Set_ICC_PassStatus(Device_Name, IccPassed & numCorrTries <= Get_Corr_MaxAllowableTries(Device_Name));

                }
            }
        }

    }
}
