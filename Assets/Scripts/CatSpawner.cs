using UnityEngine;
using System.Collections;

namespace IsoTools.Examples.Kenney {
	public class CatSpawner : MonoBehaviour {

		[SerializeField] private int maxCatCount   = 1;
		[SerializeField] private GameObject catPrefab = null;
		private IsoWorld iso_world = null;
		
		void Start() {
			if ( !catPrefab ) {
				throw new UnityException("CatSpawner. Cat prefab not found!");
			}
			iso_world = IsoWorld.GetWorld(0);
			StartCoroutine(SpawnCats());

			for (var n = 0; n < maxCatCount; ++n)
			{
				Spawn();
			}
		}
		
		IEnumerator SpawnCats() {
			while ( true ) {
				var cats = GameObject.FindObjectsOfType<Cat>();
				if (cats.Length < maxCatCount)
				{
					Spawn();
				}
				yield return new WaitForSeconds(Random.Range(2.0f, 5.0f));
			}
		}

		void Spawn()
		{
			var dx = Random.Range(-5.0f, 5.0f);
			var dy = Random.Range(-5.0f, 5.0f);
			var catObj = Instantiate(catPrefab, iso_world.transform);
			var cat_iso_obj = catObj.GetComponent<IsoObject>();
			cat_iso_obj.position = new Vector3(dx, dy, 0.5f);
		}
	}
}