using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Video;
using KanKikuchi.AudioManager;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;

public class IntroStoryManager : MonoBehaviour
{
    [Serializable]
    public class SceneMsgs
    {
        public String[] msgs;
    }

    [SerializeField] private TextMeshProUGUI textUI = null;
    [SerializeField] private float charInterval = 0.05f;

    [SerializeField] private SceneMsgs[] sceneMsgs;
    [SerializeField] private VideoPlayer videoPlayer;

    private int sceneIndex = 0;
    private int textIndex = 0;
    private Coroutine typingCoroutine;
    private bool isTyping;

    private WaitForSeconds wait;
    private WaitForSeconds longWait;
    private string currentMessage;

    private void Awake()
    {
        wait = new(charInterval);
        longWait = new(charInterval * 4);
    }

    private void Start()
    {
        videoPlayer.url = System.IO.Path.Combine(Application.streamingAssetsPath, "opening example.mov");
        StartCoroutine(PlayVideoAndWait());
    }

    IEnumerator PlayVideoAndWait()
    {
        // 再生準備
        videoPlayer.Prepare();

        // 準備完了まで待つ
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        // 再生開始
        videoPlayer.Play();

        // ★ここで「再生が始まるまで待つ」
        while (!videoPlayer.isPlaying)
        {
            yield return null;
        }

        StartCoroutine(WriteMsgAuto());

        // BGMの再生
        BGMManager.Instance.Play(
            BGMPath.AUDIO_CUTSCENE,
            volumeRate: 1,
            delay: 0,
            pitch: 1,
            isLoop: false);
    }

    private IEnumerator WriteMsgAuto()
    {
        // Scene1
        ShowCurrentText();

        // Scene2
        yield return new WaitForSeconds(4.3f);
        sceneIndex++;
        ShowCurrentText();

        // Scene3
        yield return new WaitForSeconds(5.9f);
        sceneIndex++;
        ShowCurrentText();

        // Scene4
        yield return new WaitForSeconds(3.6f);
        sceneIndex++;
        ShowCurrentText();

        // Scene遷移
        yield return new WaitForSeconds(7f);
        SceneManager.LoadScene("InGame");

    }

    private void ShowCurrentText()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        currentMessage = sceneMsgs[sceneIndex].msgs[textIndex].Replace("\\n", "\n");
        typingCoroutine = StartCoroutine(TypeText(currentMessage));
    }

    private IEnumerator TypeText(string message)
    {
        isTyping = true;

        StringBuilder sb = new StringBuilder();
        textUI.text = "";

        var longWaitChars = new[]
        {
            '、',
            '。',
            '！',
        };

        foreach (char c in message)
        {
            sb.Append(c);
            textUI.text = sb.ToString();
            yield return longWaitChars.Contains(c)
                         ? longWait
                         : wait;
        }

        isTyping = false;
    }
}
