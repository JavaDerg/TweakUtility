using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using TweakUtility.Forms;
using TweakUtility.TweakPages;

using Newtonsoft.Json;
using TweakUtility.Attributes;

namespace TweakUtility
{
    internal static class Program
    {
        public static RegistryKey LocalMachine;
        public static RegistryKey CurrentUser;

        public static Config Config { get; private set; }

        public static List<TweakPage> Pages { get; private set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            LocalMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, GetRegistryView());
            CurrentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, GetRegistryView());

            LoadConfig();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //Pages has to be initialized after
            //Application.SetCompatibleTextRenderingDefault(), since TweakPageView
            //would already initialize and cause Windows Forms to error out
            Pages = new List<TweakPage>()
            {
                new CustomizationPage(),
                new InternetExplorerPage(),
                new SnippingToolPage(),
                new AdvancedPage(),
                new UncategorizedPage()
            };

            if (Debugger.IsAttached)
            {
                Pages.Add(new DebugPage());
            }

            using (var splash = new SplashForm())
            {
                Application.Run(splash);
            }

            Application.Run(new MainForm());
        }

        /// <summary>
        /// Finds a running Windows Explorer instance and causes it restart.
        /// </summary>
        public static void RestartExplorer()
        {
            try
            {
                IntPtr handle = NativeMethods.FindWindow("Shell_TrayWnd", null);
                NativeMethods.PostMessage(handle, NativeMethods.WM_USER + 436, (IntPtr)0, (IntPtr)0);

                while (true)
                {
                    handle = NativeMethods.FindWindow("Shell_TrayWnd", null);

                    if (handle.ToInt32() == 0)
                    {
                        break;
                    }
                }
            }
            finally
            {
                Process.Start(new ProcessStartInfo("explorer.exe")
                {
                    UseShellExecute = true
                });
            }
        }

        /// <summary>
        /// Finds a suitable registry view for this system architecture
        /// </summary>
        private static RegistryView GetRegistryView() => Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;

        public static int ToBgrInt(this Color color) => (0 << 24) + (color.B << 16) + (color.G << 8) + color.R;

        public static Color ToBgrColor(this int bgrColor)
        {
            byte[] bytes = BitConverter.GetBytes(bgrColor);
            return Color.FromArgb(bytes[0], bytes[1], bytes[2]);
        }

        /// <summary>
        /// Loads the configuration of TweakUtility
        /// </summary>
        /// <remarks>Creates a new one if it doesn't exist yet.</remarks>
        private static void LoadConfig()
        {
            if (!File.Exists("config.json"))
            {
                Config = new Config();
                SaveConfig();
            }

            string json = File.ReadAllText("config.json");
            Config = JsonConvert.DeserializeObject<Config>(json);
        }

        public static void SaveConfig()
        {
            string json = JsonConvert.SerializeObject(Config);
            File.WriteAllText("config.json", json);
        }
    }
}


/// <summary>
/// Opens the GitHub Issues page of TweakUtility, preset with exception details.
/// </summary>
public static void SendCrashReport(Exception ex)
{
    string title = HttpUtility.UrlEncode(ex.Message);
    string body = $"***Please make sure this report doesn't contain any personal details accidentally picked up by this program.***\n\n"
        + $"**Message**\n{ex.Message}\n\n"
        + $"**Source**\n{ex.Source}\n\n"
        + $"**Stack Trace**\n```{ex.StackTrace}```\n\n";

    body = HttpUtility.UrlEncode(body);

    string url = $"https://github.com/Craftplacer/TweakUtility/issues/new?labels=crash+report&title={title}&body={body}";

    OpenURL(url);
}
