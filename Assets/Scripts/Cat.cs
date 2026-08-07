using UnityEngine;
using IsoTools.Physics;
using System.Collections;

namespace IsoTools.Examples.Kenney {
	[RequireComponent(typeof(IsoRigidbody))]
	public class Cat : MonoBehaviour {

		IsoObject    _isoObject    = null;
		IsoRigidbody _isoRigidbody = null;

		[SerializeField] private Transform _spriteObj = null;
		
		[SerializeField] private bool _isPhantom = false;

		/// <summary>
		/// 自動移動
		/// </summary>
		public bool _autoMove = true;

		public bool IsPhantom => _isPhantom;

		/// <summary>
		/// ポリスドッグに捕まったか。捕まった後は自動移動も操作も受け付けない。
		/// 通信対戦では、捕獲判定が走らない側の端末でも
		/// CatchAttempt の通知を受けて同じ状態になる。
		/// </summary>
		public bool IsCaught { get; private set; }

		/// <summary>自動移動のコルーチン。捕まったときに止める。</summary>
		Coroutine _autoMoveCoroutine = null;

		/// <summary>捕まった状態にする。何度呼んでも一度しか効かない。</summary>
		public void MarkCaught() {
			if ( IsCaught ) {
				return;
			}
			IsCaught = true;

			// NPCとして徘徊している場合は、その場で止める
			_autoMove = false;
			if ( _autoMoveCoroutine != null ) {
				StopCoroutine(_autoMoveCoroutine);
				_autoMoveCoroutine = null;
			}

			if ( _isoRigidbody ) {
				_isoRigidbody.velocity = Vector3.zero;
			}
		}

		void Start() {
			_isoObject = GetComponent<IsoObject>();
			if ( !_isoObject ) {
				throw new UnityException("AlienBallController. IsoObject component not found!");
			}
			_isoRigidbody = GetComponent<IsoRigidbody>();
			if ( !_isoRigidbody ) {
				throw new UnityException("AlienBallController. IsoRigidbody component not found!");
			}

			// 自動移動
			if (_autoMove) _autoMoveCoroutine = StartCoroutine(AddRndForce());
		}

		void Update() {
			if ( _isoObject.positionZ < 0.0f ) {
				Destroy(gameObject);
				return;
			}

			// 捕まったら動かない。AddForce で与えた勢いが残っていると
			// コルーチンを止めただけでは滑り続けるので、毎フレーム打ち消す。
			// 操作中・NPC・通信相手のどれであってもここを通る
			if ( IsCaught && _isoRigidbody ) {
				_isoRigidbody.velocity = Vector3.zero;
			}
		}

		IEnumerator AddRndForce() {
			while ( true ) {
				var dx = Random.Range(-2.0f, 2.0f);
				var dy = Random.Range(-2.0f, 2.0f);
				_isoRigidbody.AddForce(new Vector3(dx, dy, 0.0f), ForceMode.Impulse);

				// 移動方向に応じてスプライトの向きを変更
				var sign = Mathf.Sign(dx);
				var scale = _spriteObj.localScale;
				_spriteObj.localScale = new Vector3(scale.x * sign, scale.y, scale.z);

				yield return new WaitForSeconds(Random.Range(0.25f, 1f));
			}
		}
	}
}