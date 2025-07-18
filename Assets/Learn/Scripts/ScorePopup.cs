using TMPro;
using UnityEngine;
using DG.Tweening;

public class ScorePopup : MonoBehaviour
{
    public TextMeshPro popupText;
    public float moveY = 1f;
    public float duration = 0.7f; // daha kýsa

    public void Setup(int amount, Color color)
    {
        popupText.text = "+" + amount;
        popupText.color = color;

        transform.localScale = Vector3.zero;
        transform.rotation = Quaternion.identity;

        Sequence seq = DOTween.Sequence();

        // 1. Hýzlý büyü + hafif zýplama
        seq.Append(transform.DOScale(1.3f, 0.12f).SetEase(Ease.OutBack));

        // 2. Küçük bir titreþim (Techno bounce)
        seq.Append(transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 10, 0.7f));

        // 3. Hafif eðilme/dönme - dans hissi
        float randomAngle = Random.Range(-10f, 10f);
        seq.Join(transform.DORotate(new Vector3(0, 0, randomAngle), 0.3f).SetEase(Ease.InOutSine));

        // 4. Yukarý zýplar gibi çýkýþ + fade out
        seq.Append(transform.DOMoveY(transform.position.y + moveY, duration).SetEase(Ease.OutQuad));
        seq.Join(popupText.DOFade(0f, duration));

        // 5. Sahneyi terk et
        seq.OnComplete(() => Destroy(gameObject));
    }
}
