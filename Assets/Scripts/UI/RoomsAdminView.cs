using Newtonsoft.Json;
using PhantomCatWorks.RealtimeP2PKit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class RoomsAdminView : MonoBehaviour
{
    [SerializeField] private GridLayoutGroup roomListRoot;
    [SerializeField] private GameObject roomCell;
    [SerializeField] private Transform waitingView;
    private bool _matchStarted;

    public Action OnStartEnterLobby = null;

    async void Start()
    {
       P2PManager.Instance.DataChannelReady += OnDataChannelReady;
       await RefreshRooms();
       if (P2PManager.Instance.Session.State == P2PSessionState.Connected)
           OnDataChannelReady();
    }

    public async void StartGame(MachingRoom room)
    {
        try
        {
            await P2PManager.Instance.JoinRoom(PlayerData.LoadSavedPlayerId, room);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Rooms] failed to join room: {ex.Message}");
            await RefreshRooms();
        }
    }

    public async Task<List<MachingRoom>> LoadRooms()
    {
        var baseUrl = P2PEndpoints.GetMatchmakingApiUrl();
        var playerId = PlayerData.LoadSavedPlayerId;
        var responseJsonString = await HttpMatchmakingClient.HttpRequestAsync("GET", $"{baseUrl}/api/matchmaking/rooms?playerId={UnityEngine.Networking.UnityWebRequest.EscapeURL(playerId)}");
        return JsonConvert.DeserializeObject<List<MachingRoom>>(responseJsonString);
    }

    public async Task RefreshRooms()
    {
        roomCell.gameObject.SetActive(true);
        foreach (Transform child in roomListRoot.transform)
            if (child.gameObject != roomCell) Destroy(child.gameObject);

        var rooms = await LoadRooms();
        this.waitingView.gameObject.SetActive(rooms.Count <= 0);
        foreach (var room in rooms)
        {
            var roomTextField = Util.InstantiateTo<RoomTextField>(roomListRoot.gameObject, roomCell);
            roomTextField.SetMatchingRoom(room);
            roomTextField.OnClickRoom = StartGame;
        }
    }

    public async void OnClickNewRoomButton ()
    {
        try
        {
            await P2PManager.Instance.CreateRoom(PlayerData.LoadSavedPlayerId);
            roomCell.SetActive(false);
            waitingView.gameObject.SetActive(true);
            Debug.Log("[Rooms] room created; waiting for an opponent");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Rooms] failed to create room: {ex.Message}");
        }
    }

    public void OnClickSearchRoomButton()
    {
        _ = RefreshRooms();
    }

    private void OnDataChannelReady()
    {
        if (_matchStarted) return;
        _matchStarted = true;
        Debug.Log("[Rooms] P2P connection ready; entering character selection");
        OnStartEnterLobby?.Invoke();
    }

    private void OnDestroy()
    {
        P2PManager.Instance.DataChannelReady -= OnDataChannelReady;
    }
}
