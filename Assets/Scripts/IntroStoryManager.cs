using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Video;
using KanKikuchi.AudioManager;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;
using UnityEngine.UI;

public class IntroStoryManager : MonoBehaviour
{
    [Serializable]
    public class SceneMsgs
    {
        public Sprite image;
        public string[] msgs;
    }

    [SerializeField] private TextMeshProUGUI textUI = null;
    [SerializeField] private float charInterval = 0.05f;

    [SerializeField] private SceneMsgs[] sceneMsgs;
    [SerializeField] private RawImage rawImage;

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
        // BGMの再生
        BGMManager.Instance.Play(BGMPath.AUDIO_CUTSCENE);
        UpdateText();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            UpdateText();
        }
    }

    void UpdateText()
    {
        if (sceneIndex > sceneMsgs.Length - 1)
        {
            SceneManager.LoadScene("InGame");
        }
        else
        {
            rawImage.texture = sceneMsgs[sceneIndex].image.texture;
            ShowCurrentText();
        }
        sceneIndex++;
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
