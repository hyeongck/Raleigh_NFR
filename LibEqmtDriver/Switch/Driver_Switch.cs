using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibEqmtDriver.SCU
{
    public interface iSwitch
    {
        void Close();
        void Initialize();
        void SetPath(string val);
        void SetPath(object state);
        void Reset();
        int SPDT1CountValue();        
        int SPDT2CountValue();        
        int SPDT3CountValue();        
        int SPDT4CountValue();       
        int SP6T1_1CountValue();
        int SP6T1_2CountValue();
        int SP6T1_3CountValue();
        int SP6T1_4CountValue();
        int SP6T1_5CountValue();
        int SP6T1_6CountValue();
        int SP6T2_1CountValue();
        int SP6T2_2CountValue();
        int SP6T2_3CountValue();
        int SP6T2_4CountValue();
        int SP6T2_5CountValue();
        int SP6T2_6CountValue();
        void SaveRemoteMechSwStatusFile();
        void SaveLocalMechSwStatusFile();
        string GetInstrumentInfo();
    }
    
}
