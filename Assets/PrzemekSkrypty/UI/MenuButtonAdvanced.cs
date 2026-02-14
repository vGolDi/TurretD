using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class MenuButtonAdvanced : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image underline;
    [SerializeField] private TextMeshProUGUI arrowText;

    [Header("Normal Colors")]
    [SerializeField] private Color normalTextColor = Color.black;
    [SerializeField] private Color normalUnderlineColor = Color.black;

    [Header("Hover Colors")]
    [SerializeField] private Color hoverTextColor = new Color(0.25f, 0.5f, 1f);
    [SerializeField] private Color hoverUnderlineColor = new Color(0.25f, 0.5f, 1f);

    [Header("Settings")]
    [SerializeField] private float animationDuration = 0.2f;

    public bool isHovered = false;

    void Start()
    {
        SetNormalState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        AnimateToHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        AnimateToNormal();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Tutaj logika przycisku
        Debug.Log($"Clicked: {titleText.text}");
    }

    private void AnimateToHover()
    {
        // Bez DOTween (podstawowa wersja)
        StartCoroutine(AnimateColor(titleText, hoverTextColor));
        StartCoroutine(AnimateColor(arrowText, hoverTextColor));
        StartCoroutine(AnimateImageColor(underline, hoverUnderlineColor));

        // LUB z DOTween (jeœli masz):
        // titleText.DOColor(hoverTextColor, animationDuration);
        // arrowText.DOColor(hoverTextColor, animationDuration);
        // underline.DOColor(hoverUnderlineColor, animationDuration);
    }

    private void AnimateToNormal()
    {
        StartCoroutine(AnimateColor(titleText, normalTextColor));
        StartCoroutine(AnimateColor(arrowText, normalTextColor));
        StartCoroutine(AnimateImageColor(underline, normalUnderlineColor));
    }

    private void SetNormalState()
    {
        titleText.color = normalTextColor;
        if (arrowText) arrowText.color = normalTextColor;
        underline.color = normalUnderlineColor;
    }

    private System.Collections.IEnumerator AnimateColor(TextMeshProUGUI text, Color targetColor)
    {
        float elapsed = 0;
        Color startColor = text.color;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            text.color = Color.Lerp(startColor, targetColor, elapsed / animationDuration);
            yield return null;
        }

        text.color = targetColor;
    }

    private System.Collections.IEnumerator AnimateImageColor(Image image, Color targetColor)
    {
        float elapsed = 0;
        Color startColor = image.color;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            image.color = Color.Lerp(startColor, targetColor, elapsed / animationDuration);
            yield return null;
        }

        image.color = targetColor;
    }
}