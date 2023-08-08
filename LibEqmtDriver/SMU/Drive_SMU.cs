using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

namespace LibEqmtDriver.SMU
{
    
    public interface iPowerSupply
    {
        void Init();
        void Close();
        void DcOn(string StrSelection, ePSupply_Channel Channel);
        void DcOff(string StrSelection, ePSupply_Channel Channel);
        void SetNPLC(string StrSelection, ePSupply_Channel Channel, float val);
        void SetVolt(string StrSelection, ePSupply_Channel Channel, double Volt, double iLimit, ePSupply_VRange VRange);
        float MeasI(string StrSelection, ePSupply_Channel Channel, ePSupply_IRange IRange);
        float MeasI(string StrSelection, ePSupply_Channel Channel, ePSupply_IRange IRange, int i);
        // Ben - PDM Current Measure [Point -> Trace Mode], 15-10-20 
        float MeasITraceMode(string StrSelection, ePSupply_Channel Channel, ePSupply_IRange IRange, int ReadDelay);																										   
        float MeasV(string StrSelection, ePSupply_Channel Channel, ePSupply_VRange VRange);
        // Self Calibration : Only NI-4139 works
        void CalSelfCalibrate(string strSelection, ePSupply_Channel Channel);
        double CheckDeviceTemperature(string strSelection, ePSupply_Channel Channel);
    }

    public class Drive_SMU
    {
        private ePSupply_Model eModel;
        private ePSupply_Channel eChannel;
        private iPowerSupply objPS;

        public ManualResetEvent[] ThreadFlags;
        public void Initialize(iPowerSupply[] Eq_PSupply)
        {
            int i = 0;

            for (i = 0; i < Eq_PSupply.Length; i++)
            {
                Eq_PSupply[i].Init();
            }
        }
        public void Close(iPowerSupply[] Eq_PSupply)
        {
            int i = 0;

            for (i = 0; i < Eq_PSupply.Length; i++)
            {
                Eq_PSupply[i].Close();
            }
        }

        public void DcOn(string[] arrSelection, iPowerSupply[] Eq_PSupply)
        {
            int i = 0;

            for (i = 0; i < arrSelection.Length; i++)
            {
                PSupply_Selection(arrSelection[i], Eq_PSupply, out objPS, out eChannel, out eModel);
                objPS.DcOn(arrSelection[i], eChannel);
            }
        }
        public void DcOff(string[] arrSelection, iPowerSupply[] Eq_PSupply)
        {
            int i = 0;

            for (i = 0; i < arrSelection.Length; i++)
            {
                PSupply_Selection(arrSelection[i], Eq_PSupply, out objPS, out eChannel, out eModel);
                objPS.DcOff(arrSelection[i], eChannel);
            }
        }
        public void SetVolt(string strSelection, iPowerSupply[] Eq_PSupply, float Volt, float iLimit)
        {
            PSupply_Selection(strSelection, Eq_PSupply, out objPS, out eChannel, out eModel);
            objPS.SetVolt(strSelection, eChannel, Volt, iLimit, ePSupply_VRange._Auto);
            
        }
        public float Set(string strSelection, iPowerSupply[] Eq_PSupply, float NPLC, ePSupply_IRange IRange)
        {
            float MeasVal = -999;

            Stopwatch tTime = new Stopwatch();

            tTime.Reset();
            tTime.Start();

            PSupply_Selection(strSelection, Eq_PSupply, out objPS, out eChannel, out eModel);

            double a = tTime.ElapsedMilliseconds;
            NPLC = 0.06f;
            SetNPLC(strSelection, Eq_PSupply, NPLC);
            // MeasVal = objPS.MeasI(strSelection, eChannel, IRange);

            double b = tTime.ElapsedMilliseconds;
            return MeasVal;
        }

