using System.Collections;
using UnityEngine;

public class SelectAnimationManager : MonoBehaviour
{
    public static SelectAnimationManager Instance { get; private set; }

    [SerializeField] private GameObject WorldSelect;
    [SerializeField] private GameObject StageSelect;

    [SerializeField] private UIAnimationController anime;

    [Header("アニメーション用SO")]
    [SerializeField] private SelectAnimationData WorldAnime;
    [SerializeField] private SelectAnimationData SelectAnime;

    [SerializeField] private SelectAnimationData BackWorldAnime;
    [SerializeField] private SelectAnimationData BackStageAnime;
    
    private RectTransform worldSelect;
    private RectTransform stageSelect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        stageSelect = StageSelect.GetComponent<RectTransform>();
        worldSelect = WorldSelect.GetComponent<RectTransform>();
    }
    void Start()
    {
        if (GameData.CurrentStage == null)
        {
            StageSelect.transform.localScale = Vector3.zero;
            StageSelect.SetActive(false);
        }
        else
        {
            worldSelect.anchoredPosition = WorldAnime.endPosition;
            WorldSelect.SetActive(false);

        }
    }

    public void BackFromStage()
    {
        WorldSelect.SetActive(false);
        StageSelect.SetActive(true);
    }
    public void WorldtoStage()
    {
        Debug.Log("WorldtoStage開始");
        StageSelect.SetActive(true);
        StartCoroutine(WorldToStageCoroutine());
    }
    
    public void StagetoWorld()
    {
        WorldSelect.SetActive(true);

        StartCoroutine(StageToWorldCoroutine());
    }

    private IEnumerator WorldToStageCoroutine()
    {
        yield return anime.Move(worldSelect, WorldAnime.endPosition, WorldAnime.time, WorldAnime.delay, WorldAnime.easetype);
        yield return anime.Scale(stageSelect, SelectAnime.endScale, SelectAnime.time, SelectAnime.delay);

        WorldSelect.SetActive(false);
        Debug.Log("WorldtoStage終了");
    } 
    
    private IEnumerator StageToWorldCoroutine()
    {
        yield return anime.Scale(stageSelect, BackStageAnime.endScale, BackStageAnime.time, BackStageAnime.delay);
        yield return anime.Move(worldSelect, BackWorldAnime.endPosition, BackWorldAnime.time, BackWorldAnime.delay, BackWorldAnime.easetype);

        StageSelect.SetActive(false);
    }
}
