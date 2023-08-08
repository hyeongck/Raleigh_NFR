using System.Collections.Generic;
using TestPlanCommon.CommonModel;

namespace TestPlanCommon
{
    public class TestConditionReaderBase
    {
        /// <summary>
        /// Main Tab.
        /// </summary>
        public Dictionary<string, string> TCF_Setting { get; set; }

        public void FillTcfSetting(string settingName, string defaultValue, TcfSheetReader mainSheet)
        {
            TCF_Setting.Add(settingName, defaultValue);

            for (int Row = 1; Row < 100; Row++)
            {
                if (mainSheet.allContents.Item3[Row, 0] == settingName)
                {
                    TCF_Setting[settingName] = mainSheet.allContents.Item3[Row, 1].ToUpper();
                    break;
                }
            }
        }

        public void FillTcfSetting(string settingName, string defaultValue, string sheetSettingName, TcfSheetReader mainSheet)
        {
            TCF_Setting.Add(settingName, defaultValue);

            for (int Row = 1; Row < 100; Row++)
            {
                if (mainSheet.allContents.Item3[Row, 0] == sheetSettingName)
                {
                    TCF_Setting[settingName] = mainSheet.allContents.Item3[Row, 1].ToUpper();
                    break;
                }
            }
        }

        public void FillTcfSettingOnTrue(string settingName, string defaultValue, TcfSheetReader mainSheet)
        {
            TCF_Setting.Add(settingName, defaultValue);

            for (int Row = 1; Row < 100; Row++)
            {
                if (mainSheet.allContents.Item3[Row, 0] == settingName)
                {
                    string zValue = mainSheet.allContents.Item3[Row, 1].ToUpper();
                    if (zValue != "TRUE") break;
                    TCF_Setting[settingName] = zValue;
                    break;
                }
            }
        }

        public void FillTcfSettingOnTrue(string settingName, string defaultValue, string sheetSettingName, TcfSheetReader mainSheet)
        {
            TCF_Setting.Add(settingName, defaultValue);

            for (int Row = 1; Row < 100; Row++)
            {
                if (mainSheet.allContents.Item3[Row, 0] == sheetSettingName)
                {
                    string zValue = mainSheet.allContents.Item3[Row, 1].ToUpper();
                    if (zValue != "TRUE") break;
                    TCF_Setting[settingName] = zValue;
                    break;
                }
            }
        }
    }
}