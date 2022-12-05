using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkRunner _runner;

    [SerializeField] private NetworkPrefabRef _playerPrefab;

    public Transform spawnA;
    public Transform spawnB;

    public GameObject pilotMovementManager;
    public GameObject titanMovementManager;
    
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    async void StartGame(GameMode gameMode)
    {
        if (_runner != null) return;
        
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = gameMode,
            SessionName = "WeBallin",
            Scene = SceneManager.GetActiveScene().buildIndex + 1,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
        });
        
        pilotMovementManager.SetActive(true);
        //// titanMovementManager.SetActive(true);
    }

    private void OnGUI()
    {
        if (_runner != null) return;
        
        if (GUI.Button(new Rect(0, 0, 200, 40), "Create"))
        {
            StartGame(GameMode.Host);
        }

        if (GUI.Button(new Rect(0, 40, 200, 40), "Join"))
        {
            StartGame(GameMode.Client);
        }
    }

    public void PlayerSingleplayer()
    {
        StartGame(GameMode.Single);
    }
    
    public void PlayMultiplayer()
    {
        //// TODO:: switch to client since we are going to use a dedicated server.
        StartGame(GameMode.AutoHostOrClient);
    }
    
    public void SwitchToTitanMovement()
    {
        pilotMovementManager.SetActive(false);
        titanMovementManager.SetActive(true);
    }
    
    public void SwitchToPilotMovement()
    {
        titanMovementManager.SetActive(false);
        pilotMovementManager.SetActive(true);
    }
    
    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("Connected to Server -> " + runner.name);
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.Log("Connection failed to Server -> " + runner.name + " (" + remoteAddress + ") " +
                  reason);
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        // TODO:: for future menu screen.
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        // TODO:: for future authentication.
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        Debug.Log("Disconnected from Server -> " + runner.name);
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("Host-Migration!");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Ignore since we dont use a Host-Client scenario rn.
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        // No need for this right now.
        Debug.Log("Input by " + player.PlayerId + " is missing! (Input -> " + input + ")");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("Player joined Server -> " + player.PlayerId);
        //// SpawnPlayer(runner, player);
    }

    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (runner.GameMode == GameMode.Shared || runner.GameMode == GameMode.Single)
        {
            if (runner.LocalPlayer == player)
            {
                // Create a unique position for the player
                Vector3 spawnPosition = spawnB.position;
                NetworkObject networkPlayerObject =
                    runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);

                runner.SetPlayerObject(player, networkPlayerObject);

                // Keep track of the player avatars so we can remove it when they disconnect
                _spawnedCharacters.Add(player, networkPlayerObject);
            }
        }
        else
        {
            if (runner.IsServer)
            {
                // Create a unique position for the player
                Vector3 spawnPosition = spawnB.position;
                NetworkObject networkPlayerObject =
                    runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);

                runner.SetPlayerObject(player, networkPlayerObject);

                // Keep track of the player avatars so we can remove it when they disconnect
                _spawnedCharacters.Add(player, networkPlayerObject);
            }
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("Player left Server -> " + runner.name);
        // Find and remove the players avatar
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject.GetComponent<AccesTitan>().TitanObject);
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
        // No need for this right now.
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        SpawnPlayer(runner, runner.LocalPlayer);
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        // TODO:: add Menu loading indicator.
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        // No need for this right now.
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        // TODO:: for future menu screen.
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        // No need for this right now.
    }
}