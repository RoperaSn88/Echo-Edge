using System.Collections.Generic;
using System.Threading;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    private const string AudioBasePath = "Assets/Addressables/Audio/";
    private const string BgmRelativePath = "BGM/";
    private const string SeRelativePath = "SE/";
    private const string AudioExtension = ".wav";

    private static readonly Dictionary<BgmAudioType, string> BgmPathCache = BuildBgmPathCache();
    private static readonly Dictionary<SeAudioType, string> SePathCache = BuildSePathCache();

    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _additionalBgmSource;
    [SerializeField, Min(1)] private int _initialSeSourceCount = 2;
    [SerializeField] private float _fadeDurationSeconds = 1.0f;

    private readonly List<AudioSource> _seSources = new();
    private readonly Dictionary<string, AsyncOperationHandle<AudioClip>> _audioClipHandleCache = new();

    private CancellationTokenSource _addBgmFadeCancellation;
    private int _nextSeSourceIndex;

    private float _masterVolume = 1.0f;
    private float _bgmVolume = 1.0f;
    private float _seVolume = 1.0f;

    /// <summary>
    /// マスター音量 (0〜1)
    /// </summary>
    public float MasterVolume => _masterVolume;

    /// <summary>
    /// BGM音量 (0〜1)
    /// </summary>
    public float BgmVolume => _bgmVolume;

    /// <summary>
    /// SE音量 (0〜1)
    /// </summary>
    public float SeVolume => _seVolume;

    private void Awake()
    {
        InitializeSeAudioSources();
        Instance = this;

        // PlayerPrefsから音量を読み込み、各AudioSourceへ反映する
        _masterVolume = AudioVolumeSaveManager.LoadMasterVolume();
        _bgmVolume = AudioVolumeSaveManager.LoadBgmVolume();
        _seVolume = AudioVolumeSaveManager.LoadSeVolume();

        AudioListener.volume = _masterVolume;
        if (_bgmSource != null) _bgmSource.volume = _bgmVolume;
        if (_additionalBgmSource != null) _additionalBgmSource.volume = _bgmVolume;
        foreach (var source in _seSources)
        {
            if (source != null) source.volume = _seVolume;
        }
    }

    /// <summary>
    /// マスター音量を設定する
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);
        AudioListener.volume = _masterVolume;
        AudioVolumeSaveManager.SaveVolumes(_masterVolume, _bgmVolume, _seVolume);
    }

    /// <summary>
    /// BGM音量を設定する
    /// </summary>
    public void SetBgmVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        if (_bgmSource != null) _bgmSource.volume = _bgmVolume;
        if (_additionalBgmSource != null) _additionalBgmSource.volume = _bgmVolume;
        AudioVolumeSaveManager.SaveVolumes(_masterVolume, _bgmVolume, _seVolume);
    }

    /// <summary>
    /// SE音量を設定する
    /// </summary>
    public void SetSeVolume(float volume)
    {
        _seVolume = Mathf.Clamp01(volume);
        foreach (var source in _seSources)
        {
            if (source != null) source.volume = _seVolume;
        }
        AudioVolumeSaveManager.SaveVolumes(_masterVolume, _bgmVolume, _seVolume);
    }

    public async UniTask PlayBgm(BgmAudioType bgmType, bool isLoop)
    {
        await PlayBgmAsync(bgmType, isLoop);
    }

    public void AddPlayBgm(BgmAudioType bgmType, bool isLoop)
    {
        _addBgmFadeCancellation?.Cancel();
        _addBgmFadeCancellation?.Dispose();
        _addBgmFadeCancellation = new CancellationTokenSource();

        AddPlayBgmAsync(bgmType, isLoop, _addBgmFadeCancellation.Token).Forget();
    }

    public void StopAddedBgm()
    {
        _addBgmFadeCancellation?.Cancel();
        _addBgmFadeCancellation?.Dispose();
        _addBgmFadeCancellation = new CancellationTokenSource();

        StopAddedBgmAsync(_addBgmFadeCancellation.Token).Forget();
    }

    public void PlaySe(SeAudioType seType)
    {
        PlaySeAsync(seType).Forget();
    }

    private async UniTask PlayBgmAsync(BgmAudioType bgmType, bool isLoop)
    {
        if (_bgmSource == null)
        {
            Debug.LogError("BGM 用 AudioSource が未設定です");
            return;
        }

        var clip = await LoadAudioClipAsync(GetBgmAddressablesPath(bgmType));
        if (clip == null)
        {
            return;
        }

        _bgmSource.loop = isLoop;
        _bgmSource.volume = 1.0f;
        _bgmSource.clip = clip;
        _bgmSource.Play();
    }

    private async UniTaskVoid AddPlayBgmAsync(BgmAudioType bgmType, bool isLoop, CancellationToken cancellationToken)
    {
        if (_additionalBgmSource == null)
        {
            Debug.LogError("追加 BGM 用 AudioSource が未設定です");
            return;
        }

        var clip = await LoadAudioClipAsync(GetBgmAddressablesPath(bgmType));
        if (clip == null)
        {
            return;
        }

        _additionalBgmSource.loop = isLoop;
        _additionalBgmSource.clip = clip;
        _additionalBgmSource.volume = 0.0f;
        _additionalBgmSource.Play();

        await FadeVolumeAsync(_additionalBgmSource, 0.0f, 1.0f, _fadeDurationSeconds, cancellationToken);
    }

    private async UniTaskVoid StopAddedBgmAsync(CancellationToken cancellationToken)
    {
        if (_additionalBgmSource == null || !_additionalBgmSource.isPlaying)
        {
            return;
        }

        await FadeVolumeAsync(_additionalBgmSource, _additionalBgmSource.volume, 0.0f, _fadeDurationSeconds, cancellationToken);

        if (!cancellationToken.IsCancellationRequested)
        {
            _additionalBgmSource.Stop();
        }
    }
    
    public async UniTask FadeBGMAsync(float durationSeconds, CancellationToken cancellationToken)
    {
        if (_bgmSource == null || !_bgmSource.isPlaying)
        {
            return;
        }

        await FadeVolumeAsync(_bgmSource, _bgmSource.volume, 0.0f, durationSeconds, cancellationToken);

        if (!cancellationToken.IsCancellationRequested)
        {
            _bgmSource.Stop();
        }
    }

    private async UniTaskVoid PlaySeAsync(SeAudioType seType)
    {
        if (!InitializeSeAudioSources())
        {
            Debug.LogError("SE 用 AudioSource の初期化に失敗しました");
            return;
        }

        var clip = await LoadAudioClipAsync(GetSeAddressablesPath(seType));
        if (clip == null)
        {
            return;
        }

        var source = GetNextSeAudioSource();
        source.PlayOneShot(clip);
    }

    private async UniTask<AudioClip> LoadAudioClipAsync(string addressablesPath)
    {
        if (_audioClipHandleCache.TryGetValue(addressablesPath, out var cachedHandle))
        {
            if (cachedHandle.IsValid() &&
                cachedHandle.Status == AsyncOperationStatus.Succeeded &&
                cachedHandle.Result != null)
            {
                return cachedHandle.Result;
            }

            if (cachedHandle.IsValid())
            {
                Addressables.Release(cachedHandle);
            }

            _audioClipHandleCache.Remove(addressablesPath);
        }

        var loadHandle = Addressables.LoadAssetAsync<AudioClip>(addressablesPath);
        var clip = await loadHandle;
        if (loadHandle.Status != AsyncOperationStatus.Succeeded || clip == null)
        {
            Debug.LogError(ZString.Concat("AudioClip の読み込みに失敗しました: ", addressablesPath));
            if (loadHandle.IsValid())
            {
                Addressables.Release(loadHandle);
            }
            return null;
        }

        _audioClipHandleCache[addressablesPath] = loadHandle;
        return clip;
    }

    private static string GetBgmAddressablesPath(BgmAudioType bgmType)
    {
        return BgmPathCache[bgmType];
    }

    private static string GetSeAddressablesPath(SeAudioType seType)
    {
        return SePathCache[seType];
    }

    private AudioSource GetNextSeAudioSource()
    {
        for (int i = 0; i < _seSources.Count; i++)
        {
            int index = (_nextSeSourceIndex + i) % _seSources.Count;
            var source = _seSources[index];
            if (!source.isPlaying)
            {
                _nextSeSourceIndex = (index + 1) % _seSources.Count;
                return source;
            }
        }

        var newSource = CreateSeAudioSource();
        _seSources.Add(newSource);
        _nextSeSourceIndex = 0;
        return newSource;
    }

    private bool InitializeSeAudioSources()
    {
        if (_seSources.Count > 0)
        {
            return true;
        }

        int sourceCount = Mathf.Max(1, _initialSeSourceCount);
        while (_seSources.Count < sourceCount)
        {
            _seSources.Add(CreateSeAudioSource());
        }

        return _seSources.Count > 0;
    }

    private AudioSource CreateSeAudioSource()
    {
        return gameObject.AddComponent<AudioSource>();
    }

    private static Dictionary<BgmAudioType, string> BuildBgmPathCache()
    {
        var cache = new Dictionary<BgmAudioType, string>();
        var bgmTypes = (BgmAudioType[])System.Enum.GetValues(typeof(BgmAudioType));
        foreach (var bgmType in bgmTypes)
        {
            cache[bgmType] = ZString.Concat(AudioBasePath, BgmRelativePath, bgmType.ToString(), AudioExtension);
        }

        return cache;
    }

    private static Dictionary<SeAudioType, string> BuildSePathCache()
    {
        var cache = new Dictionary<SeAudioType, string>();
        var seTypes = (SeAudioType[])System.Enum.GetValues(typeof(SeAudioType));
        foreach (var seType in seTypes)
        {
            cache[seType] = ZString.Concat(AudioBasePath, SeRelativePath, seType.ToString(), AudioExtension);
        }

        return cache;
    }

    private static async UniTask FadeVolumeAsync(AudioSource source, float from, float to, float durationSeconds, CancellationToken cancellationToken)
    {
        if (source == null)
        {
            return;
        }

        if (durationSeconds <= 0.0f)
        {
            source.volume = to;
            return;
        }

        float elapsed = 0.0f;
        source.volume = from;

        while (elapsed < durationSeconds && !cancellationToken.IsCancellationRequested)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / durationSeconds);
            source.volume = Mathf.Lerp(from, to, progress);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        if (!cancellationToken.IsCancellationRequested)
        {
            source.volume = to;
        }
    }

    private void OnDestroy()
    {
        _addBgmFadeCancellation?.Cancel();
        _addBgmFadeCancellation?.Dispose();
        _addBgmFadeCancellation = null;

        foreach (var handle in _audioClipHandleCache.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        _audioClipHandleCache.Clear();
    }
}
