using UnityEngine;
using IsoTools.Physics;

namespace IsoTools.Examples.Kenney {
	[RequireComponent(typeof(IsoRigidbody))]
	public class CatController : MonoBehaviour {

		public float speed = 1f;

		IsoRigidbody _isoRigidbody = null;

		private CatDetector _catDetector = null;

		void OnIsoCollisionEnter(IsoCollision iso_collision) {
			if ( iso_collision.gameObject ) {
				var alient = iso_collision.gameObject.GetComponent<AlienBallController>();
				if ( alient ) {
					Destroy(alient.gameObject);
				}
			}
		}

		void Start() {
			_catDetector = GetComponent<CatDetector>();
			_isoRigidbody = GetComponent<IsoRigidbody>();
			if ( !_isoRigidbody ) {
				throw new UnityException("PlayerController. IsoRigidbody component not found!");
			}
		}

		void Update () {
			if (_catDetector != null
				&& _catDetector.CatCaught)
			{
				return;
			}
			
			// 左下方向に移動
			if ( Input.GetKey(KeyCode.A) ) {
				var velocity = _isoRigidbody.velocity;
				velocity.x = -speed;
				_isoRigidbody.velocity = velocity;
			}
			// 右上方向に移動
			else if ( Input.GetKey(KeyCode.D) ) {
				var velocity = _isoRigidbody.velocity;
				velocity.x = speed;
				_isoRigidbody.velocity = velocity;
			}
			// 右下方向に移動
			else if ( Input.GetKey(KeyCode.S) ) {
				var velocity = _isoRigidbody.velocity;
				velocity.y = -speed;
				_isoRigidbody.velocity = velocity;
			}
			// 左上方向に移動
			else if ( Input.GetKey(KeyCode.W) ) {
				var velocity = _isoRigidbody.velocity;
				velocity.y = speed;
				_isoRigidbody.velocity = velocity;
			}
		}
	}
}