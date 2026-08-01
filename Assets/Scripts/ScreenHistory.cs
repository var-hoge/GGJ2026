using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 直前に表示していた画面を覚えておく。
/// 複数の画面から遷移してくる画面 (キャラクター選択など) で、戻るボタンの行き先を決めるのに使う。
/// </summary>
public static class ScreenHistory
{
    /// <summary>直前に表示していた画面名。まだ画面を移動していなければ空。</summary>
    public static string PreviousSceneName { get; private set; }

    // このプロジェクトはドメインリロードを無効にしているため、再生を止めても static の値が残る。
    // 明示的に消さないと、前回の再生で記録した画面へ戻ろうとしてしまう
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        PreviousSceneName = null;
    }

    /// <summary>今の画面を記録してから次の画面へ移る。</summary>
    public static void LoadScene(string sceneName)
    {
        PreviousSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);
    }
}
