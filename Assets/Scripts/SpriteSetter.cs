using UnityEngine;

public class SpriteSetter : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites = null;
    [SerializeField] private SpriteRenderer spriteRenderer = null;

    private static bool display = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!display)
        {
            display = true;
            return;
        }

        var index = Random.Range(0, sprites.Length);
        spriteRenderer.sprite = sprites[index];
        
        var signs = new[] {1, -1};
        var signIndex = Random.Range(0, signs.Length);
        var scale = spriteRenderer.transform.localScale;
        spriteRenderer.transform.localScale = new(scale.x * signs[signIndex], scale.y, scale.z);

        display = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
