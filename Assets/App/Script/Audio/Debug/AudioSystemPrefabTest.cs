using UnityEngine;

/// <summary>
/// AudioSystem.prefab の動作確認用スクリプト
/// 本番では使用せず、確認後は削除または無効化する
/// </summary>
public class AudioSystemPrefabTest : MonoBehaviour
{
    [Header("Test Audio Ids")]
    [SerializeField] private string bgmId = "test_bgm";
    [SerializeField] private string uiId = "test_ui";
    [SerializeField] private string seId = "test_se";

    [Header("SE Test Position")]
    [SerializeField] private Vector3 sePosition = new Vector3(0.0f, 0.0f, 5.0f);

    private void Update()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log($"BGM再生テスト: {bgmId}");
            AudioManager.Instance.PlayBgm(bgmId);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log($"UI音再生テスト: {uiId}");
            AudioManager.Instance.PlayUi(uiId);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log($"SE再生テスト: {seId}");
            AudioManager.Instance.PlaySe(seId, sePosition);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log("BGM停止テスト");
            AudioManager.Instance.StopBgm();
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Debug.Log("UI音停止テスト");
            AudioManager.Instance.StopUi();
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Debug.Log("SE全停止テスト");
            AudioManager.Instance.StopAllSe();
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            Debug.Log("SE連続再生テスト");
            PlaySeBurst();
        }
    }

    /// <summary>
    /// SEプール確認用に連続再生する
    /// </summary>
    private void PlaySeBurst()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 offsetPosition = sePosition + new Vector3(i * 1.0f, 0.0f, 0.0f);
            AudioManager.Instance.PlaySe(seId, offsetPosition);
        }
    }
}