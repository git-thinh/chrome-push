using System;
using System.Security.Permissions;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace chrome_push
{
    [PermissionSet(SecurityAction.LinkDemand, Name = "Everything"),
        PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public class app
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        static app()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (se, ev) =>
            {
                Assembly asm = null;
                string comName = ev.Name.Split(',')[0];
                string resourceName = @"DLL\" + comName + ".dll";
                var assembly = Assembly.GetExecutingAssembly();
                resourceName = typeof(app).Namespace + "." + resourceName.Replace(" ", "_").Replace("\\", ".").Replace("/", ".");
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        byte[] buffer = new byte[stream.Length];
                        using (MemoryStream ms = new MemoryStream())
                        {
                            int read;
                            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                                ms.Write(buffer, 0, read);
                            buffer = ms.ToArray();
                        }
                        asm = Assembly.Load(buffer);
                    }
                }
                return asm;
            };
        }

        static Dictionary<string, string> dicBookmark = new Dictionary<string, string>() { };
        public static void RUN()
        {
            var handle = GetConsoleWindow(); 
            // Hide
            ShowWindow(handle, SW_HIDE); 
            // Show
            //ShowWindow(handle, SW_SHOW);

            if (File.Exists("host.txt"))
            {
                dicBookmark = File.ReadAllLines("host.txt")
                    .Select(x => x.Trim())
                    .Where(x => x.Trim() != string.Empty)
                    .Select(x => x.Split('=')).Where(x => x.Length > 1)
                    .ToDictionary(x => x[0].Trim().ToLower(), x => x[1]);
            }

            var chrome = new Chrome("http://localhost:9222");

            var sessions = chrome.GetAvailableSessions();
            if (sessions.Count == 0) return;
            int index = -1;
            for (int i = 0; i < sessions.Count; i++)
            {
                if (sessions[i].url.StartsWith("http"))
                {
                    index = i;
                    break;
                }
            }
            if (index == -1) return;

            var uri = new Uri(sessions[index].url);
            string selector = string.Empty;
            if (dicBookmark.ContainsKey(uri.Host)) selector = dicBookmark[uri.Host];
            if (!string.IsNullOrEmpty(selector))
            {
                string[] a = selector.Split('¦');
                selector = a[0];
                string tag = string.Empty;
                if (a.Length > 1) tag = a[1];

                // Will drive first tab session
                var sessionWSEndpoint = sessions[index].webSocketDebuggerUrl;
                chrome.SetActiveSession(sessionWSEndpoint);

                string result = chrome.Eval("var s = ''; Array.from(document.querySelectorAll('" + selector + "')).forEach(function (it) { s += it.innerHTML; s += '<hr>' }); s;");
                chrome_data dt = JsonConvert.DeserializeObject<chrome_data>(result);
                string s = dt.result.result.value;
                if (string.IsNullOrEmpty(s))
                {
                    MessageBox.Show("Cannot get data from selector " + uri.Host + "=" + selector + " in file host.txt");
                    return;
                }

                string text = new Html2Text().Convert(s).Trim();
                string title = text.Split(new char[] { '\r', '\n' })[0];
                string file = TiengViet.convertToUnSign2(title);

                if (!string.IsNullOrEmpty(tag))
                {
                    result = chrome.Eval("var s = ''; Array.from(document.querySelectorAll('" + tag + "')).forEach(function (it) { s += it.innerText.trim(); s += ';' }); s;");
                    dt = JsonConvert.DeserializeObject<chrome_data>(result);
                    tag = dt.result.result.value;
                    if (tag != null) text = text.Replace("[§]", "§" + string.Join(";", tag.Split(';').Select(x => x.Trim()).Distinct()));
                }
                else text = text.Replace("[§]", string.Empty);

                Regex regex = new Regex("[^a-zA-Z0-9]", RegexOptions.None);
                file = regex.Replace(file, " ");
                regex = new Regex("[ ]{2,}", RegexOptions.None);
                file = regex.Replace(file, "-").Replace(" ", "-").ToLower() + "_" + uri.Host + ".txt";
                File.WriteAllText(file, text);
                //Console.Title = file;
                MessageBox.Show("OK: " + title);
            }
            else
            {
                string msg = "Cannot find setting for selector host[" + uri.Host + "] in file host.txt";
                MessageBox.Show(msg);
                string input = Microsoft.VisualBasic.Interaction.InputBox(msg, "Input config Selector:", "", -1, -1);
                while (string.IsNullOrWhiteSpace(input)) {
                    input = Microsoft.VisualBasic.Interaction.InputBox(msg, "Input config Selector:", "", -1, -1);
                }
                using (StreamWriter sw = File.AppendText("host.txt"))
                {
                    sw.WriteLine(string.Format("\r\n{0}={1}", uri.Host, input)); 
                }
                Application.Restart();
                Environment.Exit(0);
                return;
            }










            //Console.WriteLine("Available debugging sessions");
            //foreach (var s in sessions)
            //    Console.WriteLine(s.url);

            //if (sessions.Count == 0)
            //    throw new Exception("All debugging sessions are taken.");

            //////// Will drive first tab session
            //////var sessionWSEndpoint = 
            //////    sessions[0].webSocketDebuggerUrl;

            //////chrome.SetActiveSession(sessionWSEndpoint);

            //////chrome.NavigateTo("http://www.google.com");

            //////var result = chrome.Eval("document.getElementById('lst-ib').value='Hello World'");

            //////result = chrome.Eval("document.forms[0].submit()");

            //Console.ReadLine();
        }
    }

    class Program
    {
        //static string _path_root = AppDomain.CurrentDomain.BaseDirectory;
        static void Main(string[] args)
        {
            app.RUN();
        }
    }
}

