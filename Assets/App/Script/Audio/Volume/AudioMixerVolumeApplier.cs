using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// AudioVolumeDataをAudioMixerへ反映するクラス
/// </summary>
public class AudioMixerVolumeApplier : MonoBehaviour
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Config")]
    [SerializeField] private AudioVolumeConfig volumeConfig;

    /// <summary>
    /// 音量データをAudioMixerへ反映する
    /// </summary>
    public void Apply(AudioVolumeData data)
    {
        if (audioMixer == null || volumeConfig == null || data == null)
        {
            Debug.LogWarning("AudioMixerVolumeApplier reference is missing.");
            return;
        }

        SetVolume(volumeConfig.MasterVolumeParameter, data.masterVolume);
        SetVolume(volumeConfig.BgmVolumeParameter, data.bgmVolume);
        SetVolume(volumeConfig.SeVolumeParameter, data.seVolume);
        SetVolume(volumeConfig.UiVolumeParameter, data.uiVolume);
    }

    /// <summary>
    /// 0.0f - 1.0f の音量をdBへ変換してMixerへ設定する
    /// </summary>
    private void SetVolume(string parameterName, float normalizedVolume)
    {
        float clampedVolume = Mathf.Clamp01(normalizedVolume);
        float dbVolume = ConvertToDecibel(clampedVolume);

        audioMixer.SetFloat(parameterName, dbVolume);
    }

    /// <summary>
    /// 正規化音量をdBへ変換する
    /// </summary>
    private float ConvertToDecibel(float normalizedVolume)
    {
        if (normalizedVolume <= 0.0001f)
        {
            return volumeConfig.MinDb;
        }

        return Mathf.Log10(normalizedVolume) * 20.0f;
    }
}