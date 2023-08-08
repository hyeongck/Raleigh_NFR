using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avago.ATF.StandardLibrary;

namespace MyProduct.MyDashLogger
{
    public class ClothoConfigurationDataObject
    {
        public string ClothoRootDir { get; set; }
        public string ConfigXmlPath { get; set; }

        public void Initialize()
        {
            ClothoRootDir = GetTestPlanPath();
            ConfigXmlPath = @"C:\Avago.ATF.3.1.4\System\Configuration\ATFConfig.xml";
        }

        private static string GetTestPlanPath()
        {
            string basePath = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_FULLPATH, "");

            if (basePath == "")   // Lite Driver mode
            {
                string tcfPath = ATFCrossDomainWrapper.GetStringFromCache(PublishTags.PUBTAG_PACKAGE_TCF_FULLPATH, "");

                int pos1 = tcfPath.IndexOf("TestPlans") + "TestPlans".Length + 1;
                int pos2 = tcfPath.IndexOf('\\', pos1);

                basePath = tcfPath.Remove(pos2);
            }

            return basePath + "\\";
        }
    }
}
