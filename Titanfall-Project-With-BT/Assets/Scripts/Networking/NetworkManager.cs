using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using Fusion.Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkRunner Runner;

    [SerializeField] private NetworkPrefabRef _playerPrefab;

    [SerializeField] private NetworkPrefabRef _vanguardTitanPrefab;

    public GameObject InputProviderGameObject;

    public GameObject[] spawnPoints;

    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private void Awake()
    {
        spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
    }

    async void StartGame(GameMode gameMode)
    {
        Runner = gameObject.AddComponent<NetworkRunner>();
        Runner.ProvideInput = true;

        Dictionary<string, object> values = new Dictionary<string, object>();

        values.Add("username", PlayerPrefs.GetString("username"));
        values.Add("password", PlayerPrefs.GetString("password"));
        values.Add("version", Application.version);

        if (PlayerPrefs.GetInt("remember") == 0)
        {
            PlayerPrefs.DeleteKey("username");
            PlayerPrefs.DeleteKey("password");
            PlayerPrefs.Save();
        }
        
        // Create a new AuthenticationValues
        AuthenticationValues authentication = new AuthenticationValues();

        // Setup
        authentication.AuthType = CustomAuthenticationType.Custom;
        authentication.SetAuthPostData(values);

        await Runner.StartGame(new StartGameArgs()
        {
            GameMode = gameMode,
            SessionName = "WeBallin",
            Scene = SceneManager.GetActiveScene().buildIndex,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            AuthValues = authentication,
        });
        
        InputProviderGameObject.SetActive(true);
    }

    private void OnGUI()
    {
        if (Runner == null)
        {
            if (GUI.Button(new Rect(0, 40, 200, 40), "Create"))
            {
                StartGame(GameMode.Host);
            }
            if (GUI.Button(new Rect(0, 100, 200, 40), "Join"))
            {
                StartGame(GameMode.Client);
            }
        }
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
        // Is being handled by the NetworkPilotInputProvider!
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        // Is being handled by the NetworkPilotInputProvider!
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("Player joined Server -> " + player.PlayerId);
        if (runner.GameMode == GameMode.Shared || runner.GameMode == GameMode.Single)
        {
            if (runner.LocalPlayer == player)
            {
                // Create a unique position for the player
                Vector3 spawnPosition = spawnPoints[new System.Random().Next(spawnPoints.Length - 1)].transform.position;
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
                Vector3 spawnPosition = spawnPoints[new System.Random().Next(spawnPoints.Length - 1)].transform.position;
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
        // TODO:: for future menu screen.
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        // TODO:: for future menu screen.
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