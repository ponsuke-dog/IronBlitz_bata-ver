using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StageSelectManager : MonoBehaviour
{
    public static StageSelectManager Instance { get; private set; }

    [SerializeField] private Button StageButtonPrefab;
    [SerializeField] Transform buttonParent;

    [SerializeField]private SelectInputController inputController;

    private List<Button> currentButtons = new();

    private WorldData currentWorldData;
    private GameObject previewSelected;
    private GameObject currentSelected;

    private void Awake()
    {
        // シングルトン
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void Start()
    {
        inputController.Open();
        SaveSystem.Load();
        Time.timeScale = 1f;
    }
    public void SetUP(WorldData world)
    {
        currentWorldData = world;
        CreateStageButtons();
        //inputController.SetStageButton();
    }

    private void Update()
    {

        currentSelected = EventSystem.current.currentSelectedGameObject;
        if (currentSelected != previewSelected)
        {
            previewSelected = currentSelected;
            if (currentSelected != null)
            {
                
                int index = inputController.GetButtonNum();

                if (currentWorldData == null)
                {
                    return;
                }
                if (index < 0 || index >= currentWorldData.stages.Length)
                {
                    return;
                }

                UpdateMissionUI(index);
            }
        }



    }
    private void UpdateMissionUI(int index)
    {
        Debug.Log(currentWorldData);

        StageData stage = currentWorldData.stages[index];

        Debug.Log(stage);

        Debug.Log(stage.stageMission);

        Debug.Log(SelectMissionManager.Instance);
        if (currentWorldData == null)
            return;

        SelectMissionManager.Instance.DrawMainMissionUIs(stage.stageMission.MainMissionPreset);
        SelectMissionManager.Instance.DrawSubMissionUIs(stage.stageMission.SubMissionPreset1, 0);
        SelectMissionManager.Instance.DrawSubMissionUIs(stage.stageMission.SubMissionPreset2, 1);
        SelectMissionManager.Instance.DrawSubMissionUIs(stage.stageMission.SubMissionPreset3, 2);
        SelectMissionManager.Instance.CheakMissionClearStar(index);
        SelectMissionManager.Instance.DrawBestTime(index);

        GameData.SetCurrentStageData(stage);
    }

    private void CreateStageButtons()
    {
        // ボタンのリセット
        foreach (var button in currentButtons)
        {
            Destroy(button.gameObject);
        }
        currentButtons.Clear();

        foreach (var stage in currentWorldData.stages)
        {
            if (SaveSystem.GetUnlock(stage.stageID))
            {
                Button button = Instantiate(StageButtonPrefab, buttonParent);

                button.GetComponentInChildren<TMPro.TMP_Text>().text = stage.stageName;

                button.onClick.AddListener(() => { LoadScene(stage.sceneData); });

                currentButtons.Add(button);
            }
        }

        inputController.SetStageButtons(currentButtons);

        if (currentButtons.Count > 0)
        {
            currentButtons[0].Select();
        }
    }

    public void LoadScene(SceneData data)
    {
        Debug.Log($"シーン遷移 {data} へ");
        
        SceneChangeManager.Instance.ChangeScene(data);
    }

    public void StagetoWorld()
    {
        SelectAnimationManager.Instance.StagetoWorld();
    }

}