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

    [SerializeField] private Text textUI = null;
    [SerializeField] private float charInterval = 0.05f;
    [SerializeField] private Sprite[] sprites = null;

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
        ShowCurrentText();
        StartCoroutine(WriteMsgAuto());
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
            Debug.Log($"Scene遷移 : {sceneIndex}");
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

        foreach (char c in message)
        {
            sb.Append(c);
            textUI.text = sb.ToString();
            yield return wait;
        }

        isTyping = false;
    }
}
