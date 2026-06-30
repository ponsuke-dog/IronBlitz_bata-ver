using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Config画面の状態を一元管理するクラス。
/// Category選択状態と項目調整状態を切り替え、
/// マウス / キーボード / ゲームパッド操作で状態が崩れないようにする。
/// </summary>
public class ConfigFocusStateController : MonoBehaviour
{
    public enum ConfigCategory
    {
        Audio,
        Mouse,
        Controller
    }

    [Header("Category Area")]
    [SerializeField] private CanvasGroup categoryCanvasGroup;

    [Header("Item Areas")]
    [SerializeField] private CanvasGroup audioPageCanvasGroup;
    [SerializeField] private CanvasGroup mousePageCanvasGroup;
    [SerializeField] private CanvasGroup controllerPageCanvasGroup;

    [Header("Category Buttons")]
    [SerializeField] private Selectable audioTabButton;
    [SerializeField] private Selectable mouseTabButton;
    [SerializeField] private Selectable controllerTabButton;
    [SerializeField] private Selectable backButton;

    [Header("Audio Items")]
    [SerializeField] private Selectable masterSlider;
    [SerializeField] private Selectable bgmSlider;
    [SerializeField] private Selectable seSlider;
    [SerializeField] private Selectable uiSlider;
    [SerializeField] private Selectable audioResetButton;
    [SerializeField] private Selectable audioItemBackButton;

    [Header("Mouse Items")]
    [SerializeField] private Selectable mouseSensitivitySlider;
    [SerializeField] private Selectable sensitivityResetButton;
    [SerializeField] private Selectable mouseItemBackButton;

    [Header("Controller Items")]
    [SerializeField] private Selectable controllerSensitivitySlider;
    [SerializeField] private Selectable controllerItemBackButton;

    private ConfigCategory currentCategory;
    private bool isEditingItem;

    public bool IsEditingItem => isEditingItem;

    private void Awake()
    {
        RegisterItemBackButton(audioItemBackButton);
        RegisterItemBackButton(mouseItemBackButton);
        RegisterItemBackButton(controllerItemBackButton);
    }

    public void SelectCategory(ConfigCategory category)
    {
        currentCategory = category;
        isEditingItem = false;

        SetCategoryAreaEnabled(true);
        SetAllItemAreasEnabled(false);

        Select(GetCategorySelectable(category));
    }

    public void EnterCurrentCategoryItems()
    {
        isEditingItem = true;

        SetCategoryAreaEnabled(false);
        SetAllItemAreasEnabled(false);

        switch (currentCategory)
        {
            case ConfigCategory.Audio:
                SetCanvasGroupEnabled(audioPageCanvasGroup, true);
                SetAudioItemsInteractable(true);
                Select(masterSlider);
                break;

            case ConfigCategory.Mouse:
                SetCanvasGroupEnabled(mousePageCanvasGroup, true);
                SetMouseItemsInteractable(true);
                Select(mouseSensitivitySlider);
                break;

            case ConfigCategory.Controller:
                SetCanvasGroupEnabled(controllerPageCanvasGroup, true);
                SetControllerItemsInteractable(true);
                Select(controllerSensitivitySlider);
                break;
        }
    }

    public bool HandleBack()
    {
        if (!isEditingItem)
        {
            return false;
        }

        ReturnToCurrentCategory();
        return true;
    }

    private void ReturnToCurrentCategory()
    {
        SelectCategory(currentCategory);
    }

    private Selectable GetCategorySelectable(ConfigCategory category)
    {
        switch (category)
        {
            case ConfigCategory.Audio:
                return audioTabButton;

            case ConfigCategory.Mouse:
                return mouseTabButton;

            case ConfigCategory.Controller:
                return controllerTabButton;

            default:
                return audioTabButton;
        }
    }

    private void SetCategoryAreaEnabled(bool enabled)
    {
        SetCanvasGroupEnabled(categoryCanvasGroup, enabled);

        SetSelectableInteractable(audioTabButton, enabled);
        SetSelectableInteractable(mouseTabButton, enabled);
        SetSelectableInteractable(controllerTabButton, enabled);
        SetSelectableInteractable(backButton, enabled);
    }

    private void SetAllItemAreasEnabled(bool enabled)
    {
        SetCanvasGroupEnabled(audioPageCanvasGroup, enabled);
        SetCanvasGroupEnabled(mousePageCanvasGroup, enabled);
        SetCanvasGroupEnabled(controllerPageCanvasGroup, enabled);

        SetAudioItemsInteractable(enabled);
        SetMouseItemsInteractable(enabled);
        SetControllerItemsInteractable(enabled);
    }

    private void SetAudioItemsInteractable(bool enabled)
    {
        SetSelectableInteractable(masterSlider, enabled);
        SetSelectableInteractable(bgmSlider, enabled);
        SetSelectableInteractable(seSlider, enabled);
        SetSelectableInteractable(uiSlider, enabled);
        SetSelectableInteractable(audioResetButton, enabled);
        SetSelectableInteractable(audioItemBackButton, enabled);
    }

    private void SetMouseItemsInteractable(bool enabled)
    {
        SetSelectableInteractable(mouseSensitivitySlider, enabled);
        SetSelectableInteractable(sensitivityResetButton, enabled);
        SetSelectableInteractable(mouseItemBackButton, enabled);
    }

    private void SetControllerItemsInteractable(bool enabled)
    {
        SetSelectableInteractable(controllerSensitivitySlider, enabled);
        SetSelectableInteractable(controllerItemBackButton, enabled);
    }

    private void SetSelectableInteractable(Selectable selectable, bool enabled)
    {
        if (selectable == null)
        {
            return;
        }

        selectable.interactable = enabled;
    }

    private void SetCanvasGroupEnabled(CanvasGroup canvasGroup, bool enabled)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;
    }

    private void Select(Selectable selectable)
    {
        if (selectable == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }

    private void RegisterItemBackButton(Selectable itemBackSelectable)
    {
        if (itemBackSelectable == null)
        {
            return;
        }

        Button itemBackButton = itemBackSelectable.GetComponent<Button>();

        if (itemBackButton == null)
        {
            Debug.LogWarning($"{itemBackSelectable.name} に Button コンポーネントがありません。");
            return;
        }

        itemBackButton.onClick.AddListener(ReturnToCurrentCategory);
    }
}