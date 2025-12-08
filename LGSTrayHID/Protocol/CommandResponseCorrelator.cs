using LGSTrayHID.HidApi;
using System.Threading.Channels;

namespace LGSTrayHID.Protocol
{
    /// <summary>
    /// Handles sending HID++ commands and correlating responses.
    /// Eliminates duplication across WriteRead10, WriteRead20, and Ping20.
    /// </summary>
    public class CommandResponseCorrelator
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly ChannelReader<byte[]> _responseChannel;

        public CommandResponseCorrelator(SemaphoreSlim semaphore, ChannelReader<byte[]> responseChannel)
        {
            _semaphore = semaphore;
            _responseChannel = responseChannel;
        }

        /// <summary>
        /// Sends a command and waits for a matching response.
        /// </summary>
        /// <typeparam name="TCommand">Type of command (byte[] or Hidpp20)</typeparam>
        /// <typeparam name="TResponse">Type of response (byte[] or Hidpp20)</typeparam>
        /// <param name="device">HID device to write to</param>
        /// <param name="command">Command to send</param>
        /// <param name="matcher">Predicate to match response to request</param>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <param name="emptyResponse">Empty response to return on timeout/failure</param>
        /// <param name="earlyExit">Optional early exit condition (e.g., for HID++ 1.0 errors)</param>
        /// <returns>Matching response or empty response on timeout</returns>
        public async Task<TResponse> SendAndWaitAsync<TCommand, TResponse>(
            HidDevicePtr device,
            TCommand command,
            Func<TResponse, bool> matcher,
            int timeout,
            TResponse emptyResponse,
            Func<TResponse, bool>? earlyExit = null)
            where TCommand : notnull
            where TResponse : notnull
        {
            bool locked = await _semaphore.WaitAsync(100);
            if (!locked)
            {
                return emptyResponse;
            }

            try
            {
                // Write command to device
                byte[] cmdBytes = command switch
                {
                    byte[] bytes => bytes,
                    Hidpp20 hidpp20 => (byte[])hidpp20,
                    _ => throw new ArgumentException($"Unsupported command type: {typeof(TCommand)}")
                };
                await device.WriteAsync(cmdBytes);

                // Wait for matching response
                CancellationTokenSource cts = new();
                cts.CancelAfter(timeout);

                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        byte[] responseBytes = await _responseChannel.ReadAsync(cts.Token);

                        // Convert byte[] to TResponse (either byte[] or Hidpp20)
                        // Using if/else instead of ternary to prevent C# implicit conversion of both branches
                        TResponse response;
                        if (typeof(TResponse) == typeof(Hidpp20))
                        {
                            response = (TResponse)(object)(Hidpp20)responseBytes;
                        }
                        else
                        {
                            response = (TResponse)(object)responseBytes;
                        }

                        // Check early exit condition (e.g., HID++ 1.0 error)
                        if (earlyExit != null && earlyExit(response))
                        {
                            break;
                        }

                        // Check if response matches request
                        if (matcher(response))
                        {
                            return response;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }

                return emptyResponse;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
