using System;
using UnityEngine;

/// <summary>
/// メニュー全体の表示状態を管理するクラス。
/// PauseMenu と ConfigMenu の切り替えを担当する。
/// </summary>
public class MenuManager : MonoBehaviour
{
    public enum MenuState
    {
        Closed,
        Pause,
        Config
    }

    [Header("Menu Controllers")]
    [SerializeField] private PauseMenuController pauseMenuController;
    [SerializeField] private ConfigMenuController configMenuController;

    [Header("Canvas")]
    [SerializeField] private Canvas menuCanvas;

    private MenuState currentState = MenuState.Closed;

    /// <summary>
    /// 外部からBack入力を処理したい場合に使用します。
    /// タイトル画面からConfigを開いた時だけ登録します。
    /// </summary>
    private Func<bool> externalBackHandler;

    public event Action<MenuState> OnMenuStateChanged;

    public bool IsMenuOpen => currentState != MenuState.Closed;
    public MenuState CurrentState => currentState;

    private void Start()
    {
        if (menuCanvas != null)
        {
            menuCanvas.gameObject.SetActive(true);
        }

        CloseAllMenus();
    }

    public void SetExternalBackHandler(Func<bool> handler)
    {
        externalBackHandler = handler;
    }

    public void ClearExternalBackHandler()
    {
        externalBackHandler = null;
    }

    public void OpenPauseMenu()
    {
        SetState(MenuState.Pause);
    }

    public void OpenConfigMenu()
    {
        SetState(MenuState.Config);
    }

    public void CloseAllMenus()
    {
        SetState(MenuState.Closed);
    }

    /// <summary>
    /// Back入力時の戻り先を現在のメニュー状態から判断します。
    /// </summary>
    public void HandleBack()
    {
        // タイトル画面など、外部側でBackを処理したい場合は最優先します。
        if (externalBackHandler != null && externalBackHandler.Invoke())
        {
            return;
        }

        switch (currentState)
        {
            case MenuState.Config:
                if (configMenuController != null && configMenuController.HandleBack())
                {
                    return;
                }

                OpenPauseMenu();
                break;

            case MenuState.Pause:
                CloseAllMenus();
                break;

            case MenuState.Closed:
                break;
        }
    }

    private void SetState(MenuState nextState)
    {
        currentState = nextState;

        switch (currentState)
        {
            case MenuState.Closed:
                pauseMenuController.Close();
                configMenuController.Close();
                Time.timeScale = 1.0f;
                break;

            case MenuState.Pause:
                pauseMenuController.Open();
                configMenuController.Close();
                Time.timeScale = 0.0f;
                break;

            case MenuState.Config:
                pauseMenuController.Close();
                configMenuController.Open();
                Time.timeScale = 0.0f;
                break;
        }

        OnMenuStateChanged?.Invoke(currentState);
    }
}