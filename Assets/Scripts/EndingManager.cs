using System;
using KanKikuchi.AudioManager;
using PhantomCatWorks.RealtimeP2PKit;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingManager : StoryTextManager
{
    [Serializable]
    class StoryLine
    {
        public string japanese;
        public string english;
        public string german;
    }

    [Serializable]
    class StoryScene
    {
        public StoryLine[] lines;
    }

    [Serializable]
    class StoryScript
    {
        public StoryScene[] scenes;
    }

    [SerializeField] private string storyTextResourcePath;

    [Header("通信対戦で相手を待っている間の表示")]
    [Tooltip("読み終えたあと、相手が読み終わるまで出す。未設定でも動く")]
    [SerializeField] private GameObject waitingForOpponentText;

    private StoryScene[] storyScenes;
    private int sceneIndex = 0;
    private int textIndex = 0;

    /// <summary>読み終えて、相手が読み終わるのを待っている状態。</summary>
    private bool waitingForOpponent;

    private void Start()
    {
        var json = Resources.Load<TextAsset>(storyTextResourcePath).text;
        storyScenes = JsonUtility.FromJson<StoryScript>(json).scenes;

        if (waitingForOpponentText != null) waitingForOpponentText.SetActive(false);

        // 相手の「読み終えた」通知を取りこぼさないよう、画面に入った時点で用意しておく
        NetSceneSync.Prepare(NetSceneStage.Ending);

        ShowCurrentText();
        SEManager.Instance.Play(SEPath.AUDIO_ENDING);
        BGMManager.Instance.Play(BGMPath.MUSIC_ENDING_LOOP);
    }

    protected override void Update()
    {
        if (waitingForOpponent)
        {
            // 待機中は文字送りの入力を受け付けない。相手が揃ったら進む
            if (NetSceneSync.IsOpponentReady(NetSceneStage.Ending))
            {
                GoToTitle();
            }

            return;
        }

        base.Update();
    }

    /// <summary>
    /// ソロプレイなら即座にタイトルへ。通信対戦なら、相手も読み終えるまで待ってから
    /// 両者ほぼ同時に遷移する。
    /// </summary>
    private void FinishStory()
    {
        if (!NetSession.IsActive)
        {
            GoToTitle();
            return;
        }

        waitingForOpponent = true;
        if (waitingForOpponentText != null) waitingForOpponentText.SetActive(true);

        NetSceneSync.MarkLocalReady(NetSceneStage.Ending);
    }

    /// <summary>
    /// タイトルへ戻る。対戦が終わったので、遷移する直前に通信を完全に畳む。
    /// ここで畳まないと、タイトルに戻ったあともロビー広告が出続けて
    /// 終わったはずの部屋が他の端末から見えてしまう。
    /// </summary>
    private void GoToTitle()
    {
        if (NetSession.Exists)
        {
            NetSession.Instance.Shutdown();
        }

        NetGameState.Clear();
        SceneManager.LoadScene("Title");
        Debug.Log("文字送り終了");
    }

    protected override void Advance()
    {
        var lines = storyScenes[sceneIndex].lines;

        if (textIndex >= lines.Length - 1)
        {
            FinishStory();
            return;
        }

        // スライドの最終テキストでない場合、次のテキストを表示
        textIndex++;
        if (textIndex < lines.Length)
        {
            ShowCurrentText();
            return;
        }

        // 最終テキストの場合、次のスライドに移動
        sceneIndex++;
        textIndex = 0;

        // 最終スライドの場合、テキスト送り終了
        if (sceneIndex >= storyScenes.Length)
        {
            return;
        }

        ShowCurrentText();
    }

    private void ShowCurrentText()
    {
        StartTyping(GetMessage(storyScenes[sceneIndex].lines[textIndex]));
    }

    private string GetMessage(StoryLine line)
    {
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
}
