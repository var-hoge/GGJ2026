using System;
using UnityEngine;
using KanKikuchi.AudioManager;
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

    private StoryLine[] storyLines;
    private int sceneIndex = 0;

    private void Start()
    {
        var json = Resources.Load<TextAsset>(StoryTextResourcePath).text;
        storyLines = JsonUtility.FromJson<StoryScript>(json).scenes;

        // BGMの再生
        BGMManager.Instance.Play(BGMPath.MUSIC_CUTSCENE_LOOP);
        Advance();
    }

    protected override void Advance()
    {
        if (sceneIndex > sceneMsgs.Length - 1)
        {
            SceneManager.LoadScene("InGame");
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
