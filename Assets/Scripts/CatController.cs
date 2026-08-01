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

			// 移動は犬猫で共通の入力を使う
			var moveInput = CharacterMoveInput.Read();
			if (moveInput != Vector3.zero)
			{
				_isoRigidbody.velocity = moveInput * speed;
			}
		}
	}
}