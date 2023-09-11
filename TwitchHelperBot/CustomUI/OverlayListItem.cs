using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LeStealthBot.CustomUI
{
    public partial class OverlayListItem : UserControl
    {
        public string OverlayName;
        public OverlayListItem(string OverlayName)
        {
            InitializeComponent();

            this.OverlayName = OverlayName;
            label1.Text = OverlayName;
            BackgroundImageLayout = ImageLayout.Zoom;
            new WebsiteThumbnailImageGenerator($"http://localhost:{Globals.webServerPort}/?overlay={OverlayName}", 1024, 768, Width, Height)
                .GenerateWebSiteThumbnailImage().ContinueWith((t) =>
             {
                 BackgroundImage = t.Result;
             }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public new void Dispose()
        {
            if(BackgroundImage != null)
                BackgroundImage.Dispose();
            base.Dispose();
        }
    }
}
