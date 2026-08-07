using Newtonsoft.Json;
using PhantomCatWorks.RealtimeP2PKit;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class RoomsAdminView : MonoBehaviour
{
    [SerializeField] private GridLayoutGroup roomListRoot;
    [SerializeField] private GameObject roomCell;
    [SerializeField] private Transform waitingView;
    async void Start()
    {
       await RefreshRooms();
    }

    public void StartGame(MachingRoom room)
    {
        P2PManager.Instance.JoinRoom(PlayerData.LoadSavedPlayerId, room);
        this.gameObject.SetActive(false);
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

    public void OnClickNewRoomButton ()
    {
        P2PManager.Instance.CreateRoom(PlayerData.LoadSavedPlayerId);
        roomCell.gameObject.SetActive(false);
        this.waitingView.gameObject.SetActive(true);
    }

    public void OnClickSearchRoomButton()
    {
        _ = RefreshRooms();
    }
}
