using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ClothoSharedItems
{
    public class PackageHelper
    {
        private float poutLimitDelta = 0.5f;
        private const double minLSL = -999999999999999;
        private const double maxUSL = 999999999999999;
        public double benchhighL = 9999999;
        public double benchlowL = -9999999;

        private List<string> m_cfIgnoreItems = new List<string>();
        private List<string> m_benchTLIgnoreItems = new List<string>();
        private List<string> m_plIncludeRule = new List<string>();
        private List<string> m_plExcludeRule = new List<string>();
        private List<GUBench> BenchRules = new List<GUBench>();
        private List<List<string>> m_guAddItems = new List<List<string>>();
        private List<List<string>> m_guMultiplyItems = new List<List<string>>();

        public PackageHelper()
        {
            if (!File.Exists(@"C:\tools\BRCMhelper.config"))
            {
                Directory.CreateDirectory(@"C:\tools");
                System.IO.File.WriteAllText(@"C:\tools\BRCMhelper.config", Properties.Resources.BRCMhelper);
            }

            if (File.Exists(@"C:\tools\BRCMhelper.config"))
            {
                XElement xmlRoot = XDocument.Load(@"C:\tools\BRCMhelper.config").Root;
                var xelPoutLimit = xmlRoot.Element("TL_PoutLimit");
                if (xelPoutLimit != null)
                {
                    var xelDeltaValue = xelPoutLimit.Element("DeltaValue");
                    if (xelDeltaValue != null && xelDeltaValue.Value.TryToDouble(out double tempLimit))
                    {
                        poutLimitDelta = (float)tempLimit;
                    }

                    var xelignoreList = xelPoutLimit.Element("IgnoreCF");
                    if (xelignoreList != null)
                    {
                        var ignoreList = xelignoreList.Value.SplitToArray(',');
                        if (ignoreList.Length > 0)
                        {
                            m_cfIgnoreItems.Clear();
                            m_cfIgnoreItems.AddRange(ignoreList);
                        }
                    }

                    var xelPoutLimitConditions = xelPoutLimit.Element("PoutLimitConditions");
                    if (xelPoutLimitConditions != null)
                    {
                        var includeRule = xelPoutLimitConditions.Element("IncludeRule").Value.SplitToArray(',');
                        var excludeRule = xelPoutLimitConditions.Element("ExcludeRule").Value.SplitToArray(',');

                        if (includeRule.Length > 0)
                        {
                            m_plIncludeRule.Clear();
                            m_plIncludeRule.AddRange(includeRule);
                        }

                        if (excludeRule.Length > 0)
                        {
                            m_plExcludeRule.Clear();
                            m_plExcludeRule.AddRange(excludeRule);
                        }
                    }
                }

                var xelGuPackage = xmlRoot.Element("GuPacakge");
                if (xelGuPackage != null)
                {
                    var xelGuCorr = xelGuPackage.Element("GuCorr");
                    var xelGuBench = xelGuPackage.Element("GuBench");

                    foreach (var item in xelGuCorr.Element("ADD").Elements("Item"))
                    {
                        m_guAddItems.Add(new List<string>(item.Value.SplitToArray(',').ToList()));
                    }

                    foreach (var item in xelGuCorr.Element("Multiply").Elements("Item"))
                    {
                        m_guMultiplyItems.Add(new List<string>(item.Value.SplitToArray(',').ToList()));
                    }

                    var guCommonSpec = xelGuBench.Element("Specs");
                    if (guCommonSpec != null)
                    {
                        if (guCommonSpec.Element("HighL").Value.TryToDouble(out double v1))
                            benchhighL = v1;
                        if (guCommonSpec.Element("LowL").Value.TryToDouble(out double v2))
                            benchlowL = v2;
                    }

                    foreach (var item in xelGuBench.Elements("Item"))
                    {
                        GUBench bench = new GUBench();

                        if (item.Element("Limit").Value.TryToDouble(out double lim))
                        {
                            bench.Limit = lim;
                        }

                        foreach (var include in item.Elements("Include"))
                        {
                            bench.PushToIncludes(new List<string>(include.Value.SplitToArray(',')));
                        }

                        foreach (var exclude in item.Elements("Exclude"))
                        {
                            bench.PushToExcludes(new List<string>(exclude.Value.SplitToArray(',')));
                        }

                        BenchRules.Add(bench);
                    }
                }
            }
        }

        public class GUBench
        {
            public double Limit;
            public List<List<string>> Includes = new List<List<string>>();
            public List<List<string>> Excludes = new List<List<string>>();

            public void PushToIncludes(List<string> item)
            {
                Includes.Add(item);
            }

            public void PushToExcludes(List<string> item)
            {
                Excludes.Add(item);
            }
        }

        public void GeneratePackage(List<string> data, string TCFname)
        {
            string TargetFileName = string.Format("{0}_{1}_{2}", ClothoDataObject.Instance.Get_TCF_Condition("GuPartNo"), ClothoDataObject.Instance.Get_TCF_Condition("Description"), ClothoDataObject.Instance.Get_TCF_Condition("Sample_Version"));
            if (ClothoDataObject.Instance.TCF_Setting == null)
            {
                TargetFileName = TCFname;
            }
                

            string cfPath = string.Format(@"C:\Avago.ATF.Common\Results\{0}_CF_Rev9999.csv", TargetFileName);
            string tsfPath = string.Format(@"C:\Avago.ATF.Common\Results\{0}_TSF_Rev9999.csv", TargetFileName);
            string corrTemplatePath = string.Format(@"C:\Avago.ATF.Common\Results\{0}_GuCorrTemplate_Rev9999.csv", TargetFileName);

            string[] CFHedaer = new string[] { "ParameterName", "Factor_Add_site1", "Factor_Add_LowLimit", "Factor_Add_HighLimit", "Factor_Multiply_site1", "Factor_Multiply_LowLimit", "Factor_Multiply_HighLimit" };

            using (StreamWriter swCF = new StreamWriter(cfPath))
            using (StreamWriter swTSF = new StreamWriter(tsfPath))
            using (StreamWriter swCorrTemplate = new StreamWriter(corrTemplatePath))
            {
                swCF.WriteLine(CFHedaer.JoinToString(","));
                swCorrTemplate.WriteLine(CFHedaer.JoinToString(","));

                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "#HEADER", "", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "TestMode", "Production", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "Title", TargetFileName, ""));
                swTSF.WriteLine(string.Format("{0},'{1:dd/MM/yyyy},{2},,,,,,,,,,,,", "Date", DateTime.Now, ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "Author", "Seoul NPI", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "Description", "Automated PoutLimit", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "SpecVersion", "1", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "#END", "", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "#", "", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "#CONTROL_PARAMETERS", "", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "TotalBinYieldAlarmLimit", "70", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "StopAfterBinLimitFail", "0", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "StopAfterAnyParaLimitFail", "0", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "StopRequiredUnitsCount", "9999999", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "ContinuousUnitsPassAlarmLimit", "9999999", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "ContinuousUnitsFailAlarmLimit", "9999999", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "MoveToQABinUnitsCount", "9999999", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "MoveToQAHwBinNum", "9999999", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "#END", "", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "#", "", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "#HWBIN_DEFINITION", "", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "1", "PASS_ALL+", "1"));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "2", "PASS_A", "2"));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "3", "PASS_B", "3"));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "4", "FAIL_A", "4"));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "5", "FAIL_ALL+", "5"));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "#END", "", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "#", "", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "#SWBIN_DEFINITION", "", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},{3},,,,,,,,,,,", "1", "PASS_ALL+", "OR", "1"));
                swTSF.WriteLine(string.Format("{0},{1},{2},{3},,,,,,,,,,,", "2", "PASS_A", "OR", "2"));
                swTSF.WriteLine(string.Format("{0},{1},{2},{3},,,,,,,,,,,", "3", "PASS_B", "OR", "3"));
                swTSF.WriteLine(string.Format("{0},{1},{2},{3},,,,,,,,,,,", "4", "FAIL_A", "OR", "4"));
                swTSF.WriteLine(string.Format("{0},{1},{2},{3},,,,,,,,,,,", "5", "FAIL_ALL+", "OR", "5"));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "#END", "", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "#", "", ""));
                swTSF.WriteLine(string.Format("{0},{1},{2},,,,,,,,,,,,", "#SERIAL_DEFINITION", "", ""));
                swTSF.WriteLine(string.Format(",,,,,1,,2,,3,,4,,5,"));
                swTSF.WriteLine(string.Format("TestNumber,TestParameter,ColumnDisplayFlag,ChartDisplayFlag,FailThresPercent,Min,Max,Min,Max,Min,Max,Min,Max,Min,Max"));

                for (int cfindex = 0; cfindex < data.Count; cfindex++)
                {
                    string paraName = data[cfindex];

                    swCF.WriteLine(string.Format("{0},0,-999999,999999,0,-999999,999999", paraName));

                    if (m_guAddItems.Any(t => paraName.CIvContainsAllOf(t.ToArray())))
                    {
                        swCorrTemplate.WriteLine(string.Format("{0},0.1,-999999,999999,0,-999999,999999", paraName));
                    }
                    else if (m_guMultiplyItems.Any(t => paraName.CIvContainsAllOf(t.ToArray())))
                    {
                        swCorrTemplate.WriteLine(string.Format("{0},0,-999999,999999,0.1,-999999,999999", paraName));
                    }
                    else
                    {
                        swCorrTemplate.WriteLine(string.Format("{0},0,-999999,999999,0,-999999,999999", paraName));
                    }

                    if (paraName.CIvContainsAllOf(m_plIncludeRule.ToArray()) && !paraName.CIvContainsAnyOf(m_plExcludeRule.ToArray()))
                    {
                        var isMatched = paraName.SplitToArray('_').FirstOrDefault(t => t.CIvEndsWith("dBm"));

                        float lsl = float.Parse(isMatched.Replace("dBm", "")) - poutLimitDelta;
                        float usl = float.Parse(isMatched.Replace("dBm", "")) + poutLimitDelta;

                        swTSF.WriteLine(string.Format("{0},{1},0,0,0,{2},{3},{2},{3},{2},{3},{2},{3},{4},{5}", cfindex + 1, paraName, lsl, usl, minLSL, maxUSL));
                    }
                    else
                    {
                        swTSF.WriteLine(string.Format("{0},{1},0,0,0,{2},{3},{2},{3},{2},{3},{2},{3},{2},{3}", cfindex + 1, paraName, minLSL, maxUSL));
                    }
                }
            }
        }
    }
}