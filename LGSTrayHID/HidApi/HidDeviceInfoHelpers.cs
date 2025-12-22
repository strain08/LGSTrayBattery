using System.Runtime.InteropServices;

namespace LGSTrayHID.HidApi;

public enum HidppMessageType : short
{
    NONE = 0,
    SHORT,
    LONG,
    VERY_LONG
}

internal static class HidDeviceInfoHelpers
{
    internal static string GetPath(this HidDeviceInfo deviceInfo)
    {
        unsafe
        {
            return Marshal.PtrToStringAnsi((nint)deviceInfo.Path)!;
        }
    }

    internal static HidppMessageType GetHidppMessageType(this HidDeviceInfo deviceInfo)
    {
        unsafe
        {
            if ((deviceInfo.UsagePage & 0xFF00) == 0xFF00)
            {
                return deviceInfo.Usage switch
                {
                    0x0001 => HidppMessageType.SHORT,
                    0x0002 => HidppMessageType.LONG,
                    _ => HidppMessageType.NONE,
                };
            }
            else
            {
                return HidppMessageType.NONE;
            }
        }
    }

}
