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

    /// <summary>1文字あたりの表示間隔(秒)。全ストーリー画面で共通。</summary>
    private const float CharInterval = 0.05f;

    [SerializeField] private TextMeshProUGUI textUI = null;

    [Header("コントローラーでテキスト送りしたときの振動")]
    [SerializeField, Range(0f, 1f)] private float rumbleStrength = 0.25f;
    [SerializeField] private float rumbleDuration = 0.06f;

    private Coroutine typingCoroutine;
    private WaitForSeconds wait;
    private WaitForSeconds longWait;
    private string currentMessage;

    protected bool IsTyping { get; private set; }

    protected virtual void Awake()
    {
        wait = new(CharInterval);
        longWait = new(CharInterval * 4);
    }

    private void Update()
    {
        if (!WasSubmitPressed(out var gamepad)) return;

        if (gamepad != null)
        {
            GamepadRumble.Play(rumbleStrength, rumbleDuration);
        }

        // 文字送り中ならば全文表示する
        if (IsTyping)
        {
            SkipTyping();
            return;
        }

        Advance();
    }

    /// <summary>
    /// キーボードのスペース、またはコントローラーの南ボタン。
    /// コントローラーで押された場合のみ gamepad にそのコントローラーが入る。
    /// </summary>
    private static bool WasSubmitPressed(out Gamepad gamepad)
    {
        gamepad = Gamepad.current;
        if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame) return true;

        gamepad = null;
        return Input.GetKeyDown(KeyCode.Space);
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
