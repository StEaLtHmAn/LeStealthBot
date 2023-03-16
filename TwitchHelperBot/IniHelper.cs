using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

public class IniHelper
{
    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    static extern long WritePrivateProfileString(string section, string key, string value, string FilePath);

    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    static extern int GetPrivateProfileString(string section, string key, string Default, StringBuilder RetVal, int Size, string FilePath);

    [DllImport("kernel32")]
    static extern uint GetPrivateProfileSectionNames(IntPtr pszReturnBuffer, uint nSize, string lpFileName);

    private readonly FileInfo FileInfo;
    public readonly string exe = Assembly.GetExecutingAssembly().GetName().Name;
    private readonly FileAccess fileAccess;

    public IniHelper(string path = null, FileAccess access = FileAccess.ReadWrite)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, string.Empty);
        }
        fileAccess = access;
        FileInfo = new FileInfo(path ?? exe);
    }

    public string Read(string key, string section = null)
    {
        try
        {
            var RetVal = new StringBuilder(65025);

            if (fileAccess != FileAccess.Write)
            {
                GetPrivateProfileString(section ?? exe, key, "", RetVal, 65025, FileInfo.FullName);
            }
            else
            {
                return null;
            }
            if (RetVal.Length > 0)
                return RetVal.ToString();
        }
        catch { }
        return null;
    }
    public void Write(string key, string value, string section = null)
    {
        if (fileAccess != FileAccess.Read)
        {
            WritePrivateProfileString(section ?? exe, key, value, FileInfo.FullName);
        }
        else
        {
            throw new Exception("Can`t write to file! No access!");
        }
    }

    public void DeleteKey(string key, string section = null)
    {
        Write(key, null, section ?? exe);
    }

    public void DeleteSection(string section = null)
    {
        Write(null, null, section ?? exe);
    }

    public bool KeyExists(string key, string section = null)
    {
        return Read(key, section).Length > 0;
    }

    public string[] SectionNames()
    {
        uint MAX_BUFFER = 32767;
        IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)MAX_BUFFER);
        uint bytesReturned = GetPrivateProfileSectionNames(pReturnedString, MAX_BUFFER, FileInfo.FullName);
        if (bytesReturned == 0)
            return null;
        string local = Marshal.PtrToStringAnsi(pReturnedString, (int)bytesReturned).ToString();
        Marshal.FreeCoTaskMem(pReturnedString);
        //use of Substring below removes terminating null for split
        return local.Substring(0, local.Length - 1).Split('\0');
    }

}