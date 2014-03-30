using Microsoft.Win32;
using Nini.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArmALauncher
{
    static class Program
    {
        private static LogForm logForm = new LogForm();

        private static void Log(string text)
        {
            logForm.Log(text);
        }

        [STAThread]
        static void Main(string[] va)
        {
            try
            {
                runApp(va);
            } catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Error running game");
                return;
            }
        }

        private static void runApp(string[] va)
        {
            Log("Hello!");
            var fi = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var configFileName = fi.DirectoryName + "\\" + fi.Name.Replace(fi.Extension, "") + ".ini";
            var configSource = File.Exists(configFileName) ? new IniConfigSource(configFileName) : new IniConfigSource();
            var config = configSource.Configs["main"] != null ? configSource.Configs["main"] : configSource.AddConfig("main");

            var showLogWindow = config.Get("showLogWindow");
            if (showLogWindow == null)
                config.Set("showLogWindow", "false");

            if (showLogWindow != null && showLogWindow != "false")
                logForm.Show();

            var arma2path = config.Get("arma2path");
            if (null == arma2path)
            {
                arma2path = Helpers.LookupRegistry("main",
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\bohemia interactive studio\\arma 2",
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\bohemia interactive studio\\arma 2");
                if (arma2path != null)
                    config.Set("arma2path", arma2path);
                Log("ArmA 2 is at: " + arma2path);
            }

            var arma2oapath = config.Get("arma2oapath");
            if (null == arma2oapath)
            {
                arma2oapath = Helpers.LookupRegistry("main",
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\bohemia interactive studio\\arma 2 oa",
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\bohemia interactive studio\\arma 2 oa");
                if (arma2oapath != null)               
                    config.Set("arma2oapath", arma2oapath);
            }
            Log("ArmA 2 OA is at: " + arma2oapath);

            var arma3path = config.Get("arma3path");
            if (null == arma3path)
            {
                arma3path = Helpers.LookupRegistry("main",
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\bohemia interactive\\arma 3",
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\bohemia interactive\\arma 3");
                if (arma3path != null)               
                    config.Set("arma3path", arma3path);
            }
            Log("ArmA 3 is at: " + arma3path);

            var ts3path = config.Get("ts3path");
            if (null == ts3path)
            {
                ts3path = Helpers.LookupRegistry("",
                    "HKEY_CURRENT_USER\\SOFTWARE\\Wow6432Node\\TeamSpeak 3 Client",
                    "HKEY_CURRENT_USER\\SOFTWARE\\TeamSpeak 3 Client",
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\TeamSpeak 3 Client",
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\TeamSpeak 3 Client");
                if (ts3path != null)
                    config.Set("ts3path", ts3path);
            }
            Log("TS3 is at: " + ts3path);

            var additionalArguments = config.Get("additionalArguments", "-world=empty -noSplash -noFilePatching");
            config.Set("additionalArguments", additionalArguments);

            var additionalMods = config.Get("additionalMods", "");
            config.Set("additionalMods", additionalMods);

            configSource.Save(configFileName);

            if (ts3path == null || !Directory.Exists(ts3path))
            {
                MessageBox.Show("Cannot find ts3. Please edit " + configFileName + " manually.");
                return;
            }

            Process processAceClippi = null;
            // Start clippy if it isn't running yet
            if (File.Exists("@ace_ark\\clippi\\aceclippi.exe"))
            {
                if (0 == Process.GetProcessesByName("aceclippi").Length)
                {
                    processAceClippi = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = "@ace_ark\\clippi\\aceclippi.exe";
                    processAceClippi.StartInfo = startInfo;
                }
            }

            if (va.Length < 1)
            {
                MessageBox.Show("This is just a small launcher that starts the game for you. " +
                    "It expects the gametype as it's first argument, and the mods as the second. " +
                    "Any further arguments are added as-is.\n\nPlease use the updater to launch instead.", "Cannae do.");
                return;
            }

            Process pProcess = new System.Diagnostics.Process();

            string mod = "";

            if (va[0] == "arma2")
            {
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

                DeployUserconfig(arma2oapath);
                DeployTS3DLL("@acre", ts3path);

                mod = arma2path + ";expansion";

                if (Directory.Exists("beta"))
                {
                    mod += ";" + Path.GetFullPath("beta");
                    mod += ";" + Path.GetFullPath("beta") + "\\expansion";
                    pProcess.StartInfo.FileName = Path.GetFullPath("beta") + "\\arma2oa.exe";
                }
                else
                    pProcess.StartInfo.FileName = arma2oapath + "\\arma2oa.exe";

                pProcess.StartInfo.WorkingDirectory = arma2oapath;                
            }
            else if (va[0] == "arma3")
            {
                if (arma3path == null || !Directory.Exists(arma3path))
                {
                    MessageBox.Show("Cannot find arma3. Please edit " + configFileName + " manually.");
                    return;
                }

                DeployUserconfig(arma3path);
                // DeployTS3DLL("@acre2", ts3path);

                mod = "";
                pProcess.StartInfo.FileName = arma3path + "\\arma3.exe";
                pProcess.StartInfo.WorkingDirectory = arma3path;
            }
            else
            {
                MessageBox.Show("Unsupported game type: " + va[0], "Usage error/manifest error");
                return;
            }

            if (va.Length > 0)
                mod += ";" + va[1];

            mod += ";" + additionalMods;

            string args = additionalArguments + " ";
            args += "\"-mod=" + mod + "\"";
            for (int i = 2; i < va.Length; i++)
                args += " " + va[i];

            pProcess.StartInfo.Arguments = args;
            pProcess.StartInfo.UseShellExecute = false;

            Log("va: " + args);

            Log("==============");
            Log("Starting game!");

            try
            {
                if (processAceClippi != null)
                    processAceClippi.Start(); 

                pProcess.Start();
                Log("Waiting for ArmA to exit.");
                pProcess.WaitForExit();

                if (processAceClippi != null && !processAceClippi.HasExited)
                {
                    processAceClippi.CloseMainWindow();
                    for (int i = 0; i < 5; i++)
                    {
                        Log("Waiting for ACE Clippi to exit .. " + (4-i));
                        Thread.Sleep(1000);
                        if (processAceClippi.HasExited)
                            break;
                    }
                    if (!processAceClippi.HasExited)
                    {
                        Log("Force-killing it.");
                        processAceClippi.Kill();

                    }
                    processAceClippi.WaitForExit();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error starting arma: " + e.Message, "Oopsies.");
            }
        }

        private static void DeployUserconfig(string rootPath)
        {
            // Deploy all userconfig/ items to the destination, not overwriting.
            // Log("Deploying userconfigs to " + rootPath);

            foreach (var dir in Directory.GetDirectories(".", "@*", SearchOption.TopDirectoryOnly).
                Where(x => Directory.Exists(x) && Directory.Exists(x + "\\userconfig\\")).
                Select(x => x + "\\userconfig\\"))
            {
                string destDir = rootPath + "\\userconfig";
                var dest = new DirectoryInfo(destDir);

                var source = new DirectoryInfo(dir);

                bool wasDeployed = source.CopyDirectoryTree(dest);

                if (wasDeployed)
                {
                    Log("userconfig: deployed " + dir + " to " + destDir);
                    MessageBox.Show("I've deployed " + dir + " to\n\n" + destDir + "\n\nThis is a one-time thing. " +
                        "You can configure mod-specific settings in there if you want to change them (do so now and then press OK to start the game).");
                }
            }
        }

        private static void DeployTS3DLL(string modPath, string ts3path)
        {
            // Deploy ACRE stuff. The directory name is hardcoded for now, since ACRE seems to depend on that anyways.
            if (Directory.Exists(modPath))
            {
                // Deploy the plugin DLL.
                string pluginName = null;
                if (File.Exists(ts3path + "\\ts3client_win64.exe"))
                    pluginName = "acre_win64.dll";
                else if (File.Exists(ts3path + "\\ts3client_win32.exe"))
                    pluginName = "acre_win32.dll";
                else
                {
                    throw new Exception("Tried to install the acre plugin dll for you, but cannot find the TS3 binary. " +
                        "That's a bug in this launcher, please report. You can edit the config file meanwhile to adjust the teamspeak path.");
                }

                string pluginTarget = ts3path + "\\plugins\\" + pluginName;

                if (!File.Exists(pluginTarget))
                {
                    var ret = MessageBox.Show("I want to deploy the " + modPath + " teamspeak DLL to " + pluginTarget + "." +
                        "Is this the correct path? if NOT, press Cancel now and edit the configuration ini (or set deploy_acre to false).",
                        "Continue?", MessageBoxButtons.OKCancel);

                    if (ret == DialogResult.Cancel)
                        throw new Exception("Aborted");

                    Log("deploying acre plugin: " + pluginTarget);
                    File.Copy("@acre\\plugin\\" + pluginName, pluginTarget);
                    MessageBox.Show("I've deployed the ACRE plugin to\n\n" + pluginTarget + "\n\nThis is a one-time thing. " +
                        "Please check in your teamspeak install if it's loaded, and restart it if neccessary (as admin!).");
                }
            }
        }

    }
}