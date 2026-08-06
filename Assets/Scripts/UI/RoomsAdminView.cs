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

    async void Start()
    {
       await RefreshRooms();
    }

    public void StartGame(MachingRoom room)
    {
        Debug.Log($"selectRoomId:{room.id}");
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
        foreach (Transform child in roomListRoot.transform)
            if (child.gameObject != roomCell) Destroy(child.gameObject);

        var rooms = await LoadRooms();
        foreach (var room in rooms)
        {
            var roomTextField = Util.InstantiateTo<RoomTextField>(roomListRoot.gameObject, roomCell);
            roomTextField.SetMatchingRoom(room);
            roomTextField.OnClickRoom = StartGame;
        }
    }

    public void OnClickNewRoomButton ()
    {
        Debug.Log("onClickNewRoomButton");
        P2PManager.Instance.CreateRoom(PlayerData.LoadSavedPlayerId);
        this.gameObject.SetActive(false);
    }

    public void OnClickSearchRoomButton()
    {
        Debug.Log("onClickSearchRoomButton");
        _ = RefreshRooms();
    }
}
