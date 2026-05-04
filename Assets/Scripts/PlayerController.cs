using UnityEngine;
using UnityEngine.InputSystem;
using IsoTools.Physics;
using System.Linq;

namespace IsoTools.Examples.Kenney {
	[RequireComponent(typeof(IsoRigidbody))]
	public class PlayerController : MonoBehaviour {
		/// <summary> 手持ちのライトオブジェクト </summary>
		[SerializeField] private GameObject _handLight = null;

		/// <summary> プレイヤー移動速度 </summary>
		[SerializeField] private float speed = 1f;
		/// <summary> 手持ちライト回転速度 </summary>
		[SerializeField] float rotateSpeed = 180f;

		// コンポネント
		private IsoRigidbody _isoRigidbody = null;
		private CatDetector _catDetector = null;

		private static readonly (KeyCode keyCode, Vector3 input)[] MoveInputs =
		{
			(KeyCode.UpArrow,    Vector3.up),
			(KeyCode.LeftArrow,  Vector3.left),
			(KeyCode.DownArrow,  Vector3.down),
			(KeyCode.RightArrow, Vector3.right),
		};

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
			// 猫を捕まえている場合、移動させない
			if (_catDetector.CatCaught) return;

			if (Gamepad.current != null) HandleGamepadInput();
			
			HandleKeyboardInput();
		}

		void HandleGamepadInput()
		{
			// ハンドライトの回転
			var lightInput = Gamepad.current.rightStick.ReadValue();
			if (!lightInput.Equals(Vector2.zero))
			{
				var angle = Mathf.Atan2(lightInput.x, lightInput.y) * Mathf.Rad2Deg;

				// 目標回転
				var targetRotation =
					Quaternion.Euler(0, 0, -angle);

				// 徐々に回転
				_handLight.transform.rotation =
					Quaternion.RotateTowards(
						_handLight.transform.rotation,
						targetRotation,
						rotateSpeed * Time.deltaTime
					);
			}

			// 移動
			var moveInput = Gamepad.current.leftStick.ReadValue();
			if (!moveInput.Equals(Vector2.zero))
			{
				_isoRigidbody.velocity = moveInput * speed;
			}
		}

		void HandleKeyboardInput()
		{
			// Aキー → 反時計回り
			if (Input.GetKey(KeyCode.A))
			{
				_handLight.transform.Rotate(
					0,
					0,
					rotateSpeed * Time.deltaTime
				);
			}

			// Dキー → 時計回り
			if (Input.GetKey(KeyCode.D))
			{
				_handLight.transform.Rotate(
					0,
					0,
					-rotateSpeed * Time.deltaTime
				);
			}

			var keyInput = MoveInputs.FirstOrDefault(v => Input.GetKey(v.keyCode)).input;
			if (!keyInput.Equals(Vector3.zero))
			{
				_isoRigidbody.velocity = keyInput * speed;
			}
		}
	}
}