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
		public bool IsPhantom => _isPhantom;
		
		void Start() {
			_isoObject = GetComponent<IsoObject>();
			if ( !_isoObject ) {
				throw new UnityException("AlienBallController. IsoObject component not found!");
			}
			_isoRigidbody = GetComponent<IsoRigidbody>();
			if ( !_isoRigidbody ) {
				throw new UnityException("AlienBallController. IsoRigidbody component not found!");
			}
			StartCoroutine(AddRndForce());
		}

		void Update() {
			if ( _isoObject.positionZ < 0.0f ) {
				Destroy(gameObject);
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