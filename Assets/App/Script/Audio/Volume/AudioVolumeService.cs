using System;
using UnityEngine;

/// <summary>
/// Audio‰¹—Ê‚Ìó‘ÔŠÇ—A•Û‘¶AMixer”½‰f‚ğ’S“–‚·‚éService
/// UI‚ª–³‚¢ó‘Ô‚Å‚àPrefab“à‚É•Û‚µ‚Ä‚¨‚­
/// </summary>
public class AudioVolumeService : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioVolumeConfig volumeConfig;
    [SerializeField] private AudioMixerVolumeApplier volumeApplier;
    [SerializeField] private PlayerPrefsVolumeSaveRepository saveRepository;

    public event Action<AudioVolumeData> OnVolumeChanged;

    public AudioVolumeData CurrentVolumeData { get; private set; }

    private void Awake()
    {
        LoadVolumes();
    }

    /// <summary>
    /// •Û‘¶Ï‚İ‰¹—Ê‚ğ“Ç‚İ‚İAMixer‚Ö”½‰f‚·‚é
    /// </summary>
    public void LoadVolumes()
    {
        if (saveRepository != null)
        {
            CurrentVolumeData = saveRepository.Load();
        }
        else if (volumeConfig != null)
        {
            CurrentVolumeData = volumeConfig.CreateDefaultVolumeData();
        }
        else
        {
            CurrentVolumeData = new AudioVolumeData(1.0f, 1.0f, 1.0f, 1.0f);
        }

        ApplyAndNotify();
    }

    public void SetMasterVolume(float value)
    {
        CurrentVolumeData.masterVolume = Mathf.Clamp01(value);
        SaveAndApply();
    }

    public void SetBgmVolume(float value)
    {
        CurrentVolumeData.bgmVolume = Mathf.Clamp01(value);
        SaveAndApply();
    }

    public void SetSeVolume(float value)
    {
        CurrentVolumeData.seVolume = Mathf.Clamp01(value);
        SaveAndApply();
    }

    public void SetUiVolume(float value)
    {
        CurrentVolumeData.uiVolume = Mathf.Clamp01(value);
        SaveAndApply();
    }

    /// <summary>
    /// ‰¹—Ê‚ğ‰Šú’l‚Ö–ß‚·
    /// </summary>
    public void ResetVolumes()
    {
        if (volumeConfig != null)
        {
            CurrentVolumeData = volumeConfig.CreateDefaultVolumeData();
        }
        else
        {
            CurrentVolumeData = new AudioVolumeData(1.0f, 1.0f, 1.0f, 1.0f);
        }

        SaveAndApply();
    }

    /// <summary>
    /// Œ»İ‚Ì‰¹—Ê‚ğ•Û‘¶‚µ‚Ä”½‰f‚·‚é
    /// </summary>
    private void SaveAndApply()
    {
        if (saveRepository != null)
        {
            saveRepository.Save(CurrentVolumeData);
        }

        ApplyAndNotify();
    }

    /// <summary>
    /// Mixer”½‰f‚Æ•ÏX’Ê’m‚ğs‚¤
    /// </summary>
    private void ApplyAndNotify()
    {
        if (volumeApplier != null)
        {
            volumeApplier.Apply(CurrentVolumeData);
        }

        OnVolumeChanged?.Invoke(CurrentVolumeData);
    }
}