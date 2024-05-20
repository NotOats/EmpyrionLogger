using Eleon.Modding;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EmpyrionLogger
{
    public class EntryPoint : IMod
    {
        private IModApi? _modApi;
        private StreamWriter? _writer;
        private bool _allocated = false;

        private int ProcessId { get; } = Process.GetCurrentProcess().Id;

        public EntryPoint()
        { 
        }

        public void Init(IModApi modApi)
        {
            _modApi = modApi;

            if (!Kernel32.AttachConsole((uint)ProcessId))
            {
                Kernel32.AllocConsole();
                _allocated = true;
            }

            UpdateConsoleTitle();

            _modApi.Application.OnPlayfieldLoaded += (pf) => { UpdateConsoleTitle(); };

            _writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            EleonUtils.LogCallback += LogOutput;
        }

        public void Shutdown()
        {
            EleonUtils.LogCallback -= LogOutput;

            _writer?.Flush();
            _writer?.Dispose();

            if (_allocated)
            {
                Kernel32.FreeConsole();
            }
        }

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

            sb.Append($"{mode} - pid: {ProcessId}");

            if(_modApi?.Application.Mode == ApplicationMode.PlayfieldServer)
            {
                var pfs = _modApi.Application.GetPfServerInfos()
                    .SelectMany(kvp => kvp.Value)
                    .OrderBy(s => s);

                var pfString = string.Join(",", pfs);

                sb.Append($" - pfs: {pfString}");
            }

            Kernel32.SetConsoleTitleA(sb.ToString());
        }
    }
}
