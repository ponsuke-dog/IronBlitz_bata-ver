using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// ポーズメニュー画面を制御するクラス。
/// Resume / Config / Restart / Return などのボタン処理を担当する。
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MenuManager menuManager;

    [Header("Root")]
    [SerializeField] private GameObject rootObject;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button configButton;
    [SerializeField] private Button restartStageButton;
    [SerializeField] private Button returnStageSelectButton;
    [SerializeField] private Button returnTitleButton;

    [Header("Scene Data")]
    [SerializeField] private SceneData restartScene;
    [SerializeField] private SceneData stageSelectScene;
    [SerializeField] private SceneData titleScene;

    private void Awake()
    {
        resumeButton.onClick.AddListener(OnClickResume);
        configButton.onClick.AddListener(OnClickConfig);
        restartStageButton.onClick.AddListener(OnClickRestartStage);
        returnStageSelectButton.onClick.AddListener(OnClickReturnStageSelect);
        returnTitleButton.onClick.AddListener(OnClickReturnTitle);
    }

    /// <summary>
    /// ポーズメニューを表示する。
    /// </summary>
    public void Open()
    {
        rootObject.SetActive(true);

        if (EventSystem.current != null && resumeButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }
    }

    /// <summary>
    /// ポーズメニューを非表示にする。
    /// </summary>
    public void Close()
    {
        rootObject.SetActive(false);
    }

    private void OnClickResume()
    {
        menuManager.CloseAllMenus();
    }

    private void OnClickConfig()
    {
        menuManager.OpenConfigMenu();
    }

    private void OnClickRestartStage()
    {
        ChangeScene(restartScene);
    }

    private void OnClickReturnStageSelect()
    {
        ChangeScene(stageSelectScene);
    }

    private void OnClickReturnTitle()
    {
        ChangeScene(titleScene);
    }

    /// <summary>
    /// SceneChangeManager経由でシーン遷移する。
    /// </summary>
    private void ChangeScene(SceneData sceneData)
    {
        if (sceneData == null)
        {
            Debug.LogWarning("SceneData が設定されていません。");
            return;
        }

        if (SceneChangeManager.Instance == null)
        {
            Debug.LogWarning("SceneChangeManager がシーン内に存在しません。");
            return;
        }

        Time.timeScale = 1.0f;
        SceneChangeManager.Instance.ChangeScene(sceneData);
    }
}