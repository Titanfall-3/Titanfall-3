using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using Networking.Inputs;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkTitanInputProvider : SimulationBehaviour, INetworkRunnerCallbacks
{
    public PlayerInput playerInput;
    public NetworkManager NetworkManager;

    Vector2 moveData;
    Vector2 look;
    bool shouldSprint;
    bool shouldWalk;
    bool canShoot;
    
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        if (NetworkManager._runner != null)
        {
            NetworkManager._runner.AddCallbacks(this);
        }
    }

    private void OnDisable()
    {
        if (NetworkManager._runner != null)
        {
            NetworkManager._runner.RemoveCallbacks(this);
        }
    }

    public void OnMove(InputValue value)
    {
        moveData = value.Get<Vector2>();
    }

    public void OnSprint(InputValue value)
    {
        shouldSprint = value.isPressed;
    }

    public void OnWalk(InputValue value)
    {
        shouldWalk = value.isPressed;
    }

    public void OnLook(InputValue value)
    {
        look = value.Get<Vector2>();
    }
    
    public void OnFire(InputValue value)
    {
        canShoot = value.isPressed;
    }
    
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // NOT NEEDED BUT FORCED TO SINCE INTERFACE IMPLEMENTATION!
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // NOT NEEDED BUT FORCED TO SINCE INTERFACE IMPLEMENTATION!
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkTitanInput networkTitanInput = new NetworkTitanInput();

        networkTitanInput.Buttons.Set(TitanButtons.Sprint, shouldSprint);
        networkTitanInput.Buttons.Set(TitanButtons.Walk, shouldWalk);
        networkTitanInput.Buttons.Set(TitanButtons.Dash, playerInput.actions["Dash"].triggered);
        networkTitanInput.Buttons.Set(TitanButtons.Reload, playerInput.actions["Reload"].triggered);
        networkTitanInput.Buttons.Set(TitanButtons.Shoot, canShoot);

        networkTitanInput.moveData = moveData;
        networkTitanInput.look = look;

        input.Set(networkTitanInput);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        // NOT NEEDED BUT FORCED TO SINCE INTERFACE IMPLEMENTATION!
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        // NOT NEEDED BUT FORCED TO SINCE INTERFACE IMPLEMENTATION!
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        // NOT NEEDED BUT FORCED TO SINCE INTERFACE IMPLEMENTATION!
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        // NOT NEEDED BUT FORCED TO SINCE INTERFACE IMPLEMENTATION!
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        // NOT NEEDED BUT FORCED TO SINCE INTERFACE IMPLEMENTATION!
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        // NOT NEEDED BUT FORCED TO SINCE INTERFACE IMPLEMENTATION!
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        // NOT NEEDED BUT FORCED TO SINCE INTERFACE IMPLEMENTATION!
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        // NOT NEEDED BUT FORCED TO SINCE INTERFACE IMPLEMENTATION!
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        // NOT NEEDED BUT FORCED TO SINCE INTERFACE IMPLEMENTATION!
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        // NOT NEEDED BUT FORCED TO SINCE INTERFACE IMPLEMENTATION!
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
        // NOT NEEDED BUT FORCED TO SINCE INTERFACE IMPLEMENTATION!
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        // NOT NEEDED BUT FORCED TO SINCE INTERFACE IMPLEMENTATION!
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        // NOT NEEDED BUT FORCED TO SINCE INTERFACE IMPLEMENTATION!
    }
}