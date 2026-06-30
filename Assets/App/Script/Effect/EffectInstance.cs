using System.Collections;
using UnityEngine;

#region エフェクト実体
/// <summary>
/// 実際に再生されるエフェクト
/// ・Transform制御
/// ・追従
/// ・任意World Transform再生
/// ・終了判定
/// ・停止時の残留対策
/// </summary>
public class EffectInstance : MonoBehaviour
{
    public bool IsPooled { get; set; } = true;

    private EffectManager manager;
    public EffectData data { get; private set; }

    public GameObject SourcePrefab { get; private set; }

    private Transform target;
    private EffectPlayParam playParam;

    private ParticleSystem[] particleSystems;

    private bool canCheck;
    private bool isStopping;
    private bool isReleased;

    private float timer;

    // StopEmitting で止めたあと、Particle が生き続けた場合の保険
    private float stopTimer;
    private const float DefaultStopTimeout = 3.0f;

    private Coroutine enableCheckCoroutine;

    // 任意World Transform再生用
    private bool useWorldTransform;
    private Vector3 worldPosition;
    private Quaternion worldRotation;
    private Vector3 worldScale;

    #region 初期化

    public void Initialize(EffectManager manager, EffectData data)
    {
        this.manager = manager;
        this.data = data;
        this.SourcePrefab = data.prefab;

        CacheParticleSystems();
    }

    private void CacheParticleSystems()
    {
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    #endregion

    #region 再生

    /// <summary>
    /// Transform基準で再生。
    /// </summary>
    public void Play(Transform target, EffectPlayParam param)
    {
        ResetState();

        CacheParticleSystems();

        this.target = target;
        this.playParam = param;

        useWorldTransform = false;

        ApplyTransform();
        RestartParticles();

        if (data.forceLifeTime > 0f)
        {
            timer = data.forceLifeTime;
        }

        StartEnableCheck();
    }

    /// <summary>
    /// 任意World Transformで再生。
    /// targetに依存しないので追従しない。
    /// </summary>
    public void PlayAt(
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        EffectPlayParam param)
    {
        ResetState();

        CacheParticleSystems();

        this.target = null;
        this.playParam = param;

        useWorldTransform = true;
        worldPosition = position;
        worldRotation = rotation;
        worldScale = scale;

        ApplyTransform();
        RestartParticles();

        if (data.forceLifeTime > 0f)
        {
            timer = data.forceLifeTime;
        }

        StartEnableCheck();
    }

    /// <summary>
    /// 再生前に必ずParticleを全消ししてから再生する。
    /// プール再利用時の残留対策。
    /// </summary>
    private void RestartParticles()
    {
        foreach (var ps in particleSystems)
        {
            if (ps == null)
                continue;

            ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(false);
            ps.Play(false);
        }
    }

    #endregion

    #region 更新

    private void LateUpdate()
    {
        if (!canCheck)
            return;

        if (isReleased)
            return;

        if (ShouldFollowTarget())
        {
            ApplyTransform();
        }

        // 強制寿命
        if (data.forceLifeTime > 0f)
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                ReturnToPool(true);
                return;
            }
        }

        // Stop() 後の自然終了待ち
        if (isStopping)
        {
            stopTimer -= Time.deltaTime;

            if (IsAllStopped())
            {
                ReturnToPool(true);
                return;
            }

            // Local / Trail / SubEmitter / Loop などで残り続ける場合の保険
            if (stopTimer <= 0f)
            {
                ReturnToPool(true);
                return;
            }

            return;
        }

