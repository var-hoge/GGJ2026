using System;
using KanKikuchi.AudioManager;
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

    private StoryScene[] storyScenes;
    private int sceneIndex = 0;
    private int textIndex = 0;

    private void Start()
    {
        var json = Resources.Load<TextAsset>(storyTextResourcePath).text;
        storyScenes = JsonUtility.FromJson<StoryScript>(json).scenes;

        ShowCurrentText();
        SEManager.Instance.Play(SEPath.AUDIO_ENDING);
        BGMManager.Instance.Play(BGMPath.MUSIC_ENDING_LOOP);
    }

    protected override void Advance()
    {
        var lines = storyScenes[sceneIndex].lines;

        if (textIndex >= lines.Length - 1)
        {
            SceneManager.LoadScene("Title");
            Debug.Log("文字送り終了");
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
