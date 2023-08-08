namespace ClothoSharedItems.Device
{
    public sealed class EspecTHChamber : DevSCPI
    {
        public enum MODE { OFF = 0, STANDBY, CONSTANT, RUN }

        public const string DEFAULT_ADDRESS = "GPIB0::1::INSTR";

        public EspecTHChamber() : base("ESPEC Environmenttal Test Chamber", DevType.GPIB, DevType.GPIB, DEFAULT_ADDRESS, DevSCPI.NA_TCPIPAddress, DevSCPI.NA_TCPIPPort)
        {
        }

        public override void Initialize()
        {
        }

        public override bool IsRightIDN(string idn)
        {
            return idn.CIvStartsWithAnyOf("T,T,P", "T,T,S", "NA:CMD_ERR", "NA:COMMAND ERR");
        } //SH241, SH242, PSL-2J

        protected override bool IsRightDevice()
        {
            return IsRightIDN(Query("TYPE?"));
        }

        public void SetMode(MODE mode)
        {
            if (WriteIfUnregistered("MODE", mode, false))
            {
                if (mode == MODE.RUN) Query("MODE,RUN 1");
                else Query("MODE," + mode);
            }
        }

        public double Temperature
        {
            set { Query("TEMP,S" + value); }
            get { return Query("TEMP?").SplitToArray()[0].ToDouble(); }
        }

        public double ReadSetTemperature()
        {
            return Query("TEMP?").SplitToArray()[1].ToDouble();
        }

        public double Humidity
        {
            set { Query("HUMI,S" + value); }
            get { return Query("HUMI?").SplitToArray()[0].ToDouble(); }
        }

        public void SetTemperature(double target)
        {
            Query("TEMP,S" + target);
        }

        public void SetTemperature(double target, double lowLimit, double highLimit)
        {
            Query(string.Format("TEMP,S{0} H{2} L{1}", target, lowLimit, highLimit));
        }

        public void SetHumidity(double target)
        {
            Query("HUMI," + (double.IsNaN(target) ? "SOFF" : ("S" + target)));
        }

        public void SetHumidity(double target, double lowLimit, double highLimit)
        {
            Query(string.Format("HUMI,S{0} H{2} L{1}", target, lowLimit, highLimit));
        }

        public void SetPowerOn()
        {
            Query("Power,ON");
        }

        public void StopSoak()
        {
            Query("MODE,STANDBY");
        }
    }
}