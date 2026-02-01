using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class TypeWriterEnd : MonoBehaviour
{
    [Serializable]
    public class SceneMsgs
    {
        public String[] msgs;
    }

    [SerializeField] private Text textUI = null;
    [SerializeField] private float charInterval = 0.05f;
    [SerializeField] private Sprite[] sprites = null;
    [SerializeField] private Image image = null;
    [SerializeField] private bool isManager = false;

    [SerializeField] private SceneMsgs[] sceneMsgs;

    private int sceneIndex = 0;
    private int textIndex = 0;
    private Coroutine typingCoroutine;
    private bool isTyping;

    private WaitForSeconds wait;
    private string currentMessage;

    private void Awake()
    {
        wait = new WaitForSeconds(charInterval);
    }

    private void Start()
    {
        if (isManager)
        {
            image.sprite = sprites[0];
        }
        
        ShowCurrentText();
        // StartCoroutine(WriteMsgAuto());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnSpaceKey();
        }
    }

    private IEnumerator WriteMsgAuto()
    {
        // Scene1
        yield return new WaitForSeconds(4.07f);
        OnSpaceKey();

        // Scene2
        yield return new WaitForSeconds(3f);
        OnSpaceKey();

        // Scene3
        yield return new WaitForSeconds(2.9f);
        OnSpaceKey();

        // Scene4
        yield return new WaitForSeconds(3.5f);
        OnSpaceKey();

        // Scene遷移
        yield return new WaitForSeconds(5f);
        OnSpaceKey();

    }

    private void OnSpaceKey()
    {
        if (isTyping)
        {
            // 文字送り中 → 全文表示
            StopCoroutine(typingCoroutine);
            textUI.text = currentMessage;
            isTyping = false;
        }
        else
        {
            if (sceneIndex >= sceneMsgs.Length - 1)
            {
                if (isManager)
                {
                    Debug.Log($"Scene遷移 : {sceneIndex}");
                }
                return;
            }

            // 次のテキストへ
            textIndex++;

            var textArray = sceneMsgs[sceneIndex].msgs;
            if (textIndex < textArray.Length)
            {
                ShowCurrentText();
            }
            else
            {
                sceneIndex++;
                textIndex = 0;

                if (sceneIndex >= sceneMsgs.Length)
                {
                    return;
                }

                ShowCurrentText();

                if (isManager)
                {
                    image.sprite = sprites[sceneIndex];
                }
            }
        }
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

        foreach (char c in message)
        {
            sb.Append(c);
            textUI.text = sb.ToString();
            yield return wait;
        }

        isTyping = false;
    }
}
