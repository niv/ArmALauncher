using Microsoft.Win32;
using Nini.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArmALauncher
{
    static class Program
    {
        private static string lookupRegistry(string key, params string[] path)
        {
            return path.
                Select(n => (string) Registry.GetValue(n, key, null)).
                Where(n => n != null).
                FirstOrDefault();
        }

        [STAThread]
        static void Main(string[] va)
        {
            var fi = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var configFileName = fi.DirectoryName + "\\" + fi.Name.Replace(fi.Extension, "") + ".ini";
            var configSource = File.Exists(configFileName) ? new IniConfigSource(configFileName) : new IniConfigSource();
            var config = configSource.Configs["main"] != null ? configSource.Configs["main"] : configSource.AddConfig("main");

            var arma2path = config.Get("arma2path");
            if (null == arma2path)
            {
                arma2path = lookupRegistry("main",
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\bohemia interactive studio\\arma 2",
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\bohemia interactive studio\\arma 2");
                config.Set("arma2path", arma2path);

            }

            var arma2oapath = config.Get("arma2oapath");
            if (null == arma2oapath)
            {
                arma2oapath = lookupRegistry("main",
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\bohemia interactive studio\\arma 2 oa",
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\bohemia interactive studio\\arma 2 oa");
                config.Set("arma2oapath", arma2oapath);
            }

            var ts3path = config.Get("ts3path");
            if (null == ts3path)
            {
                ts3path = lookupRegistry("",
                    "HKEY_CURRENT_USER\\SOFTWARE\\Wow6432Node\\TeamSpeak 3 Client",
                    "HKEY_CURRENT_USER\\SOFTWARE\\TeamSpeak 3 Client",
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\TeamSpeak 3 Client",
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\TeamSpeak 3 Client");
                config.Set("ts3path", ts3path);
            }

            var additionalArguments = config.Get("additionalArguments", "-world=empty -noSplash -noFilePatching");
            config.Set("additionalArguments", additionalArguments);

            var additionalMods = config.Get("additionalMods", "");
            config.Set("additionalMods", additionalMods);

            var launchRoot = config.Get("launchRoot", fi.DirectoryName + "\\beta");
            config.Set("launchRoot", launchRoot);

            configSource.Save(configFileName);



            if (arma2path == null || !Directory.Exists(arma2path))
            {
                MessageBox.Show("Cannot find arma2. Please edit " + configFileName + " manually.");
                return;
            }

            if (arma2oapath == null || !Directory.Exists(arma2oapath))
            {
                MessageBox.Show("Cannot find arma2oa. Please edit " + configFileName + " manually.");
                return;
            }

            if (ts3path == null || !Directory.Exists(ts3path))
            {
                MessageBox.Show("Cannot find ts3. Please edit " + configFileName + " manually.");
                return;
            }

            if (va.Length == 0)
            {
                var r = MessageBox.Show("This is just a small launcher that starts arma2 for you. " +
                    "It expects the mods as arguments, which is what catflap.exe does. " +
                    "Do you want to test starting ArmA2 without any mods?", "Now what?", MessageBoxButtons.YesNo);
                if (r == DialogResult.No)
                    return;
            }

            // Deploy ACRE stuff. The directory name is hardcoded for now, since ACRE seems to depend on that anyways.
            if (Directory.Exists("@acre"))
            {
                string targetDir = arma2oapath + "\\userconfig\\acre";
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                    foreach (string fn in Directory.GetFiles("@acre\\userconfig"))
                    {
                        string tgFn = targetDir + "\\" + new FileInfo(fn).Name;
                        Console.WriteLine("deploying userconfig: " + tgFn);
                        File.Copy(fn, tgFn);
                    }
                    MessageBox.Show("I've deployed a new ACRE userconfig to\n\n" + targetDir + "\n\nThis is a one-time thing. " +
                        "You can configure your radio keys in there if you want to change them (do so now and then press OK to start the game).");
                }

                // Deploy the plugin DLL.
                Console.WriteLine("ts3 is at " + ts3path);
                string pluginName = null;
                if (File.Exists(ts3path + "\\ts3client_win64.exe"))
                    pluginName = "acre_win64.dll";
                else if (File.Exists(ts3path + "\\ts3client_win32.exe"))
                    pluginName = "acre_win32.dll";
                else
                {
                    MessageBox.Show("Tried to install the acre plugin dll for you, but cannot find the TS3 binary. " +
                        "That's a bug in this launcher, please report. You can edit " + configFileName + " meanwhile to adjust the teamspeak path.");
                    return;
                }

                string pluginTarget = ts3path + "\\plugins\\" + pluginName;

                if (!File.Exists(pluginTarget))
                {
                    Console.WriteLine("deploying acre plugin: " + pluginTarget);
                    File.Copy("@acre\\plugin\\" + pluginName, pluginTarget);
                    MessageBox.Show("I've deployed the ACRE plugin to\n\n" + pluginTarget + "\n\nThis is a one-time thing. " +
                        "Please check in your teamspeak install if it's loaded, and restart it if neccessary (as admin!).");

                }
            }

            if (Directory.Exists("@ace"))
            {
                // TODO: Start Clippi as well.
            }

            string mod = arma2path + ";expansion";

            mod += ";" + launchRoot;
            mod += ";" + launchRoot + "\\expansion";

            if (va.Length > 0)
                mod += ";" + va[0];

            mod += ";" + additionalMods;

            Console.WriteLine("mod = " + mod);

            string args = additionalArguments + " ";
            args += "\"-mod=" + mod + "\"";
            for (int i = 1; i < va.Length; i++)
                args += " " + va[i];

            Process pProcess = new System.Diagnostics.Process();
            pProcess.StartInfo.FileName = launchRoot + "\\arma2oa.exe";
            pProcess.StartInfo.Arguments = args;
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.WorkingDirectory = arma2oapath;

            try
            {
                pProcess.Start();
                pProcess.WaitForExit();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error starting arma2: " + e.Message, "Oopsies.");
            }            
        }
    }
}
