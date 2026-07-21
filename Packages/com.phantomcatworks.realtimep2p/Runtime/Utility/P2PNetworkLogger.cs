using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PhantomCatWorks.RealtimeP2PKit
{
    /// <summary>
    /// Dedicated logger for raw network traffic: HTTP matchmaking requests/responses,
    /// WebSocket signaling messages, and WebRTC data-channel payloads. Modeled after a
    /// typical Unity SDK's HTTPLogger pattern (request/response pairing, RTT, size,
    /// color-coded errors).
    ///
    /// This is intentionally separate from P2PLogger/P2PConfig.LogLevel: it is a single
    /// on/off toggle, controllable ONLY from the Unity Editor (via
    /// P2PConnectionSettingsWindow, "Network Logging" section), persisted per-machine in
    /// PlayerPrefs. Outside the Editor (i.e. in any Player build) this is always disabled
    /// and cannot be turned on - there is no UI for it and IsEnabled always returns false,
    /// so none of this traffic is ever logged in a shipped build.
    /// </summary>
    public static class P2PNetworkLogger
    {
        public const string PrefKeyEnabled = "RealtimeP2PKit.NetworkLoggingEnabled";
        private const string Tag = "[RealtimeP2PKit.Net]";

        public static bool IsEnabled
        {
#if UNITY_EDITOR
            get => PlayerPrefs.GetInt(PrefKeyEnabled, 0) == 1;
            set => PlayerPrefs.SetInt(PrefKeyEnabled, value ? 1 : 0);
#else
            get => false;
            // No-op outside the Editor: network logging can never be turned on in a build.
            set { }
#endif
        }

        // ---------------------------------------------------------------
        // HTTP (matchmaking REST API)
        // ---------------------------------------------------------------

        internal static void LogHttpRequest(string method, string url, string body)
        {
            if (!IsEnabled) return;
            var sb = new StringBuilder();
            sb.Append("-> ").Append(method.ToUpperInvariant()).Append(' ').Append(url);
            if (!string.IsNullOrEmpty(body))
            {
                sb.Append('\n').Append(body);
            }
            Debug.Log($"{Tag} {sb}");
        }

        internal static void LogHttpResponse(string method, string url, long statusCode, bool isError,
            string body, TimeSpan elapsed)
        {
            if (!IsEnabled) return;
            var sb = new StringBuilder();
            var openColor = isError ? "<color=red>" : "";
            var closeColor = isError ? "</color>" : "";
            sb.Append(openColor);
            sb.Append("<- ").Append(method.ToUpperInvariant()).Append(' ').Append(url).Append('\n');
            sb.Append(statusCode).Append("  (RTT: ").Append((int)elapsed.TotalMilliseconds).Append("ms");
            if (!string.IsNullOrEmpty(body))
            {
                sb.Append(", Size: ").Append(FormatSize(Encoding.UTF8.GetByteCount(body)));
            }
            sb.Append(')');
            sb.Append(closeColor);
            if (!string.IsNullOrEmpty(body))
            {
                sb.Append('\n').Append(body);
            }
            Debug.Log($"{Tag} {sb}");
        }

        // ---------------------------------------------------------------
        // WebSocket (signaling: lobby push / room offer-answer-ICE relay)
        // ---------------------------------------------------------------

        internal static void LogWebSocketOpen(string context, string url)
        {
            if (!IsEnabled) return;
            Debug.Log($"{Tag} [{context}] websocket OPEN {url}");
        }

        internal static void LogWebSocketClose(string context, string url, string reason)
        {
            if (!IsEnabled) return;
            Debug.Log($"{Tag} [{context}] websocket CLOSED {url} ({reason})");
        }

        internal static void LogWebSocketSend(string context, string message)
        {
            if (!IsEnabled) return;
            Debug.Log($"{Tag} [{context}] -> ws {message}");
        }

        internal static void LogWebSocketReceive(string context, string message)
        {
            if (!IsEnabled) return;
            Debug.Log($"{Tag} [{context}] <- ws {message}");
        }

        // ---------------------------------------------------------------
        // WebRTC (data channel)
        // ---------------------------------------------------------------

        internal static void LogWebRtcSend(byte[] payload)
        {
            if (!IsEnabled) return;
            Debug.Log($"{Tag} [DataChannel] -> {P2PLogger.ToHexPreview(payload)}");
        }

        internal static void LogWebRtcReceive(byte[] payload)
        {
            if (!IsEnabled) return;
            Debug.Log($"{Tag} [DataChannel] <- {P2PLogger.ToHexPreview(payload)}");
        }

        private static string FormatSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB" };
            double len = bytes;
            var order = 0;
            while (len >= 1024 && order + 1 < units.Length)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.#} {units[order]}";
        }
    }
}
