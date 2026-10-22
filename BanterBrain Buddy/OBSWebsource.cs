using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// CODING RULES:
/// •	Local variables, private instance, static fields and method parameters should be camelCase.
/// •	Public instance fields, methods, constants, properties, events and classes should be PascalCase.
/// </summary>

namespace BanterBrain_Buddy
{

    internal class OBSWebsource
    {
        private static readonly log4net.ILog _bBBlog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private HttpListener listener;
        private Thread listenerThread;
        private SettingsManager UserSettingsManager = SettingsManager.Instance;

        private void StartListener()
        {
            //GetContext throws when the listener is stopped, on a raw thread that would take down
            //the whole app, so the loop must end gracefully instead
            try
            {
                while (listener.IsListening)
                {
                    var context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(o => HandleRequest(context));
                }
            }
            catch (HttpListenerException ex)
            {
                _bBBlog.Info($"Websource listener stopped: {ex.Message}");
            }
            catch (Exception ex)
            {
                _bBBlog.Error("Websource listener thread ended with an error: " + ex.Message);
            }
        }

        public void OBSStartServer()
        {
            //an empty prefix would make HttpListener throw on Start
            if (string.IsNullOrWhiteSpace(UserSettingsManager.Settings.WebsourceServer))
            {
                _bBBlog.Error("Websource server cannot start, no valid URL configured");
                return;
            }

            // Stop the current listener and listener thread if they're running
            if (listener?.IsListening == true)
            {
                listener.Stop();
                if (listenerThread != null)
                    listenerThread.Join();  // Wait for the listener thread to finish
            }

            listener = new HttpListener();
            listener.Prefixes.Add(UserSettingsManager.Settings.WebsourceServer);
            listener.Start();

            listenerThread = new Thread(StartListener);
            listenerThread.Start();
        }

        private void HandleRequest(HttpListenerContext context)
        {
            string filepath = "";
            string responseString = "";

            try
            {
                switch (context.Request.RawUrl)
                {
                    case "/timer":
                        filepath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/html/timer.html";
                        break;
                    case "/test":
                        filepath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/html/test.html";
                        break;
                    default:
                        //an unclosed response makes the requesting OBS browser source hang forever
                        CloseResponse(context, 404, "Not found");
                        return;
                }

                if (!File.Exists(filepath))
                {
                    CloseResponse(context, 404, "File not found");
                    return;
                }

                responseString = File.ReadAllText(filepath);

                // Modify the response string
                responseString = responseString.Replace("PLACEHOLDER", "New Text");

                CloseResponse(context, 200, responseString);
            }
            catch (Exception ex)
            {
                //this runs on a ThreadPool item, nothing may escape unnoticed
                _bBBlog.Error("Error handling websource request: " + ex.Message);
                try
                {
                    CloseResponse(context, 500, "Internal server error");
                }
                catch (Exception)
                {
                    //the client is probably already gone, nothing else to do
                }
            }
        }

        //writes and properly closes the response so the client never waits on an open stream
        private static void CloseResponse(HttpListenerContext context, int statusCode, string body)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(body);
            context.Response.StatusCode = statusCode;
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }


        public OBSWebsource()
        {
            _bBBlog.Info("OBSWebsource starting");
        }

    }
}
