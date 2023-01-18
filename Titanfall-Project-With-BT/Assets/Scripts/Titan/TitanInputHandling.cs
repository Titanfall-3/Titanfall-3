using Fusion;
using UnityEngine;

public class TitanInputHandling : NetworkBehaviour
{
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }

    VanguardMovement moveScript;
    VanguardCamera titanCamera;
    FireTitanRifle fireTitanRifle;
    
    private void Awake()
    {
        titanCamera = GetComponent<VanguardCamera>();
        moveScript = GetComponent<VanguardMovement>();
        fireTitanRifle = GetComponent<FireTitanRifle>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;
        
        if (GetInput<NetworkPlayerInput>(out var input) == false) return;

        var pressed = input.Buttons.GetPressed(ButtonsPrevious);
        
        ButtonsPrevious = input.Buttons;
        
        moveScript.shouldSprint = input.Buttons.IsSet(NetworkPlayerButtons.SPRINT);
        moveScript.moveData = input.move;
        titanCamera.look = input.look * titanCamera.sensitivity;

        if (input.Buttons.IsSet(NetworkPlayerButtons.SHOOT))
        {
            if (!moveScript.isSprinting)
            {
                fireTitanRifle.canShoot = true;
            }
        }
        else
        {
            fireTitanRifle.canShoot = false;
        }

        fireTitanRifle.shouldReload = pressed.IsSet(NetworkPlayerButtons.RELOAD);
    }
}