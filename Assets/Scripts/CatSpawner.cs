using UnityEngine;
using System.Collections;

namespace IsoTools.Examples.Kenney {
	public class CatSpawner : MonoBehaviour {

		[SerializeField, Range(0, 200)] private int maxCatCount = 50;
		[SerializeField] private GameObject[] dummyCatPrefabs = null;
		[SerializeField] private GameObject phantomCatPrefab = null;
		private IsoWorld iso_world = null;

		/// <summary>生成済みのファントムキャット。生成前は null。</summary>
		public GameObject PhantomCat { get; private set; }

		/// <summary>ファントムキャットは実行時生成なので、操作権を割り当てる側へ生成を知らせる。</summary>
		public event System.Action<GameObject> PhantomCatSpawned;

		void Start() {
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
			cat_iso_obj.position = new Vector3(dx, dy, 0.5f);

			return catObj;
		}
	}
}