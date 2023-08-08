using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClothoSharedItems.Common
{
    public static class Common
    {
        public static string FindADBPath(string pathFolder = null, bool searchParentFolder = false)
        {
            var dirListToSearch = new List<DirectoryInfo>();

            if (pathFolder == null)
                pathFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            else
            {
                if (!Directory.Exists(pathFolder)) return null;
            }

            if (File.Exists(Path.Combine(pathFolder, "adb.exe")))
            {
                return pathFolder;
            }
            else
            {
                var dirNow = new DirectoryInfo(pathFolder);

                if (searchParentFolder)
                {
                    while (dirNow != null)
                    {
                        dirListToSearch.Add(dirNow);
                        dirNow = dirNow.Parent;
                    }
                }
                else dirListToSearch.Add(dirNow);

                foreach (var dirToSearch in dirListToSearch)
                {
                    foreach (var pathCadidate in dirToSearch.GetDirectories("*ADB*").Select(di => di.FullName))
                    {
                        if (File.Exists(Path.Combine(pathCadidate, "adb.exe")))
                            return pathCadidate;
                    }
                }
            }
            return null;
        }
    }
}