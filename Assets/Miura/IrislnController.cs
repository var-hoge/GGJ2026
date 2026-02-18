using UnityEngine;
using UnityEngine.UI;

public class IrisInController : MonoBehaviour
{
    [Header("アイリスインの設定")]
    [SerializeField] private float duration = 1.0f;       // 開くのにかかる秒数
    [SerializeField] private float delay = 0.0f;          // 開始までの待ち時間
    [SerializeField] private bool playOnEnable = true;     // 有効時に自動再生するか

    [Header("イージング")]
    [SerializeField] private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Material material;
    private float timer;
    private bool isPlaying;

    private static readonly int progressId = Shader.PropertyToID("_Progress");

    private void Awake()
    {
        // Image コンポーネントのマテリアルをインスタンス化
        Image image = GetComponent<Image>();
        if (image != null)
        {
            material = new Material(image.material);
            image.material = material;
        }
    }

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    /// <summary>
    /// アイリスインを開始する
    /// </summary>
    public void Play()
    {
        timer = -delay;
        isPlaying = true;
        if (material != null)
            material.SetFloat(progressId, 0);
    }

    private void Update()
    {
        if (!isPlaying || material == null)
            return;

        timer += Time.deltaTime;

        if (timer < 0)
            return;

        // 0〜1 の進行度を計算
        float t = Mathf.Clamp01(timer / duration);

        // イージングカーブを適用
        float progress = easingCurve.Evaluate(t);

        material.SetFloat(progressId, progress);

        // 完了
        if (t >= 1.0f)
            isPlaying = false;
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }
}