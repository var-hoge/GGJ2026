using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// コントローラーを一定時間だけ振動させる。
/// シーン遷移などで Play() のコルーチンが打ち切られても振動が鳴り続けないよう、
/// 呼び出し側は OnDisable() などで Stop() を呼ぶこと。
/// </summary>
public static class GamepadRumble
{
    private static Gamepad rumblingGamepad;

    /// <summary>
    /// strength の強さで duration 秒だけ振動させる。
    /// 呼び出し側の MonoBehaviour から StartCoroutine() で回す。
    /// コントローラーが繋がっていなければ何もしない。
    /// </summary>
    public static IEnumerator Play(float strength, float duration)
    {
        var gamepad = Gamepad.current;
        if (gamepad == null) yield break;

        Stop();

        rumblingGamepad = gamepad;
        gamepad.SetMotorSpeeds(strength, strength);

        yield return new WaitForSeconds(duration);

        Stop();
    }

    /// <summary>振動中であれば止める。</summary>
    public static void Stop()
    {
        if (rumblingGamepad == null) return;

        rumblingGamepad.ResetHaptics();
        rumblingGamepad = null;
    }
}
