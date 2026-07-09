using System;
using System.Collections;
using System.Text;
using UnityEngine;
using KanKikuchi.AudioManager;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class IntroStoryManager : MonoBehaviour
{
    [Serializable]
    public class SceneMsgs
    {
        public Sprite image;
        public string[] msgs;
        public SESound[] sounds;
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
        BGMManager.Instance.Play(BGMPath.MUSIC_CUTSCENE_LOOP);
        UpdateText();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)
        || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame))
        {
            OnSubmit();
        }
    }

    void OnSubmit()
    {
        // 文字送り中ならば全文表示する
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            textUI.text = currentMessage;
            isTyping = false;
            return;
        }

        UpdateText();
    }

    void UpdateText()
    {
        if (sceneIndex > sceneMsgs.Length - 1)
        {
            SceneManager.LoadScene("InGame");
        }
        else
        {
            var current = sceneMsgs[sceneIndex];
            rawImage.texture = current.image.texture;
            ShowCurrentText(current);
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
    private void ShowCurrentText(SceneMsgs current)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        currentMessage = current.msgs[textIndex].Replace("\\n", "\n");
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
