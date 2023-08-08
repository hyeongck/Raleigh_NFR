Rev1.00 - 20/07/2020
1) Update the Lightning NFR Initial Dev2/Proto-1 Code
Changes :
1. Changed VST Temperature check target : from 1.0 C to 2.0 C
2. Updated and modified Cable Calibration Function.
   2-1. (New) : add 'PXI_RF_LOPWR_CAL' -> calibration function the RF Cable using with spectrum mode on VST
   2-2. (Modified) : When performing cable calibration using power sensor, average point is used from 3 to 16. (In actual measurement, set Average point to 3)


Rev1.10 - 22/09/2020
1) Modified & added the code.
Changes : 
<@ MIPI : NI6570_Rev5.cs>
1. Modified the function of 'TurnOff_VIO' (PXIE 6570)
- added "Lib_Var.b_setNIVIO = true;"

<@ NF-VST : NF_NI-VST.cs>
1. Modified the frequency of stop & start and step.
- ex) stopFreq = Math.Round(stopFreq, 3);

2. Modified the value of local variable
- @ Function : MeasurePower, "double dBThreshold = 0.75 * 10 + PowerTrace[index]" to "double dBThreshold = 0.70 * 10 + PowerTrace[index]"

<@ SMU : NiPXISMU.cs>
1. Modified the function of 'SetVolt' (NiPXISMU)
- modified the round value of 'iLimit' -> '5' to '10

<@ MyProduct : MyDUT.cs>
1. @ Test : "SMU_LEAKAGE", "TempLabel = "SMUI_CH" + MeasSMU[i];" to "TempLabel = "SMUI_CH" + SetSMU[i]
2. @ Test : "Read_OTP", Added the function of 'READ_2DID_FROM_OTHER_OTP_BIT'
3. @ Test : "readout_OTPReg_viaEffectiveBit", Added the function of 'TurnOff_VIO'

Rev1.20 - 16/10/2020
1) Modified & added the code.
<@ Myproduct : MyDUT.CS>
1. @ Test : "SMU_LEAKGE", change current measurement type (From Point measurement to Trace measurement) // (for PDM leakage measurement, need to sync with RF1 / RF2)

<@ SMU : Aemulus1340.cs, AePxiSMU.cs, KeithleySMU.cs, NiPXISMU.cs, PowerSupplyDriver.cs>
1. added : current measure trace mode
2. @ NiPXISMU.cs :
   - current measure (point mode - normal measurement) : SMU is working on normal mode
   - current measure (trace mode - PDM leakage measurement) : SMU is working on custom mode (current compliance : 10uA - Don't set current limit under 10uA)


Rev1.3 - 24/11/2020
1) Modified the code.
<@ Myproduct : MyDUT.CS>
1. @ TEST : "NF_MAX_MIN", build "NF_COLD-Ampl/Freq-MAX" when ¨¬Disp_ColdTrace is checked.


From Seoul, Merged, 07/01/2021
1) Modified & added the code.
Changes :
1. Added test-time enhancement code.
 - Minimize the overhead of switching time by adding the switch configuration verification step and thread function.
2. Modified parameters.
 - NI6570 voh level changed: 0.9 -> 0.6
 - minor smu settings.

Rev1.4 - 22/01/2021
1) Modified the code : debugging error at "PXI_RF_LOPWR_CAL".
2) Update for clotho v3.1.3.
 - modified the code at "BuildResults".


Rev1.5 - 24/01/2021
1) Modified the code : 
 - modified variable type 'int' to 'long'
   : long R_MfgId = -999;
   : long R_OtpModuleId = -999;

Rev1.6 - 27/01/2021
1) Modified the code :
 - @ TEST : "PXI_NF_HOT", if Setting Tx Freq is over than 'Stop Tx Freq', Make Tx freq equal to stop tx freq.

2) Added the code :
 - @ Test : "PXI_NF_HOT", if step Tx Freq is not 0 and different with 'StepTxFreq1' when stop Tx Freq is same with stop Tx Freq, Make Tx step freq equal to 'StepTxFreq1'.

Rev1.7 - 02/03/2021
1) Added the code (Function)
1. NFR DPAT
2. Enable RF1 outlier flag readback

2) Modified the code.
1. Enable load board ID readback
2. Enable test contactor ID readback
3. Enable load board temp sensor readback
4. Solve first unit temp readback - 20deg issue
5. Return '-1' for duplicated module ID (double unit detection)
6. Read back card info

