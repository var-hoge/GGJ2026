using System;
using System.Collections;
using MessagePack;
using PhantomCatWorks.RealtimeP2PKit;
using UnityEngine;
using KanKikuchi.AudioManager;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroStoryManager : StoryTextManager
{
    private const byte IntroStoryReadyPacketId = 3;
    private const float ReadyResendIntervalSeconds = 0.5f;

    [MessagePackObject(AllowPrivate = true)]
    internal struct IntroStoryReadyPacket
    {
        [Key(0)] public bool Ready;
    }

    [Serializable]
    public class SceneMsgs
    {
        public Sprite image;
        public SESound[] sounds;
    }

    [Serializable]
    class StoryLine
    {
        public string japanese;
        public string english;
        public string german;
    }

    [Serializable]
    class StoryScript
    {
        public StoryLine[] scenes;
    }

    private const string StoryTextResourcePath = "StoryText/IntroStory";

    [SerializeField] private SceneMsgs[] sceneMsgs;
    [SerializeField] private RawImage rawImage;

    private StoryLine[] storyLines;
    private int sceneIndex = 0;
    private bool onlineReadySyncEnabled;
    private bool localPlayerReady;
    private bool opponentReady;
    private bool loadingInGame;

    private void Start()
    {
        var json = Resources.Load<TextAsset>(StoryTextResourcePath).text;
        storyLines = JsonUtility.FromJson<StoryScript>(json).scenes;

        // BGMの再生
        BGMManager.Instance.Play(BGMPath.MUSIC_CUTSCENE_LOOP);
        StartOnlineReadySync();
        Advance();
    }

    protected override void Advance()
    {
        if (sceneIndex > sceneMsgs.Length - 1)
        {
            RequestInGameStart();
            return;
        }
        else
        {
            var current = sceneMsgs[sceneIndex];
            rawImage.texture = current.image.texture;
            StartTyping(GetMessage(sceneIndex));
            PlaySound(current);
        }
        sceneIndex++;
    }

    private void StartOnlineReadySync()
    {
        var p2pManager = P2PManager.Instance;
        onlineReadySyncEnabled = p2pManager.IsOnlineMatch
            && p2pManager.Session.State == P2PSessionState.Connected;

        if (!onlineReadySyncEnabled)
        {
            return;
        }

        p2pManager.RegisterPacketHandler<IntroStoryReadyPacket>(
            IntroStoryReadyPacketId,
            OnOpponentReadyReceived);
        Debug.Log("[IntroStory] P2P session retained; waiting for both players to finish the story.");
    }

    private void RequestInGameStart()
    {
        if (!onlineReadySyncEnabled)
        {
            LoadInGame();
            return;
        }

        if (localPlayerReady)
        {
            return;
        }

        localPlayerReady = true;
        SendReadyPacket();
        StartCoroutine(ResendReadyUntilGameStarts());
        Debug.Log("[IntroStory] Local story finished; waiting for opponent.");
        TryLoadInGame();
    }

    private void OnOpponentReadyReceived(IntroStoryReadyPacket packet)
    {
        if (!packet.Ready)
        {
            return;
        }

        opponentReady = true;
        Debug.Log("[IntroStory] Opponent is ready to start the game.");
        TryLoadInGame();
    }

    private IEnumerator ResendReadyUntilGameStarts()
    {
        while (!loadingInGame && !opponentReady)
        {
            yield return new WaitForSecondsRealtime(ReadyResendIntervalSeconds);
            SendReadyPacket();
        }
    }

    private void SendReadyPacket()
    {
        P2PManager.Instance.Send(IntroStoryReadyPacketId, new IntroStoryReadyPacket { Ready = true });
    }

    private void TryLoadInGame()
    {
        if (!localPlayerReady || !opponentReady)
        {
            return;
        }

        LoadInGame();
    }

    private void LoadInGame()
    {
        if (loadingInGame)
        {
            return;
        }

        loadingInGame = true;
        SceneManager.LoadScene("InGame");
    }

    private void OnDestroy()
    {
        if (onlineReadySyncEnabled && P2PManager.TryGetExistingInstance(out var p2pManager))
        {
            p2pManager.UnregisterPacketHandler(IntroStoryReadyPacketId);
        }
    }

    string GetMessage(int index)
    {
        var line = storyLines[index];
        if (!LanguageManager.Exist)
        {
            return line.japanese;
        }

        return LanguageManager.Instance.CurrentLanguage switch
        {
            Language.English => line.english,
            Language.German => line.german,
            _ => line.japanese,
        };
    }

    void PlaySound(SceneMsgs current)
    {
        if (current.sounds == null) return;

        SEManager.Instance.Stop();

        foreach (var s in current.sounds)
        {
            if (!string.IsNullOrEmpty(s.sound))
            {
                SEManager.Instance.Play(s.sound, s.volume);
            }
        }
    }
}
