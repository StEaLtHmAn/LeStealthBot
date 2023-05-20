using System.Drawing;
using System.Windows.Forms;
using TwitchHelperBot;

public class PopupWindow : ToolStripDropDown
{
    private ToolStripControlHost _host;

    public PopupWindow(Control content, bool autoClose = false)
    {
        //Basic setup...
        AutoSize = false;
        DoubleBuffered = true;
        ResizeRedraw = true;
        AutoClose = autoClose;
        DropShadowEnabled = true;
        Margin = Padding.Empty;
        Padding = Padding.Empty;
        _host = new ToolStripControlHost(content);

        //Positioning and Sizing
        content.Location = Point.Empty;
        Size = content.Size;

        //Add the host to the list
        Items.Add(_host);

        //custom dark mode for popups
        if (bool.Parse(Globals.iniHelper.Read("DarkModeEnabled")))
        {
            if (content.BackColor == SystemColors.Control)
            {
                content.BackColor = Globals.DarkColour;
                content.ForeColor = SystemColors.ControlLightLight;
            }
            else if (content.BackColor == SystemColors.Window)
            {
                content.BackColor = Globals.DarkColour2;
                content.ForeColor = SystemColors.ControlLightLight;
            }
            foreach (Control component in content.Controls)
            {
                if (component.BackColor == SystemColors.Control)
                    component.BackColor = Globals.DarkColour;
                else if (component.BackColor == SystemColors.Window)
                    component.BackColor = Globals.DarkColour2;
                if (!(component is Button))
                    component.ForeColor = SystemColors.ControlLightLight;
            }
        }
    }
}