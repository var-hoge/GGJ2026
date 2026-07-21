using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PhantomCatWorks.RealtimeP2PKit.Editor
{
    /// <summary>
    /// "RealtimeP2PKit &gt; Connection Settings" - lets a developer switch between a
    /// "Local" (e.g. `wrangler dev` on localhost) and "Remote" (deployed) backend, and
    /// edit each environment's own matchmaking API URL / signaling WebSocket URL / STUN
    /// server list, all persisted per-machine in PlayerPrefs (see P2PEndpoints).
    ///
    /// Everything in this window is Editor-only by construction: it lives under an
    /// Editor/-only asmdef and is never compiled into a Player build. The environment
    /// switch and the "Network Logging" toggle it also exposes both read/write
    /// PlayerPrefs directly through P2PEndpoints / P2PNetworkLogger, which themselves
    /// only honor PlayerPrefs when UNITY_EDITOR is defined - so even code that isn't in
    /// this window can't accidentally end up reading a stale Editor-only setting at
    /// runtime in a build.
    /// </summary>
    public class P2PConnectionSettingsWindow : EditorWindow
    {
        private bool _foldoutEnvironment = true;
        private bool _foldoutLocal = true;
        private bool _foldoutRemote = true;
        private bool _foldoutLogging = true;

        private P2PEnvironment _environment;

        private string _localMatchmakingApiUrl;
        private string _localSignalingWebSocketUrl;
        private List<string> _localStunServerUrls;

        private string _remoteMatchmakingApiUrl;
        private string _remoteSignalingWebSocketUrl;
        private List<string> _remoteStunServerUrls;

        private bool _networkLoggingEnabled;

        private Vector2 _scrollPos;

        [MenuItem("RealtimeP2PKit/Connection Settings")]
        private static void Open()
        {
            GetWindow<P2PConnectionSettingsWindow>("P2P Connection Settings");
        }

        private void OnEnable()
        {
            LoadFromPlayerPrefs();
        }

        private void LoadFromPlayerPrefs()
        {
            _environment = P2PEndpoints.GetCurrentEnvironment();

            _localMatchmakingApiUrl = PlayerPrefs.GetString(P2PEndpoints.PrefKeyLocalMatchmakingApiUrl, P2PEndpoints.DefaultLocalMatchmakingApiUrl);
            _localSignalingWebSocketUrl = PlayerPrefs.GetString(P2PEndpoints.PrefKeyLocalSignalingWebSocketUrl, P2PEndpoints.DefaultLocalSignalingWebSocketUrl);
            _localStunServerUrls = P2PEndpoints.LoadStunServerUrls(P2PEndpoints.PrefKeyLocalStunServerUrls);

            _remoteMatchmakingApiUrl = PlayerPrefs.GetString(P2PEndpoints.PrefKeyRemoteMatchmakingApiUrl, P2PEndpoints.DefaultRemoteMatchmakingApiUrl);
            _remoteSignalingWebSocketUrl = PlayerPrefs.GetString(P2PEndpoints.PrefKeyRemoteSignalingWebSocketUrl, P2PEndpoints.DefaultRemoteSignalingWebSocketUrl);
            _remoteStunServerUrls = P2PEndpoints.LoadStunServerUrls(P2PEndpoints.PrefKeyRemoteStunServerUrls);

            _networkLoggingEnabled = P2PNetworkLogger.IsEnabled;
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.HelpBox(
                "この画面の設定はUnityEditor上でのみ有効です。ビルドしたアプリは常にRemoteの" +
                "ハードコードされた既定値(P2PEndpoints.DefaultRemote*)を使用し、ここで保存した値は参照しません。",
                MessageType.Info);
            EditorGUILayout.Space();

            WithInFoldoutBlock("Environment", ref _foldoutEnvironment, () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("現在の接続先", GUILayout.Width(100));
                var newEnv = (P2PEnvironment)EditorGUILayout.EnumPopup(_environment);
                EditorGUILayout.EndHorizontal();
                if (newEnv != _environment)
                {
                    _environment = newEnv;
                    P2PEndpoints.SetCurrentEnvironment(_environment);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("実際に参照されるURL", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Web API: " + P2PEndpoints.GetMatchmakingApiUrl(), EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Signaling: " + P2PEndpoints.GetSignalingWebSocketUrl(), EditorStyles.miniLabel);
            });

            WithInFoldoutBlock("Local", ref _foldoutLocal, () =>
            {
                DrawEndpointFields(ref _localMatchmakingApiUrl, ref _localSignalingWebSocketUrl, _localStunServerUrls);
                if (GUILayout.Button("Save Local"))
                {
                    PlayerPrefs.SetString(P2PEndpoints.PrefKeyLocalMatchmakingApiUrl, _localMatchmakingApiUrl);
                    PlayerPrefs.SetString(P2PEndpoints.PrefKeyLocalSignalingWebSocketUrl, _localSignalingWebSocketUrl);
                    P2PEndpoints.SaveStunServerUrls(P2PEndpoints.PrefKeyLocalStunServerUrls, _localStunServerUrls);
                    PlayerPrefs.Save();
                }
            });

            WithInFoldoutBlock("Remote", ref _foldoutRemote, () =>
            {
                DrawEndpointFields(ref _remoteMatchmakingApiUrl, ref _remoteSignalingWebSocketUrl, _remoteStunServerUrls);
                if (GUILayout.Button("Save Remote"))
                {
                    PlayerPrefs.SetString(P2PEndpoints.PrefKeyRemoteMatchmakingApiUrl, _remoteMatchmakingApiUrl);
                    PlayerPrefs.SetString(P2PEndpoints.PrefKeyRemoteSignalingWebSocketUrl, _remoteSignalingWebSocketUrl);
                    P2PEndpoints.SaveStunServerUrls(P2PEndpoints.PrefKeyRemoteStunServerUrls, _remoteStunServerUrls);
                    PlayerPrefs.Save();
                }
            });

            WithInFoldoutBlock("Network Logging", ref _foldoutLogging, () =>
            {
                EditorGUILayout.HelpBox(
                    "HTTP(マッチングAPI)/WebSocket(シグナリング)/WebRTC DataChannelの送受信内容を" +
                    "そのままログ出力します。UnityEditor上でのみON/OFFを切り替えられ、この設定自体もビルドには" +
                    "含まれません(ビルドしたアプリでは常にOFFです)。",
                    MessageType.None);
                var newValue = EditorGUILayout.Toggle("Enable Network Logging", _networkLoggingEnabled);
                if (newValue != _networkLoggingEnabled)
                {
                    _networkLoggingEnabled = newValue;
                    P2PNetworkLogger.IsEnabled = _networkLoggingEnabled;
                }
            });

            EditorGUILayout.EndScrollView();
        }

        private void DrawEndpointFields(ref string matchmakingApiUrl, ref string signalingWebSocketUrl, List<string> stunServerUrls)
        {
            matchmakingApiUrl = EditorGUILayout.TextField("Web API URL", matchmakingApiUrl);
            signalingWebSocketUrl = EditorGUILayout.TextField("Signaling WebSocket URL", signalingWebSocketUrl);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("STUN Server URLs (上から順に使用)", EditorStyles.boldLabel);
            for (var i = 0; i < stunServerUrls.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                stunServerUrls[i] = EditorGUILayout.TextField(stunServerUrls[i]);
                GUI.enabled = i > 0;
                if (GUILayout.Button("↑", GUILayout.Width(24)))
                {
                    (stunServerUrls[i - 1], stunServerUrls[i]) = (stunServerUrls[i], stunServerUrls[i - 1]);
                }
                GUI.enabled = i < stunServerUrls.Count - 1;
                if (GUILayout.Button("↓", GUILayout.Width(24)))
                {
                    (stunServerUrls[i + 1], stunServerUrls[i]) = (stunServerUrls[i], stunServerUrls[i + 1]);
                }
                GUI.enabled = true;
                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    stunServerUrls.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Add STUN Server"))
            {
                stunServerUrls.Add("stun:");
            }
        }

        private static void WithInFoldoutBlock(string title, ref bool foldout, System.Action callback)
        {
            EditorGUI.indentLevel = 0;
            EditorGUILayout.BeginVertical(GUI.skin.box);
            foldout = EditorGUILayout.Foldout(foldout, title, true);
            if (foldout)
            {
                EditorGUI.indentLevel = 1;
                EditorGUILayout.Space();
                callback();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
    }
}
