using System;
using KanKikuchi.AudioManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingManager : StoryTextManager
{
    [Serializable]
    public class SceneMsgs
    {
        public String[] msgs;
    }

    [SerializeField] private SceneMsgs[] sceneMsgs;

    private int sceneIndex = 0;
    private int textIndex = 0;

    private string[] Messages => sceneMsgs[0].msgs;

    private void Start()
    {
        ShowCurrentText();
        SEManager.Instance.Play(SEPath.AUDIO_ENDING);
        BGMManager.Instance.Play(BGMPath.MUSIC_ENDING_LOOP);
    }

    protected override void Advance()
    {
        if (textIndex >= Messages.Length - 1)
        {
            SceneManager.LoadScene("Title");
            Debug.Log("文字送り終了");
            return;
        }

        // スライドの最終テキストでない場合、次のテキストを表示
        textIndex++;
        var textArray = sceneMsgs[sceneIndex].msgs;
        if (textIndex < textArray.Length)
        {
            ShowCurrentText();
            return;
        }

        // 最終テキストの場合、次のスライドに移動
        sceneIndex++;
        textIndex = 0;

        // 最終スライドの場合、テキスト送り終了
        if (sceneIndex >= sceneMsgs.Length)
        {
            return;
        }

        ShowCurrentText();
    }

    private void ShowCurrentText()
    {
        StartTyping(sceneMsgs[sceneIndex].msgs[textIndex]);
    }
}
