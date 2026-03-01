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

        ShowCurrentText();
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
        yield return new WaitForSeconds(4.09f);
        OnSpaceKey();

        // Scene2
        yield return new WaitForSeconds(3f);
        OnSpaceKey();

        // Scene3
        yield return new WaitForSeconds(2.9f);
        OnSpaceKey();

        // Scene4
        yield return new WaitForSeconds(3.6f);
        OnSpaceKey();

        // Scene5
        yield return new WaitForSeconds(3.6f);
        OnSpaceKey();

        // Scene遷移
        yield return new WaitForSeconds(4f);
        OnSpaceKey();

    }

    private void OnSpaceKey()
    {
        // 文字送り中ならば全文表示する
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            textUI.text = currentMessage;
            isTyping = false;
            return;
        }

        // 最後のスライドなら次のSceneに遷移する
        if (sceneIndex >= sceneMsgs.Length - 1)
        {
            SceneManager.LoadScene("InGame");
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
