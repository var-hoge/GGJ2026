using PhantomCatWorks.RealtimeP2PKit;
using System;
using TMPro;
using UnityEngine;

public class RoomTextField : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    private MachingRoom machingRoom;
    public Action<MachingRoom> OnClickRoom = null;

    public void SetMatchingRoom(MachingRoom room)
    {
        this.machingRoom = room;
        UpdateView();
    }

    private void UpdateView()
    {
        titleText.text = $"{machingRoom.id}";
    }

    public void OnClickRoomButton()
    {
        if (this.OnClickRoom != null)
        {
            this.OnClickRoom(this.machingRoom);
        }
    }
}
