using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibEqmtDriver.DC_1CH
{
    public interface iDCSupply_1CH
    {
        void Init();
        void Close();
        void DcOn(int Channel);
        void DcOff(int Channel);
        void SetVolt(int Channel, double Volt, double iLimit);
        float MeasI(int Channel);
        float MeasV(int Channel);
    }
}