        public float MeasI(string strSelection, iPowerSupply[] Eq_PSupply, float NPLC, ePSupply_IRange IRange)
        {
            float MeasVal = -999;

            PSupply_Selection(strSelection, Eq_PSupply, out objPS, out eChannel, out eModel);
            //SetNPLC(strSelection, Eq_PSupply, NPLC);
            MeasVal = objPS.MeasI(strSelection, eChannel, IRange);


            return MeasVal;
        }
        public float MeasI(string strSelection, iPowerSupply[] Eq_PSupply, float NPLC, ePSupply_IRange IRange, int i)
        {
            float MeasVal = -999;

            ThreadPool.QueueUserWorkItem((state) => { Thread_MeasI(strSelection, Eq_PSupply, NPLC, IRange, i, ref MeasVal); });

            return MeasVal;
        }
        public void Thread_MeasI(string strSelection, iPowerSupply[] Eq_PSupply, float NPLC, ePSupply_IRange IRange, int i, ref float MeasVal)
        {

            PSupply_Selection(strSelection, Eq_PSupply, out objPS, out eChannel, out eModel);
            SetNPLC(strSelection, Eq_PSupply, NPLC);
            MeasVal = objPS.MeasI(strSelection, eChannel, IRange);
            ThreadFlags[i].Set();

        }
            // Ben - PDM Current Measure [Point -> Trace Mode], 15-10-20 																															  
         public float MeasITraceMode(string strSelection, iPowerSupply[] Eq_PSupply, float NPLC, ePSupply_IRange IRange, int ReadDelay)
        {
            float MeasVal = -999;
            PSupply_Selection(strSelection, Eq_PSupply, out objPS, out eChannel, out eModel);
            SetNPLC(strSelection, Eq_PSupply, NPLC);
            MeasVal = objPS.MeasITraceMode(strSelection, eChannel, IRange, ReadDelay);

            return MeasVal;
        } 
        public float MeasV(string strSelection, iPowerSupply[] Eq_PSupply, float NPLC, ePSupply_VRange VRange)
        {
            float MeasVal = -999;
            PSupply_Selection(strSelection, Eq_PSupply, out objPS, out eChannel, out eModel);
            SetNPLC(strSelection, Eq_PSupply, NPLC);
            MeasVal = objPS.MeasV(strSelection, eChannel, VRange);

            return MeasVal;
        }
        // Self Calibration : Only NI-4139 works 
        public void CalSelfCalibrate(string strSelection, iPowerSupply[] Eq_PSupply)
        {
            PSupply_Selection(strSelection, Eq_PSupply, out objPS, out eChannel, out eModel);
            objPS.CalSelfCalibrate(strSelection, eChannel);
        }

