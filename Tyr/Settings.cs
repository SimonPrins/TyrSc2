using System;
using System.Collections.Generic;
using System.Text;
using Tyr.Util;

namespace Tyr
{
    public class Settings
    {
        private static string[] Lines;
        private static void Initialize()
        {
            if (Lines == null)
                Lines = FileUtil.ReadSettingsFile();
        }
        public static bool ExtendTime()
        {
            Initialize();
            foreach (string line in Lines)
            {
                string[] setting = line.Split('=');
                if (setting.Length != 2)
                    continue;
                if (setting[0].Trim() != "extendTime")
                    continue;
                if (setting[1].Trim() == "true")
                    return true;
            }
            return false;
        }
        public static bool IgnoreMissingDataFiles()
        {
            Initialize();
            foreach (string line in Lines)
            {
                string[] setting = line.Split('=');
                if (setting.Length != 2)
                    continue;
                if (setting[0].Trim() != "ignoreMissingDataFiles")
                    continue;
                if (setting[1].Trim() == "true")
                    return true;
            }
            return false;
        }
        public static bool ArchonMode()
        {
            Initialize();
            foreach (string line in Lines)
            {
                string[] setting = line.Split('=');
                if (setting.Length != 2)
                    continue;
                if (setting[0].Trim() != "archonMode")
                    continue;
                if (setting[1].Trim() == "true")
                    return true;
            }
            return false;
        }

        public static bool MapAllowed(string mapName)
        {
            Initialize();
            foreach (string line in Lines)
            {
                if (line.Trim() == "map " + mapName)
                    return true;
                string[] setting = line.Split('=');
                if (setting.Length != 2)
                    continue;
                if (setting[0].Trim() != "allowAllMaps")
                    continue;
                if (setting[1].Trim() == "true")
                    return true;
            }
            return false;
        }

        public static string ResultsFilePrefix()
        {
            Initialize();
            foreach (string line in Lines)
            {
                string[] setting = line.Split('=');
                if (setting.Length != 2)
                    continue;
                if (setting[0].Trim() != "resultsFilePrefix")
                    continue;
                return setting[1].Trim();
            }
            return "";
        }

    }
}
