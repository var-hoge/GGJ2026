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
        public string[] msgs;
        public SESound[] sounds;
    }

    [SerializeField] private SceneMsgs[] sceneMsgs;
    [SerializeField] private RawImage rawImage;

    private int sceneIndex = 0;
    private int textIndex = 0;

    private void Start()
    {
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
            StartTyping(current.msgs[textIndex]);
            PlaySound(current);
        }
        sceneIndex++;
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