        public double CheckDeviceTemperature(string strSelection, iPowerSupply[] Eq_PSupply)
        {
            PSupply_Selection(strSelection, Eq_PSupply, out objPS, out eChannel, out eModel);

            return objPS.CheckDeviceTemperature(strSelection, eChannel);
        }
        public void SetNPLC(string strSelection, iPowerSupply[] Eq_PSupply, float valNPLC)
        {
            PSupply_Selection(strSelection, Eq_PSupply, out objPS, out eChannel, out eModel);
            objPS.SetNPLC(strSelection, eChannel, valNPLC);
        }
        private void PSupply_Selection(string val, iPowerSupply[] PSAvailable, out iPowerSupply objPS, out ePSupply_Channel eChannel, out ePSupply_Model eModel)
        {
            string[] arSelected = new string[4];
            arSelected = val.Split('_');

            const string
                PreFix_P1 = "P1",
                PreFix_P2 = "P2",
                PreFix_P3 = "P3",
                PreFix_P4 = "P4",
                PreFix_P5 = "P5",
                PreFix_P6 = "P6",
                PreFix_P7 = "P7",
                PreFix_Ch0 = "CH0",
                PreFix_Ch1 = "CH1",
                PreFix_Ch2 = "CH2",
                PreFix_Ch3 = "CH3",
                PreFix_Ch4 = "CH4",
                PreFix_Ch5 = "CH5",
                PreFix_Ch6 = "CH6",
                PreFix_Ch7 = "CH7",
                PreFix_Ch8 = "CH8",
                PreFix_ChA = "CHA",
                PreFix_ChB = "CHB",
                PreFix_Ae1340 = "AE1340",
                PreFix_AePXI = "AEPXI",
                PreFix_NiPXI = "NIPXI",
                PreFix_Keith = "KEITH",
                PreFix_AgilentPXI = "AGPXI",
                PreFix_AM471e = "AM471E",
                PreFix_AM430e = "AM430E",
                PreFix_NI4143 = "NI4143",
                PreFix_NI4139 = "NI4139",
                PreFix_NI4154 = "NI4154",
                PreFix_Agilent = "AG";

            switch (arSelected[0].ToUpper())
            {
                case PreFix_P1:
                    objPS = PSAvailable[0];
                    break;
                case PreFix_P2:
                    objPS = PSAvailable[1];
                    break;
                case PreFix_P3:
                    objPS = PSAvailable[2];
                    break;
                case PreFix_P4:
                    objPS = PSAvailable[3];
                    break;
                case PreFix_P5:
                    objPS = PSAvailable[4];
                    break;
                case PreFix_P6:
                    objPS = PSAvailable[5];
                    break;
                case PreFix_P7:
                    objPS = PSAvailable[6];
                    break;
                default:
                    objPS = null;
                    MessageBox.Show("Power Supply in Local Setting file PowerSupply portion have invalid setting, P1-7 only");
                    break;
            }

            switch (arSelected[1].ToUpper())
            {
                case PreFix_Ch0:
                    eChannel = ePSupply_Channel.Ch0;
                    break;
                case PreFix_Ch1:
                    eChannel = ePSupply_Channel.Ch1;
                    break;
                case PreFix_Ch2:
                    eChannel = ePSupply_Channel.Ch2;
                    break;
                case PreFix_Ch3:
                    eChannel = ePSupply_Channel.Ch3;
                    break;
                case PreFix_Ch4:
                    eChannel = ePSupply_Channel.Ch4;
                    break;
                case PreFix_Ch5:
                    eChannel = ePSupply_Channel.Ch5;
                    break;
                case PreFix_Ch6:
                    eChannel = ePSupply_Channel.Ch6;
                    break;
                case PreFix_Ch7:
                    eChannel = ePSupply_Channel.Ch7;
                    break;
                case PreFix_Ch8:
                    eChannel = ePSupply_Channel.Ch8;
                    break;
                case PreFix_ChA:
                    eChannel = ePSupply_Channel.a;
                    break;
                case PreFix_ChB:
                    eChannel = ePSupply_Channel.b;
                    break;
                default:
                    eChannel = new ePSupply_Channel();
                    MessageBox.Show("Power Supply channel in Local Setting file PowerSupply portion have invalid setting.");
                    break;
            }

            switch (arSelected[2].ToUpper())
            {
                case PreFix_Keith:
                    eModel = ePSupply_Model.Keithley;
                    break;
                case PreFix_NiPXI:
                    eModel = ePSupply_Model.NIPxi;
                    break;
                case PreFix_AgilentPXI:
                    eModel = ePSupply_Model.AgilentPxi;
                    break;
                case PreFix_Agilent:
                    eModel = ePSupply_Model.Agilent;
                    break;
                case PreFix_AePXI:
                    eModel = ePSupply_Model.AemulusPXI;
                    break;
                case PreFix_Ae1340:
                    eModel = ePSupply_Model.Aemulus1340;
                    break;
                case PreFix_AM471e:
                    eModel = ePSupply_Model.AM471e;
                    break;
                case PreFix_AM430e:
                    eModel = ePSupply_Model.AM430e;
                    break;
                case PreFix_NI4143:
                    eModel = ePSupply_Model.NI4143;
                    break;
                case PreFix_NI4139:
                    eModel = ePSupply_Model.NI4139;
                    break;
                case PreFix_NI4154:
                    eModel = ePSupply_Model.NI4154;
                    break;
                default:
                    eModel = new ePSupply_Model();
                    MessageBox.Show("Power Supply model in Local Setting file PowerSupply portion have invalid setting.");
                    break;
            }
        }
    }

    public enum ePSupply_VRange
    {
        //260xB range
        _Auto,
        Keith260x_100mV,
        Keith260x_1V,
        Keith260x_6V,
        Keith260x_40V,
        //261xB range
        Keith261x_200mV,
        Keith261x_2V,
        Keith261x_20V,
        Keith261x_200V,
        //Aemulus 
        _1V,
        _2V,
        _5V,
        _10V,
    }
    public enum ePSupply_IRange
    {
        //all model range
        _Auto,
        _all_100nA,
        _all_1uA,
        _all_10uA,
        _all_100uA,
        _all_1mA,
        _all_10mA,
        _all_100mA,
        _all_1A,

        //260x range
        _260x_3A,

        //261x range
        _261x_1_5A,
        _261x_10A,

    }
    public enum ePSupply_Channel
    {
        Ch0 = 0,
        Ch1 = 1,
        Ch2 = 2,
        Ch3 = 3,
        Ch4 = 4,
        Ch5 = 5,
        Ch6 = 6,
        Ch7 = 7,
        Ch8 = 8,
        a,
        b
    }
    public enum ePSupply_Model
    {
        Agilent,
        AgilentPxi,
        Aemulus1340,
        AemulusPXI,
        Keithley,
        AM471e,
        AM430e,
        NI4143,
        NI4139,
        NI4154,
        NIPxi
    }
}
