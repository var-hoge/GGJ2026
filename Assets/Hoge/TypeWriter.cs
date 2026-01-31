using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class TypeWriter : MonoBehaviour
{
    [SerializeField] private Text textUI;
    [SerializeField] private float charInterval = 0.05f;
    [SerializeField] private Image[] images;

    private string[][] texts; // 2行ずつ用意

    private int sceneIndex = 0;
    private int textIndex = 0;
    private Coroutine typingCoroutine;
    private bool isTyping;

    private WaitForSeconds wait;
    private string currentMessage;

    private void Awake()
    {
        var scene1 = new string[]
        {
            "規律と秩序を重んじ、社会や組織の安定や調和を大切にする「犬」たち。",
            "その行動理念と長年に渡る人間との信頼関係が評価されて、公共の安全と秩序を維持する治安機関を担うこととなる。",
            "しかし、権力を手にすることで、癒着や汚職が横行。一方、搾取をされて貧しいながらも、自由や公平な社会を望む「猫」たち",
        };
        var scene2 = new string[]
        {
            "莫大な富と権力を握る「人間」と「犬」に管理された、息苦しくて退屈な社会に対して、一石を投じるべく、「怪盗キャット」が立ち上がる",
        };
        var scene3 = new string[]
        {
            "怪盗キャットは、貧しい猫たちを救うべく、犬たちがため込んだ宝石を厳重に保管している銀行へ忍び込む。",
            "首尾よく財宝を盗み出すことに成功するが、警備の犬に見つかり警報がなる。",
        };
        texts = new string[][]
        {
            scene1,
            scene2,
            scene3,
        };
        wait = new WaitForSeconds(charInterval);
    }

    private void Start()
    {
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
            if (sceneIndex >= texts.Length)
            {
                Debug.Log($"テキスト終了 : {sceneIndex}");
                return;
            }

            // 次のテキストへ
            textIndex++;

            var textArray = texts[sceneIndex];
            if (textIndex < textArray.Length)
            {
                ShowCurrentText();
            }
            else
            {
                sceneIndex++;
                textIndex = 0;

                if (sceneIndex >= texts.Length)
                {
                    return;
                }

                ShowCurrentText();
            }
        }
    }

    private void ShowCurrentText()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        currentMessage = texts[sceneIndex][textIndex];
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
