using System.Collections.Generic;
using UnityEngine;

public class ResultUIManager : MonoBehaviour
{
    public static ResultUIManager Instance { get; private set; }

    [Header("登録するミッションUIの１行")]
   // [SerializeField] private MissionUILine MainMissionUI;
    [SerializeField] private List<ResultUILine> SubMissionUIs;

    private void Awake()
    {
       // シングルトン化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

    }

    //public void DrawResultMainMissionUI(MainMissionRunTime mission)
    //{
    //    switch(mission.presetMain.presetType)
    //    {
    //        case MainMissionPreset.StageMission.Goal:
    //            MainMissionUI.Initialize("ゴールへ目指せ!");
    //            MainMissionUI.UpdateText();
    //            break;
    //        case MainMissionPreset.StageMission.Kill:
    //            if (mission.presetMain.killType == MainMissionPreset.KillConditionType.SpecificEnemy)
    //            {
    //                if (mission.presetMain.KillObject != null)
    //                {
    //                    MainMissionUI.Initialize("{1}を{0}体倒せ!");
    //                    MainMissionUI.UpdateText(mission.presetMain.KillCount, mission.presetMain.KillObject.GroupName);
    //                }
    //                else
    //                {
    //                    MainMissionUI.Initialize("エネミーがnullです。");
    //                    MainMissionUI.UpdateText();
    //                }
    //            }
    //            else
    //            {
    //                MainMissionUI.Initialize("全て倒せ!");
    //                MainMissionUI.UpdateText(0, StageClearManager.Instance.GetTotalEnemyCount());
    //            }
    //            break;
    //        case MainMissionPreset.StageMission.Collect:
    //            if (mission.presetMain.CollectObject != null)
    //            {
    //                MainMissionUI.Initialize("{1}を{0}個集めろ!");
    //                MainMissionUI.UpdateText(mission.presetMain.CollectCount, mission.presetMain.CollectObject.name);
    //            }
    //            else
    //            {
    //                MainMissionUI.Initialize("収集物がNullです。");
    //                MainMissionUI.UpdateText();
    //            }
    //            break;
    //    }
    //}

    public void DrawSubMissionUIs(SubMissionRunTime mission,int uiIndex)
    {
        switch (mission.presetSub.presetType)
        {
            case SubMissionPreset.MissionType.ClearTime:
                SubMissionUIs[uiIndex].Initialize("残り{0}秒までにクリア!");
                SubMissionUIs[uiIndex].UpdateText(mission.presetSub.TimeCount);
                break;
            case SubMissionPreset.MissionType.KillCount:
                if (mission.presetSub.killType == SubMissionPreset.KillConditionType.SpecificEnemy)
                {
                    SubMissionUIs[uiIndex].Initialize("{1}を{0}体倒せ!");
                    SubMissionUIs[uiIndex].UpdateText(mission.presetSub.ObjectCount, mission.presetSub.EnemyObject.GroupName);
                }
                else
                {
                    SubMissionUIs[uiIndex].Initialize("全て倒せ!");
                    SubMissionUIs[uiIndex].UpdateText();
                }
                break;
            case SubMissionPreset.MissionType.TackleCountLimit:
                SubMissionUIs[uiIndex].Initialize("タックル{0}回までにクリア!");
                SubMissionUIs[uiIndex].UpdateText(mission.presetSub.PlayerActionCount);
                break;
            case SubMissionPreset.MissionType.TackleCountOverthan:
                SubMissionUIs[uiIndex].Initialize("タックル{0}回以上でクリア!");
                SubMissionUIs[uiIndex].UpdateText(mission.presetSub.PlayerActionCount);
                break;
            case SubMissionPreset.MissionType.JumpCountLimit:
                SubMissionUIs[uiIndex].Initialize("ジャンプ{0}回までにクリア!");
                SubMissionUIs[uiIndex].UpdateText(mission.presetSub.PlayerActionCount);
                break;
            case SubMissionPreset.MissionType.JumpCountOverthan:
                SubMissionUIs[uiIndex].Initialize("ジャンプ{0}回以上でクリア!");
                SubMissionUIs[uiIndex].UpdateText(mission.presetSub.PlayerActionCount);
                break;
            case SubMissionPreset.MissionType.BreakBlockCountOverthan:
                SubMissionUIs[uiIndex].Initialize("ブロックを{0}個壊せ!");
                SubMissionUIs[uiIndex].UpdateText(mission.presetSub.ObjectCount);
                break;
            case SubMissionPreset.MissionType.BreakBlockCountLimit:
                SubMissionUIs[uiIndex].Initialize("ブロックを{0}個まで壊すな!");
                SubMissionUIs[uiIndex].UpdateText(mission.presetSub.ObjectCount);
                break;
            case SubMissionPreset.MissionType.HPSaving:
                SubMissionUIs[uiIndex].Initialize("HP {0}% までダメージを食らうな!");
                SubMissionUIs[uiIndex].UpdateText(mission.presetSub.PlayerHP);
                break;
        }
        if (mission.isClear)
        {
            SubMissionUIs[uiIndex].SetClear(true);
        }
        else
        {
            SubMissionUIs[uiIndex].SetClear(false);
        }
    }

    public void CallResult()
    {
    
        // ミッション表示を消す (リザルト画面で見せるため)
        MissionUIManager.Instance.SetUIRootFlg(false);

    }
}
