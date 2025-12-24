using LGSTrayCore.Interfaces;
using Newtonsoft.Json;
using System.Reactive.Subjects;
using Websocket.Client;

namespace LGSTrayCore.Tests.Mocks;

/// <summary>
/// Mock WebSocket client for testing GHUB manager without actual WebSocket connection
/// </summary>
public class MockWebSocketClient : IWebSocketClient
{
    private Subject<ResponseMessage> _messageSubject;
    private Subject<DisconnectionInfo> _disconnectionSubject;
    private Subject<ReconnectionInfo> _reconnectionSubject;
    private bool _disposed;

    public MockWebSocketClient()
    {
        _messageSubject = new Subject<ResponseMessage>();
        _disconnectionSubject = new Subject<DisconnectionInfo>();
        _reconnectionSubject = new Subject<ReconnectionInfo>();
    }

    public IObservable<ResponseMessage> MessageReceived
    {
        get
        {
            // Auto-recreate subject if disposed (handles RediscoverDevices scenario)
            if (_disposed && _messageSubject != null)
            {
                _messageSubject = new Subject<ResponseMessage>();
                _disconnectionSubject = new Subject<DisconnectionInfo>();
                _reconnectionSubject = new Subject<ReconnectionInfo>();
                _disposed = false;
            }
            return _messageSubject;
        }
    }

    public List<string> SentMessages { get; } = new();

    public TimeSpan? ReconnectTimeout { get; set; }
    public TimeSpan? ErrorReconnectTimeout { get; set; }

    public IObservable<DisconnectionInfo> DisconnectionHappened
    {
        get
        {
            // Auto-recreate subject if disposed (handles RediscoverDevices scenario)
            if (_disposed && _disconnectionSubject != null)
            {
                _messageSubject = new Subject<ResponseMessage>();
                _disconnectionSubject = new Subject<DisconnectionInfo>();
                _reconnectionSubject = new Subject<ReconnectionInfo>();
                _disposed = false;
            }
            return _disconnectionSubject;
        }
    }

    public IObservable<ReconnectionInfo> ReconnectionHappened
    {
        get
        {
            // Auto-recreate subject if disposed (handles RediscoverDevices scenario)
            if (_disposed && _reconnectionSubject != null)
            {
                _messageSubject = new Subject<ResponseMessage>();
                _disconnectionSubject = new Subject<DisconnectionInfo>();
                _reconnectionSubject = new Subject<ReconnectionInfo>();
                _disposed = false;
            }
            return _reconnectionSubject;
        }
    }

    public void Send(string message) => SentMessages.Add(message);

    public Task Start()
    {
        // Subjects are auto-recreated by property accessors if disposed
        // Just return success
        return Task.CompletedTask;
    }

    public Task Stop()
    {
        // Stop doesn't dispose - allows restart
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Complete the subjects to signal no more messages
            _messageSubject?.OnCompleted();
            _disconnectionSubject?.OnCompleted();
            _reconnectionSubject?.OnCompleted();

            // Dispose the subjects
            _messageSubject?.Dispose();
            _disconnectionSubject?.Dispose();
            _reconnectionSubject?.Dispose();

            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    // Test helper methods

    /// <summary>
    /// Simulate a GHUB message with the given path and payload
    /// </summary>
    public void SimulateMessage(string path, object payload)
    {
        var ghubMsg = new
        {
            msgId = "",
            verb = "GET",
            path,
            payload
        };
        var json = JsonConvert.SerializeObject(ghubMsg);
        _messageSubject.OnNext(ResponseMessage.TextMessage(json));
    }

    /// <summary>
    /// Simulate GHUB /devices/list response
    /// </summary>
    public void SimulateDeviceListResponse(params (string id, string name, bool hasBattery)[] devices)
    {
        var deviceInfos = devices.Select(d => new
        {
            id = d.id,
            extendedDisplayName = d.name,
            deviceType = "mouse",
            capabilities = new { hasBatteryStatus = d.hasBattery }
        }).ToArray();

        SimulateMessage("/devices/list", new { deviceInfos });
    }

    /// <summary>
    /// Simulate GHUB /devices/state/changed event
    /// </summary>
    public void SimulateDeviceStateChange(string id, string state)
    {

        SimulateMessage("/devices/state/changed", new 
        { 
            id, 
            state, 
            deviceType = "MOUSE", 
            extendedDisplayName= "G305 Lightspeed Wireless Gaming Mouse", 
            capabilities= new
            {
                hasBatteryStatus = "true"
            }
        }
        );
    }

    /// <summary>
    /// Simulate GHUB /battery/state/changed event
    /// </summary>
    public void SimulateBatteryUpdate(string deviceId, int percentage, bool charging, double mileage = 0)
    {
        SimulateMessage("/battery/state/changed", new { deviceId, percentage, charging, mileage });
    }
}
