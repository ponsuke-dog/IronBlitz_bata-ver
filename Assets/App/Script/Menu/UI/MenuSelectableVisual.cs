using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 選択中または最新操作対象のUIを強調表示する。
/// </summary>
public class MenuSelectableVisual : MonoBehaviour,
    ISelectHandler,
    IDeselectHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private Graphic targetGraphic;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightedColor = new Color(1.0f, 0.82f, 0.2f, 1.0f);

    [SerializeField] private float normalScale = 1.0f;
    [SerializeField] private float highlightedScale = 1.08f;

    private bool isSelected;
    private bool isPointerOver;

    private void Reset()
    {
        targetGraphic = GetComponent<Graphic>();
    }

    private void Awake()
    {
        ApplyVisual();
    }

    private void OnDisable()
    {
        isSelected = false;
        isPointerOver = false;
        ApplyVisual();
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        isPointerOver = false;

        ApplyVisual();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        ApplyVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable())
        {
            return;
        }

        isPointerOver = true;
        isSelected = false;

        ApplyVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        ApplyVisual();
    }

    private bool IsInteractable()
    {
        Selectable selectable = GetComponent<Selectable>();

        if (selectable == null)
        {
            return true;
        }

        return selectable.interactable;
    }

    private void ApplyVisual()
    {
        bool highlighted = isSelected || isPointerOver;

        if (targetGraphic != null)
        {
            targetGraphic.color = highlighted
                ? highlightedColor
                : normalColor;
        }

        transform.localScale = Vector3.one *
            (highlighted ? highlightedScale : normalScale);
    }
}