using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Backend;

namespace Gameplay
{
    public class PlinkoSessionManager
    {
        public Action<float> OnTimerUpdated;    // Seconds until reset.
        public Action OnResetNeeded;
        
        private readonly IBackendService _backend;
        private readonly float _checkInterval;
        
        private Task _sessionTimerTask;
        private CancellationTokenSource _sessionCancellation;
        
        public PlinkoSessionManager(IBackendService backend, float checkInterval = 1f)
        {
            _backend = backend;
            _checkInterval = checkInterval;
        }

        public void StartMonitoring()
        {
            if (_sessionCancellation != null)
            {
                Debug.LogWarning("[SESSION] Already monitoring, stopping previous task.");
                StopMonitoring();
            }

            _sessionCancellation = new CancellationTokenSource();
            _sessionTimerTask = RunSessionTimer(_sessionCancellation.Token);

            Debug.Log("[SESSION] Timer monitoring started");
        }

        public void StopMonitoring()
        {
            _sessionCancellation?.Cancel();
            _sessionCancellation?.Dispose();
            _sessionCancellation = null;

            Debug.Log("[SESSION] Timer monitoring stopped.");
        }

        private async Task RunSessionTimer(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay((int)(_checkInterval * 1000), cancellationToken);

                    SessionCheckResponse response = await _backend.CheckSessionStatus();

                    if (response.Success)
                    {
                        OnTimerUpdated?.Invoke(response.TimeUntilReset);

                        if (response.NeedsReset)
                        {
                            Debug.Log("[SESSION] 15-minute timer expired, reset needed.");
                            OnResetNeeded?.Invoke();
                            break;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[SESSION] Timer cancelled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SESSION] Timer error: {ex.Message}");
            }
        }
    }
}