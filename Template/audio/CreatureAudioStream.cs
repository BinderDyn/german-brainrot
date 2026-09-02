using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BinderDyn;
using Unity.Netcode;
using UnityEngine;

namespace BinderDyn.audio;

public class CreatureAudioStream : NetworkBehaviour
{
    private const float StreamCooldownSeconds = 5f;

    private AudioSender? _audioSender;
    private AudioReceiver? _audioReceiver;
    private CancellationTokenSource? _streamCancellation;
    private Coroutine? _streamCoroutine;
    private Transform? _followTarget;
    private ulong? _allowedSenderId;
    private float _lastStreamStartTime = float.NegativeInfinity;

    public AudioSource StreamAudioSource { get; private set; } = null!;

    public bool IsStreaming => _streamCoroutine != null;

    public string? LastPlayedClipPath { get; private set; }

    private void Awake()
    {
        var playbackObject = new GameObject("GermanBrainrotAudio");
        playbackObject.transform.SetParent(transform, false);
        StreamAudioSource = playbackObject.AddComponent<AudioSource>();
        StreamAudioSource.spatialBlend = 1f;
        StreamAudioSource.minDistance = 1f;
        StreamAudioSource.maxDistance = 25f;
        StreamAudioSource.rolloffMode = AudioRolloffMode.Linear;
        _followTarget = transform;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        NetcodeRpcInitializer.InitializeInstance(this);
        if (IsHost)
        {
            _allowedSenderId = NetworkManager.LocalClientId;
        }
    }

    private bool HasRemoteClients =>
        NetworkManager != null && NetworkManager.ConnectedClientsIds.Count > 1;

    private void LateUpdate()
    {
        if (_followTarget == null || StreamAudioSource == null)
        {
            return;
        }

        StreamAudioSource.transform.position = _followTarget.position;
    }

    public override void OnDestroy()
    {
        StopStreaming();
        base.OnDestroy();
    }

    public void StreamOpusFromFile(string filePath)
    {
        if (!IsSpawned)
        {
            Plugin.Log.LogWarning($"CreatureAudioStream not spawned; cannot stream {filePath}");
            return;
        }

        if (IsStreaming)
        {
            return;
        }

        if (Time.time - _lastStreamStartTime < StreamCooldownSeconds)
        {
            return;
        }

        var localId = NetworkManager.LocalClientId;
        if (_allowedSenderId.HasValue && localId != _allowedSenderId.Value)
        {
            Plugin.Log.LogWarning($"Client {localId} is not allowed to stream audio on {gameObject.name}");
            return;
        }

        _lastStreamStartTime = Time.time;
        LastPlayedClipPath = filePath;
        _streamCoroutine = StartCoroutine(StreamOpusFromFileRoutine(filePath));
    }

    private IEnumerator StreamOpusFromFileRoutine(string filePath)
    {
        CleanupStreamResources();
        _streamCancellation = new CancellationTokenSource();

        OpusFileReader? reader = null;
        Exception? loadError = null;
        var loadComplete = false;

        Task.Run(() =>
        {
            try
            {
                reader = OpusFileReader.FromFile(filePath);
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
            Plugin.Log.LogError($"Failed to read audio file {filePath}: {loadError.Message}");
            CleanupStreamResources();
            _streamCoroutine = null;
            yield break;
        }

        if (reader == null || reader.TotalSamples == 0)
        {
            Plugin.Log.LogWarning($"Audio file has no samples: {filePath}");
            reader?.Dispose();
            CleanupStreamResources();
            _streamCoroutine = null;
            yield break;
        }

        Exception? streamError = null;
        if (IsHost)
        {
            InitializeAudioReceiver(reader.TotalSamples);
            if (HasRemoteClients)
            {
                InitializeAudioReceiverClientRpc(reader.TotalSamples);
            }

            _audioSender = new AudioSender(packet =>
            {
                _audioReceiver?.ReceivePacket(packet);
                if (HasRemoteClients)
                {
                    SendPacketClientRpc(packet);
                }
            }, reader);

            yield return RunSendRoutine(error => streamError = error);
        }
        else
        {
            var serverRpcParams = new ServerRpcParams();
            InitializeAudioReceiverServerRpc(reader.TotalSamples, serverRpcParams);
            _audioSender = new AudioSender(packet =>
            {
                SendPacketServerRpc(packet, serverRpcParams);
                Array.Clear(packet.Samples, 0, packet.SampleCount);
            }, reader);

            yield return RunSendRoutine(error => streamError = error);
        }

        if (streamError != null)
        {
            Plugin.Log.LogError($"Error streaming audio: {streamError}");
        }

        if (_audioReceiver != null)
        {
            yield return _audioReceiver.WaitForPlaybackComplete();
        }

        CleanupStreamResources();
        _streamCoroutine = null;
    }

    private IEnumerator RunSendRoutine(Action<Exception> onError)
    {
        if (_audioSender == null)
        {
            yield break;
        }

        var sendRoutine = _audioSender.SendRoutine();
        while (true)
        {
            object current;
            try
            {
                if (!sendRoutine.MoveNext())
                {
                    yield break;
                }

                current = sendRoutine.Current;
            }
            catch (Exception ex)
            {
                onError(ex);
                yield break;
            }

            yield return current;
        }
    }

    private void InitializeAudioReceiver(int totalSamples)
    {
        _audioReceiver?.Dispose();
        _audioReceiver = new AudioReceiver(StreamAudioSource, totalSamples, _streamCancellation!.Token);
    }

    [ClientRpc]
    private void InitializeAudioReceiverClientRpc(int totalSamples)
    {
        if (IsHost)
        {
            return;
        }

        _streamCancellation ??= new CancellationTokenSource();
        InitializeAudioReceiver(totalSamples);
    }

    [ServerRpc(RequireOwnership = false)]
    private void InitializeAudioReceiverServerRpc(int totalSamples, ServerRpcParams serverRpcParams)
    {
        if (!IsValidSender(serverRpcParams.Receive.SenderClientId))
        {
            return;
        }

        InitializeAudioReceiver(totalSamples);
        InitializeAudioReceiverClientRpc(totalSamples);
    }

    [ClientRpc]
    private void SendPacketClientRpc(OpusPacket packet)
    {
        if (IsHost)
        {
            return;
        }

        _audioReceiver?.ReceivePacket(packet);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendPacketServerRpc(OpusPacket packet, ServerRpcParams serverRpcParams)
    {
        if (!IsValidSender(serverRpcParams.Receive.SenderClientId))
        {
            return;
        }

        _audioReceiver?.ReceivePacket(packet);
        SendPacketClientRpc(packet);
    }

    private bool IsValidSender(ulong senderClientId) =>
        IsHost &&
        NetworkManager.ConnectedClients.ContainsKey(senderClientId) &&
        _allowedSenderId.HasValue &&
        senderClientId == _allowedSenderId.Value;

    private void StopStreaming()
    {
        if (_streamCoroutine != null)
        {
            StopCoroutine(_streamCoroutine);
            _streamCoroutine = null;
        }

        CleanupStreamResources();
    }

    private void CleanupStreamResources()
    {
        _streamCancellation?.Cancel();
        _streamCancellation?.Dispose();
        _streamCancellation = null;
        _audioSender?.Dispose();
        _audioSender = null;
        _audioReceiver?.Dispose();
        _audioReceiver = null;
    }
}
