using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class EnterVanguardTitan : NetworkBehaviour
{
    public Animator titanAnimator;
 
    CharacterController controller;
    BoxCollider rangeCheck;
 
    public GameObject embarkRifle;
    public GameObject Rifle;
 
    public GameObject playerCamera;
    public GameObject player;
    public GameObject titanCamera;
    public GameObject embarkTitanCamera;
    public Transform groundCheck;
 
    public LayerMask groundMask;
 
    Vector3 Yvelocity;
 
    bool isFalling;
    bool isGrounded;
    public bool isEmbarking;
    public bool inTitan;
    public bool inRangeForEmbark;
 
    public float fallingSpeed;
 
    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        rangeCheck = GetComponent<BoxCollider>();
    }
 
    public void StartFall()
    {
        isFalling = true;
        titanAnimator.SetTrigger("StartFall");
    }

    void CheckGround()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, groundMask);
        if (isGrounded)
        {
            isFalling = false;
        }
    }
    
    [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.All)]
    private void Player_HideRPC()
    {
        if (Runner.TryGetPlayerObject(Object.InputAuthority, out NetworkObject networkObject))
        {
            networkObject.gameObject.SetActive(false);
        }
    }
    
    [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.All)]
    private void Player_ShowRPC()
    {
        if (Runner.TryGetPlayerObject(Object.InputAuthority, out NetworkObject networkObject))
        {
            networkObject.gameObject.SetActive(true);
        }
        else
        {
            Debug.Log("Could not find owning Object!");
        }
    }
 
    public IEnumerator Embark()
    {
        GameObject.FindWithTag("NetworkManager").GetComponent<NetworkManager>().SwitchToTitanMovement();
        playerCamera.SetActive(false);
        embarkTitanCamera.SetActive(true);
        rangeCheck.enabled = false;
        titanAnimator.SetTrigger("Embark");
        isEmbarking = true;
 
        yield return new WaitForSeconds(1.5f);
 
        embarkRifle.SetActive(false);
        Rifle.SetActive(true);
 
        yield return new WaitForSeconds(2.4f);
 
        player.transform.parent = transform;
        Player_HideRPC();
        embarkTitanCamera.SetActive(false);
        titanCamera.SetActive(true);

        inTitan = true;
        isEmbarking = false;
    }
 
    void OnTriggerEnter()
    {
        inRangeForEmbark = true;
    }
 
    void OnTriggerExit()
    {
        inRangeForEmbark = false;
    }
 
    void ExitTitan()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            GameObject.FindWithTag("NetworkManager").GetComponent<NetworkManager>().SwitchToPilotMovement();
            Player_ShowRPC();
            playerCamera.SetActive(true);
            titanCamera.SetActive(false);
            inTitan = false;
		    rangeCheck.enabled = true;
            player.transform.parent = null;
        }
    }
 
    // Update is called once per frame
    void Update()
    {
        if (!HasInputAuthority) return;
        
        if (inTitan)
        {
            ExitTitan();
        }
        else if (isFalling && !inTitan)
        {
            Fall();
        }
    }
 
    void Fall()
    {
        Yvelocity.y += fallingSpeed * Time.deltaTime;
        controller.Move( Yvelocity * Time.deltaTime );
    }

}
