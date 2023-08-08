using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using NationalInstruments.SystemConfiguration;
using NationalInstruments.ModularInstruments.SystemServices.DeviceServices;

namespace NI_PXIe_Modules_Info
{
    public class HardwareProperty
    {
        /// <summary>
        /// IMPORTANT: Supported drivers to query is defined by this constant variable
        /// </summary>
        static readonly string[] driverNames = new string[] { "NI-DCPower", "NI-Digital", "NI-RFSA", "NI-HSDIO", "NI-SWITCH",
                                                     "NI-SCOPE", "NI-FGEN", "NI-DMM"};

        private Dictionary<string, string> listHardwareByDriverNames(SystemConfiguration session)
        {
            Dictionary<string, string> DriverVersionOf = new Dictionary<string, string>();

            var dVersionList = session.GetInstalledSoftwareComponents(ComponentTypeFilter.AllVisible, false);

            foreach (var dName in driverNames)
            {
                var dName2 = (dName == "NI-Digital") ? "NI-Digital Pattern" : dName;   // dVersionList requires full name search keyword for NI-Digital
                var dVersion = dVersionList.FirstOrDefault(v => v.Title.Contains(dName2));

                if (dVersion == null)
                    continue;

                var dCollection = new ModularInstrumentsSystem(dName).DeviceCollection;
                foreach (DeviceInfo d in dCollection)
                {
                    DriverVersionOf[d.Model] = dName + " " + dVersion.Version;
                }
            }

            return DriverVersionOf;
        }

        private void initializePropertyArrays()
        {
            AliasName = new string[Count];
            ProductName = new string[Count];
            SerialNumber = new string[Count];
            DriverVersion = new string[Count];
            FirmwareRevision = new string[Count];
            NextCalDate = new DateTime[Count];
        }

        /// <summary>
        /// Get the NI hardware devices information that currently connected to the specified TargetName.
        /// </summary>
        /// <param name="TargetName">Target name can be localhost (default) or IP address of the target</param>
        public HardwareProperty(String TargetName = "localhost")
        {
            // Initialize system configuration session based on TargetName
            var session = new SystemConfiguration(TargetName);

            // List down NI Product that is present and support calibration
            var filter = new Filter(session) { IsPresent = IsPresentType.Present, IsNIProduct = true, SupportsCalibration = true };
            var hwListByPresentCalAble = session.FindHardware(filter);

            // List down hardware by driver names 
            var hwListByDrivers = listHardwareByDriverNames(session);

            // Get the intersection list of above two lists
            var hwlist = from HardwareResource hw in hwListByPresentCalAble
                         where hwListByDrivers.ContainsKey(hw.ProductName)
                         select hw;

            // Build the hardware property arrays
            Count = hwlist.Count();
            initializePropertyArrays();

            int i = 0;
            foreach (var item in hwlist)
            {
                AliasName[i] = item.UserAlias;
                ProductName[i] = item.ProductName;
                SerialNumber[i] = item.SerialNumber;
                DriverVersion[i] = hwListByDrivers[ProductName[i]];

                try
                {
                    FirmwareRevision[i] = item.FirmwareRevision;
                }
                catch (SystemConfigurationException)
                {
                    FirmwareRevision[i] = "";
                }

                ProductResource productResource = item as ProductResource;

                // Found that PXIe-6570 will return exception although it has ExternalCalibrationDueDate property
                try
                {
                    NextCalDate[i] = productResource.ExternalCalibrationDueDate.ToLocalTime();
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Remain NextCalDate[i] = new DateTime()  
                }
                catch (SystemConfigurationException)
                {
                    // Hence ExternalCalibrationDate is used and is added 1 year calibration interval as stated in Specification document
                    if (ProductName[i].Contains("6570"))
                    {
                        NextCalDate[i] = productResource.ExternalCalibrationDate.AddYears(1).ToLocalTime();
                    }

                    else
                        NextCalDate[i] = DateTime.MinValue;
                }

                i++;
            }

        }

        public string[] AliasName
        {
            get;
            private set;
        }
        public string[] ProductName
        {
            get;
            private set;
        }
        public string[] SerialNumber
        {
            get;
            private set;
        }
        public string[] DriverVersion
        {
            get;
            private set;
        }
        public string[] FirmwareRevision
        {
            get;
            private set;
        }
        public DateTime[] NextCalDate
        {
            get;
            private set;
        }

        public int Count
        {
            get;
            private set;
        }

        public string[] ToStringArray()
        {
            string[] info = new string[Count];
            for (int i = 0; i < Count; i = i + 1)
            {
                info[i] = String.Format("{0};;{1};{2};{3};{4:dd/MM/yy}", AliasName[i], ProductName[i], SerialNumber[i], DriverVersion[i], NextCalDate[i]);
            }

            return info;
        }
        public override string ToString()
        {
            var info = new StringBuilder();
            for (int i = 0; i < Count; i = i + 1)
            {
                info.AppendFormat("{0};;{1};{2};{3};{4:dd/MM/yy}\n", AliasName[i], ProductName[i], SerialNumber[i], DriverVersion[i], NextCalDate[i]);
            }
            return info.ToString();
        }
    }

}
