using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : SingletonBehaviour<GameManager>
{
    [SerializeField] private float timeLimitSecond;
    private float remainSecond;
    private bool gameEnded;
    private bool waitForNetworkAuthority;

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
        SceneManager.LoadScene(success ? "VeryHappyEnd" : "HappyEnd");
    }

    public float RemainTimeSecond {
        get {
            return this.remainSecond;
        }
    }
}
