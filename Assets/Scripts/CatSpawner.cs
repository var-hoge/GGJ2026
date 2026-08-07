using UnityEngine;
using System.Collections;

namespace IsoTools.Examples.Kenney {
	public class CatSpawner : MonoBehaviour {

		/// <summary>
		/// 地面の高さ。床タイルは z が 0 から 0.53 までの厚みを持つブロックなので、
		/// その上に立つものはこの高さに置く。フェンス・木・プレイヤーも同じ値で配置されている。
		/// </summary>
		private const float GroundHeight = 0.53f;

		[SerializeField, Range(0, 200)] private int maxCatCount = 50;
		[SerializeField] private GameObject[] dummyCatPrefabs = null;
		[SerializeField] private GameObject phantomCatPrefab = null;
		private IsoWorld iso_world = null;

		/// <summary>生成済みのファントムキャット。生成前は null。</summary>
		public GameObject PhantomCat { get; private set; }

		/// <summary>ファントムキャットは実行時生成なので、操作権を割り当てる側へ生成を知らせる。</summary>
		public event System.Action<GameObject> PhantomCatSpawned;

		void Start() {
			// 通信対戦では、猫の配置が両端末で一致していなければゲームが成立しない
			// (どれが本物かを探す遊びなので、フェイクキャットの並びがずれると別のゲームになる)。
			// 部屋を作った側が決めたシードを使い、同じ乱数列から同じ配置を作る
			if (MatchRandomSeed.HasValue)
			{
				Random.InitState(MatchRandomSeed.Value);
			}

			iso_world = IsoWorld.GetWorld(0);

			// ダミーキャットを生成
			for (var n = 0; n < maxCatCount; ++n)
			{
				var index = Random.Range(0, dummyCatPrefabs.Length);
				var prefab = dummyCatPrefabs[index];
				Spawn(prefab, 1, 5);
			}

			// ファントムキャットを生成
			PhantomCat = Spawn(phantomCatPrefab, 1, 3);
			PhantomCatSpawned?.Invoke(PhantomCat);
		}

		GameObject Spawn(GameObject prefab, float minInclude, float maxInclude)
		{

			var catObj = Instantiate(prefab, iso_world.transform);

			var dx = Random.Range(minInclude, maxInclude);
			var dy = Random.Range(minInclude, maxInclude);
			var cat_iso_obj = catObj.GetComponent<IsoObject>();
			cat_iso_obj.position = new Vector3(dx, dy, GroundHeight);

			return catObj;
		}
	}
}