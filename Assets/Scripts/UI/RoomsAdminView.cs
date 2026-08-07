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

    public Action OnStartEnterLobby = null;

    async void Start()
    {
       await RefreshRooms();
    }

    public async void StartGame(MachingRoom room)
    {
        await P2PManager.Instance.JoinRoom(PlayerData.LoadSavedPlayerId, room);
        if (OnStartEnterLobby != null)
        {
            OnStartEnterLobby();
        }
//        this.gameObject.SetActive(false);
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
        await P2PManager.Instance.CreateRoom(PlayerData.LoadSavedPlayerId);
        if (OnStartEnterLobby != null)
        {
            OnStartEnterLobby();
        }
//        roomCell.gameObject.SetActive(false);
//        this.waitingView.gameObject.SetActive(true);
    }

    public void OnClickSearchRoomButton()
    {
        _ = RefreshRooms();
    }
}
