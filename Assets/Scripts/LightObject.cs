using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using IsoTools;

[Serializable]
public class LightObjectMovePattern {
    public float reachedSpan = 1.0f;
    public Vector2 startPosition;
    public Vector2 endPosition;
}

public enum HeliTypes
{
    HeliType1,
    HeliType2
}

[RequireComponent(typeof(IsoObject))]
public class LightObject : MonoBehaviour
{
    [SerializeField] private Light2D lightObjectSetting;
    [SerializeField] private Sprite enemyHeli1Sprite;
    [SerializeField] private Sprite enemyHeli2Sprite;
    [SerializeField] private HeliTypes heliType;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private List<LightObjectMovePattern> movePatternList = new List<LightObjectMovePattern>();

    private LightObjectMovePattern currentPattern;
    private int patternIndex = 0;
    private IsoObject currentIsoObject;
    private Coroutine patrolCoroutine;
    private Coroutine convergeCoroutine;

    public void Init(HeliTypes heliType)
    {
        this.heliType = heliType;
    }

    void Start()
    {
        this.currentIsoObject = GetComponent<IsoObject>();
        if(heliType == HeliTypes.HeliType1)
        {
            spriteRenderer.sprite = enemyHeli1Sprite;
        } else
        {
            spriteRenderer.sprite = enemyHeli2Sprite;
        }
        this.currentPattern = movePatternList[patternIndex];
        this.currentIsoObject.positionX = currentPattern.startPosition.x;
        this.currentIsoObject.positionY = currentPattern.startPosition.y;
        this.patrolCoroutine = StartCoroutine(this.LoopingAnimationCoroutine());
    }

    /// <summary>
    /// 巡回を中断し、子のSpotLight2Dがターゲット位置に重なるようにdurationかけて移動する。
    /// 到達後はEndConvergeが呼ばれるまでターゲットに追従し続ける。
    /// </summary>
    public void BeginConverge(Transform target, float duration)
    {
        if (convergeCoroutine != null) return;
        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
            patrolCoroutine = null;
        }
        convergeCoroutine = StartCoroutine(ConvergeCoroutine(target, duration));
    }

    /// <summary>
    /// 収束を中断して巡回に戻る
    /// </summary>
    public void EndConverge()
    {
        if (convergeCoroutine != null)
        {
            StopCoroutine(convergeCoroutine);
            convergeCoroutine = null;
        }
        ResumePatrol();
    }

    private void ResumePatrol()
    {
        if (patrolCoroutine == null && isActiveAndEnabled)
        {
            patrolCoroutine = StartCoroutine(this.LoopingAnimationCoroutine());
        }
    }

    private IEnumerator ConvergeCoroutine(Transform target, float duration)
    {
        float frameTime = 0f;
        Vector2 startIsoPosition = new Vector2(this.currentIsoObject.positionX, this.currentIsoObject.positionY);
        while (target != null)
        {
            frameTime += Time.deltaTime;
            float ratio = duration > 0f ? Mathf.Clamp01(frameTime / duration) : 1f;

            // SpotLight2Dがターゲットに重なるためのルートのワールド目標位置を求め、アイソメトリック座標に変換する
            Vector2 spotLightOffset = lightObjectSetting.transform.position - transform.position;
            Vector2 targetScreenPosition = (Vector2)target.position - spotLightOffset;
            Vector3 targetIsoPosition = this.currentIsoObject.isoWorld.ScreenToIso(
                targetScreenPosition, this.currentIsoObject.positionZ);

            Vector2 isoPosition = Vector2.Lerp(startIsoPosition, targetIsoPosition, ratio);
            this.currentIsoObject.positionX = isoPosition.x;
            this.currentIsoObject.positionY = isoPosition.y;
            yield return null;
        }
        // ターゲットが消えた場合は巡回に戻る
        convergeCoroutine = null;
        ResumePatrol();
    }

    private IEnumerator LoopingAnimationCoroutine() {
        float frameTime = 0f;
        Vector2 startPosition = new Vector2(this.currentIsoObject.positionX, this.currentIsoObject.positionY);
        while (true) {
            float prevIsoPositionX = this.currentIsoObject.positionX;
            frameTime += Time.deltaTime;
            float ratio = frameTime / currentPattern.reachedSpan;
            Vector2 isoPosition = Vector2.Lerp(
                new Vector2(startPosition.x, startPosition.y),
                new Vector2(currentPattern.endPosition.x, currentPattern.endPosition.y),
                ratio
            );
            this.currentIsoObject.positionX = isoPosition.x;
            this.currentIsoObject.positionY = isoPosition.y;
            if (isoPosition.x - prevIsoPositionX > 0)
            {
                spriteRenderer.flipX = heliType == HeliTypes.HeliType1;
            } else
            {
                spriteRenderer.flipX = heliType == HeliTypes.HeliType2;
            }
                yield return null;
            if (frameTime >= currentPattern.reachedSpan) {
                ++patternIndex;
                if (patternIndex >= movePatternList.Count) {
                    patternIndex = 0;
                }
                this.currentPattern = movePatternList[patternIndex];
                startPosition = new Vector2(currentPattern.startPosition.x, currentPattern.startPosition.y);
                frameTime = 0;
            }
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
