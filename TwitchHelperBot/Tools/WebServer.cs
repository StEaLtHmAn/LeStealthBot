using System.Net;
using System.Text;
using System.IO;
using System.Reflection;
using System.Web;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace LeStealthBot
{
    public class HttpServer
    {
        private Task task;
        private static HttpListener listener;
        private static bool runServer = false;
        public static string defaultPageData =
            "<!DOCTYPE>" +
            "<html>" +
            "<head>" +
            "<title>LeStealthBot - Overlay</title>" +
            "</head>" +
            "<body>" +
            "Overlay Error" +
            "</body>" +
            "</html>";

        public void HandleIncomingConnections()
        {
            string overlayName = string.Empty;
            string overlayDirectory = string.Empty;
            string dataFilePath = string.Empty;
            byte[] responseData = new byte[0];
            HttpListenerRequest req = null;
            HttpListenerResponse resp = null;
            HttpListenerContext ctx = null;
            NameValueCollection QueryData = null;

            while (runServer)
            {
                // Will wait here until we hear from a connection
                ctx = listener.GetContext();
                req = ctx.Request;
                resp = ctx.Response;

                //determine response by using the last segment of the url
                //
                try
                {
                    QueryData = HttpUtility.ParseQueryString(req.Url.Query);
                    //get overlay name from query string
                    if (QueryData.Get("overlay") != null)
                    {
                        overlayName = QueryData.Get("overlay");
                        //get the overlay directory
                        overlayDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Overlays", overlayName) + "\\";
                    }
                    //check if required to serve a file
                    dataFilePath = Path.Combine(overlayDirectory, req.Url.AbsolutePath.Replace("/", "\\").TrimStart('\\'));
                    if (File.Exists(dataFilePath))
                    {
                        responseData = File.ReadAllBytes(dataFilePath);
                        resp.ContentType = MimeMapping.GetMimeMapping(dataFilePath);
                    }
                    else//return the overlay file
                    {
                        dataFilePath = Path.Combine(overlayDirectory, overlayName + ".html");
                        if (File.Exists(dataFilePath))
                        {
                            responseData = File.ReadAllBytes(dataFilePath);
                        }
                        else
                        {
                            responseData = Encoding.UTF8.GetBytes(defaultPageData);
                        }
                        resp.ContentType = "text/html";
                    }
                }
                catch
                {
                    responseData = Encoding.UTF8.GetBytes(defaultPageData);
                    resp.ContentType = "text/html";
                }

                // Write the response info
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = responseData.LongLength;
                resp.OutputStream.Write(responseData, 0, responseData.Length);
                resp.Close();
            }
        }


        public void start(string url = "http://localhost", int port = -1)
        {
            if (port == -1)
                port = Globals.webServerPort;
            runServer = true;
            task = Task.Run(() =>
            {
                // Create a Http server and start listening for incoming connections
                listener = new HttpListener();
                listener.Prefixes.Add($"{url}:{port}/");
                listener.Start();

                // Handle requests
                HandleIncomingConnections();

                // Close the listener
                listener.Close();
            });
        }

        public void stop()
        {
            runServer = false;
        }
    }
}