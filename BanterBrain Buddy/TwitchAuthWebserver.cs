using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// CODING RULES:
/// •	Local variables, private instance, static fields and method parameters should be camelCase.
/// •	Methods, constants, properties, events and classes should be PascalCase.
/// </summary>

namespace BanterBrain_Buddy
{

    public class Authorization(string code)
    {
        public string Code { get; } = code;
    }

    public class TwitchAuthWebserver
    {
        private static readonly log4net.ILog _bBBlog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly HttpListener _listener;

        //HttpListener URL prefixes conflict machine-wide. An abandoned authorization attempt (browser
        //closed before approving, or Broadcaster and Bot authorized back-to-back) leaves the previous
        //listener's prefix still registered, so the next Start() throws "conflicts with an existing
        //registration" and the browser falls through to an HTTP.sys 404 page. Keep a single static
        //handle so every new server can release the old registration before listening again.
        private static HttpListener _activeListener;

        private static void ReleaseActiveListener()
        {
            var old = _activeListener;
            if (old == null) return;
            _activeListener = null;
            try { if (old.IsListening) old.Stop(); } catch (Exception ex) { _bBBlog.Debug("Stop previous listener: " + ex.Message); }
            try { old.Abort(); } catch { }
            try { old.Close(); } catch { }
        }

        public TwitchAuthWebserver(string uri)
        {
            ReleaseActiveListener();
            _listener = new HttpListener();
            if (uri.Last() != '/')
            {
                //HttpListener requires the prefix to end with '/'. Bind to the root of the configured
                //URL so it matches the plain redirect URI (e.g. http://localhost:8080) sent to Twitch.
                uri += "/";
            }
            _bBBlog.Info("Listening on: " + uri);
            _listener.Prefixes.Add(uri);
            _activeListener = _listener;
        }

        public async Task<Authorization> AuthorizationListen()
        {
            _bBBlog.Info("Authorization code OAUTH starting");
            StartListenerOrThrow();
            var result = await OnAuthorizationRequest();
            return result;
        }

        public async Task<Authorization> ImplicitListen()
        {
            _bBBlog.Info("Implicit grant OAUTH starting");
            StartListenerOrThrow();
            var result = await OnImplicitRequest();
            return result;
        }

        //If Start still fails after the previous registration was released (a stray BanterBrain process
        //or another app holds the port), free our own half-registered listener so it doesn't linger and
        //surface as a 404, then rethrow so the UI reports the real reason.
        private void StartListenerOrThrow()
        {
            try
            {
                _listener.Start();
            }
            catch (HttpListenerException ex)
            {
                _bBBlog.Error("Failed to start auth listener: " + ex.Message);
                ReleaseCurrentListener();
                throw;
            }
        }

        //Fully deregister the HTTP.sys prefix (Stop alone is not enough) and clear the static handle.
        private void ReleaseCurrentListener()
        {
            try { if (_listener.IsListening) _listener.Stop(); } catch { }
            try { _listener.Abort(); } catch { }
            try { _listener.Close(); } catch { }
            if (ReferenceEquals(_activeListener, _listener))
                _activeListener = null;
        }

        public void Stop()
        {
            _bBBlog.Debug("Stopping listener");
            ReleaseCurrentListener();
        }   

        private async Task<Authorization> OnImplicitRequest()
        {
            bool firstTime = true;
            while (_listener.IsListening)
            {
                try
                {
                    HttpListenerContext ctx = await _listener.GetContextAsync();
                    var req = ctx.Request;
                    var resp = ctx.Response;

                    using var writer = new StreamWriter(resp.OutputStream);
                    //so, with implicit flow, the first time we get back an URl that can only be read by the browser context
                    //because it has #, we rewrite that in a Uri resource (changing # to ?) and call ourselves again.
                    //The second time the parser will pick up the code correctly then.
                    if (firstTime)
                    {
                        //its a hack cos implicit gets a #access_token instead of ?code so we do some rewrite magic
                        //and redirect ourselves to the new url
                        string response = "<!DOCTYPE html>\r\n" +
                            "<html>\r\n " +
                            "<p id=\"demo\">placeholder.</p>\r\n" +
                            "<body>\r\n" +
                            "<script>\r\n" +
                            "let Url = window.location.href;\r\n" +
                            "let newUrl = Url.replace(\"#\",\"?\");\r\n" +
                            "window.location.replace(newUrl);\r\n" +
                            "document.getElementById(\"demo\").innerHTML=`Browser should refresh if not click <a href=\\\"${newUrl}\\\">here</a>`;\r\n" +
                            "</script> \r\n" +
                            "</body>\r\n" +
                            "</html>";
                        byte[] encoded = Encoding.UTF8.GetBytes(response);
                        resp.ContentLength64 = encoded.Length;
                        resp.OutputStream.Write(encoded, 0, encoded.Length);
                        resp.OutputStream.Close();
                        firstTime = false;
                    }
                    else if (req.QueryString.AllKeys.Any("access_token".Contains) && !firstTime)
                    {
                        writer.WriteLine("Implicit grant started! Check your application! You can close this window! Remember to log out on Twitch if you want to authorize other accounts");
                        _bBBlog.Info("OAUTH Implcit grant started! Check your application!");
                        writer.Flush();
                        return new Authorization(req.QueryString["access_token"]);
                    }
                    else
                    {
                        _bBBlog.Info("No access_token found in query string! Problem with OAUTH");
                        writer.WriteLine("No access_code found in query string! Problem with OAUTH");
                        writer.Flush();
                    }
                }
                catch (Exception ex)
                {
                    _bBBlog.Error("Webserver: " + ex.ToString());
                    ReleaseCurrentListener();
                }

            }
            return null;
        }

        //this for general authorization style requests
        private async Task<Authorization> OnAuthorizationRequest()
        {
            while (_listener.IsListening)
            {
                try
                {
                    HttpListenerContext ctx = await _listener.GetContextAsync();
                    var req = ctx.Request;
                    var resp = ctx.Response;

                    using var writer = new StreamWriter(resp.OutputStream);
                    if (req.QueryString.AllKeys.Any("code".Contains))
                    {
                        writer.WriteLine("Authorization started! Check your application! You can close this window!");
                        _bBBlog.Info("OAUTH Authorization started! Check your application!");
                        writer.Flush();
                        return new Authorization(req.QueryString["code"]);
                    }
                    else
                    {
                        _bBBlog.Info("No code found in query string! Problem with OAUTH");
                        writer.WriteLine("No code found in query string! Problem with OAUTH");
                        writer.Flush();
                    }
                }
                catch (Exception ex)
                {
                    _bBBlog.Error("Webserver: " + ex.ToString());
                    ReleaseCurrentListener();
                }

            }
            return null;
        }
    }

}
