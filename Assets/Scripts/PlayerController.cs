using UnityEngine;
using UnityEngine.InputSystem;
using IsoTools.Physics;

namespace IsoTools.Examples.Kenney {
	[RequireComponent(typeof(IsoRigidbody))]
	public class PlayerController : MonoBehaviour {
		/// <summary> 手持ちのライトオブジェクト </summary>
		[SerializeField] private GameObject _handLight = null;

		/// <summary>
		/// 手持ちライト。通信対戦で向きを送受信するために外から参照する。
		/// 猫側の端末ではこのコンポーネント自体が無効化されていてライトが回らないため、
		/// 相手から届いた角度をここへ適用しないと固定方向を向いたままになる。
		/// </summary>
		public Transform HandLight => _handLight != null ? _handLight.transform : null;

		/// <summary> プレイヤー移動速度 </summary>
		[SerializeField] private float speed = 1f;
		/// <summary> 手持ちライト回転速度 </summary>
		[SerializeField] float rotateSpeed = 180f;

		// コンポネント
		private IsoRigidbody _isoRigidbody = null;
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
			// 猫を捕まえている場合、移動させない
			if (_catDetector.CatCaught) return;

			if (Gamepad.current != null) HandleGamepadInput();

			HandleKeyboardInput();

			// 移動は犬猫で共通の入力を使う
			var moveInput = CharacterMoveInput.Read();
			if (moveInput != Vector3.zero)
			{
				_isoRigidbody.velocity = moveInput * speed;
			}
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
		}
	}
}