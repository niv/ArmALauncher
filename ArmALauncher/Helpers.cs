using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmALauncher
{
    static class Helpers
    {

        public static string LookupRegistry(string key, params string[] path)
        {
            return path.
                Select(n => (string)Registry.GetValue(n, key, null)).
                Where(n => n != null).
                FirstOrDefault();
        }

        public static bool CopyDirectoryTree(this DirectoryInfo source, DirectoryInfo dest)
        {
            bool wasModified = false;

            if (!Directory.Exists(dest.FullName))
                Directory.CreateDirectory(dest.FullName);

            foreach (FileInfo file in source.EnumerateFiles())
            {
                var fileDest = Path.Combine(dest.ToString(), file.Name);
                if (!File.Exists(fileDest))
                {
                    file.CopyTo(fileDest);
                    wasModified = true;
                }
            }

            foreach (DirectoryInfo subDirectory in source.GetDirectories())
            {
                var dirDest = Path.Combine(dest.ToString(), subDirectory.Name);
                DirectoryInfo newDirectory = dest.CreateSubdirectory(subDirectory.Name);
                if (CopyDirectoryTree(subDirectory, newDirectory))
                    wasModified = true;
            }

            return wasModified;
        }
    }
}
