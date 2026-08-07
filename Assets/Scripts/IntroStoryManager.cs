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

        /// <summary>
        /// 猫の配置に使う乱数シード。部屋を作った側 (IsInitiator) が決めた値だけが有効。
        /// InGame へ移る前に必ず往復するこのパケットに相乗りさせている。
        /// </summary>
        [Key(1)] public int CatSeed;
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

        // 部屋を作った側が猫の配置シードを決め、以降の準備完了パケットに載せて配る。
        // 片方だけが決めないと、互いに違うシードを送り合って配置が食い違う
        if (p2pManager.Session.IsInitiator)
        {
            // このファイルは using System; もしているため Random だけだと曖昧になる
            MatchRandomSeed.Set(UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            Debug.Log($"[IntroStory] cat spawn seed generated: {MatchRandomSeed.Value}");
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

        // シードは部屋を作った側が決める。参加側はここで受け取る。
        // TryLoadInGame より先に反映しないと、シードを持たないまま InGame へ入り
        // 猫の配置が食い違う
        if (!P2PManager.Instance.Session.IsInitiator)
        {
            MatchRandomSeed.Set(packet.CatSeed);
            Debug.Log($"[IntroStory] cat spawn seed received: {packet.CatSeed}");
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
        P2PManager.Instance.Send(IntroStoryReadyPacketId, new IntroStoryReadyPacket
        {
            Ready = true,
            // 参加側は 0 を送るが、受け手 (部屋を作った側) は自分の値を使うので無害
            CatSeed = MatchRandomSeed.Value,
        });
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
