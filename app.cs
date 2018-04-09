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

        public static void RUN()
        { 
            var chrome = new Chrome("http://localhost:9222");

            var sessions = chrome.GetAvailableSessions();

            Console.WriteLine("Available debugging sessions");
            foreach (var s in sessions)
                Console.WriteLine(s.url);

            if (sessions.Count == 0)
                throw new Exception("All debugging sessions are taken.");

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

