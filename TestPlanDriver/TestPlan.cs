
#region COMMNET and Copyright SECTION (NO MANUAL TOUCH!)
// This is AUTOMATIC generated template Test Plan (.cs) file for ATF (Clotho) of WSD, AvagoTech: V2.2.1.0
// Any Questions or Comments, Please Contact: YU HAN, yu.han@avagotech.com
// NOTE 1: Test Plan template .cs has 'FIXED' Sections which should NEVER be Manually Touched 
// NOTE 2: Starting from V2.2.0, Clotho follows new Package style test plan management:
//       (a) Requires valid integer Version defined for TestPlan, TestLimit, and ExcelUI
//               For TestPlan.cs, refer to header item 'TestPlanVersion=1'
//               For TestLiimit.csv, refer to row #7 'SpecVersion,1'
//               For ExcelUI.xlsx, refer to sheet #1, row #1 'VER	1'
//       Note TestPlanTemplateGenerator generated items holds default version as '1'
//       (b) About ExcelUI file and TestLimit file:
//               Always load from same parent folder as Test Plan .cs, @ root level
//       (c) About Correlation File:
//               When Development mode, loaded from  C:\Avago.ATF.Common.x64\CorrelationFiles\Development\
//               When Production mode, loaded from package folder within C:\Avago.ATF.Common.x64\CorrelationFiles\Production\
#endregion COMMNET and Copyright SECTION

#region Test Plan Properties Section (NO MANUAL TOUCH)
////<TestPlanVersion>TestPlanVersion=1<TestPlanVersion/>
////<ExcelBuddyConfig>BuddyExcel = ENGR-8234-AP2-NS_BE-PXI-NI_Proto1_TCF_Rev0002.xlsx;ExcelDisplay = 1<ExcelBuddyConfig/>
////<TestLimitBuddyConfig>BuddyTestLimit = ENGR-8234-AP2-NS_BE-PXI-NI_Proto1_TSF_Rev0002.csv<TestLimitBuddyConfig/>
////<CorrelationBuddyConfig>BuddyCorrelaton = ENGR-8234-AP2-NS_BE-PXI-NI_Proto1_CF_Rev0002.csv<CorrelationBuddyConfig/>
#endregion Test Plan Properties Section


#region Test Plan Hardware Configuration Section
#endregion Test Plan Hardware Configuration Section


#region Test Plan Parameters Section
////<TestParameter>Name="SimHW";Type="IntType";Unit=""<TestParameter/>
#endregion Test Plan Parameters Section


#region Singel Value Parameters Section
////<SingelValueParameter>Name="SimHW";Value="1";Type="IntType";Unit=""<SingelValueParameter/>
#endregion Singel Value Parameters Section


#region Test Plan Sweep Control Section (NO MANUAL TOUCH!)
#endregion Test Plan Sweep Control Section


#region 'FIXED' Reference Section (NO MANUAL TOUCH!)
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Ivi.Visa.Interop;

using Avago.ATF.StandardLibrary;
using Avago.ATF.Shares;
using System.Data.SQLite;
using AvagoGU;
#endregion 'FIXED' Reference Section


#region Custom Reference Section
//////////////////////////////////////////////////////////////////////////////////
// ----------- ONLY provide your Custom Reference 'Usings' here --------------- //
using TestPlan_BansheeFull;
using TestPlanCommon;

// ----------- END of Custom Reference 'Usings' --------------- //
//////////////////////////////////////////////////////////////////////////////////
#endregion Custom Reference Section


public class ENGR_8234_AP2_NS_PROD_Proto1B_Rev0000 : MarshalByRefObject, IATFTest
{
    private IATFTest testPlan = new TestPlan_BansheeFull.BansheeNF();

    public string DoATFInit(string args)
    {
        //Debugger.Break();

        try
        {
            testPlan.DoATFInit(args);
        }

        catch (Exception ex)
        {
            MessageBox.Show("Error Message : " + ex.Message + "\n" + "Stack Trace : " + ex.StackTrace + "\n");
        }





        return string.Concat("Enter DoATFInit: {0}\nDo Minimum HW Init:\n{1}\n", args, ATFInitializer.DoMinimumHWInit());
    }

    public string DoATFLot(string args)
    {
        //Debugger.Break();

        testPlan.DoATFLot(args);

        return string.Concat("Enter DoATFLot: {0}\n", args);
    }

    public ATFReturnResult DoATFTest(string args)
    {
        //Debugger.Break();

        return testPlan.DoATFTest(args);
    }

    public string DoATFUnInit(string args)
    {
        //Debugger.Break();
        if (testPlan == null) return string.Concat("Enter DoATFUnInit: {0}\n", args);
        testPlan.DoATFUnInit(args);

        return string.Concat("Enter DoATFUnInit: {0}\n", args);
    }
}