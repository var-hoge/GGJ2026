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
        StartCoroutine(this.LoopingAnimationCoroutine());
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
