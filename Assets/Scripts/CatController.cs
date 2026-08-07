using UnityEngine;
using IsoTools.Physics;

namespace IsoTools.Examples.Kenney {
	[RequireComponent(typeof(IsoRigidbody))]
	public class CatController : MonoBehaviour {

		public float speed = 1f;

		IsoRigidbody _isoRigidbody = null;

		/// <summary>
		/// 捕まったかどうかを見るために持つ。
		/// 以前はここで CatDetector を GetComponent していたが、CatDetector が付いているのは
		/// ポリスドッグ (Player.prefab) だけで猫には無いため、常に null で判定が効いていなかった。
		/// </summary>
		private Cat _cat = null;

		void OnIsoCollisionEnter(IsoCollision iso_collision) {
			if ( iso_collision.gameObject ) {
				var alient = iso_collision.gameObject.GetComponent<AlienBallController>();
				if ( alient ) {
					Destroy(alient.gameObject);
				}
			}
		}

		void Start() {
			_cat = GetComponent<Cat>();
			_isoRigidbody = GetComponent<IsoRigidbody>();
			if ( !_isoRigidbody ) {
				throw new UnityException("PlayerController. IsoRigidbody component not found!");
			}
		}

		void Update () {
			// 捕まったら操作を受け付けない。
			// 慣性の打ち消しは Cat 側で毎フレーム行っている
			if (_cat != null && _cat.IsCaught)
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