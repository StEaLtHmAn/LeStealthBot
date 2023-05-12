using System.Drawing;

public class PopupWindow : System.Windows.Forms.ToolStripDropDown
{
    private System.Windows.Forms.ToolStripControlHost _host;

    public PopupWindow(System.Windows.Forms.Control content)
    {
        //Basic setup...
        AutoSize = false;
        DoubleBuffered = true;
        ResizeRedraw = true;
        AutoClose = false;
        DropShadowEnabled = true;
        Margin = System.Windows.Forms.Padding.Empty;
        Padding = System.Windows.Forms.Padding.Empty;
        _host = new System.Windows.Forms.ToolStripControlHost(content);

        //Positioning and Sizing
        //MinimumSize = content.MinimumSize;
        //MaximumSize = content.Size;
        Size = content.Size;
        content.Location = Point.Empty;

        //Add the host to the list
        Items.Add(_host);
    }
}