using static LGSTrayHID.HidApi.HidApi;

namespace LGSTrayHID.HidApi;

public readonly struct HidDevicePtr
{
    private readonly nint _ptr;

    private HidDevicePtr(nint ptr)
    {
        _ptr = ptr;
    }

    public static implicit operator nint(HidDevicePtr ptr) => ptr._ptr;

    public static implicit operator HidDevicePtr(nint ptr) => new(ptr);

    public Task<int> WriteAsync(byte[] buffer)
    {
        var ret = HidWrite(this, buffer, (nuint)buffer.Length);

        return Task.FromResult(ret);
    }

    public int Read(byte[] buffer, int count, int timeout)
    {
        var ret = HidReadTimeOut(this, buffer, (nuint)count, timeout);
        return ret;
    }
}
