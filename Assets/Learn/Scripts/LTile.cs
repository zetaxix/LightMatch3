using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LTile : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    private LGridM gridManager;
    private Vector2 startPos;
    private Vector2 endPos;
    private float swipeThreshold = 0.3f; // ne kadar kayýnca swipe saysýn

    public Light2D tileLight; // Inspector'dan veya otomatik bul

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetSprite(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;

        // Iþýk varsa, rengi sprite adýna göre ayarla
        if (tileLight != null)
        {
            string name = sprite.name.ToLower();

            if (name.Contains("red")) tileLight.color = Color.red;
            else if (name.Contains("blue")) tileLight.color = Color.blue;
            else if (name.Contains("green")) tileLight.color = Color.green;
            else if (name.Contains("yellow")) tileLight.color = Color.yellow;
            else if (name.Contains("purple")) tileLight.color = new Color(0.6f, 0f, 0.8f);
            else tileLight.color = Color.white; // fallback
        }
    }

    public Sprite GetSprite()
    {
        return spriteRenderer.sprite;
    }

    public void MoveToPosition(Vector2 targetPos, float duration = 0.5f)
    {
        transform.DOMove(targetPos, duration).SetEase(Ease.InOutQuad);
    }

    public void SetManager(LGridM manager)
    {
        gridManager = manager;
    }

    private void OnMouseDown()
    {
        startPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private void OnMouseUp()
    {
        endPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 swipe = endPos - startPos;

        if (swipe.magnitude >= swipeThreshold)
        {
            Vector2Int direction = Vector2Int.zero;

            // Hangi yöne daha çok gidilmiþse orayý al
            if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
                direction = swipe.x > 0 ? Vector2Int.right : Vector2Int.left;
            else
                direction = swipe.y > 0 ? Vector2Int.up : Vector2Int.down;

            if (gridManager != null)
            {
                gridManager.TrySwipe(this, direction);
            }
        }
    }

    public void DestroyWithEffect(float duration = 0.5f)
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack));
        seq.Join(spriteRenderer.DOFade(0f, duration));

        if (tileLight != null)
            seq.Join(DOTween.To(() => tileLight.intensity, x => tileLight.intensity = x, 0f, duration));

        seq.OnComplete(() => Destroy(gameObject));
    }

}
