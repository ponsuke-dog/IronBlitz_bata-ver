using UnityEngine;

/// <summary>
/// BGM再生専用プレイヤー
/// </summary>
public class BgmPlayer : AudioSourcePlayer
{
    [Header("Database")]
    [SerializeField] private AudioDatabase audioDatabase;

    /// <summary>
    /// 指定IDのBGMを再生する
    /// </summary>
    public void PlayBgm(string audioId)
    {
        if (audioDatabase == null)
        {
            Debug.LogWarning("AudioDatabase is not assigned.");
            return;
        }

        AudioData data = audioDatabase.GetById(audioId);

        if (data == null || data.Category != AudioCategory.BGM)
        {
            Debug.LogWarning($"BGM not found. audioId: {audioId}");
            return;
        }

        if (data.IsValid() == false)
        {
            Debug.LogWarning($"BGM data is invalid. audioId: {audioId}");
            return;
        }

        ApplyAudioData(data);
        audioSource.Play();
    }
}