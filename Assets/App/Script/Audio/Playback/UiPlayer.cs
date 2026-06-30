using UnityEngine;

/// <summary>
/// UI音再生専用プレイヤー。
/// 現在は単一AudioSourceで再生する。
/// ハンドル指定による個別停止にも対応する。
/// </summary>
public class UiPlayer : AudioSourcePlayer
{
    [Header("Database")]
    [SerializeField] private AudioDatabase audioDatabase;

    private string currentAudioId;
    private int currentHandle = -1;

    /// <summary>
    /// 指定IDのUI音を再生する。
    /// 従来互換用。ハンドルは返さない。
    /// </summary>
    public void PlayUi(string audioId)
    {
        PlayUiInternal(audioId, -1);
    }

    /// <summary>
    /// 指定IDのUI音を再生し、個別停止用ハンドルを返す。
    /// </summary>
    public int PlayUiWithHandle(string audioId)
    {
        int handle = AudioPlaybackHandleGenerator.Create();

        bool played = PlayUiInternal(audioId, handle);

        if (!played)
        {
            return -1;
        }

        return handle;
    }

    /// <summary>
    /// UI音を再生する内部処理。
    /// </summary>
    private bool PlayUiInternal(string audioId, int handle)
    {
        if (audioDatabase == null)
        {
            Debug.LogWarning("AudioDatabase is not assigned.");
            return false;
        }

        AudioData data = audioDatabase.GetById(audioId);

        if (data == null || data.Category != AudioCategory.UI)
        {
            Debug.LogWarning($"UI sound not found. audioId: {audioId}");
            return false;
        }

        if (data.IsValid() == false)
        {
            Debug.LogWarning($"UI sound data is invalid. audioId: {audioId}");
            return false;
        }

        currentAudioId = audioId;
        currentHandle = handle;

        ApplyAudioData(data);
        audioSource.Play();

        return true;
    }

    /// <summary>
    /// 現在のUI音を停止する。
    /// </summary>
    public new void StopAudio()
    {
        currentAudioId = null;
        currentHandle = -1;

        base.StopAudio();
    }

    /// <summary>
    /// 指定IDのUI音が再生中なら停止する。
    /// </summary>
    public void StopUi(string audioId)
    {
        if (string.IsNullOrEmpty(audioId))
        {
            return;
        }

        if (currentAudioId != audioId)
        {
            return;
        }

        StopAudio();
    }

    /// <summary>
    /// 指定ハンドルのUI音が再生中なら停止する。
    /// </summary>
    public void StopUi(int handle)
    {
        if (handle < 0)
        {
            return;
        }

        if (currentHandle != handle)
        {
            return;
        }

        StopAudio();
    }
}