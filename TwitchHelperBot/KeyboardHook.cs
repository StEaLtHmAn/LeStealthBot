using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class KeyboardHook : IDisposable
{
    // Registers a hot key with Windows.
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    // Unregisters the hot key with Windows.
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    /// <summary>
    /// Represents the window that is used internally to get the messages.
    /// </summary>
    private class Window : NativeWindow, IDisposable
    {
        private static int WM_HOTKEY = 0x0312;

        public Window()
        {
            // create the handle for the window.
            this.CreateHandle(new CreateParams());
        }

        /// <summary>
        /// Overridden to get the notifications.
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            // check if we got a hot key pressed.
            if (m.Msg == WM_HOTKEY)
            {
                // get the keys.
                Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                ModifierKeys modifier = (ModifierKeys)((int)m.LParam & 0xFFFF);

                // invoke the event to notify the parent.
                if (KeyPressed != null)
                    KeyPressed(this, new KeyPressedEventArgs(modifier, key));
            }
        }

        public event EventHandler<KeyPressedEventArgs> KeyPressed;

        #region IDisposable Members

        public void Dispose()
        {
            this.DestroyHandle();
        }

        #endregion
    }

    private Window _window = new Window();
    private Dictionary<string, int> ID_Dictionary = new Dictionary<string, int>();

    public KeyboardHook()
    {
        // register the event of the inner native window.
        _window.KeyPressed += delegate (object sender, KeyPressedEventArgs args)
        {
            if (KeyPressed != null)
                KeyPressed(this, args);
        };
    }

    public void clearHotkeys()
    {
        foreach (KeyValuePair<string, int> kvp in ID_Dictionary)
        {
            if (!UnregisterHotKey(_window.Handle, kvp.Value))
                throw new InvalidOperationException("Couldn’t unregister the hot key.");
        }
        ID_Dictionary.Clear();
    }

    /// <summary>
    /// Registers a hot key in the system.
    /// </summary>
    /// <param name="modifier">The modifiers that are associated with the hot key.</param>
    /// <param name="key">The key itself that is associated with the hot key.</param>
    public void RegisterHotKey(ModifierKeys modifier, Keys key)
    {
        if (ID_Dictionary.ContainsKey($"{modifier}_{key}"))
        {
            return;//hotkey already setup
        }
        int newID = 0;
        while (ID_Dictionary.ContainsValue(newID))
        {
            newID++;
        }
        ID_Dictionary.Add($"{modifier}_{key}", newID);

        // register the hot key.
        if (!RegisterHotKey(_window.Handle, newID, (uint)modifier, (uint)key))
            throw new InvalidOperationException("Couldn’t register the hot key.");
    }

    public void UnregisterHotKey(ModifierKeys modifier, Keys key)
    {
        int ID = ID_Dictionary[$"{modifier}_{key}"];
        ID_Dictionary.Remove($"{modifier}_{key}");
        // register the hot key.
        if (!UnregisterHotKey(_window.Handle, ID))
            throw new InvalidOperationException("Couldn’t unregister the hot key.");
    }

    /// <summary>
    /// A hot key has been pressed.
    /// </summary>
    public event EventHandler<KeyPressedEventArgs> KeyPressed;

    #region IDisposable Members

    public void Dispose()
    {
        foreach (KeyValuePair<string, int> kvp in ID_Dictionary)
        {
            UnregisterHotKey(_window.Handle, kvp.Value);
        }

        // dispose the inner native window.
        _window.Dispose();
    }

    #endregion
}

/// <summary>
/// Event Args for the event that is fired after the hot key has been pressed.
/// </summary>
public class KeyPressedEventArgs : EventArgs
{
    private ModifierKeys _modifier;
    private Keys _key;

    internal KeyPressedEventArgs(ModifierKeys modifier, Keys key)
    {
        _modifier = modifier;
        _key = key;
    }

    public ModifierKeys Modifier
    {
        get { return _modifier; }
    }

    public Keys Key
    {
        get { return _key; }
    }

    public static ModifierKeys GetModifiers(Keys keydata, out Keys key)
    {
        key = keydata;
        ModifierKeys modifers = ModifierKeys.None;

        // Check whether the keydata contains the CTRL modifier key.
        // The value of Keys.Control is 131072.
        if ((keydata & Keys.Control) == Keys.Control)
        {
            modifers |= ModifierKeys.Control;

            key = keydata ^ Keys.Control;
        }

        // Check whether the keydata contains the SHIFT modifier key.
        // The value of Keys.Control is 65536.
        if ((keydata & Keys.Shift) == Keys.Shift)
        {
            modifers |= ModifierKeys.Shift;
            key = key ^ Keys.Shift;
        }

        // Check whether the keydata contains the ALT modifier key.
        // The value of Keys.Control is 262144.
        if ((keydata & Keys.Alt) == Keys.Alt)
        {
            modifers |= ModifierKeys.Alt;
            key = key ^ Keys.Alt;
        }

        // Check whether a key other than SHIFT, CTRL or ALT (Menu) is pressed.
        if (key == Keys.ShiftKey || key == Keys.ControlKey || key == Keys.Menu)
        {
            key = Keys.None;
        }

        return modifers;
    }
}

/// <summary>
/// The enumeration of possible modifiers.
/// </summary>
[Flags]
public enum ModifierKeys : uint
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8
}