using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ESCenter.Core;
using ESCenter.Models;

namespace ESCenter.Services
{
    public sealed class DockControlIntegrationServer : IDisposable
    {
        private const string PipeName = "dockcontrol_esc_pipe";

        private readonly Func<EscCommandRequest, Task> _commandHandler;
        private readonly CancellationTokenSource _shutdownCts = new CancellationTokenSource();
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private Task? _listenerTask;

        public DockControlIntegrationServer(Func<EscCommandRequest, Task> commandHandler)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        }

        public void Start()
        {
            if (_listenerTask != null)
            {
                return;
            }

            _listenerTask = Task.Run(() => ListenLoopAsync(_shutdownCts.Token));
            AppLogger.Info($"DockControl pipe server started on '{PipeName}'.");
        }

        public async Task StopAsync()
        {
            _shutdownCts.Cancel();

            if (_listenerTask == null)
            {
                return;
            }

            try
            {
                await _listenerTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // expected during shutdown
            }
            catch (Exception ex)
            {
                AppLogger.Error($"DockControl pipe server stopped with error: {ex.Message}");
            }
        }

        private async Task ListenLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

                    using var reader = new StreamReader(server);
                    var message = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(message))
                    {
                        continue;
                    }

                    await HandleIncomingMessageAsync(message).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"DockControl pipe listener error: {ex.Message}");
                }
            }
        }

        private async Task HandleIncomingMessageAsync(string message)
        {
            try
            {
                var request = JsonSerializer.Deserialize<EscCommandRequest>(message, _jsonOptions);
                if (request == null || string.IsNullOrWhiteSpace(request.Command))
                {
                    AppLogger.Warning("DockControl sent an invalid command payload.");
                    return;
                }

                AppLogger.Info($"DockControl command received: {request.Command}");
                await _commandHandler(request).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                AppLogger.Warning($"DockControl payload JSON parse failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"DockControl command handling failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _shutdownCts.Cancel();
            _shutdownCts.Dispose();
        }
    }
}
