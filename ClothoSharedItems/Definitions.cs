using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ClothoSharedItems
{
    [Flags]
    public enum RunOption
    {
        None = 0,
        Read2DID = 1 << 0,
        Burn2DID = 1 << 1,
        SIMULATE = 1 << 2,
        RxFunctional = 1 << 3,
        SkipSelfCalibration = 1 << 4,
        SkipOTPBurn = 1 << 5,

        QCFAIL_LOG = 1 << 6,
        TracePoutDropPlots = 1 << 7,
        TraceCapEstimation = 1 << 8,
        TraceSwitchTime = 1 << 9,

        //For RF2 SNP Trace Enable
        ENA_SnPFile_Enable_Manual = 1 << 10,

        TracePDMCurrent = 1 << 11,
        GenPackageRF1 = 1 << 12,
        GenPackageRF2 = 1 << 13,
        GenPackageNFR = 1 << 14,
    }

    [Flags]
    public enum RunOptionControlItemsFromTCF
    {
        RxFunctional = 1 << 3,
        QCFAIL_LOG = 1 << 6,
        TracePoutDropPlots = 1 << 7,
        TraceCapEstimation = 1 << 8,
        TraceSwitchTime = 1 << 9,
        ENA_SnPFile_Enable_Manual = 1 << 10,
        TracePDMCurrent = 1 << 11,
        GenPackageRF1 = 1 << 12,
        GenPackageRF2 = 1 << 13,
        GenPackageNFR = 1 << 14,
    }

    [Flags]
    public enum eUSERTYPE { PRODUCTIONUSER = 0b0000, DEBUG = 0b0001, SUSER = 0b0010, PPUSER = 0b0100, VUSER = 0b1000 }

    [Flags]
    public enum eTesterType
    {
        None = 0,
        RF1 = 1 << 0,
        RF2 = 1 << 1,
        NFR = 1 << 2,
    }

    [Flags]
    public enum eLoadItems
    {
        QC = 1 << 0,
        Waveform = 1 << 1,
    }

    public class XmlPAConfigField
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class ATFConfigXmlField
    {
        public string name { get; set; }
        public string value { get; set; }
        public string type { get; set; }
    }

    public class DigitalOption
    {
        public bool EnableWrite0 { get; set; }
        public bool EnableRegWrite { get; set; }
        public List<int> RegWriteFrames { get; set; }
        public bool TDRtoTx { get; set; }
        public bool TDRtoRx { get; set; }

        public DigitalOption()
        {
            EnableWrite0 = true;
            EnableRegWrite = true;
            RegWriteFrames = new List<int>();
            TDRtoTx = false;
            TDRtoRx = true;
        }
    }

    public class ATFConfiguration
    {
        public IEnumerable<ATFConfigXmlField> ToolSection { get; set; }
        public IEnumerable<ATFConfigXmlField> SystemSection { get; set; }
        public IEnumerable<ATFConfigXmlField> UserSection { get; set; }

        public ATFConfiguration(XElement xdoc)
        {
            ToolSection = xdoc.Element("ToolSection").Elements("ConfigItem").Select(x => new ATFConfigXmlField
            {
                name = (string)x.Attribute("name"),
                value = (string)x.Attribute("value"),
                type = (string)x.Attribute("type")
            });

            SystemSection = xdoc.Element("SystemSection").Elements("ConfigItem").Select(x => new ATFConfigXmlField
            {
                name = (string)x.Attribute("name"),
                value = (string)x.Attribute("value"),
                type = (string)x.Attribute("type")
            });

            UserSection = xdoc.Element("UserSection").Elements("ConfigItem").Select(x => new ATFConfigXmlField
            {
                name = (string)x.Attribute("name"),
                value = (string)x.Attribute("value"),
                type = (string)x.Attribute("type")
            });
        }
    }
}