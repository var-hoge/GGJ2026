using UnityEngine;

namespace IsoTools.Examples.Kenney {
	/// <summary>
	/// ファントムキャットがBuildingに完全に隠れている間、
	/// シーン上のLightObjectのSpotLight2Dを自分の位置へ収束させる
	/// </summary>
	[RequireComponent(typeof(PhantomCatOcclusionDetector))]
	public class PhantomCatSpotlightAttractor : MonoBehaviour {

		[SerializeField] private float _convergeDuration = 3f;

		private PhantomCatOcclusionDetector _occlusionDetector = null;
		private LightObject[] _lightObjects = null;
		private bool _wasHidden = false;

		void Start() {
			_occlusionDetector = GetComponent<PhantomCatOcclusionDetector>();
			_lightObjects = FindObjectsByType<LightObject>(FindObjectsSortMode.None);
		}

		void Update() {
			var hidden = _occlusionDetector.IsHidden;
			if ( hidden == _wasHidden ) {
				return;
			}
			_wasHidden = hidden;
			for ( int i = 0, e = _lightObjects.Length; i < e; ++i ) {
				var light_object = _lightObjects[i];
				if ( !light_object ) {
					continue;
				}
				if ( hidden ) {
					light_object.BeginConverge(transform, _convergeDuration);
				} else {
					light_object.EndConverge();
				}
			}
		}

		void OnDestroy() {
			// 猫の消滅時にライトを巡回に戻す
			if ( _lightObjects == null ) {
				return;
			}
			for ( int i = 0, e = _lightObjects.Length; i < e; ++i ) {
				var light_object = _lightObjects[i];
				if ( light_object ) {
					light_object.EndConverge();
				}
			}
		}
	}
}
