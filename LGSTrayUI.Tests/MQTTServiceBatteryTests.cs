using LGSTrayPrimitives;
using LGSTrayPrimitives.MessageStructs;
using LGSTrayUI.Messages;
using LGSTrayUI.Services;
using LGSTrayUI.Tests.Mocks;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Xunit;

namespace LGSTrayUI.Tests;

/// <summary>
/// Tests for MQTT service battery reporting behavior.
/// Ensures that battery data payload is formatted correctly in different states:
/// - Startup with invalid data (-1) should report -1
/// - Device with valid battery that goes offline should report last good value
/// NOTE: These tests verify the data structure/payload format, not the MQTT client itself.
/// </summary>
[Collection("Sequential")]
public class MQTTServiceBatteryTests
{

    private static LogiDeviceViewModel CreateTestDevice(string deviceId, string deviceName)
    {
        var appSettings = new AppSettings
        {
            UI = new UISettings(),
            Logging = new LoggingSettings { Enabled = false },
            HTTPServer = new HttpServerSettings { Enabled = false },
            GHub = new GHubManagerSettings { Enabled = false },
            Native = new NativeDeviceManagerSettings { Enabled = false },
            Notifications = new NotificationSettings()
        };

        var userSettings = new UserSettingsWrapper();
        var iconFactory = new MockLogiDeviceIconFactory();
        var device = new LogiDeviceViewModel(iconFactory, appSettings, userSettings);

        device.UpdateState(new InitMessage(
            deviceId: deviceId,
            deviceName: deviceName,
            hasBattery: true,
            deviceType: DeviceType.Mouse
        ));

        return device;
    }

    [Fact]
    public void PublishStateUpdate_Uninitialized_ReportsNegativeOne()
    {
        // Arrange - Device at startup with no battery data
        var device = CreateTestDevice("test001", "Test Mouse");
        device.BatteryPercentage = -1;
        device.IsOnline = false;

        // Act - Simulate receiving device update
        // We'll call the Receive method directly via reflection or test the data structure
        var stateJson = JsonSerializer.Serialize(new
        {
            percentage = (int)Math.Round(device.BatteryPercentage),
            voltage = device.BatteryVoltage,
            charging = device.PowerSupplyStatus == PowerSupplyStatus.POWER_SUPPLY_STATUS_CHARGING,
            online = device.IsOnline,
            mileage = device.BatteryMileage,
            last_update = device.LastUpdate.ToString("o"),
            data_source = device.DataSource.ToString(),
            device_name = device.DeviceName,
            device_type = device.DeviceType.ToString()
        });

        var parsed = JsonSerializer.Deserialize<JsonElement>(stateJson);

        // Assert
        Assert.Equal(-1, parsed.GetProperty("percentage").GetInt32());
        Assert.False(parsed.GetProperty("online").GetBoolean());
    }

    [Fact]
    public void PublishStateUpdate_ValidBattery_ReportsCorrectValue()
    {
        // Arrange - Device online with valid battery
        var device = CreateTestDevice("test002", "Test Keyboard");
        device.UpdateState(new UpdateMessage(
            deviceId: "test002",
            batteryPercentage: 75.5,
            powerSupplyStatus: PowerSupplyStatus.POWER_SUPPLY_STATUS_DISCHARGING,
            batteryMVolt: 3800,
            updateTime: DateTimeOffset.Now
        ));

        // Act - Build state payload
        var stateJson = JsonSerializer.Serialize(new
        {
            percentage = (int)Math.Round(device.BatteryPercentage),
            voltage = device.BatteryVoltage,
            charging = device.PowerSupplyStatus == PowerSupplyStatus.POWER_SUPPLY_STATUS_CHARGING,
            online = device.IsOnline,
            mileage = device.BatteryMileage,
            last_update = device.LastUpdate.ToString("o"),
            data_source = device.DataSource.ToString(),
            device_name = device.DeviceName,
            device_type = device.DeviceType.ToString()
        });

        var parsed = JsonSerializer.Deserialize<JsonElement>(stateJson);

        // Assert
        Assert.Equal(76, parsed.GetProperty("percentage").GetInt32()); // Rounded from 75.5
        Assert.Equal(3.8, parsed.GetProperty("voltage").GetDouble());
        Assert.True(parsed.GetProperty("online").GetBoolean());
        Assert.False(parsed.GetProperty("charging").GetBoolean());
    }

