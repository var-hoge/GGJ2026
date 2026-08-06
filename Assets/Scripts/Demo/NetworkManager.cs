using UnityEngine;

namespace PhantomCatWorks.RealtimeP2PKit.Demo
{
    /// <summary>
    /// Entry point for the P2P demo scene. Wires up P2PManager and kicks off
    /// matchmaking on Start. This is the "integration point" between the reusable
    /// RealtimeP2PKit library and this specific demo game.
    /// </summary>
    public class NetworkManager : SingletonBehaviour<NetworkManager>
    {
        [SerializeField] private P2PConfig _config;
        [SerializeField] private GameObject _localPlayerPrefab;
        [SerializeField] private GameObject _remotePlayerPrefab;
        private bool _playersSpawned;

        // Called by SingletonBehaviour.Awake(). Keeping initialization here
        // avoids hiding Unity's Awake message in the base class.
        public override void SingleAwake()
        {
            var localPlayerId = PlayerData.LoadSavedPlayerId;
            Debug.Log($"[Demo] local playerId = {localPlayerId}");

            P2PManager.Instance.Initialize(_config);
            P2PManager.Instance.StateChanged += state => Debug.Log($"[Demo] session state -> {state}");
            P2PManager.Instance.Matched += info => Debug.Log($"[Demo] matched with {info.OpponentId} in room {info.RoomId}");
            P2PManager.Instance.DataChannelReady += OnDataChannelReady;
            P2PManager.Instance.ConnectionClosed += reason => Debug.LogWarning($"[Demo] connection closed: {reason}");
            P2PManager.Instance.OpponentLeft += () => Debug.LogWarning("[Demo] opponent left the room");

        }

        private void OnDataChannelReady()
        {
            SpawnPlayersIfNeeded();
        }

        private void Update()
        {
            // The manager's Connected state is authoritative. This fallback
            // covers a DataChannelReady event that fired before a listener was
            // attached, or was not reported by a Unity.WebRTC package version.
            if (P2PManager.Instance.Session.State == P2PSessionState.Connected)
                SpawnPlayersIfNeeded();
        }

        private void SpawnPlayersIfNeeded()
        {
            if (_playersSpawned) return;
            if (_localPlayerPrefab == null || _remotePlayerPrefab == null)
            {
                Debug.LogError("[Demo] LocalPlayer / RemotePlayer prefab is not assigned on NetworkManager.");
                return;
            }
            _playersSpawned = true;
            Debug.Log("[Demo] data channel ready, spawning player objects");
            Instantiate(_localPlayerPrefab, Vector3.zero, Quaternion.identity);
            var remote = Instantiate(_remotePlayerPrefab, new Vector3(2, 0, 0), Quaternion.identity);
            remote.AddComponent<DemoRemotePlayerSync>();
        }

        private void OnDestroy()
        {
            P2PManager.Instance.Disconnect();
        }
    }
}
