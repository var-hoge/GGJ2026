using UnityEngine;
using KanKikuchi.AudioManager;
using System.Collections;
using System.Collections.Generic;

namespace IsoTools.Examples.Kenney {
	public class PhantomCatOcclusionDetector : MonoBehaviour {

		// IsoWorldプレハブ内の建物コンテナ名
		private const string ObstacleRootName = "Buildings";

		[SerializeField] private SpriteRenderer _spriteRenderer = null;

		private readonly List<SpriteRenderer> _obstacleRenderers = new List<SpriteRenderer>();

		public bool IsHidden { get; private set; } = false;

		IEnumerator Start() {
			if ( !_spriteRenderer ) {
				_spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			}

			var obstacle_root = GameObject.Find(ObstacleRootName);
			if ( obstacle_root ) {
				obstacle_root.GetComponentsInChildren(_obstacleRenderers);
			} else {
				Debug.LogWarning($"PhantomCatOcclusionDetector. {ObstacleRootName} not found!");
			}

			// IsoWorldがLateUpdateで描画順(z値)を確定させた後に判定する
			var wait = new WaitForEndOfFrame();
			while ( true ) {
				yield return wait;
				var hidden = IsFullyHiddenByObstacle();
				if ( hidden && !IsHidden ) {
					PlayHiddenSe();
				}
				IsHidden = hidden;
			}
		}

		void PlayHiddenSe() {
			var sfx = new[]
			{
				SEPath.SFX_HELICOPTER_DOG_01_JP,
				SEPath.SFX_HELICOPTER_DOG_02_JP,
				SEPath.SFX_HELICOPTER_DOG_03_JP,
			};
			var index = Random.Range(0, sfx.Length);
			SEManager.Instance.Play(sfx[index]);
		}

		bool IsFullyHiddenByObstacle() {
			if ( !_spriteRenderer ) {
				return false;
			}
			var cat_bounds = _spriteRenderer.bounds;
			for ( int i = 0, e = _obstacleRenderers.Count; i < e; ++i ) {
				var obstacle = _obstacleRenderers[i];
				if ( !obstacle ) {
					continue;
				}
				var bounds = obstacle.bounds;
				// カメラは平行投影で+z方向を向いているため、zが小さいほど手前に描画される
				var in_front = bounds.center.z < cat_bounds.center.z;
				if ( in_front
					&& bounds.min.x <= cat_bounds.min.x && bounds.max.x >= cat_bounds.max.x
					&& bounds.min.y <= cat_bounds.min.y && bounds.max.y >= cat_bounds.max.y )
				{
					return true;
				}
			}
			return false;
		}
	}
}
