using IsoTools;
using UnityEngine;

public class TileGenerator : MonoBehaviour
{
    [SerializeField] private GameObject tilePrefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        for (var x = -9; x <= 9; ++x)
        {
            for (var y = -9; y <= 9; ++y)
            {
                var tile = Instantiate(tilePrefab, transform).GetComponent<IsoObject>();
                tile.position = new(x, y, -1);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
