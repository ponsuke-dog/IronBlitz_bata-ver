using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SEを複数同時再生するためのプール型プレイヤー。
/// audioId単位の停止と、ハンドル単位の個別停止に対応する。
/// </summary>
public class SePoolPlayer : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private AudioDatabase audioDatabase;

    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private int maxPoolSize = 30;

    private readonly List<AudioSource> audioSources = new List<AudioSource>();

    /// <summary>
    /// AudioSourceごとの再生中audioId。
    /// </summary>
    private readonly Dictionary<AudioSource, string> playingAudioIds =
        new Dictionary<AudioSource, string>();

    /// <summary>
    /// AudioSourceごとの再生ハンドル。
    /// 同じaudioIdを複数再生した時に1つだけ止めるために使う。
    /// </summary>
    private readonly Dictionary<AudioSource, int> playingHandles =
        new Dictionary<AudioSource, int>();

    /// <summary>
    /// AudioSourceごとの自動解放Coroutine。
    /// 個別停止時にCoroutineも止めるために保持する。
    /// </summary>
    private readonly Dictionary<AudioSource, Coroutine> releaseCoroutines =
        new Dictionary<AudioSource, Coroutine>();

    private void Awake()
    {
        CreateInitialPool();
    }

    private void CreateInitialPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateAudioSource();
        }
    }

    private AudioSource CreateAudioSource()
    {
        GameObject sourceObject = new GameObject($"SeAudioSource_{audioSources.Count}");
        sourceObject.transform.SetParent(transform);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1.0f;

        audioSources.Add(source);
        return source;
    }

    /// <summary>
    /// 指定IDのSEを指定位置で再生する。
    /// 従来互換用。ハンドルは返さない。
    /// </summary>
    public void PlaySe(string audioId, Vector3 position)
    {
        PlaySeInternal(audioId, position, -1);
    }

    /// <summary>
    /// 指定IDのSEを指定位置で再生し、個別停止用ハンドルを返す。
    /// </summary>
    public int PlaySeWithHandle(string audioId, Vector3 position)
    {
        int handle = AudioPlaybackHandleGenerator.Create();

        bool played = PlaySeInternal(audioId, position, handle);

        if (!played)
        {
            return -1;
        }

        return handle;
    }

    /// <summary>
    /// SE再生の内部処理。
    /// </summary>
    private bool PlaySeInternal(string audioId, Vector3 position, int handle)
    {
        if (audioDatabase == null)
        {
            Debug.LogWarning("AudioDatabase is not assigned.");
            return false;
        }

        AudioData data = audioDatabase.GetById(audioId);

        if (data == null || data.Category != AudioCategory.SE)
        {
            Debug.LogWarning($"SE not found. audioId: {audioId}");
            return false;
        }

        if (data.IsValid() == false)
        {
            Debug.LogWarning($"SE data is invalid. audioId: {audioId}");
            return false;
        }

        AudioSource source = GetAvailableAudioSource();

        if (source == null)
        {
            Debug.LogWarning("No available SE AudioSource.");
            return false;
        }

        StopReleaseCoroutine(source);

        ApplyAudioData(source, data, position);

        playingAudioIds[source] = audioId;
        playingHandles[source] = handle;

        source.Play();

        if (data.Loop == false)
        {
            Coroutine coroutine = StartCoroutine(
                ReleaseAfterPlayback(source, data.Clip.length)
            );

            releaseCoroutines[source] = coroutine;
        }

        return true;
    }

    /// <summary>
    /// 指定IDのSEをすべて停止する。
    /// 同じaudioIdが複数同時再生されている場合は全て止める。
    /// </summary>
    public void StopSe(string audioId)
    {
        if (string.IsNullOrEmpty(audioId))
        {
            return;
        }

        List<AudioSource> stopTargets = new List<AudioSource>();

        foreach (KeyValuePair<AudioSource, string> pair in playingAudioIds)
        {
            if (pair.Value == audioId)
            {
                stopTargets.Add(pair.Key);
            }
        }

        for (int i = 0; i < stopTargets.Count; i++)
        {
            StopSource(stopTargets[i]);
        }
    }

    /// <summary>
    /// 指定ハンドルのSEを1つだけ停止する。
    /// </summary>
    public void StopSe(int handle)
    {
        if (handle < 0)
        {
            return;
        }

        AudioSource targetSource = null;

        foreach (KeyValuePair<AudioSource, int> pair in playingHandles)
        {
            if (pair.Value == handle)
            {
                targetSource = pair.Key;
                break;
            }
        }

        if (targetSource == null)
        {
            return;
        }

        StopSource(targetSource);
    }

    private AudioSource GetAvailableAudioSource()
    {
        CleanupStoppedSources();

        for (int i = 0; i < audioSources.Count; i++)
        {
            if (audioSources[i].isPlaying == false)
            {
                return audioSources[i];
            }
        }

        if (audioSources.Count < maxPoolSize)
        {
            return CreateAudioSource();
        }

        return null;
    }

    private void ApplyAudioData(AudioSource source, AudioData data, Vector3 position)
    {
        source.transform.position = position;
        source.clip = data.Clip;
        source.loop = data.Loop;
        source.outputAudioMixerGroup = data.OutputMixerGroup;

        if (data.Use3DSound)
        {
            source.spatialBlend = data.SpatialBlend;
            source.minDistance = data.MinDistance;
            source.maxDistance = data.MaxDistance;
        }
        else
        {
            source.spatialBlend = 0.0f;
        }
    }

    private IEnumerator ReleaseAfterPlayback(AudioSource source, float clipLength)
    {
        yield return new WaitForSeconds(clipLength);

        if (source == null)
        {
            yield break;
        }

        StopSource(source);
    }

    /// <summary>
    /// 指定AudioSourceを停止し、再生情報をクリアする。
    /// </summary>
    private void StopSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        StopReleaseCoroutine(source);

        source.Stop();
        source.clip = null;

        playingAudioIds.Remove(source);
        playingHandles.Remove(source);
    }

    /// <summary>
    /// 自動解放Coroutineを停止する。
    /// </summary>
    private void StopReleaseCoroutine(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        if (releaseCoroutines.TryGetValue(source, out Coroutine coroutine))
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }

            releaseCoroutines.Remove(source);
        }
    }

    /// <summary>
    /// 再生終了済みAudioSourceの管理情報を掃除する。
    /// </summary>
    private void CleanupStoppedSources()
    {
        List<AudioSource> stoppedSources = new List<AudioSource>();

        foreach (KeyValuePair<AudioSource, string> pair in playingAudioIds)
        {
            if (pair.Key == null || pair.Key.isPlaying == false)
            {
                stoppedSources.Add(pair.Key);
            }
        }

        for (int i = 0; i < stoppedSources.Count; i++)
        {
            AudioSource source = stoppedSources[i];

            if (source != null)
            {
                StopReleaseCoroutine(source);
                source.clip = null;
            }

            playingAudioIds.Remove(source);
            playingHandles.Remove(source);
        }
    }

    /// <summary>
    /// 再生中のSEをすべて停止する。
    /// </summary>
    public void StopAllSe()
    {
        for (int i = 0; i < audioSources.Count; i++)
        {
            StopSource(audioSources[i]);
        }

        playingAudioIds.Clear();
        playingHandles.Clear();
        releaseCoroutines.Clear();
    }
}