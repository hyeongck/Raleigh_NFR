using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Aemulus.Hardware;

namespace Aemulus_PXIe_Modules_Info
{
    public class AemHardwareProperty
    {
        public static UInt16 VI_PXI_BAR0_SPACE = 11;
        public static UInt32 VI_ATTR_MANF_ID = 0x3FFF00D9;
        public static UInt32 VI_ATTR_SLOT = 0x3FFF00E8;
        public static UInt32 VI_ATTR_MANF_NAME = 0xBFFF0072;
        public static UInt32 VI_ATTR_MODEL_NAME = 0xBFFF0077;
        public static UInt32 VI_ATTR_MODEL_CODE = 0x3FFF00DF;
        public static UInt32 VI_ATTR_USB_SERIAL_NUM = 0xBFFF01A0;

        public AemHardwareProperty()
        {
            int status = 0;
            string[] address;
            string[] modelName;
            string[] manufacturer;
            int[] modelID;

            IntPtr vi;
            string optionStr = string.Empty;

            StringBuilder serialNumber = new StringBuilder(100);

            AemDetectPXIeModules(out address, out modelName, out manufacturer, out modelID);

            initializePropertyArrays();

            ProductName = modelName;

            int counter = 0;
            //Read serial number
            foreach(string name in ProductName)
            {
                if (name.Contains("AM"))
                {
                    optionStr = "Simulate=0,DriverSetup=Model:" + name;
                    status = CAemSMU.AemDCPwr_InitChannels(address[counter], "0", 0, optionStr, out vi);
                    status = CAemSMU.AemDCPwr_ReadSerialNumber(vi, serialNumber);
                    status = CAemSMU.AemDCPwr_Close(vi);

                    SerialNumber[counter] = serialNumber.ToString();
                    serialNumber.Clear();
                    
                    counter++;
                }
                else if (name.Contains("DM"))
                {
                    optionStr = "Simulate=0,DriverSetup=Model:" + name;
                    status = CAemDM.DM482e_DPINOpen(address[counter], 0x3, 0, optionStr, out vi);
                    status = CAemDM.DM482e_ReadSerialNumber(vi, serialNumber);
                    status = CAemDM.DM482e_DPINClose(vi);

                    SerialNumber[counter] = serialNumber.ToString();
                    serialNumber.Clear();

                    counter++;
                }
            }
        }

        private void initializePropertyArrays()
        {
            AliasName = new string[Count];
            ProductName = new string[Count];
            SerialNumber = new string[Count];
        }

        private void AemDetectPXIeModules(out string[] addr, out string[] model, out string[] manufacturer, out int[] model_id)
        {
            List<string> _address = new List<string>();
            List<string> _model = new List<string>();
            List<string> _manufacturer = new List<string>();
            List<int> _model_id = new List<int>();
            int status = 0;
            int InstrCount = 0;
            IntPtr defaultRM = IntPtr.Zero;
            IntPtr fList = IntPtr.Zero;
            IntPtr vi;
            int manf_id;
            int model_code;
            int slot;
            var manf_name = new StringBuilder(128);  //Change from 32 to 128 since v1.5.4.0
            var model_name = new StringBuilder(128); //Change from 32 to 128 since v1.5.4.0
            var address = new StringBuilder(32);

            try
            {
                status = CVISA.viOpenDefaultRM(out defaultRM);
                if (status == 0)
                {
                    status = CVISA.viFindRsrc(defaultRM, "PXI?*INSTR", out fList, out InstrCount, address);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("QueryAEMModule Error. \r" + ex.Message);
            }

            Count = InstrCount;

            while ((InstrCount--) > 0)
            {
                try
                {
                    status = CVISA.viOpen(defaultRM, address.ToString(), 0, 0, out vi);
                }
                catch
                {
                    continue;
                }

                status = CVISA.viGetAttribute(vi, VI_ATTR_MODEL_CODE, out model_code);
                status = CVISA.viGetAttribute(vi, VI_ATTR_MODEL_NAME, model_name);
                status = CVISA.viGetAttribute(vi, VI_ATTR_MANF_NAME, manf_name);
                _address.Add(address.ToString());
                _model.Add(model_name.ToString());
                _manufacturer.Add(manf_name.ToString());
                _model_id.Add(model_code);

                if (InstrCount > 0)
                {
                    CVISA.viClose(vi);
                    status = CVISA.viFindNext(fList, address);
                }
            }

            addr = _address.ToArray();
            model = _model.ToArray();
            manufacturer = _manufacturer.ToArray();
            model_id = _model_id.ToArray();
        }

        public string[] AliasName
        {
            get;
            private set;
        }

        public string[] ProductName
        {
            get;
            private set;
        }

        public string[] SerialNumber
        {
            get;
            private set;
        }

        public int Count
        {
            get;
            private set;
        }
    }

    class CVISA
    {
        [DllImport("visa32.dll", EntryPoint = "viOpenDefaultRM")]
        public static extern int viOpenDefaultRM(out IntPtr vi);

        [DllImport("visa32.dll", EntryPoint = "viFindRsrc")]
        public static extern int viFindRsrc(IntPtr sesn, string expr, out IntPtr vi, out int retCnt, StringBuilder desc);

        [DllImport("visa32.dll", EntryPoint = "viOpen")]
        public static extern int viOpen(IntPtr sesn, string name, UInt32 mode, UInt32 timeout, out IntPtr vi);

        [DllImport("visa32.dll", EntryPoint = "viClose")]
        public static extern int viClose(IntPtr sesn);

        [DllImport("visa32.dll", EntryPoint = "viFindNext")]
        public static extern int viFindNext(IntPtr vi, StringBuilder desc);

        [DllImport("visa32.dll", EntryPoint = "viGetAttribute")]
        public static extern int viGetAttribute(IntPtr vi, UInt32 attrName, out int attrValue);

        [DllImport("visa32.dll", EntryPoint = "viGetAttribute")]
        public static extern int viGetAttribute(IntPtr vi, UInt32 attrName, StringBuilder attrValue);

        [DllImport("visa32.dll", EntryPoint = "viOut32")]
        public static extern int viOut32(IntPtr vi, UInt16 space, UInt32 offset, UInt32 val32);

        [DllImport("visa32.dll", EntryPoint = "viIn32")]
        public static extern int viIn32(IntPtr vi, UInt16 space, UInt32 offset, out UInt32 val32);

    }

    class CAemSMU
    {
        [DllImport("AemDCPwr.dll", EntryPoint = "AemDCPwr_InitChannels")]
        public static extern int AemDCPwr_InitChannels(String resourceName, string chName, int initOptions, string optionString, out IntPtr vi);

        [DllImport("AemDCPwr.dll", EntryPoint = "AemDCPwr_Close")]
        public static extern int AemDCPwr_Close(IntPtr vi);

        [DllImport("AemDCPwr.dll", EntryPoint = "AemDCPwr_ReadSerialNumber")]
        public static extern int AemDCPwr_ReadSerialNumber(IntPtr vi, StringBuilder serialNumber);
    }

    class CAemDM
    {
        [DllImport("DM482e.dll", EntryPoint = "DM482e_DPINOpen")]
        public static extern int DM482e_DPINOpen(String resourceName, int dpinGrpSel, int initOptions, string optionString, out IntPtr vi);

        [DllImport("DM482e.dll", EntryPoint = "DM482e_DPINClose")]
        public static extern int DM482e_DPINClose(IntPtr vi);

        [DllImport("DM482e.dll", EntryPoint = "DM482e_ReadSerialNumber")]
        public static extern int DM482e_ReadSerialNumber(IntPtr vi, StringBuilder serialNumber);
    }

}
