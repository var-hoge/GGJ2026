using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : SingletonBehaviour<GameManager>
{
    [SerializeField] private float timeLimitSecond;
    private float remainSecond;

    void Start()
    {
        this.remainSecond = timeLimitSecond;
    }

    // Update is called once per frame
    void Update()
    {
        remainSecond = Mathf.Max(remainSecond - Time.deltaTime, 0);
        if (remainSecond <= 0f)
        {
            this.MoveToFailScene();
        }
    }

    public void MoveToFailScene()
    {
        SceneManager.LoadScene("HappyEnd");
    }

    public float RemainTimeSecond {
        get {
            return this.remainSecond;
        }
    }
}
