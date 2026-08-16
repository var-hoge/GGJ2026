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
		/// <summary>正しいファントムキャットを捕獲した直後に一度だけ発火する。</summary>
		public event System.Action PhantomCatCaught;

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
			PhantomCatCaught?.Invoke();
			SEManager.Instance.Play(SEPath.SFX_GAME_CORRECT);
			GamepadRumble.Play(_correctRumbleStrength, _correctRumbleDuration);

			yield return new WaitForSeconds(3f);

			GameManager.Instance.MoveToSuccessScene();
			SEManager.Instance.Stop();
		}

		private IEnumerator Wrong()
		{
			CatCaught = true;

			var sfx = new[]
			{
				SEPath.SFX_GAME_WRONG_1,
				SEPath.SFX_GAME_WRONG_2,
				SEPath.SFX_GAME_WRONG_3,
			};
			var index = Random.Range(0, sfx.Length);
			SEManager.Instance.Play(sfx[index]);
			GamepadRumble.Play(_wrongRumbleStrength, _wrongRumbleDuration);

			yield return new WaitForSeconds(2f);

			CatCaught = false;
			if (_wrongCount++ >= MAX_WRONG_COUNT)
			{
				GameManager.Instance.MoveToFailScene();
				SEManager.Instance.Stop();
			}
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