        // 通常の自動終了
        if (data.autoRelease && IsAllStopped())
        {
            ReturnToPool(true);
        }
    }

    #endregion

    #region Transform適用

    private bool ShouldFollowTarget()
    {
        if (useWorldTransform)
            return false;

        if (target == null)
            return false;

        bool follow =
            playParam.overrideFollowTarget
                ? playParam.followTarget
                : data.followTarget;

        return follow;
    }

    private void ApplyTransform()
    {
        if (useWorldTransform)
        {
            ApplyWorldTransform();
        }
        else
        {
            ApplyTargetTransform();
        }
    }

    private void ApplyTargetTransform()
    {
        if (target == null)
            return;

        // === 位置 ===
        if (playParam.overridePosition)
        {
            transform.position =
                target.position +
                playParam.positionOffset;
        }
        else
        {
            transform.position =
                target.position +
                data.positionOffset +
                playParam.positionOffset;
        }

        // === 回転 ===
        if (playParam.overrideRotation)
        {
            transform.rotation =
                Quaternion.Euler(playParam.rotationOffset);
        }
        else
        {
            transform.rotation =
                target.rotation *
                Quaternion.Euler(data.rotationOffset + playParam.rotationOffset);
        }

        // === スケール ===
        if (playParam.overrideScale)
        {
            transform.localScale = playParam.scale;
        }
        else
        {
            transform.localScale =
                Vector3.Scale(data.scale, playParam.scale);
        }
    }

    private void ApplyWorldTransform()
    {
        // === 位置 ===
        if (playParam.overridePosition)
        {
            transform.position =
                worldPosition +
                playParam.positionOffset;
        }
        else
        {
            transform.position =
                worldPosition +
                data.positionOffset +
                playParam.positionOffset;
        }

        // === 回転 ===
        if (playParam.overrideRotation)
        {
            transform.rotation =
                Quaternion.Euler(playParam.rotationOffset);
        }
        else
        {
            transform.rotation =
                worldRotation *
                Quaternion.Euler(data.rotationOffset + playParam.rotationOffset);
        }

        // === スケール ===
        if (playParam.overrideScale)
        {
            transform.localScale = playParam.scale;
        }
        else
        {
            transform.localScale =
                Vector3.Scale(
                    Vector3.Scale(data.scale, worldScale),
                    playParam.scale
                );
        }
    }

    #endregion

    #region Particle終了判定

    private bool IsAllStopped()
    {
        if (particleSystems == null)
            return true;

        foreach (var ps in particleSystems)
        {
            if (ps == null)
                continue;

            if (ps.IsAlive(true))
                return false;
        }

        return true;
    }

    #endregion

    #region データのリセット

    private void ResetState()
    {
        if (enableCheckCoroutine != null)
        {
            StopCoroutine(enableCheckCoroutine);
            enableCheckCoroutine = null;
        }

        canCheck = false;
        isStopping = false;
        isReleased = false;

        target = null;
        playParam = EffectPlayParam.Default;

        useWorldTransform = false;
        worldPosition = Vector3.zero;
        worldRotation = Quaternion.identity;
        worldScale = Vector3.one;

        timer = 0f;
        stopTimer = 0f;
    }

    private void StartEnableCheck()
    {
        if (enableCheckCoroutine != null)
        {
            StopCoroutine(enableCheckCoroutine);
        }

        enableCheckCoroutine = StartCoroutine(EnableCheck());
    }

    private IEnumerator EnableCheck()
    {
        yield return null;
        yield return null;

        canCheck = true;
        enableCheckCoroutine = null;
    }

    #endregion

    #region 停止

    /// <summary>
    /// 外部から停止。
    /// ループエフェクト用。
    /// Stop後は追従解除してその場に残し、自然消滅を待つ。
    /// ただし一定時間後には強制的にClearしてプールへ返す。
    /// </summary>
    public void Stop()
    {
        if (isReleased)
            return;

        if (isStopping)
            return;

        isStopping = true;
        canCheck = true;

        // その場で固定
        target = null;
        useWorldTransform = true;
        worldPosition = transform.position;
        worldRotation = transform.rotation;
        worldScale = transform.localScale;

        stopTimer = DefaultStopTimeout;

        foreach (var ps in particleSystems)
        {
            if (ps == null)
                continue;

            // 余韻を残す停止
            // 既存ParticleはLifetimeが尽きるまで残る
            ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    /// <summary>
    /// 即停止。
    /// 画面上からすぐ消したい場合はこちら。
    /// </summary>
    public void StopImmediate()
    {
        if (isReleased)
            return;

        ReturnToPool(true);
    }

    #endregion

    #region 返却

    private void ReturnToPool(bool clearParticles)
    {
        if (isReleased)
            return;

        isReleased = true;
        canCheck = false;
        isStopping = false;

        if (enableCheckCoroutine != null)
        {
            StopCoroutine(enableCheckCoroutine);
            enableCheckCoroutine = null;
        }

        target = null;

        if (clearParticles)
        {
            ClearParticles();
        }

        if (manager == null)
        {
            gameObject.SetActive(false);
            return;
        }

        manager.Release(this);
    }

    /// <summary>
    /// Particleの残留を完全に消す。
    /// プール返却前・再生前に使う。
    /// </summary>
    private void ClearParticles()
    {
        if (particleSystems == null)
            return;

        foreach (var ps in particleSystems)
        {
            if (ps == null)
                continue;

            ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(false);
        }
    }

    #endregion
}
#endregion