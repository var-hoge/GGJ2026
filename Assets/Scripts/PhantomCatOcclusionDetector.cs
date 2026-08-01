using UnityEngine;
using KanKikuchi.AudioManager;
using System.Collections;
using System.Collections.Generic;

namespace IsoTools.Examples.Kenney {
	public class PhantomCatOcclusionDetector : MonoBehaviour {

		[SerializeField] private SpriteRenderer _spriteRenderer = null;

		[Header("遮蔽物のコンテナ名 (IsoWorldプレハブ内)")]
		[SerializeField] private string[] _obstacleRootNames = { "Buildings", "Fences", "Trees" };

		[Header("隠れたときのSEを再生する最短間隔(秒)")]
		[SerializeField] private float _seMinInterval = 2f;
		private float _lastSeTime = float.NegativeInfinity;

		private readonly List<SpriteRenderer> _obstacleRenderers = new List<SpriteRenderer>();

		public bool IsHidden { get; private set; } = false;

		IEnumerator Start() {
			if ( !_spriteRenderer ) {
				_spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			}

			CollectObstacleRenderers();

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

		/// <summary>建物・フェンス・木など、猫を隠しうるものの SpriteRenderer をまとめて集める。</summary>
		void CollectObstacleRenderers() {
			_obstacleRenderers.Clear();

			var buffer = new List<SpriteRenderer>();
			for ( int i = 0, e = _obstacleRootNames.Length; i < e; ++i ) {
				var root_name = _obstacleRootNames[i];
				var obstacle_root = GameObject.Find(root_name);
				if ( !obstacle_root ) {
					Debug.LogWarning($"PhantomCatOcclusionDetector. {root_name} not found!");
					continue;
				}
				// GetComponentsInChildrenは渡したリストを毎回クリアするので、別のリストで受けて足していく
				obstacle_root.GetComponentsInChildren(buffer);
				_obstacleRenderers.AddRange(buffer);
			}
		}

		void PlayHiddenSe() {
			if ( Time.time - _lastSeTime < _seMinInterval ) {
				return;
			}
			_lastSeTime = Time.time;

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