    [Fact]
    public void PublishStateUpdate_GoesOffline_PreservesLastKnownBattery()
    {
        // Arrange - Device with battery data
        var device = CreateTestDevice("test003", "Test Headset");
        device.UpdateState(new UpdateMessage(
            deviceId: "test003",
            batteryPercentage: 50.0,
            powerSupplyStatus: PowerSupplyStatus.POWER_SUPPLY_STATUS_DISCHARGING,
            batteryMVolt: 3700,
            updateTime: DateTimeOffset.Now
        ));

        Assert.True(device.IsOnline);
        Assert.Equal(50.0, device.BatteryPercentage);

        // Simulate device going offline (BatteryPercentage NOT updated to -1)
        device.UpdateState(new UpdateMessage(
            deviceId: "test003",
            batteryPercentage: -1, // Negative indicates offline
            powerSupplyStatus: PowerSupplyStatus.POWER_SUPPLY_STATUS_UNKNOWN,
            batteryMVolt: 0,
            updateTime: DateTimeOffset.Now
        ));

        // Act - Build state payload
        var stateJson = JsonSerializer.Serialize(new
        {
            percentage = (int)Math.Round(device.BatteryPercentage),
            voltage = device.BatteryVoltage,
            charging = device.PowerSupplyStatus == PowerSupplyStatus.POWER_SUPPLY_STATUS_CHARGING,
            online = device.IsOnline,
            mileage = device.BatteryMileage,
            last_update = device.LastUpdate.ToString("o"),
            data_source = device.DataSource.ToString(),
            device_name = device.DeviceName,
            device_type = device.DeviceType.ToString()
        });

        var parsed = JsonSerializer.Deserialize<JsonElement>(stateJson);

        // Assert - Should report last known battery (50), not -1
        Assert.False(device.IsOnline);
        Assert.Equal(50.0, device.BatteryPercentage); // Preserved!
        Assert.Equal(50, parsed.GetProperty("percentage").GetInt32());
        Assert.False(parsed.GetProperty("online").GetBoolean());
    }

    [Fact]
    public void PublishStateUpdate_WiredModeWithoutBattery_ReportsNegativeOne()
    {
        // Arrange - Wired device that doesn't report battery percentage
        var device = CreateTestDevice("test004", "Test G515");
        device.UpdateState(new UpdateMessage(
            deviceId: "test004",
            batteryPercentage: -1, // Wired mode, no battery data
            powerSupplyStatus: PowerSupplyStatus.POWER_SUPPLY_STATUS_CHARGING,
            batteryMVolt: 0,
            updateTime: DateTimeOffset.Now,
            isWiredMode: true
        ));

        // Act - Build state payload
        var stateJson = JsonSerializer.Serialize(new
        {
            percentage = (int)Math.Round(device.BatteryPercentage),
            voltage = device.BatteryVoltage,
            charging = device.PowerSupplyStatus == PowerSupplyStatus.POWER_SUPPLY_STATUS_CHARGING,
            online = device.IsOnline,
            mileage = device.BatteryMileage,
            last_update = device.LastUpdate.ToString("o"),
            data_source = device.DataSource.ToString(),
            device_name = device.DeviceName,
            device_type = device.DeviceType.ToString()
        });

        var parsed = JsonSerializer.Deserialize<JsonElement>(stateJson);

        // Assert - Wired mode without battery data should report -1
        Assert.True(device.IsOnline); // Wired devices are online
        Assert.Equal(-1, parsed.GetProperty("percentage").GetInt32());
        Assert.True(parsed.GetProperty("charging").GetBoolean());
    }

    [Fact]
    public void PublishStateUpdate_ChargingStatus_ReportsCorrectly()
    {
        // Arrange - Charging device
        var device = CreateTestDevice("test005", "Test Charging");
        device.UpdateState(new UpdateMessage(
            deviceId: "test005",
            batteryPercentage: 85.0,
            powerSupplyStatus: PowerSupplyStatus.POWER_SUPPLY_STATUS_CHARGING,
            batteryMVolt: 3900,
            updateTime: DateTimeOffset.Now
        ));

        // Act - Build state payload
        var stateJson = JsonSerializer.Serialize(new
        {
            percentage = (int)Math.Round(device.BatteryPercentage),
            charging = device.PowerSupplyStatus == PowerSupplyStatus.POWER_SUPPLY_STATUS_CHARGING,
            online = device.IsOnline
        });

        var parsed = JsonSerializer.Deserialize<JsonElement>(stateJson);

        // Assert
        Assert.Equal(85, parsed.GetProperty("percentage").GetInt32());
        Assert.True(parsed.GetProperty("charging").GetBoolean());
        Assert.True(parsed.GetProperty("online").GetBoolean());
    }

    [Fact]
    public void PublishStateUpdate_MileageFromGHub_IncludedInPayload()
    {
        // Arrange - GHUB device with mileage
        var device = CreateTestDevice("dev001", "G Pro Wireless");
        device.DataSource = DataSource.GHub;
        device.UpdateState(new UpdateMessage(
            deviceId: "dev001",
            batteryPercentage: 60.0,
            powerSupplyStatus: PowerSupplyStatus.POWER_SUPPLY_STATUS_DISCHARGING,
            batteryMVolt: 3750,
            updateTime: DateTimeOffset.Now,
            mileage: 123.5
        ));

        // Act - Build state payload
        var stateJson = JsonSerializer.Serialize(new
        {
            percentage = (int)Math.Round(device.BatteryPercentage),
            mileage = device.BatteryMileage,
            data_source = device.DataSource.ToString()
        });

        var parsed = JsonSerializer.Deserialize<JsonElement>(stateJson);

        // Assert
        Assert.Equal(60, parsed.GetProperty("percentage").GetInt32());
        Assert.Equal(123.5, parsed.GetProperty("mileage").GetDouble());
        Assert.Equal("GHub", parsed.GetProperty("data_source").GetString());
    }
}
