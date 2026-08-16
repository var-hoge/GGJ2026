using PhantomCatWorks.RealtimeP2PKit;
using System.Collections;
using UnityEngine;

public class PlayerData {

    private const string PLAYER_ID_KEY = "SavedPlayerId";

    public static string LoadSavedPlayerId {
        get {
            string playerId = PlayerPrefs.GetString(PLAYER_ID_KEY);
            if(string.IsNullOrEmpty(playerId)) {
                playerId = "player-" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
                PlayerPrefs.SetString(PLAYER_ID_KEY, playerId);
            }
            return playerId;
        }
    }

}
