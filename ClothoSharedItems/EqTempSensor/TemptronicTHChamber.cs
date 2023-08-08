namespace ClothoSharedItems.Device
{
    public partial class TemptronicTHChamber : DevSCPI
    {
        public const string DEFAULT_ADDRESS = "GPIB0::9::INSTR";
        private double m_NMODE;

        public TemptronicTHChamber() : base("TEMPTRONIC Environmenttal Test Chamber", DevType.GPIB, DevType.GPIB, DEFAULT_ADDRESS, DevSCPI.NA_TCPIPAddress, DevSCPI.NA_TCPIPPort)
        {
        }

        public override void Initialize()
        {
        }

        public override bool IsRightIDN(string idn)
        {
            return idn.CIvContains("TEMPTRONIC") && idn.CIvContainsAnyOf("ATS-515");
        }

        protected override bool IsRightDevice()
        {
            return IsRightIDN(Query("*IDN?"));
        }

        public ONOFF Power
        {
            get { return Query("FLOW?") == "1" ? ONOFF.ON : ONOFF.OFF; }
            set { Write("FLOW " + (value == ONOFF.ON ? "1" : "0")); }
        }

        public ChamberHeadONOFF ChamberHeadUpDown
        {
            get { return Query("HEAD?") == "1" ? ChamberHeadONOFF.DOWN : ChamberHeadONOFF.UP; }
            set { Write("HEAD " + (value == ChamberHeadONOFF.DOWN ? "1" : "0")); }
        }

        private double NMODE
        {
            get { return m_NMODE = Query("SETN?").ToDouble(); }
            set
            {
                m_NMODE = value;
                Write("SETN " + m_NMODE);
            }
        }

        public double Temperature
        {
            set
            {
                if (value < 20) NMODE = 2;
                else if (value > 30) NMODE = 0;
                else NMODE = 1;

                Write("SETP " + value);
            }
            get { return Query("TMPA?").ToDouble(); }
        }

        public void SetTemperature(double target)
        {
            Temperature = target;
        }
    }
}