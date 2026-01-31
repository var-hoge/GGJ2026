using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class PlayerMove : MonoBehaviour
{
    //まずはプレイヤーのトランスフォームを取得する
    Transform _playerTrs;
    PlayerDirection _currentDirection; //
    float _moveSpeed = 10; //仮で10
    bool _isDiving = false;

    float _catchEndTime = 0.8f;
    float _catchEndDistance = 2.5f;
    private void Awake()
    {
        _playerTrs = transform;
    }
    
    void Update()
    {
        if (!_isDiving)
        {
            float vertical = Input.GetAxisRaw("Vertical"); //マップ上、上下方向に動ける場合だけここのコードが動くようにする
            float horizontal = Input.GetAxisRaw("Horizontal");
            _playerTrs.Translate(new Vector3(horizontal, vertical) * Time.deltaTime); //キー入力で上下左右 //斜め押し対応はどうすれば良いのだろうか？ → 
            // CatchDive
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                CatchDive();
            }
        }
    }

    void CatchDive()
    {
        _isDiving = true;
        Sequence s = DOTween.Sequence();
        // 向いている方向にイージングを付けて移動する
        s.Append(_playerTrs.DOMoveY(_catchEndDistance, _catchEndTime));
        _isDiving = false;
    }
}

public enum PlayerDirection
{
    Up,
    Down,
    Right,
    Left
}