using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LeStealthBot
{
    public class WebsiteThumbnailImageGenerator
    {
        private WebView2 webBrowser;
        private Bitmap image = null;
        private string Url = null;
        private int BrowserWidth = 0;
        private int BrowserHeight = 0;
        private int ThumbnailWidth = 0;
        private int ThumbnailHeight = 0;
        private bool isLoading = true;

        public WebsiteThumbnailImageGenerator(string Url, int BrowserWidth, int BrowserHeight, int ThumbnailWidth, int ThumbnailHeight)
        {
            this.Url = Url;
            this.BrowserWidth = BrowserWidth;
            this.BrowserHeight = BrowserHeight;
            this.ThumbnailWidth = ThumbnailWidth;
            this.ThumbnailHeight = ThumbnailHeight;
        }

        public Task<Bitmap> GenerateWebSiteThumbnailImage()
        {
            webBrowser = new WebView2();
            webBrowser.Source = new Uri(Url);
            webBrowser.NavigationCompleted += new EventHandler<CoreWebView2NavigationCompletedEventArgs>(WebBrowser_DocumentCompleted);

            return Task.Run(() =>
            {
                while (isLoading)
                {
                    Thread.Sleep(1);
                }
                return image;
            });
        }

        private async void WebBrowser_DocumentCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            webBrowser.CoreWebView2.DOMContentLoaded += (s, a) =>
            {
                webBrowser.ExecuteScriptAsync("document.querySelector('body').style.overflow='scroll';var style=document.createElement('style');style.type='text/css';style.innerHTML='::-webkit-scrollbar{display:none}';document.getElementsByTagName('body')[0].appendChild(style)");
            };
            webBrowser.Size = new Size(BrowserWidth, BrowserHeight);
            webBrowser.ClientSize = new Size(BrowserWidth, BrowserHeight);
            using (Bitmap bmp = await GetWebBrowserBitmap())
            {
                image = new Bitmap(ThumbnailWidth, ThumbnailHeight);
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.DrawImage(bmp, 0, 0, ThumbnailWidth, ThumbnailHeight);
                }
            }

            webBrowser.Dispose();
            isLoading = false;
        }

        private async Task<Bitmap> GetWebBrowserBitmap()
        {
            try
            {
                JObject settings = new JObject
                    {
                        { "format","jpeg" },
                        { "clip", new JObject
                            {
                                { "x", 0 },
                                { "y", 0 },
                                { "width", webBrowser.Width },
                                { "height", webBrowser.Height },
                                { "scale", 1 }
                            } },
                        { "fromSurface", true },
                        { "captureBeyondViewport", true }
                    };

                var devData = await webBrowser.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot", settings.ToString(Newtonsoft.Json.Formatting.None));
                using (Image screenshot = Image.FromStream(new MemoryStream(Convert.FromBase64String(JObject.Parse(devData)["data"].ToString()))))
                    return (Bitmap)screenshot.GetThumbnailImage(ThumbnailWidth,ThumbnailHeight, null, IntPtr.Zero);
            }
            catch (Exception ex)
            {
            }
            return null;
        }
    }
}