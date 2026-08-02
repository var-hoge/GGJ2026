using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// クレジット画面。決定入力でタイトルに戻る。
/// </summary>
public class CreditsScreenManager : MonoBehaviour
{
    [Header("タイトルに戻ったときのコントローラーの振動")]
    [SerializeField, Range(0f, 1f)] private float rumbleStrength = 0.25f;
    [SerializeField] private float rumbleDuration = 0.06f;

    private void Update()
    {
        if (!SubmitInput.WasPressed(out var gamepad)) return;

        if (gamepad != null)
        {
            GamepadRumble.Play(rumbleStrength, rumbleDuration);
        }

        SceneManager.LoadScene("Title");
    }
}
