using System.Collections.Generic;
using UnityEngine;

public class WorldSelectManager : MonoBehaviour
{
    public static WorldSelectManager Instance { get; private set; }

    [Header("WorldSet")]
    [SerializeField] private List<WorldData> worlds;

    [Header("TitleScene")]
    [SerializeField] private SceneData TitleScene;

    private void Awake()
    {
        // シングルトン
        if (Instance!=null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

    }

    private void Start()
    {
        if (GameData.CurrentStage == null)
        {
            return;
        }

        Debug.Log( "今のステージ情報" + GameData.CurrentStage);
        foreach (var world in worlds)
        {
            foreach (var stage in world.stages)
            {
                if (stage == GameData.CurrentStage)
                {
                    SelectAnimationManager.Instance.BackFromStage();
                    StageSelectManager.Instance.SetUP(world);
                }
            }
        }
    }

    public void GoToWorld(int id)
    {
        if (id >= worlds.Count || id < 0)
        {
            Debug.LogError("IDが現存するWorldに合いません");
            return;
        }

        StageSelectManager.Instance.SetUP(worlds[id]);
        SelectAnimationManager.Instance.WorldtoStage();
    }

    public void OnBack()
    {
        SceneChangeManager.Instance.ChangeScene(TitleScene);
    }
}