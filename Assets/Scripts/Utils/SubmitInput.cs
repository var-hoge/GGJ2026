using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 「決定」の入力。キーボードのスペース、またはコントローラーの南ボタン。
/// ストーリーの文字送りとクレジット画面で同じ操作になるよう、ここにまとめている。
/// </summary>
public static class SubmitInput
{
    /// <summary>
    /// このフレームで決定が押されたか。
    /// コントローラーで押された場合のみ gamepad にそのコントローラーが入る。
    /// </summary>
    public static bool WasPressed(out Gamepad gamepad)
    {
        gamepad = Gamepad.current;
        if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame) return true;

        gamepad = null;
        return Input.GetKeyDown(KeyCode.Space);
    }
}
