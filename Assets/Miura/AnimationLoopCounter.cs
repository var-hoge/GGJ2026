using System;
using UnityEngine;
/// <summary> (T)[Enter]Nodeのループ回数が_nextClipCountを越えた時、○拍目専用のAnimationを起動する </summary>
public class AnimationLoopController : MonoBehaviour
{
    AnimatorStateInfo _info;
    Animator _animator;
    int _nextClipCount = 16;
    bool _isNoLoop = true;
    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    void Update()
    {
        _info = _animator.GetCurrentAnimatorStateInfo(0);
        float _normalizedTime = _info.normalizedTime;
        int loopCount = Mathf.FloorToInt(_normalizedTime); //クリップが再生され直せるはずなのでloopCountを初期化する必要はない
        if (loopCount > (_isNoLoop ? _nextClipCount - 4 : _nextClipCount))
        {
            _animator.Play("Bounce");
        }
    }

    public void ResetClip()
    {
        _isNoLoop = false;
        _animator.Play("(T)[Enter]");
    }
}
