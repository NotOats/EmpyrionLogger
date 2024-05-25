using Eleon.Modding;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EmpyrionLogger
{
    public class EntryPoint : IMod, ModInterface
    {
        private readonly CancellationTokenSource _shutdownCts = new CancellationTokenSource();
        private readonly AsyncManualResetEvent _forceUpdate = new AsyncManualResetEvent();
        private readonly StreamWriter? _writer;
        private readonly bool _allocated = false;

        private IModApi? _modApi;
        private int ProcessId { get; } = Process.GetCurrentProcess().Id;

        public EntryPoint()
        {
            if (!Kernel32.AttachConsole((uint)ProcessId))
            {
                Kernel32.AllocConsole();
                _allocated = true;
            }

            _writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            EleonUtils.LogCallback += LogOutput;
        }


        #region ModInterface
        public void Game_Start(ModGameAPI dediAPI)
        {
        }

        public void Game_Update()
        {
        }

        public void Game_Exit()
        {
        }

        public void Game_Event(CmdId eventId, ushort seqNr, object data)
        {
            if (_modApi == null)
                return;

            // ModInterface.Game_Event is only ever ran on the dedicated server, use this to update dedi console title
            if (eventId == CmdId.Event_Playfield_Loaded ||
                eventId == CmdId.Event_Playfield_Unloaded)
                _forceUpdate.Set();
        }
        #endregion

        #region IMod
        public void Init(IModApi modApi)
        {
            _modApi = modApi;

            // OnPlayfieldLoaded is only called on playfield servers, use this to update pf console title
            _modApi.Application.OnPlayfieldLoaded += pf => { _forceUpdate.Set(); };
            _modApi.Application.OnPlayfieldUnloading += pf => { _forceUpdate.Set(); };

            UpdateConsoleTitle();

            _ = Task.Run(async () =>
            {
                var shutdownToken = _shutdownCts.Token;

                while (!shutdownToken.IsCancellationRequested)
                {
                    await _forceUpdate.WaitAsync(10_000, shutdownToken);

                    if (shutdownToken.IsCancellationRequested)
                        return;

                    // Reset forceUpdate if it was set
                    if (_forceUpdate.IsSet)
                        _forceUpdate.Reset();

                    UpdateConsoleTitle();
                }
            });
        }

        public void Shutdown()
        {
            _shutdownCts.Cancel();

            EleonUtils.LogCallback -= LogOutput;

            _writer?.Flush();
            _writer?.Dispose();

            if (_allocated)
            {
                Kernel32.FreeConsole();
            }
        }
        #endregion

        private void LogOutput(string condition, string stackTrace, LogType type)
        {
            var prefix = type switch
            {
                LogType.Error => "ERR",
                LogType.Warning => "WRN",
                LogType.Log => "LOG",
                LogType.Exception => "EXC",
                _ => "UNK"
            };

            _writer?.WriteLine($"{DateTime.Now.ToShortTimeString()} -{prefix}- {condition}");
            _writer?.Flush();
        }

        private void UpdateConsoleTitle()
        {
            var sb = new StringBuilder();

            var mode = _modApi?.Application.Mode switch
            {
                ApplicationMode.SinglePlayer => "SinglePlayer",
                ApplicationMode.Client => "Client",
                ApplicationMode.DedicatedServer => "Dedicated",
                ApplicationMode.PlayfieldServer => "Playfield",
                _ => "Unknown",
            };

            var mem = GC.GetTotalMemory(false) / 1048576;

            sb.Append($"{mode} ({ProcessId}) {mem,4:F1}MB");

            if (_modApi?.Application.Mode == ApplicationMode.DedicatedServer)
            {
                var pfs = _modApi.Application.GetPfServerInfos();
                var totalPfs = pfs.SelectMany(kvp => kvp.Value).Count();

                sb.Append($" - pf procs: {pfs.Count} - pfs loaded: {totalPfs}");
            }
            else if(_modApi?.Application.Mode == ApplicationMode.PlayfieldServer)
            {
                var pfs = _modApi.Application.GetPfServerInfos()
                    .SelectMany(kvp => kvp.Value)
                    .OrderBy(s => s);

                var pfString = string.Join(",", pfs);
                if (string.IsNullOrEmpty(pfString))
                    pfString = "N/A";

                sb.Append($" - pfs: {pfString}");
            }

            Kernel32.SetConsoleTitleA(sb.ToString());
        }
    }
}
