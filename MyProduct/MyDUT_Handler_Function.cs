using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using Microsoft.VisualBasic;
using Avago.ATF.StandardLibrary;
using Ivi.Visa.Interop;
using LibEqmtDriver;
using NationalInstruments.ModularInstruments.NIRfsg;
using NationalInstruments.ModularInstruments.NIRfsa;
using NationalInstruments.ModularInstruments.SystemServices.DeviceServices;
//using ni_NoiseFloor;
using NationalInstruments.RFmx.InstrMX;
using NationalInstruments.RFmx.SpecAnMX;
using MPAD_TestTimer;
using TCPHandlerProtocol;
using ni_NoiseFloorWrapper;
using Avago.ATF.Logger;
using Avago.ATF.LogService;
using System.Threading.Tasks;
using ClothoSharedItems;
using Avago.ATF.Shares;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MyProduct
{
    public partial class MyDUT : IDisposable
    {
        private static string Handler_SiteNo = "0";
        private static string Handler_ArmNo = "0";
        private static string Handler_WorkpressForce = "0";

        private void HTS_Handler_Command_Readback()
        {
            if (Flag_HandlerInfor == true)
            {
                if (Handler_Info == "TRUE")
                {
                    try
                    {
                        HandlerForce hli = handler.ContactForceQuery();
                        Handler_SiteNo = HandlerAddress;
                        Handler_ArmNo = hli.ArmNo.ToString();
                        Handler_WorkpressForce = hli.PlungerForce.ToString();
                    }
                    catch
                    {
                        Handler_SiteNo = "-1"; //error
                        Handler_ArmNo = "-1";
                        Handler_WorkpressForce = "-1";
                    }
                }
                else
                {
                    Handler_SiteNo = "999";
                    Handler_ArmNo = "999";
                    Handler_WorkpressForce = "999";
                }
            }
            else
            {
                Handler_SiteNo = "999";
                Handler_ArmNo = "999";
                Handler_WorkpressForce = "999";
            }
        }
        private double[] HTS_Handler_Command_Readback_AddResult_Array()
        {
            double[] cb = new double[3];
            cb[0] = Convert.ToDouble(Handler_SiteNo);
            cb[1] = Convert.ToDouble(Handler_ArmNo);
            cb[2] = Convert.ToDouble(Handler_WorkpressForce);
            return cb;
        }
        public static int GetNextModuleID(int maxModuleID, out bool status)
        {
            status = true;      //default set to true
            int siteNumber = 0;
            string lotId = "999999";
            int moduleId = 0;
            string[] Id;
            string ModuleDir = @"C:\Avago.ATF.Common\ModuleID\";
            string moduleIDLogFile = "";
            char[] separator = new char[] { '-' };
            int site1InitID = 0, site2InitID = 10000, site3InitID = 20000;
            string warning = "";

            string testerId = "999999";

#if (!DEBUG)
            testerId = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_TESTER_ID, "");
            lotId = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_LOT_ID, "");
#else
            testerId = "DUMMY-02";      // Need to enable this during debug mode
            lotId = "PT0000000000-E";
            MessageBox.Show("Program Running in Debug Mode Compilation - For Lab Usage only", "!!! WARNING !!!", MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif

            ////For debug purpose
            //MessageBox.Show("TesterId: " + testerId + "@ LotId: " + lotId);

            Id = testerId.Split(separator);

            try
            {
                siteNumber = Convert.ToInt32(Id[1]);
            }
            catch (Exception e)
            {
                MessageBox.Show("Invalid Tester_ID (" + testerId + ") was entered", "!!! WARNING !!!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                status = false;
                return 0;
            }

            moduleIDLogFile = string.Format("{0}{1}.txt", ModuleDir, lotId);

            if (File.Exists(moduleIDLogFile))
            {
                moduleId = Convert.ToInt32(System.IO.File.ReadAllText(moduleIDLogFile)) + 1;
            }
            else
            {
                if (siteNumber == 1) { moduleId = site1InitID + 1; }
                else if (siteNumber == 2) { moduleId = site2InitID + 1; }
                else if (siteNumber == 3) { moduleId = site3InitID + 1; }
                else { moduleId = 0; }
            }

            //To prevent duplicate module ID in case test sites are down and only single test site is down.
            if (siteNumber == 1)
            {
                if (moduleId > site2InitID)
                {
                    warning = string.Format("Module ID for Site{0} exceeded {1}!\nIf test is continue, there may be duplicated module ID",
                        siteNumber, site2InitID);
                    status = false;
                    MessageBox.Show(warning, "!!! WARNING !!!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (siteNumber == 2)
            {
                if (moduleId > site3InitID)
                {
                    warning = string.Format("Module ID for Site{0} exceeded {1}!\nIf test is continue, there may be duplicated module ID",
                        siteNumber, site3InitID);
                    status = false;
                    MessageBox.Show(warning, "!!! WARNING !!!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (siteNumber == 3)
            {
                if (moduleId > maxModuleID)
                {
                    warning = string.Format("Module ID for Site{0} exceeded {1}!\nQuit test, Module ID not supported",
                        siteNumber, maxModuleID);
                    status = false;
                    MessageBox.Show(warning, "!!! WARNING !!!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (!Directory.Exists(ModuleDir)) { Directory.CreateDirectory(ModuleDir); }

            try
            {
                System.IO.File.WriteAllText(moduleIDLogFile, moduleId.ToString());
            }
            catch (Exception e)
            {
                status = false;
                MessageBox.Show(e.Message);
            }

            return moduleId;
        }
        public static int GetTestSiteID(out bool status, out string testerId)
        {
            status = true;      //default set to true
            int siteNumber = 0;
            string[] Id;
            char[] separator = new char[] { '-' };

            testerId = "999999";

#if (!DEBUG)
            testerId = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_TESTER_ID, "");
#else
            testerId = "DUMMY-02";      // Need to enable this during debug mode
            MessageBox.Show("Program Running in Debug Mode Compilation - For Lab Usage only", "!!! WARNING !!!", MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif

            Id = testerId.Split(separator);

            try
            {
                siteNumber = Convert.ToInt32(Id[1]);
            }
            catch (Exception e)
            {
                MessageBox.Show("Invalid Tester_ID (" + testerId + ") was entered", "!!! WARNING !!!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                status = false;
                return 0;
            }
            return siteNumber;
        }
    }
}
