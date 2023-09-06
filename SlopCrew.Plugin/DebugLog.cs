using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using Reptile;
using SlopCrew.Common.Network;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Network.Serverbound;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace SlopCrew.Plugin;

public class DebugLog : IDisposable {
    private const int BufferSize = 5_000;
    private List<string> storage = new();
    private bool logSaveActivated;

    // These get spammed... a lot... let's focus on only logging bugs we care about
    private static List<Type> SkipLoggingPackets = new() {
        typeof(ClientboundPlayerPositionUpdate),
        typeof(ClientboundPlayerScoreUpdate),
        typeof(ClientboundPlayerVisualUpdate),
        typeof(ClientboundPlayerAnimation),
        typeof(ServerboundPositionUpdate),
        typeof(ServerboundScoreUpdate),
        typeof(ServerboundVisualUpdate),
        typeof(ServerboundAnimation)
    };

    public DebugLog() {
        Core.OnUpdate += this.Update;
    }

    public void Dispose() {
        Core.OnUpdate -= this.Update;
    }

    public void LogPacketIncoming(NetworkPacket packet, byte[] serialized) {
        if (SkipLoggingPackets.Contains(packet.GetType())) return;
        this.Log($"[Packet in] {packet.GetType().Name} {this.BytesToHex(serialized)}");
    }

    public void LogPacketOutgoing(NetworkPacket packet, byte[] serialized) {
        if (SkipLoggingPackets.Contains(packet.GetType())) return;
        this.Log($"[Packet out] {packet.GetType().Name} {this.BytesToHex(serialized)}");
    }

    private string BytesToHex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "");

    public void Log(string message) {
        var time = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
        lock (this.storage) {
            this.storage.Add($"[{time}] {message}");
            if (this.storage.Count > BufferSize) this.storage.RemoveAt(0);
        }
    }

    private void Update() {
        var ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        var s = Input.GetKey(KeyCode.S);
        var c = Input.GetKey(KeyCode.C);
        var l = Input.GetKey(KeyCode.L);
        var any = ctrl || s || c || l;
        var all = ctrl && s && c && l;

        if (all && !this.logSaveActivated) {
            this.logSaveActivated = true;
            this.SaveLog();
        } else if (!any) {
            this.logSaveActivated = false;
        }
    }

    private void SaveLog() {
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
        var logPath = Path.Combine(Path.GetTempPath(), $"slopcrew-{now}.log.gz");
        using var stream = File.OpenWrite(logPath);
        using var gz = new GZipStream(stream, CompressionLevel.Optimal);

        lock (this.storage) {
            foreach (var line in this.storage) {
                var bytes = System.Text.Encoding.UTF8.GetBytes(line + "\n");
                gz.Write(bytes, 0, bytes.Length);
            }

            this.storage.Clear();
        }

        gz.Flush();

        Process.Start("explorer.exe", $"/select,\"{logPath}\"");
    }
}
