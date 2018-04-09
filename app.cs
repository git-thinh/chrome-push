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

namespace chrome_push
{
    [PermissionSet(SecurityAction.LinkDemand, Name = "Everything"),
        PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public class app
    {
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
            var uri = new Uri(sessions[0].url);
            string selector = string.Empty;
            if (dicBookmark.ContainsKey(uri.Host)) selector = dicBookmark[uri.Host];
            if (!string.IsNullOrEmpty(selector))
            { 
                // Will drive first tab session
                var sessionWSEndpoint = sessions[0].webSocketDebuggerUrl; 
                chrome.SetActiveSession(sessionWSEndpoint); 

                string result = chrome.Eval("var el = document.querySelector('" + selector + "'); el.innerHTML;");
                chrome_data dt = JsonConvert.DeserializeObject<chrome_data>(result);
                string s = dt.result.result.value;
                string text = new Html2Text().Convert(s).Trim();
                string file = TiengViet.convertToUnSign2(text.Split(new char[] { '\r', '\n' })[0]);

                text += "\r\n§";

                Regex regex = new Regex("[^a-zA-Z0-9]", RegexOptions.None);
                file = regex.Replace(file, " ");
                regex = new Regex("[ ]{2,}", RegexOptions.None);
                file = regex.Replace(file, "-").Replace(" ","-").ToLower() + "." + uri.Host + ".txt";
                File.WriteAllText(file, text);
                Console.Title = file;
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

            Console.ReadLine();
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

