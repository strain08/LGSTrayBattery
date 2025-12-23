using CommunityToolkit.Mvvm.ComponentModel;
using LGSTrayCore;
using LGSTrayPrimitives;
using LGSTrayPrimitives.MessageStructs;
using LGSTrayUI.Interfaces;
using System;
using System.Text;

namespace LGSTrayUI;

public class LogiDeviceViewModelFactory
{
    private readonly ILogiDeviceIconFactory _logiDeviceIconFactory;

    public LogiDeviceViewModelFactory(ILogiDeviceIconFactory logiDeviceIconFactory)
    {
        _logiDeviceIconFactory = logiDeviceIconFactory;
    }

    public LogiDeviceViewModel CreateViewModel(Action<LogiDeviceViewModel>? config = null)
    {
        LogiDeviceViewModel output = new(_logiDeviceIconFactory);
        config?.Invoke(output);

        return output;
    }
}

public partial class LogiDeviceViewModel : LogiDevice
{
    private readonly ILogiDeviceIconFactory _logiDeviceIconFactory;

    [ObservableProperty]
    private bool _isChecked = false;

    private LogiDeviceIcon? taskbarIcon;

    public LogiDeviceViewModel(ILogiDeviceIconFactory logiDeviceIconFactory)
    {
        _logiDeviceIconFactory = logiDeviceIconFactory;

        // Subscribe to property changes from base class to update computed properties
        PropertyChanged += (sender, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(DataSource):
                    OnPropertyChanged(nameof(DataSourceDisplayName));
                    OnPropertyChanged(nameof(BadgeLetter));
                    OnPropertyChanged(nameof(DetailedMenuTooltip));
                    break;
                case nameof(DeviceName):
                case nameof(DeviceId):
                case nameof(BatteryPercentage):
                case nameof(BatteryVoltage):
                case nameof(BatteryMileage):
                case nameof(LastUpdate):
                    OnPropertyChanged(nameof(DetailedMenuTooltip));
                    break;
            }
        };
    }

    partial void OnIsCheckedChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            taskbarIcon ??= _logiDeviceIconFactory.CreateDeviceIcon(this);
        }
        else
        {
            taskbarIcon?.Dispose();
            taskbarIcon = null;
        }
    }

    public void UpdateState(InitMessage initMessage)
    {
        if (string.IsNullOrEmpty(DeviceId) || DeviceId == NOT_FOUND)
        {
            DeviceId = initMessage.deviceId;
        }

        DeviceName = initMessage.deviceName;
        HasBattery = initMessage.hasBattery;
        DeviceType = initMessage.deviceType;
        DataSource = DataSourceHelper.GetDataSource(initMessage.deviceId);
    }

    public void UpdateState(UpdateMessage updateMessage)
    {
        BatteryPercentage = updateMessage.batteryPercentage;
        PowerSupplyStatus = updateMessage.powerSupplyStatus;
        BatteryVoltage = updateMessage.batteryMVolt / 1000.0;
        BatteryMileage = updateMessage.Mileage;
        LastUpdate = updateMessage.updateTime;
    }

    /// <summary>
    /// Human-readable data source name for display
    /// </summary>
    public string DataSourceDisplayName => DataSource switch
    {
        DataSource.Native => "Native HID++ 2.0",
        DataSource.GHub => "Logitech G Hub",
        _ => "Unknown"
    };

    /// <summary>
    /// Badge letter for menu display ("N" or "G")
    /// </summary>
    public string BadgeLetter => DataSource switch
    {
        DataSource.Native => "N",
        DataSource.GHub => "G",
        _ => "?"
    };

    /// <summary>
    /// Detailed tooltip for menu items
    /// </summary>
    public string DetailedMenuTooltip
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Device: {DeviceName}");
            sb.AppendLine($"Source: {DataSourceDisplayName}");
            sb.AppendLine($"ID: {DeviceId}");

            if (LastUpdate != DateTimeOffset.MinValue)
                sb.AppendLine($"Last Update: {LastUpdate:g}");

            if (DataSource == DataSource.Native && BatteryVoltage > 0)
                sb.AppendLine($"Voltage: {BatteryVoltage:F2}V");

            if (DataSource == DataSource.GHub && BatteryMileage > 0)
                sb.AppendLine($"Mileage: {BatteryMileage:F1}h");

            return sb.ToString().TrimEnd();
        }
    }
}
