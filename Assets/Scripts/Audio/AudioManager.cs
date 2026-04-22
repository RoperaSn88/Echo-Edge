using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public readonly struct BgmAudioType
{
    public string Name { get; }
    public string AddressablesPath { get; }

    private BgmAudioType(string name, string addressablesPath)
    {
        Name = name;
        AddressablesPath = addressablesPath;
    }

    public static readonly BgmAudioType Title = new("Title", "Assets/Addressables/Audio/BGM/Title.wav");
    public static readonly BgmAudioType Battle = new("Battle", "Assets/Addressables/Audio/BGM/Battle.wav");

    public override string ToString() => Name;
}

public readonly struct SeAudioType
{
    public string Name { get; }
    public string AddressablesPath { get; }

    private SeAudioType(string name, string addressablesPath)
    {
        Name = name;
        AddressablesPath = addressablesPath;
    }

    public static readonly SeAudioType Click = new("Click", "Assets/Addressables/Audio/SE/Click.wav");
    public static readonly SeAudioType Attack = new("Attack", "Assets/Addressables/Audio/SE/Attack.wav");

    public override string ToString() => Name;
}

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _additionalBgmSource;
    [SerializeField] private AudioSource[] _seSources = Array.Empty<AudioSource>();
    [SerializeField] private float _fadeDurationSeconds = 1.0f;

    private readonly Dictionary<string, AsyncOperationHandle<AudioClip>> _audioClipHandleCache = new();

    private CancellationTokenSource _addBgmFadeCancellation;
    private int _nextSeSourceIndex;

    public void PlayBgm(BgmAudioType bgmType, bool isLoop)
    {
        PlayBgmAsync(bgmType, isLoop).Forget();
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

    private async UniTaskVoid PlayBgmAsync(BgmAudioType bgmType, bool isLoop)
    {
        if (_bgmSource == null)
        {
            Debug.LogError("BGM 用 AudioSource が未設定です");
            return;
        }

        var clip = await LoadAudioClipAsync(bgmType.AddressablesPath);
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

        var clip = await LoadAudioClipAsync(bgmType.AddressablesPath);
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

    private async UniTaskVoid PlaySeAsync(SeAudioType seType)
    {
        if (_seSources.Length == 0)
        {
            Debug.LogError("SE 用 AudioSource が未設定です");
            return;
        }

        var clip = await LoadAudioClipAsync(seType.AddressablesPath);
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
            Debug.LogError($"AudioClip の読み込みに失敗しました: {addressablesPath}");
            if (loadHandle.IsValid())
            {
                Addressables.Release(loadHandle);
            }
            return null;
        }

        _audioClipHandleCache[addressablesPath] = loadHandle;
        return clip;
    }

    private AudioSource GetNextSeAudioSource()
    {
        for (int i = 0; i < _seSources.Length; i++)
        {
            int index = (_nextSeSourceIndex + i) % _seSources.Length;
            var source = _seSources[index];
            if (!source.isPlaying)
            {
                _nextSeSourceIndex = (index + 1) % _seSources.Length;
                return source;
            }
        }

        var fallback = _seSources[_nextSeSourceIndex];
        _nextSeSourceIndex = (_nextSeSourceIndex + 1) % _seSources.Length;
        return fallback;
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
