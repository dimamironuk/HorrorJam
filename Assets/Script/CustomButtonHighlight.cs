using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CustomButtonHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerClickHandler
{
    [Header("Transparency Settings")]
    [SerializeField] private Image highlightIndicator;
    [SerializeField] private float normalAlpha = 0f;
    [SerializeField] private float activeAlpha = 0.8f; // Можна міняти в Inspector (0.8, 0.5 тощо)
    [SerializeField] private float fadeSpeed = 14f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    private float _targetAlpha;

    private void Awake()
    {
        _targetAlpha = normalAlpha;
        if (highlightIndicator != null)
        {
            Color c = highlightIndicator.color;
            c.a = normalAlpha;
            highlightIndicator.color = c;
        }
    }

    private void Update()
    {
        if (highlightIndicator == null) return;

        Color c = highlightIndicator.color;
        c.a = Mathf.Lerp(c.a, _targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
        highlightIndicator.color = c;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _targetAlpha = normalAlpha;
    }

    public void OnSelect(BaseEventData eventData)
    {
        _targetAlpha = activeAlpha;
        PlaySound(hoverSound);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _targetAlpha = normalAlpha;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlaySound(clickSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}