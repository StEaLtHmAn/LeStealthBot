using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

public class AutoCompleteTextBox : TextBox
{
    public ListBox listBox;
    private PopupWindow popup;
    private bool _isAdded;
    private JArray _Data = null;
    private string _formerValue = string.Empty;

    public JArray Data
    {
        get { return _Data; }
        set
        {
            if (_Data != value)
            {
                _Data = value;
                UpdateListBox();
            }
        }
    }

    public AutoCompleteTextBox()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        listBox = new ListBox();
        listBox.Width = Width;
        listBox.Click += _listBox_Click;
        listBox.FormattingEnabled = true;
        listBox.Format += _listBox_Format;
        popup = new PopupWindow(listBox);
        KeyDown += this_KeyDown;
        Click += AutoCompleteTextBox_Click;
        //LostFocus += AutoCompleteTextBox_LostFocus;
        Leave += AutoCompleteTextBox_LostFocus;
    }

    private void AutoCompleteTextBox_LostFocus(object sender, EventArgs e)
    {
        if(!listBox.Focused)
            ResetListBox();
    }

    private void _listBox_Format(object sender, ListControlConvertEventArgs e)
    {
        e.Value = (e.Value as JObject)["name"].ToString();
    }

    private void AutoCompleteTextBox_Click(object sender, EventArgs e)
    {
        ShowListBox();
    }

    private void _listBox_Click(object sender, EventArgs e)
    {
        if (listBox.SelectedItem != null)
        {
            Text = (listBox.SelectedItem as JObject)["name"].ToString();
            ResetListBox();
            _formerValue = Text;
        }
    }

    private void ShowListBox()
    {
        if (!_isAdded && Data != null && Data.Count > 0 && Focused)
        {
            popup.Show(this, new Point(0, Height));
            _isAdded = true;
        }
    }

    private void ResetListBox()
    {
        popup.Close();
        _isAdded = false;
    }

    private void this_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Tab:
            case Keys.Enter:
                {
                    if (listBox.SelectedItem != null)
                    {
                        Text = (listBox.SelectedItem as JObject)["name"].ToString();
                        ResetListBox();
                        _formerValue = Text;
                    }
                    break;
                }
            case Keys.Down:
                {
                    if (listBox.SelectedIndex < listBox.Items.Count - 1)
                        listBox.SelectedIndex++;
                    break;
                }
            case Keys.Up:
                {
                    if (listBox.SelectedIndex > 0)
                        listBox.SelectedIndex--;
                    break;
                }
            case Keys.Escape:
                {
                    ResetListBox();
                    break;
                }
        }
    }

    protected override bool IsInputKey(Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Tab:
            case Keys.Enter:
            case Keys.Escape:
                return true;
            default:
                return base.IsInputKey(keyData);
        }
    }

    private void UpdateListBox()
    {
        if (string.IsNullOrEmpty(Text) || Text == _formerValue) return;
            _formerValue = Text;

        if (Data != null)
        {
            string[] matches = Data.Select(x => x["name"].ToString()).ToArray();

            listBox.Items.Clear();
            foreach (JObject data in Data)
            {
                listBox.Items.Add(data);
            }
            listBox.SelectedIndex = 0;
            listBox.Height = 0;
            using (Graphics graphics = listBox.CreateGraphics())
            {
                for (int i = 0; i < listBox.Items.Count; i++)
                {
                    listBox.Height += listBox.GetItemHeight(i);
                    //int itemWidth = (int)graphics.MeasureString(((string)_listBox.Items[i]) + "_", _listBox.Font).Width;
                    //_listBox.Width = (_listBox.Width < itemWidth) ? itemWidth : _listBox.Width;
                }
            }
            popup.Size = new Size(listBox.Width, listBox.Height);
            ResetListBox();
            ShowListBox();
        }
        else
        {
            ResetListBox();
        }
    }
}