Rev1.8 - 02/03/2021
1) Modified the code
1. Init VST temp calibration/ (Init) dForceAlignmentDelta = 0.5c, (After) dForceAlignmentDelta = 2C >> need more test, disabled
2. Change variable type of TestTimePA (Long to double)
3. Change type of test time of each parameters (tTime.ElapsedMilliseconds to tTime.Elapsed.TotalMilliseconds)


Rev1.9 - 08/03/2021
1) Re-upload the code. (Rev 1.6 code)

Rev1.10 - 08/03/2021
2) Go back to the old code for gethering NFR-B32RX DIAG. (need to change later)

Rev1.11 - 18/03/2021
1) Re-Upload the code. (Rev 1.6 code)
2) Added the code.
 - About 'MIPI-PPMU Init' (Please refer to '#region MIPI-PPMU Init')
 - move the code about 'Forcing Voltage & Measure Current the pin that use the PPMU'. (From MIPI Class & Interface to MIPI-PPMU Class & Interface -> NI6570 Only) 
3) Modified the code.
 - Change the pin from Digital to PPMU (VIOP0 - Tx, VIOP1 - Rx)
 - Forcing voltage smoothly (0 V -> 0.3 V -> 0.6 V -> 1 V / To prevent the VIO voltage is charging fast)

Rev1.12 - 19/03/21
1) Modified the code.
 - Change the enum "PPMUVioOverrideString" : {RESET, VIOOFF, VIOON} => {RESET, HIZ, VIOON}
 - TurnOn_VIO @ NI6570 => 'HSDIO.Reset.ToLower()' => 'HSDIO.VIOON.ToLower();

Rev1.13 - 24/03/21
1) Modified the code.
 - Delete unnessary 'TurnOff_VIO' function on some parameters.

2) Added the code.
 - DM482e : pushing VIO voltage by stepping. and it was also declared with 'PriorVoltage & PriorCurrentLimit'

Rev1.14 - 26/03/21
1) Modified the code.
 - @ DM482e MIPI .cs : change from 'double prirorVoltage & priorCurrentLim & priorApertureTime' to 'double[] prirorVoltage & priorCurrentLim & priorApertureTime'
                       (To control Tx & Rx vio separately)

Rev1.15 - 29/03/21
1) Modified the code.
 - @ DM482e MIPI .CS : change size of double from '2' to '4'. 
                       (double[] priorVoltage = new double[4]; // VIO0 -> Tx, VIO1 -> Rx)

Rev2.1 - 06/04/21, Cheddar NFR Test Program Start
1) Added the code.
 - add the information of "Tx" & "ANT" & "Rx" Port in GU Header.

2) Modified the code.
 - @TestPlan.cs : delete the function about "Debugger.Break()" at "DOATFTest()".
 - @FormDoNOTTOuch.cs : change "TestPlanPath" & "RulePath" (From "ATFTestPlanTemplate" to "ATFTestPlanTemplate-Cheddar".
 - @NI6570_Rev5.cs : Select whether "debug variable" is true or false according to debug and release mode

Rev2.2 - 22/04/21
1) Added the code.
 - Modulation infomation for N70
 - Function about Icc (VCC_ET_40) & Icc2 (VCC_ET_100)

Rev2.3 - 25/05/21
1) Added the code.
 - New switch configuration : NI6509 + ZTM (Mini circuit Mech SP6T : 6EA Switch)

Rev2.4 - 24/06/2021
1) Modify : 
 - @ NF_NI-VST.cs : Change code for VST5644 when measuring Rx gain.
 - @ MyDUT.cs : Change the code for the SMU's temperature reading function

Rev2.5 - 03/08/2021
1) Modify : 
 - @ MyDUT.cs : change the code for reading 'OTP_MODULE_ID' (MSB = RX, LSB = TX)
 - @ MyUtility.cs : change the count of reading TCF (Y axis : from '1500' to '2500')

Rev2.6 - 08/08/2021
1) Add : 
 - @ MyUtility.cs : Add the function (Call calibration loss and Generate dictionary)
 - @ LocalSettingFile : Add the variable (VCC_SMUCHANNEL, for more correct setting) 

2) Modified :
 - @ MyDUT.cs : change the call type about LocalSettingFile.

Rev2.7 - 08/17/2021
1) Add : 
 - @ NI6570_Rev5.cs : Add the alternative selection function between 'RZ' and 'NRZ' (@ NI6570)

2) Modified : 
 - @ MyDUT.cs : Way to read the 'Local setting file'

Rev2.8 - 19/10/2021
1) Modified :
 - @ MyDUT.cs : modified the code about GU HEADER @ NF RISE
 - @ MyDUT_Variabel.cs : modified the structure "s_TraceNo"
