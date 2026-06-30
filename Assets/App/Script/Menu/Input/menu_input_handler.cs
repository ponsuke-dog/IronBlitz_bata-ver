using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// GameJam_InputSystem を使って、Menu入力とActionMap切り替えを管理するクラス。
/// 
/// メニューを開いた時:
/// Player ActionMap を無効化し、UI ActionMap を有効化する。
/// 
/// メニューを閉じた時:
/// UI ActionMap を無効化し、Player ActionMap を有効化する。
/// </summary>
public class MenuInputHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private InputActionAsset inputActions;

    [Header("Action Map Names")]
    [SerializeField] private string playerActionMapName = "Player";
    [SerializeField] private string uiActionMapName = "UI";

    [Header("Action Names")]
    [SerializeField] private string menuActionName = "Menu";
    [SerializeField] private string backActionName = "Back";

    private InputActionMap playerActionMap;
    private InputActionMap uiActionMap;

    private InputAction menuAction;
    private InputAction backAction;

    private void Awake()
    {
        playerActionMap = inputActions.FindActionMap(playerActionMapName, true);
        uiActionMap = inputActions.FindActionMap(uiActionMapName, true);

        menuAction = playerActionMap.FindAction(menuActionName, true);
        backAction = uiActionMap.FindAction(backActionName, true);

        playerActionMap.Disable();
    }

    private void OnEnable()
    {
        menuAction.performed += OnMenuPerformed;
        backAction.performed += OnBackPerformed;

        menuManager.OnMenuStateChanged += OnMenuStateChanged;

        EnablePlayerInput();
    }

    private void OnDisable()
    {
        menuAction.performed -= OnMenuPerformed;
        backAction.performed -= OnBackPerformed;

        menuManager.OnMenuStateChanged -= OnMenuStateChanged;
    }

    /// <summary>
    /// Player操作中のMenu入力。
    /// </summary>
    private void OnMenuPerformed(InputAction.CallbackContext context)
    {
        if (menuManager.IsMenuOpen)
        {
            return;
        }

        menuManager.OpenPauseMenu();
    }

    /// <summary>
    /// UI操作中のBack入力。
    /// </summary>
    private void OnBackPerformed(InputAction.CallbackContext context)
    {
        menuManager.HandleBack();
    }

    /// <summary>
    /// MenuManagerの状態変更に合わせてActionMapを切り替える。
    /// Button操作 / キー操作 / マウス操作のどれでも同期される。
    /// </summary>
    private void OnMenuStateChanged(MenuManager.MenuState state)
    {
        if (state == MenuManager.MenuState.Closed)
        {
            EnablePlayerInput();
            return;
        }

        EnableUiInput();
    }

    /// <summary>
    /// Player操作用ActionMapを有効化する。
    /// </summary>
    private void EnablePlayerInput()
    {
        uiActionMap.Disable();
        playerActionMap.Enable();
    }

    /// <summary>
    /// UI操作用ActionMapを有効化する。
    /// </summary>
    private void EnableUiInput()
    {
        playerActionMap.Disable();
        uiActionMap.Enable();
    }
}