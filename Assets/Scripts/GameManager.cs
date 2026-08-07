using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using PhantomCatWorks.RealtimeP2PKit;

public class GameManager : SingletonBehaviour<GameManager>
{
    [SerializeField] private float timeLimitSecond;
    private float remainSecond;
    private bool gameEnded;
    private bool waitForNetworkAuthority;
    private const float NetworkResultFlushDelaySeconds = 0.15f;

    /// <summary>Raised immediately before moving to an ending scene.</summary>
    public event System.Action<bool> GameEnded;

    void Start()
    {
        Application.targetFrameRate = 30;
        this.remainSecond = timeLimitSecond;
    }

    // Update is called once per frame
    void Update()
    {
        remainSecond = Mathf.Max(remainSecond - Time.deltaTime, 0);
        if (!gameEnded && !waitForNetworkAuthority && remainSecond <= 0f)
        {
            this.MoveToFailScene();
        }
    }

    public void MoveToFailScene()
    {
        MoveToEnding(success: false);
    }

    public void MoveToSuccessScene()
    {
        MoveToEnding(success: true);
    }

    /// <summary>
    /// In an online match, PhantomCat waits for PoliceDog's authoritative
    /// result. Standalone and local play continue using their normal timer.
    /// </summary>
    public void WaitForNetworkAuthority()
    {
        waitForNetworkAuthority = true;
    }

    private void MoveToEnding(bool success)
    {
        if (gameEnded)
        {
            return;
        }

        gameEnded = true;
        GameEnded?.Invoke(success);

        // Give the final result packet one frame to enter WebRTC's send queue,
        // then clear all online-session state before the ending scene loads.
        if (P2PManager.TryGetExistingInstance(out var p2pManager)
            && p2pManager.IsOnlineMatch)
        {
            StartCoroutine(DisconnectThenLoadEnding(success));
            return;
        }

        SceneManager.LoadScene(success ? "VeryHappyEnd" : "HappyEnd");
    }

    private IEnumerator DisconnectThenLoadEnding(bool success)
    {
        yield return new WaitForSecondsRealtime(NetworkResultFlushDelaySeconds);

        if (P2PManager.TryGetExistingInstance(out var p2pManager))
        {
            p2pManager.Disconnect();
        }

        SceneManager.LoadScene(success ? "VeryHappyEnd" : "HappyEnd");
    }

    public float RemainTimeSecond {
        get {
            return this.remainSecond;
        }
    }
}
