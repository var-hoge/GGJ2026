using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// 1文字ずつテキストを送るストーリー画面の共通処理。
/// シーンの進行やメッセージの選び方は継承先が持つ。
/// </summary>
public abstract class StoryTextManager : MonoBehaviour
{
    private static readonly char[] LongWaitChars =
    {
        '、',
        '。',
        '！',
    };

    [SerializeField] private TextMeshProUGUI textUI = null;
    [SerializeField] private float charInterval = 0.05f;

    private Coroutine typingCoroutine;
    private WaitForSeconds wait;
    private WaitForSeconds longWait;
    private string currentMessage;

    protected bool IsTyping { get; private set; }

    protected virtual void Awake()
    {
        wait = new(charInterval);
        longWait = new(charInterval * 4);
    }

    private void Update()
    {
        if (!WasSubmitPressed()) return;

        // 文字送り中ならば全文表示する
        if (IsTyping)
        {
            SkipTyping();
            return;
        }

        Advance();
    }

    /// <summary>キーボードのスペース、またはコントローラーの南ボタン。</summary>
    private static bool WasSubmitPressed()
    {
        return Input.GetKeyDown(KeyCode.Space)
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
    }

    /// <summary>文字送りが終わった状態で入力されたときに呼ばれる。次のメッセージへ進める。</summary>
    protected abstract void Advance();

    /// <summary>メッセージの文字送りを始める。表示中のものがあれば打ち切る。</summary>
    protected void StartTyping(string message)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        currentMessage = message.Replace("\\n", "\n");
        typingCoroutine = StartCoroutine(TypeText(currentMessage));
    }

    /// <summary>文字送り中の残りを飛ばして全文表示する。</summary>
    protected void SkipTyping()
    {
        StopCoroutine(typingCoroutine);
        textUI.text = currentMessage;
        IsTyping = false;
    }

    private IEnumerator TypeText(string message)
    {
        IsTyping = true;

        StringBuilder sb = new StringBuilder();
        textUI.text = "";

        foreach (char c in message)
        {
            sb.Append(c);
            textUI.text = sb.ToString();
            yield return LongWaitChars.Contains(c)
                         ? longWait
                         : wait;
        }

        IsTyping = false;
    }
}
