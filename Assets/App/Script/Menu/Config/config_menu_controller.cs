using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// コンフィグメニュー全体を制御するクラス。
/// Audio / Mouse / Controller ページの切り替えを担当します。
/// </summary>
public class ConfigMenuController : MonoBehaviour
{
    private enum ConfigPageType
    {
        Audio,
        Mouse,
        Controller
    }

    [Header("References")]
    [SerializeField] private MenuManager menuManager;

    [Header("Root")]
    [SerializeField] private GameObject rootObject;

    [Header("Page Roots")]
    [SerializeField] private GameObject audioPageRoot;
    [SerializeField] private GameObject mousePageRoot;
    [SerializeField] private GameObject controllerPageRoot;

    [Header("Buttons")]
    [SerializeField] private Button audioTabButton;
    [SerializeField] private Button mouseTabButton;
    [SerializeField] private Button controllerTabButton;
    [SerializeField] private Button backButton;

    [SerializeField] private ConfigFocusStateController focusStateController;

    private Action externalCloseRequest;

    private void Awake()
    {
        audioTabButton.onClick.AddListener(() => OnClickCategory(ConfigPageType.Audio));
        mouseTabButton.onClick.AddListener(() => OnClickCategory(ConfigPageType.Mouse));
        controllerTabButton.onClick.AddListener(() => OnClickCategory(ConfigPageType.Controller));
        backButton.onClick.AddListener(OnClickBack);
    }

    public void SetExternalCloseRequest(Action closeRequest)
    {
        externalCloseRequest = closeRequest;
    }

    public void ClearExternalCloseRequest()
    {
        externalCloseRequest = null;
    }

    private void OnClickCategory(ConfigPageType pageType)
    {
        ChangePage(pageType);

        if (focusStateController != null)
        {
            focusStateController.EnterCurrentCategoryItems();
        }
    }

    public void Open()
    {
        rootObject.SetActive(true);
        ChangePage(ConfigPageType.Audio);
    }

    public void Close()
    {
        rootObject.SetActive(false);
    }

    private void ChangePage(ConfigPageType pageType)
    {
        audioPageRoot.SetActive(pageType == ConfigPageType.Audio);
        mousePageRoot.SetActive(pageType == ConfigPageType.Mouse);
        controllerPageRoot.SetActive(pageType == ConfigPageType.Controller);

        if (focusStateController == null)
        {
            return;
        }

        switch (pageType)
        {
            case ConfigPageType.Audio:
                focusStateController.SelectCategory(ConfigFocusStateController.ConfigCategory.Audio);
                break;

            case ConfigPageType.Mouse:
                focusStateController.SelectCategory(ConfigFocusStateController.ConfigCategory.Mouse);
                break;

            case ConfigPageType.Controller:
                focusStateController.SelectCategory(ConfigFocusStateController.ConfigCategory.Controller);
                break;
        }
    }

    private void OnClickBack()
    {
        RequestBack();
    }

    public bool HandleBack()
    {
        return RequestBack();
    }

    private bool RequestBack()
    {
        if (focusStateController != null && focusStateController.HandleBack())
        {
            return true;
        }

        if (externalCloseRequest != null)
        {
            externalCloseRequest.Invoke();
            return true;
        }

        if (menuManager != null)
        {
            menuManager.OpenPauseMenu();
            return true;
        }

        return false;
    }
}