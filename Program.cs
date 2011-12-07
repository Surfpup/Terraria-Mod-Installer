using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;
using System.IO;
using BsDiff;
using System.Resources;
using System.Reflection;
using Microsoft.Win32;
using Terraria;
namespace Patcher
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("tConfig Installer "+Constants.version);
            AppDomain.CurrentDomain.AssemblyResolve += FindAssem;

            string exeName = "TerrariaOriginalBackup.exe"; //"Terraria.exe";
            string steamPath = "";
            try
            {
                //RegistryKey regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE").OpenSubKey("Valve").OpenSubKey("Steam");
                //steamPath = (string)regKey.GetValue("InstallPath");
                RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE").OpenSubKey("Valve").OpenSubKey("Steam");
                steamPath = (string)regKey.GetValue("SteamPath");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error accessing registry: " + e);
                Console.WriteLine("Will proceed with current directory used instead.");
            }
            string folder = "";
            if(steamPath!="") folder=steamPath + @"\steamapps\common\terraria\";

            if (!File.Exists(folder + exeName))
            {
                Console.WriteLine(folder+exeName+" could not be found - cannot continue with the patch.\nDo you have the Terraria Game Launcher installed?\n Press Enter to exit.");
                Console.ReadLine();
                return;
            }
            //Check hash for Terraria.exe
            string expectedHash = "1139E2577BFEA5B5EC3DE444330FB09B48E26D4783601BFAFEC82ACC9D1BDE68"; //For 1.1 Terraria
                //"A5774195387F3AB230BDA08BBDF38DF32B4D3AE5C79425D5D04C8288C01D6570"; //Hash value for 1.0.6.1 Terraria

            byte[] data = File.ReadAllBytes(folder+exeName);
            byte[] result;
            SHA256 shaM = new SHA256Managed();
            result = shaM.ComputeHash(data);
            string actualHash = String.Concat(Array.ConvertAll(result, x => x.ToString("X2")));
            //File.WriteAllText("hash.txt", actualHash);
            if (expectedHash != actualHash)
            {
                Console.WriteLine("The hash value for "+exeName+" is incorrect. Your hash value is: "+actualHash+"\nPress Enter to exit.");
                Console.ReadLine();
                return;
            }

            Program p = new Program();
            if (!p.ApplyPatch(folder, exeName))
            {
                Console.WriteLine("Patch Failed! Press Enter to exit.");
                Console.ReadLine();
                return;
            }
            //Now we need to write out the launcher file with the hash of the mod

            data = File.ReadAllBytes(folder + "tConfig.exe");
            result = shaM.ComputeHash(data);
            string modHash = String.Concat(Array.ConvertAll(result, x => x.ToString("X2")));
            File.WriteAllText(folder + @"tConfig.gli", "1.1 tConfig "+Constants.version+"\n" + modHash);

            //Write out the DLL files!

            File.WriteAllBytes(folder + @"Microsoft.Xna.Framework.dll", Patch.Microsoft_Xna_Framework);
            File.WriteAllBytes(folder + @"Microsoft.Xna.Framework.Game.dll", Patch.Microsoft_Xna_Framework_Game);
            File.WriteAllBytes(folder + @"Microsoft.Xna.Framework.Graphics.dll", Patch.Microsoft_Xna_Framework_Graphics);
            File.WriteAllBytes(folder + @"Microsoft.Xna.Framework.Xact.dll", Patch.Microsoft_Xna_Framework_Xact);
            File.WriteAllText(folder + @"bsdiff .net readme.txt", Patch.bsdiff__net_ReadMe);
            

            Console.WriteLine("Patch successful! Press Enter to exit.");
            Console.ReadLine();
        }
        static Assembly FindAssem(object sender, ResolveEventArgs args)
        {
            string simpleName = new AssemblyName(args.Name).Name;
            string path = simpleName + ".dll";
            if(path=="ICSharpCode.SharpZipLib.dll") {
                return Assembly.Load(Patch.ICSharpCode_SharpZipLib);
            }
            if (path == "Logos.Utility.dll")
            {
                return Assembly.Load(Patch.Logos_Utility);
            }
            else return null;
        }
        public bool ApplyPatch(string folder, string exeName)
        {
            string oldFile = folder+exeName;
            string newFile = folder+@"tConfig.exe";
            try {
                using (FileStream input = new FileStream(oldFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (FileStream output = new FileStream(newFile, FileMode.Create))
                    BinaryPatchUtility.Apply(input, () => new MemoryStream(Patch.TerrariaConfigMod), output);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return false;
            }
            return true;
        }
    }
}
