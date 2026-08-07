using System;
using UnityEngine;
using KanKikuchi.AudioManager;
using PhantomCatWorks.RealtimeP2PKit;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroStoryManager : StoryTextManager
{
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

    [Header("通信対戦で相手を待っている間の表示")]
    [Tooltip("読み終えたあと、相手が読み終わるまで出す。未設定でも動く")]
    [SerializeField] private GameObject waitingForOpponentText;

    private StoryLine[] storyLines;
    private int sceneIndex = 0;

    /// <summary>読み終えて、相手が読み終わるのを待っている状態。</summary>
    private bool waitingForOpponent;

    private void Start()
    {
        var json = Resources.Load<TextAsset>(StoryTextResourcePath).text;
        storyLines = JsonUtility.FromJson<StoryScript>(json).scenes;

        if (waitingForOpponentText != null) waitingForOpponentText.SetActive(false);

        // 相手の「読み終えた」通知を取りこぼさないよう、画面に入った時点で用意しておく
        NetSceneSync.Prepare(NetSceneStage.Intro);

        // BGMの再生
        BGMManager.Instance.Play(BGMPath.MUSIC_CUTSCENE_LOOP);
        Advance();
    }

    protected override void Update()
    {
        if (waitingForOpponent)
        {
            // 待機中は文字送りの入力を受け付けない。相手が揃ったら進む
            if (NetSceneSync.IsOpponentReady(NetSceneStage.Intro))
            {
                SceneManager.LoadScene("InGame");
            }

            return;
        }

        base.Update();
    }

    protected override void Advance()
    {
        if (sceneIndex > sceneMsgs.Length - 1)
        {
            GoToInGame();
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

    /// <summary>
    /// ソロプレイなら即座に InGame へ。通信対戦なら、相手も読み終えるまで待ってから
    /// 両者ほぼ同時に遷移する (先に読み終えた側だけが始まってしまうのを防ぐ)。
    /// </summary>
    void GoToInGame()
    {
        if (!NetSession.IsActive)
        {
            SceneManager.LoadScene("InGame");
            return;
        }

        waitingForOpponent = true;
        if (waitingForOpponentText != null) waitingForOpponentText.SetActive(true);

        NetSceneSync.MarkLocalReady(NetSceneStage.Intro);
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
