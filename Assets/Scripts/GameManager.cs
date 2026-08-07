using PhantomCatWorks.RealtimeP2PKit;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : SingletonBehaviour<GameManager>
{
    [SerializeField] private float timeLimitSecond;
    private float remainSecond;

    /// <summary>二重に決着させないためのフラグ。通信対戦で自分の判定と相手の通知が交差しても一度で済む。</summary>
    private bool finished;

    void Start()
    {
        Application.targetFrameRate = 30;
        this.remainSecond = timeLimitSecond;
    }

    // Update is called once per frame
    void Update()
    {
        remainSecond = Mathf.Max(remainSecond - Time.deltaTime, 0);
        if (remainSecond <= 0f)
        {
            // タイマーは端末ごとに独立して進むため、フレームレート差で僅かにずれる。
            // 時間切れの宣言はホストだけが行い、参加側は結果の通知を待つ
            if (!NetSession.IsActive || NetSession.IsHost)
            {
                this.MoveToFailScene();
            }
        }
    }

    public void MoveToFailScene()
    {
        Finish(caught: false);
    }

    public void MoveToSuccessScene()
    {
        Finish(caught: true);
    }

    /// <summary>
    /// 決着させて結果画面へ移る。通信対戦なら相手にも同じ結果を伝えるので、
    /// 両端末が必ず同じ画面へ行く。
    /// </summary>
    void Finish(bool caught)
    {
        if (finished) return;
        finished = true;

        if (NetSession.IsActive)
        {
            NetSession.Instance.Send(GameNetPacketId.GameResult, new GameResultPacket { Caught = caught });
        }

        StopInGameSounds();
        SceneManager.LoadScene(caught ? "VeryHappyEnd" : "HappyEnd");
    }

    /// <summary>
    /// 相手の端末が下した判定を反映する。こちらからは送り返さない (無限に往復するため)。
    /// InGameNetworkSync から呼ばれる。
    /// </summary>
    public void ApplyRemoteResult(bool caught)
    {
        if (finished) return;
        finished = true;

        StopInGameSounds();
        SceneManager.LoadScene(caught ? "VeryHappyEnd" : "HappyEnd");
    }

    /// <summary>
    /// InGame で鳴っている効果音を止める。
    ///
    /// 隠れたときのヘリコプター音 (PhantomCatOcclusionDetector) のように長めの SE は、
    /// 止めずにシーンを切り替えると次の画面まで鳴り続ける。決着の経路は
    /// 「自分で判定した」「相手から通知された」「時間切れ」の3つがあるため、
    /// どれを通っても必ず止まるようここに集約している。
    /// </summary>
    static void StopInGameSounds()
    {
        KanKikuchi.AudioManager.SEManager.Instance.Stop();
    }

    public float RemainTimeSecond {
        get {
            return this.remainSecond;
        }
    }
}
