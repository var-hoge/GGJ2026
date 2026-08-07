using System.Collections.Generic;
using PhantomCatWorks.RealtimeP2PKit;
using UnityEngine;

/// <summary>
/// 「両者の準備が揃ってから同時に次の画面へ進む」ための同期。
///
/// オープニングやエンディングは各自のペースで読み進めるため、放っておくと
/// 先に読み終えた側だけが次の画面へ行ってしまう。そこで
/// <see cref="MarkLocalReady"/> で自分の準備完了を相手へ送り、
/// <see cref="IsOpponentReady"/> が true になるのを待ってから遷移する。
///
/// 相手の通知は、こちらが次の画面を読み込んでいる最中に届くことがある。
/// 画面ごとにハンドラを登録すると、その隙間に届いた通知を取りこぼすので、
/// このクラスは DontDestroyOnLoad で常駐し、一度だけ登録したハンドラを
/// 画面をまたいで保持し続ける。
/// </summary>
public class NetSceneSync : MonoBehaviour
{
    static NetSceneSync _instance;

    // このプロジェクトはドメインリロードを無効にしているため、再生を止めても static が残る。
    // 破棄済みのインスタンスを掴んだままにしないよう明示的に消す
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        _instance = null;
    }

    /// <summary>相手から準備完了が届いている Stage。</summary>
    readonly HashSet<byte> _opponentReady = new HashSet<byte>();

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 常駐している間ずっと受け続ける。画面ごとの登録/解除はしない
        NetSession.Instance.RegisterPacketHandler<SceneReadyPacket>(
            GameNetPacketId.SceneReady, OnOpponentReady);
    }

    void OnOpponentReady(SceneReadyPacket packet)
    {
        _opponentReady.Add(packet.Stage);
    }

    /// <summary>
    /// これから待ち合わせる画面に入ったときに呼ぶ。
    /// 通信対戦でなければ何もしない (常駐オブジェクトも作らない)。
    /// </summary>
    public static void Prepare(byte stage)
    {
        if (!NetSession.IsActive) return;

        EnsureInstance();
        // 前回のプレイで残った通知を持ち越さない
        _instance._opponentReady.Remove(stage);
    }

    /// <summary>自分の準備が終わったことを相手へ知らせる。</summary>
    public static void MarkLocalReady(byte stage)
    {
        if (!NetSession.IsActive) return;

        EnsureInstance();
        NetSession.Instance.Send(GameNetPacketId.SceneReady, new SceneReadyPacket { Stage = stage });
    }

    /// <summary>相手の準備が終わっているか。通信対戦でなければ常に true (待つ相手が居ない)。</summary>
    public static bool IsOpponentReady(byte stage)
    {
        if (!NetSession.IsActive) return true;
        return _instance != null && _instance._opponentReady.Contains(stage);
    }

    static void EnsureInstance()
    {
        if (_instance != null) return;

        var go = new GameObject(nameof(NetSceneSync));
        _instance = go.AddComponent<NetSceneSync>();
    }
}
