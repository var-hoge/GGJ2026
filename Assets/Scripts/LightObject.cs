using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[Serializable]
public class LightObjectMovePattern {
    public float reachedSpan = 1.0f;
    public Vector2 startPosition;
    public Vector2 endPosition;
}

public class LightObject : MonoBehaviour
{
    [SerializeField] private Light2D lightObjectSetting;
    [SerializeField] private List<LightObjectMovePattern> movePatternList = new List<LightObjectMovePattern>();

    private LightObjectMovePattern currentPattern;
    private int patternIndex = 0;

    void Start()
    {
        this.currentPattern = movePatternList[patternIndex];
        this.transform.localPosition = new Vector3(currentPattern.startPosition.x, currentPattern.startPosition.y, this.transform.localPosition.z);
        StartCoroutine(this.LoopingAnimationCoroutine());
    }

    private IEnumerator LoopingAnimationCoroutine() {
        float frameTime = 0f;
        Vector3 startPosition = transform.localPosition;
        while (true) {
            frameTime += Time.deltaTime;
            float ratio = frameTime / currentPattern.reachedSpan;
            this.transform.localPosition = Vector3.Lerp(
                new Vector3(startPosition.x, startPosition.y, transform.position.z),
                new Vector3(currentPattern.endPosition.x, currentPattern.endPosition.y, transform.position.z),
                ratio
            );
            yield return null;
            if (frameTime >= currentPattern.reachedSpan) {
                ++patternIndex;
                if (patternIndex >= movePatternList.Count) {
                    patternIndex = 0;
                }
                this.currentPattern = movePatternList[patternIndex];
                startPosition = new Vector3(currentPattern.startPosition.x, currentPattern.startPosition.y, transform.position.z);
                frameTime = 0;
            }
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
