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
        // Delay routine to avoid using Thread.Sleep()
        public void DelayMs(int mSec)
        {
            LibEqmtDriver.Utility.HiPerfTimer timer = new LibEqmtDriver.Utility.HiPerfTimer();
            timer.wait(mSec);
        }
        public void DelayUs(int uSec)
        {
            LibEqmtDriver.Utility.HiPerfTimer timer = new LibEqmtDriver.Utility.HiPerfTimer();
            timer.wait_us(uSec);
        }
        public string SearchLocalSettingDictionary(string sMainKey, string sSubKey)
        {
            if (myUtility.DicLocalfile.ContainsKey(sMainKey))
                if (myUtility.DicLocalfile[sMainKey].ContainsKey(sSubKey))
                    return myUtility.DicLocalfile[sMainKey][sSubKey];
                else
                    return "NONE";
            else
                return "";
        }
        public static string GetTestPlanPath()
        {
            string basePath = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_FULLPATH, "");

            if (basePath == "")   // Lite Driver mode
            {
                string tcfPath = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_TCF_FULLPATH, "");

                int pos1 = tcfPath.IndexOf("TestPlans") + "TestPlans".Length + 1;
                int pos2 = tcfPath.IndexOf('\\', pos1);

                basePath = tcfPath.Remove(pos2);
            }

            return basePath + "\\";
        }
    }
}
