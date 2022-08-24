using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

class Program
{
  [StructLayout(LayoutKind.Sequential)]
  struct UNICODE_STRING
  {
    public ushort Length;
    public ushort MaximumLength;
    public IntPtr Buffer;

    public override string ToString()
    {
      if (Buffer != IntPtr.Zero)
        return Marshal.PtrToStringUni(Buffer, Length / 2);
      return "(null)";
    }
  }

  [StructLayout(LayoutKind.Sequential)]
  class RTL_RELATIVE_NAME
  {
    public UNICODE_STRING RelativeName;
    public IntPtr ContainingDirectory;
    public IntPtr CurDirRef;
  }

  [DllImport("ntdll.dll", CharSet = CharSet.Unicode)]
  static extern int RtlDosPathNameToRelativeNtPathName_U_WithStatus(
    string DosFileName,
    out UNICODE_STRING NtFileName,
    out IntPtr ShortPath,
    [Out] RTL_RELATIVE_NAME RelativeName
    );

  enum RTL_PATH_TYPE
  {
    RtlPathTypeUnknown,
    RtlPathTypeUncAbsolute,
    RtlPathTypeDriveAbsolute,
    RtlPathTypeDriveRelative,
    RtlPathTypeRooted,
    RtlPathTypeRelative,
    RtlPathTypeLocalDevice,
    RtlPathTypeRootLocalDevice
  }

  [DllImport("ntdll.dll", CharSet = CharSet.Unicode)]
  static extern RTL_PATH_TYPE RtlDetermineDosPathNameType_U(string Path);

  [DllImport("ntdll.dll", CharSet = CharSet.Unicode)]
  static extern int RtlGetFullPathName_UEx(
    string FileName, 
    int BufferLength, 
    [Out] StringBuilder Buffer, 
    IntPtr FilePart, 
    out int FinalLength);

  [DllImport("ntdll.dll")]
  static extern int RtlNtStatusToDosError(int NtStatus);
  
  static void PrintStatus(int status)
  {
    Console.WriteLine("Error:        {0}",
      new Win32Exception(RtlNtStatusToDosError(status)).Message);
  }

  static void ConvertPath(string path)
  {
    Console.WriteLine("Converting:   '{0}'", path);
    UNICODE_STRING ntname = new UNICODE_STRING();
    IntPtr filename = IntPtr.Zero;
    RTL_RELATIVE_NAME relative_name = new RTL_RELATIVE_NAME();
    int status = RtlDosPathNameToRelativeNtPathName_U_WithStatus(
                    path,
                    out ntname,
                    out filename,
                    relative_name);
    if (status == 0)
    {
      Console.WriteLine("To:           '{0}'",
        ntname);
      Console.WriteLine("Type:         {0}",
        RtlDetermineDosPathNameType_U(path));
      Console.WriteLine("FileName:     {0}",
        Marshal.PtrToStringUni(filename));
      if (relative_name.RelativeName.Length > 0)
      {
        Console.WriteLine("RelativeName: '{0}'",
          relative_name.RelativeName);
        Console.WriteLine("Directory:    0x{0:X}",
          relative_name.ContainingDirectory.ToInt64());
        Console.WriteLine("CurDirRef:    0x{0:X}",
          relative_name.CurDirRef.ToInt64());
      }
    }
    else
    {
      PrintStatus(status);
    }

    int length = 0;
    StringBuilder builder = new StringBuilder(260);
    status = RtlGetFullPathName_UEx(
      path,
      builder.Capacity * 2,
      builder,
      IntPtr.Zero,
      out length);
    if (status == 0)
    {
      Console.WriteLine("FullPathName: '{0}'",
        builder.ToString());
    }
    else
    {
      PrintStatus(status);
    }
  }
  
  static void Main(string[] args)
  {
    if (args.Length < 1)
    {
      Console.WriteLine("Usage: ConvertDosPathToNtPath DosPath");
    }
    else
    {
      ConvertPath(args[0]);      
    }
  }
}
