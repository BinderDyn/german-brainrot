using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BinderDyn.audio;

/// <summary>
/// Plain MonoBehaviour added to enemies on demand (no network involvement).
/// Loads and plays audio from the local disk with a spatial AudioSource.
/// </summary>
public class LocalAudioPlayer : MonoBehaviour
{
    private AudioSource? _audioSource;
    private Coroutine? _playCoroutine;

    public bool IsPlaying => _playCoroutine != null;

    /// <summary>Full path of the last clip played on this enemy, for no-repeat logic.</summary>
    public string? LastPlayedClipPath { get; set; }

    private static readonly HashSet<string> _loggedOnce = new(StringComparer.Ordinal);

    private void Awake()
    {
        var audioObject = new GameObject("GermanBrainrotLocal");
        audioObject.transform.SetParent(transform, false);
        _audioSource = audioObject.AddComponent<AudioSource>();
        _audioSource.spatialBlend = 1f;
        _audioSource.minDistance = 1f;
        _audioSource.maxDistance = 25f;
        _audioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    private void LateUpdate()
    {
        if (_audioSource != null)
        {
            _audioSource.transform.position = transform.position;
        }
    }

    public void PlayClip(string fullPath)
    {
        if (_playCoroutine != null)
        {
            StopCoroutine(_playCoroutine);
        }

        _playCoroutine = StartCoroutine(LoadAndPlayRoutine(fullPath));
    }

    private IEnumerator LoadAndPlayRoutine(string fullPath)
    {
        OpusFileReader? reader = null;
        Exception? loadError = null;
        var loadComplete = false;

        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                reader = OpusFileReader.FromFile(fullPath);
            }
            catch (Exception ex)
            {
                loadError = ex;
            }
            finally
            {
                loadComplete = true;
            }
        });

        while (!loadComplete)
        {
            yield return null;
        }

        if (loadError != null)
        {
            LogOnce($"load_err_{Path.GetFileName(fullPath)}", $"LocalAudioPlayer: failed to load {Path.GetFileName(fullPath)}: {loadError.Message}");
            _playCoroutine = null;
            yield break;
        }

        if (reader == null || reader.TotalSamples == 0)
        {
            LogOnce($"empty_{Path.GetFileName(fullPath)}", $"LocalAudioPlayer: empty clip {Path.GetFileName(fullPath)}");
            reader?.Dispose();
            _playCoroutine = null;
            yield break;
        }

        var samples = reader.Samples;
        var clip = AudioClip.Create(
            Path.GetFileNameWithoutExtension(fullPath),
            samples.Count,
            1,     // Channels
            48000, // SampleRate
            false);

        var sampleArray = new float[samples.Count];
        for (var i = 0; i < samples.Count; i++)
        {
            sampleArray[i] = samples[i];
        }

        clip.SetData(sampleArray, 0);
        reader.Dispose();

        if (_audioSource == null)
        {
            _playCoroutine = null;
            yield break;
        }

        if (_audioSource.isPlaying)
        {
            _audioSource.Stop();
        }

        _audioSource.clip = clip;
        _audioSource.Play();

        yield return new WaitWhile(() => _audioSource != null && _audioSource.isPlaying);

        UnityEngine.Object.Destroy(clip);
        _playCoroutine = null;
    }

    private void OnDestroy()
    {
        if (_playCoroutine != null)
        {
            StopCoroutine(_playCoroutine);
            _playCoroutine = null;
        }

        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
    }

    /// <summary>Log a message only once per session, keyed by <paramref name="key"/>.</summary>
    public static void LogOnce(string key, string message)
    {
        if (_loggedOnce.Add(key))
        {
            Plugin.Log.LogWarning(message);
        }
    }
}
