using UnityEngine;
using IsoTools.Physics;
using KanKikuchi.AudioManager;

namespace IsoTools.Examples.Kenney {
	[RequireComponent(typeof(IsoTriggerListener), typeof(IsoCollisionListener))]
	public class CatDetector : MonoBehaviour {
		private Cat _targetCat = null;
        void Update()
        {
			if (_targetCat != null
				&& Input.GetKeyDown(KeyCode.Space))
			{
				if (_targetCat.IsPhantom)
				{
				    GameManager.Instance.MoveToSuccessScene();
				}
				else
				{
				    GameManager.Instance.MoveToFailScene();
				}
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