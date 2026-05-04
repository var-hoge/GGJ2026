using UnityEngine;
using UnityEngine.InputSystem;
using IsoTools.Physics;

namespace IsoTools.Examples.Kenney {
	[RequireComponent(typeof(IsoRigidbody))]
	public class PlayerController : MonoBehaviour {

		public float speed = 2.0f;

		IsoRigidbody _isoRigidbody = null;

		private CatDetector _catDetector = null;

		/// <summary>
		/// 手持ちのライトオブジェクト
		/// </summary>
		[SerializeField] private GameObject _handLight = null;

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

			var inputR = Gamepad.current.rightStick.ReadValue();
			if (!inputR.Equals(Vector2.zero))
			{
				var angle = Mathf.Atan2(inputR.x, inputR.y) * Mathf.Rad2Deg;
				_handLight.transform.rotation = Quaternion.Euler(0, 0, -angle);
			}

			var inputL = Gamepad.current.leftStick.ReadValue();
			if (!inputL.Equals(Vector2.zero))
			{
				_isoRigidbody.velocity = inputL * 2;
			}
			
			// 左下方向に移動
			if ( Input.GetKey(KeyCode.LeftArrow) ) {
				var velocity = _isoRigidbody.velocity;
				velocity.x = -speed;
				_isoRigidbody.velocity = velocity;
				
			}
			// 右上方向に移動
			else if ( Input.GetKey(KeyCode.RightArrow) ) {
				var velocity = _isoRigidbody.velocity;
				velocity.x = speed;
				_isoRigidbody.velocity = velocity;
				_handLight.transform.localScale = new(1, 1, 1);
			}
			// 右下方向に移動
			else if ( Input.GetKey(KeyCode.DownArrow) ) {
				var velocity = _isoRigidbody.velocity;
				velocity.y = -speed;
				_isoRigidbody.velocity = velocity;
				_handLight.transform.localScale = new(1, -1, 1);
			}
			// 左上方向に移動
			else if ( Input.GetKey(KeyCode.UpArrow) ) {
				var velocity = _isoRigidbody.velocity;
				velocity.y = speed;
				_isoRigidbody.velocity = velocity;
				_handLight.transform.localScale = new(-1, 1, 1);
			}
		}
	}
}