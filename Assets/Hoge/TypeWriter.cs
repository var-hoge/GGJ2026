using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class TypeWriter : MonoBehaviour
{
    [Serializable]
    public class SceneMsgs
    {
        public String[] msgs;
    }

    [SerializeField] private Text textUI;
    [SerializeField] private float charInterval = 0.05f;
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private Image image;

    [SerializeField]
    private SceneMsgs[] sceneMsgs;

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
        image.sprite = sprites[0];
        ShowCurrentText();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnSpaceKey();
        }
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
            if (sceneIndex >= sceneMsgs.Length)
            {
                Debug.Log($"Scene遷移 : {sceneIndex}");
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

                if (sceneIndex >= textArray.Length)
                {
                    return;
                }

                ShowCurrentText();
                image.sprite = sprites[sceneIndex];
            }
        }
    }

    private void ShowCurrentText()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        currentMessage = sceneMsgs[sceneIndex].msgs[textIndex];
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
