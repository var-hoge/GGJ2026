using UnityEngine;

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
        remainSecond -= Time.deltaTime;
    }

    public float RemainTimeSecond {
        get {
            return this.remainSecond;
        }
    }
}
