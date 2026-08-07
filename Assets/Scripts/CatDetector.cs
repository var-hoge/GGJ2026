using UnityEngine;
using UnityEngine.InputSystem;
using IsoTools.Physics;
using KanKikuchi.AudioManager;
using System.Collections;

namespace IsoTools.Examples.Kenney {
	[RequireComponent(typeof(IsoTriggerListener), typeof(IsoCollisionListener))]
	public class CatDetector : MonoBehaviour {

		private const int MAX_WRONG_COUNT = 1;
		private Cat _targetCat = null;
		private int _wrongCount = 0;
		public bool CatCaught { get; private set; } = false;

		/// <summary>
		/// 外したときに鳴らす SE の候補。通信対戦では番号を相手へ送って同じ音を鳴らすため、
		/// 両端末で並び順が一致している必要がある。
		/// </summary>
		public static readonly string[] WrongSounds =
		{
			SEPath.SFX_GAME_WRONG_1,
			SEPath.SFX_GAME_WRONG_2,
			SEPath.SFX_GAME_WRONG_3,
		};

		[Header("捕獲したときのコントローラーの振動")]
		[SerializeField, Range(0f, 1f)] private float _correctRumbleStrength = 0.6f;
		[SerializeField] private float _correctRumbleDuration = 1f;
		[SerializeField, Range(0f, 1f)] private float _wrongRumbleStrength = 0.25f;
		[SerializeField] private float _wrongRumbleDuration = 0.5f;

		bool WasKeyPressed => Input.GetKeyDown(KeyCode.Space)
							  || (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame);

        void Update()
        {
			if (!CatCaught
				&& _targetCat != null
				&& WasKeyPressed)
			{
				var coroutine = _targetCat.IsPhantom ? Correct() : Wrong();
				StartCoroutine(coroutine);
			}
        }

		private IEnumerator Correct()
		{
			CatCaught = true;
			// 捕獲判定はポリスドッグ側の端末でしか走らないので、
			// 猫プレイヤーにも同じ音が鳴るように知らせる
			NotifyCatchAttempt(wasPhantom: true, wrongSoundIndex: 0);
			SEManager.Instance.Play(SEPath.SFX_GAME_CORRECT);
			GamepadRumble.Play(_correctRumbleStrength, _correctRumbleDuration);

			yield return new WaitForSeconds(3f);

			GameManager.Instance.MoveToSuccessScene();
			SEManager.Instance.Stop();
		}

		private IEnumerator Wrong()
		{
			CatCaught = true;

			// どの音を鳴らすかはランダムなので、番号ごと相手へ送って揃える
			var index = Random.Range(0, WrongSounds.Length);
			NotifyCatchAttempt(wasPhantom: false, wrongSoundIndex: (byte)index);
			SEManager.Instance.Play(WrongSounds[index]);
			GamepadRumble.Play(_wrongRumbleStrength, _wrongRumbleDuration);

			yield return new WaitForSeconds(2f);

			CatCaught = false;
			if (_wrongCount++ >= MAX_WRONG_COUNT)
			{
				GameManager.Instance.MoveToFailScene();
				SEManager.Instance.Stop();
			}
		}

		/// <summary>捕獲を試みたことを相手へ知らせる。ソロプレイなら何も起きない。</summary>
		private void NotifyCatchAttempt(bool wasPhantom, byte wrongSoundIndex)
		{
			if (!PhantomCatWorks.RealtimeP2PKit.NetSession.IsActive) return;

			PhantomCatWorks.RealtimeP2PKit.NetSession.Instance.Send(
				GameNetPacketId.CatchAttempt,
				new CatchAttemptPacket
				{
					WasPhantom = wasPhantom,
					WrongSoundIndex = wrongSoundIndex,
				});
		}

        void OnIsoCollisionEnter(IsoCollision iso_collision) {
			if (iso_collision.gameObject.TryGetComponent<Cat>(out var cat))
			{
				_targetCat = cat;
			}
		}

		void OnIsoCollisionExit(IsoCollision iso_collision) {
			if (iso_collision.gameObject.TryGetComponent<Cat>(out var cat))
			{
				_targetCat = null;
			}
		}
	}
}