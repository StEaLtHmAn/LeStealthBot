using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TwitchHelperBot
{
    class RichTextBoxEx : RichTextBox
    {
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, Int32 wMsg, Int32 wParam, ref Point lParam);

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, Int32 wMsg, Int32 wParam, IntPtr lParam);

        const int WM_USER = 0x400;
        const int WM_SETREDRAW = 0x000B;
        const int EM_GETEVENTMASK = WM_USER + 59;
        const int EM_SETEVENTMASK = WM_USER + 69;
        const int EM_GETSCROLLPOS = WM_USER + 221;
        const int EM_SETSCROLLPOS = WM_USER + 222;

        Point _ScrollPoint;
        bool _Painting = true;
        IntPtr _EventMask;
        int _SuspendIndex = 0;
        int _SuspendLength = 0;

        public bool Autoscroll = false;

        public RichTextBoxEx()
        {
            //SetStyle(ControlStyles.DoubleBuffer, true);
        }

        //public const string RTFBold = "\\b";
        //public const string RTFBoldEnd = "\\b0";
        //public const string RTFUnderline = "\\ul";
        //public const string RTFUnderlineEnd = "\\ulnone";
        //public const string RTFParagraph = "\\par";
        //public string RTFFontSize(int size)
        //{
        //    return $"\\fs{size}";
        //}
        //public string RTFColour(int index)
        //{
        //    return $"\\cf{index}";
        //}
        //public string RTFFont(int index)
        //{
        //    return $"\\f{index}";
        //}
        //public int RTFAddColour(Color colour)
        //{
        //    int coloursIndex = Rtf.IndexOf("{\\colortbl");
        //    string coloursString = Rtf.Substring(coloursIndex+11, Rtf.IndexOf("}", coloursIndex) - coloursIndex - 11);
        //    string newColoursString = $"{coloursString}\\red{colour.R}\\green{colour.G}\\blue{colour.B};";
        //    Rtf = Rtf.Replace(coloursString, newColoursString);
        //    return coloursString.Count(f => f == ';');
        //}
        //public string RTFBuffer = string.Empty;
        //public void RTFApplyBuffer()
        //{
        //    Rtf = RTFBuffer;
        //}
        //public void RTFAppendBuffer(string text)
        //{
        //    if (string.IsNullOrEmpty(RTFBuffer))
        //        RTFBuffer = Rtf;
        //    RTFBuffer = RTFBuffer.Insert(RTFBuffer.LastIndexOf('}'), text);
        //}

        public void SuspendPainting()
        {
            if (_Painting)
            {
                _SuspendIndex = this.SelectionStart;
                _SuspendLength = this.SelectionLength;
                SendMessage(this.Handle, EM_GETSCROLLPOS, 0, ref _ScrollPoint);
                SendMessage(this.Handle, WM_SETREDRAW, 0, IntPtr.Zero);
                _EventMask = SendMessage(this.Handle, EM_GETEVENTMASK, 0, IntPtr.Zero);
                _Painting = false;
            }
        }

        public void ResumePainting()
        {
            if (!_Painting)
            {
                this.Select(_SuspendIndex, _SuspendLength);
                SendMessage(this.Handle, EM_SETSCROLLPOS, 0, ref _ScrollPoint);
                SendMessage(this.Handle, EM_SETEVENTMASK, 0, _EventMask);
                SendMessage(this.Handle, WM_SETREDRAW, 1, IntPtr.Zero);
                _Painting = true;
                this.Invalidate();
            }
        }
    }
}
