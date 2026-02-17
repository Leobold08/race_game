using System.Collections;
using PurrNet;
using PurrNet.Logging;
using PurrNet.Transports;
using UnityEngine;
using PurrNet.Steam;

#if UTP_LOBBYRELAY
using PurrNet.UTP;
using Unity.Services.Relay.Models;
#endif


namespace PurrLobby
{
    public class ConnectionStarter : MonoBehaviour
    {
        private NetworkManager _networkManager;
        private LobbyDataHolder _lobbyDataHolder;
        
        private void Awake()
        {
            if(!TryGetComponent(out _networkManager)) {
                PurrLogger.LogError($"Failed to get {nameof(NetworkManager)} component.", this);
            }
            
            _lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
            if(!_lobbyDataHolder)
                PurrLogger.LogError($"Failed to get {nameof(LobbyDataHolder)} component.", this);
        }

        private void Start()
        {
            Debug.Log("[ConnectionStarter] Start() called");
            
            if (!_networkManager)
            {
                PurrLogger.LogError($"Failed to start connection. {nameof(NetworkManager)} is null!", this);
                return;
            }
            
            if (!_lobbyDataHolder)
            {
                PurrLogger.LogError($"Failed to start connection. {nameof(LobbyDataHolder)} is null!", this);
                return;
            }
            
            if (!_lobbyDataHolder.CurrentLobby.IsValid)
            {
                PurrLogger.LogError($"Failed to start connection. Lobby is invalid!", this);
                return;
            }

            Debug.Log($"[ConnectionStarter] Transport type: {_networkManager.transport?.GetType().Name ?? "NULL"}");
            Debug.Log($"[ConnectionStarter] Is Owner: {_lobbyDataHolder.CurrentLobby.IsOwner}");

            if(_networkManager.transport is PurrTransport) {
                Debug.Log("[ConnectionStarter] Using PurrTransport");
                (_networkManager.transport as PurrTransport).roomName = _lobbyDataHolder.CurrentLobby.LobbyId;
                Debug.Log($"[ConnectionStarter] Set roomName to {_lobbyDataHolder.CurrentLobby.LobbyId}");
            } 
            
#if UTP_LOBBYRELAY
            else if(_networkManager.transport is UTPTransport) {
                Debug.Log("[ConnectionStarter] Using UTPTransport");
                if(_lobbyDataHolder.CurrentLobby.IsOwner) {
                    Debug.Log("[ConnectionStarter] Initializing Relay Server...");
                    (_networkManager.transport as UTPTransport).InitializeRelayServer((Allocation)_lobbyDataHolder.CurrentLobby.ServerObject);
                }
                Debug.Log("[ConnectionStarter] Initializing Relay Client...");
                if (_lobbyDataHolder.CurrentLobby.Properties.TryGetValue("JoinCode", out var joinCode))
                {
                    (_networkManager.transport as UTPTransport).InitializeRelayClient(joinCode);
                    Debug.Log($"[ConnectionStarter] Set JoinCode to {joinCode}");
                }
                else
                {
                    Debug.LogError("[ConnectionStarter] JoinCode not found in lobby properties");
                }
            }
#else
                //Steam P2P Connection - address will be set in StartClient()
                Debug.Log("[ConnectionStarter] Using Steam P2P transport (UTP_LOBBYRELAY not defined)");
#endif

            if(_lobbyDataHolder.CurrentLobby.IsOwner)
            {
                Debug.Log("[ConnectionStarter] Starting server...");
                _networkManager.StartServer();
            }
            
            StartCoroutine(StartClient());
        }

        private IEnumerator StartClient()
        {
            Debug.Log("[ConnectionStarter] StartClient() coroutine started");
            yield return new WaitForSeconds(1f);
            Debug.Log("[ConnectionStarter] StartClient() wait complete, proceeding...");

            var transport = _networkManager.transport;
            if (transport != null && !_lobbyDataHolder.CurrentLobby.IsOwner)
            {
                var transportType = transport.GetType();
                Debug.Log($"[ConnectionStarter] Transport type: {transportType.FullName}");
                
                if (transportType.FullName == "PurrNet.Steam.SteamTransport")
                {
                    Debug.Log("[ConnectionStarter] Detected SteamTransport, attempting to set host address...");
                    
                    // Log all available properties
                    Debug.Log("[ConnectionStarter] Available lobby properties:");
                    if (_lobbyDataHolder.CurrentLobby.Properties != null)
                    {
                        foreach (var prop in _lobbyDataHolder.CurrentLobby.Properties)
                        {
                            Debug.Log($"  - {prop.Key}: {prop.Value}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[ConnectionStarter] Lobby properties are NULL!");
                    }

                    string hostSteamId = null;
                    float timeoutSeconds = 10f;
                    float elapsed = 0f;

                    while (elapsed < timeoutSeconds)
                    {
                        if (_lobbyDataHolder.CurrentLobby.Properties != null)
                        {
                            // Try both possible property keys for compatibility
                            bool foundHostId = _lobbyDataHolder.CurrentLobby.Properties.TryGetValue("HostSteamId", out hostSteamId);
                            if (!foundHostId)
                            {
                                foundHostId = _lobbyDataHolder.CurrentLobby.Properties.TryGetValue("__gameserverSteamID", out hostSteamId);
                            }
                            
                            if (foundHostId && !string.IsNullOrWhiteSpace(hostSteamId))
                            {
                                Debug.Log($"[ConnectionStarter] Found HostSteamId: {hostSteamId}");
                                var addressProp = transportType.GetProperty("address");
                                if (addressProp != null)
                                {
                                    addressProp.SetValue(transport, hostSteamId);
                                    Debug.Log($"[ConnectionStarter] Successfully set transport address to {hostSteamId}");
                                }
                                else
                                {
                                    Debug.LogError("[ConnectionStarter] Could not find 'address' property on SteamTransport");
                                }
                                break;
                            }
                        }

                        elapsed += Time.deltaTime;
                        Debug.Log($"[ConnectionStarter] Waiting for HostSteamId... ({elapsed:F2}s/{timeoutSeconds}s)");
                        yield return null;
                    }

                    var addressPropAfter = transportType.GetProperty("address");
                    var addressValue = addressPropAfter?.GetValue(transport) as string;
                    
                    if (string.IsNullOrWhiteSpace(hostSteamId))
                    {
                        Debug.LogWarning($"[ConnectionStarter] Timeout: HostSteamId was not found in lobby properties after {timeoutSeconds}s");
                    }
                    
                    if (string.IsNullOrWhiteSpace(addressValue))
                    {
                        PurrLogger.LogError("Failed to start connection. Transport address is empty after setup.", this);
                        yield break;
                    }
                    
                    Debug.Log($"[ConnectionStarter] Transport address is set to: {addressValue}");
                }
                else
                {
                    Debug.Log($"[ConnectionStarter] Transport is not SteamTransport, skipping address setup");
                }
            }
            else
            {
                Debug.Log($"[ConnectionStarter] Skipping address setup - transport: {transport}, isOwner: {_lobbyDataHolder?.CurrentLobby.IsOwner}");
            }

            Debug.Log("[ConnectionStarter] Starting network client...");
            _networkManager.StartClient();
            Debug.Log("[ConnectionStarter] StartClient() coroutine completed");
        }
    }
}