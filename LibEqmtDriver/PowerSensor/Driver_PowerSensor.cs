using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibEqmtDriver.PS
{
    public interface iPowerSensor
    {
        void Initialize(int ch);
        void Reset();
        void Close();
        void SetOffset(int ch, double val);
        void EnableOffset(int ch, bool status);
        void SetFreq(int ch, double val, int measuretype); // meausr type : 0 = Calibration Type, 1 = DUT Measuring Type
        float MeasPwr(int ch);
    }
}
