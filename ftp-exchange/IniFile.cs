// based on: http://www.codeproject.com/Articles/1966/An-INI-file-handling-class-using-C
using System;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Create a New INI file to store or load data
/// </summary>
public class IniFile
{
    public string path;

    [DllImport("kernel32")]
    private static extern long WritePrivateProfileString(string section,
        string key, string val, string filePath);
    [DllImport("kernel32")]
    private static extern int GetPrivateProfileString(string section,
             string key, string def, StringBuilder retVal,
        int size, string filePath);
    [DllImport("kernel32")]
    private static extern int GetPrivateProfileSectionNames(byte[] lpszReturnBuffer,
        int nSize, string lpFileName);

    /// <summary>
    /// INIFile Constructor.
    /// </summary>
    /// <PARAM name="INIPath"></PARAM>
    public IniFile(string INIPath)
    {
        path = INIPath;
    }

    /// <summary>
    /// Write Data to the INI File
    /// </summary>
    /// <PARAM name="Section"></PARAM>
    /// Section name
    /// <PARAM name="Key"></PARAM>
    /// Key Name
    /// <PARAM name="Value"></PARAM>
    /// Value Name
    public void WriteValue(string Section, string Key, string Value)
    {
        WritePrivateProfileString(Section, Key, Value, this.path);
    }

    /// <summary>
    /// Read Data Value From the Ini File
    /// </summary>
    /// <PARAM name="Section"></PARAM>
    /// <PARAM name="Key"></PARAM>
    /// <PARAM name="Path"></PARAM>
    /// <returns></returns>
    public string ReadValue(string Section, string Key)
    {
        StringBuilder temp = new StringBuilder(255);
        int i = GetPrivateProfileString(Section, Key, "", temp,
                                        255, this.path);
        return temp.ToString();

    }

    public string[] GetSectionNames()
    {
        byte[] buffer = new byte[1024];
        int ret = GetPrivateProfileSectionNames(buffer, buffer.Length, path);
        if (ret == (buffer.Length - 2))
        {
            buffer = new byte[4096];
            ret = GetPrivateProfileSectionNames(buffer, buffer.Length, path);
            if (ret == (buffer.Length - 2))
                return null;
        }
        string allSections = System.Text.Encoding.Default.GetString(buffer);
        string[] strSections = allSections.Split('\0');

        int len = 0;
        foreach (string item in strSections)
        {
            if (string.IsNullOrEmpty(item))
                break;
            len++;
        }
        string[] result = new string[len];
        Array.Copy(strSections, 0, result, 0, len);
        return result;
    }
}