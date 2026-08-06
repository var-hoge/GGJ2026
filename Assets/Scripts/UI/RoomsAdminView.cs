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
       var rooms = await LoadRooms();
       foreach(var room in rooms)
       {
            var roomTextField = Util.InstantiateTo<RoomTextField>(roomListRoot.gameObject, roomCell);
            roomTextField.SetMatchingRoom(room);
            roomTextField.OnClickRoom = StartGame;
       }
    }

    public void StartGame(MachingRoom room)
    {
        Debug.Log($"selectRoomId:{room.id}");
        P2PManager.Instance.StartMatchmaking(room.id);
        this.gameObject.SetActive(false);
    }

    public async Task<List<MachingRoom>> LoadRooms()
    {
        var baseUrl = P2PEndpoints.GetMatchmakingApiUrl();
        var responseJsonString = await HttpMatchmakingClient.HttpRequestAsync("GET", $"{baseUrl}/api/matchmaking/rooms");
        return JsonConvert.DeserializeObject<List<MachingRoom>>(responseJsonString);
    }

    public void OnClickNewRoomButton ()
    {
        Debug.Log("onClickNewRoomButton");
    }

    public void OnClickSearchRoomButton()
    {
        Debug.Log("onClickSearchRoomButton");
    }
}
