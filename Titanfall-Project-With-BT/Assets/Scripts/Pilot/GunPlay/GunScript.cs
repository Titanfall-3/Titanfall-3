using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class GunScript : MonoBehaviour
{
    //bools 
    bool alreadyShot;
    float timeSinceLastShot;
    public GunTemplate gunValues;

    //Reference
    Camera cam;
    public RaycastHit rayHit;

    PlayerInputHandling inputHandling;

    public AudioSource source;
    public GameObject muzzlePoint;
    GameObject publicMuzzlePoint;

    Transform playerMain;
    Recoil recoil;
    WeaponSwitching weaponSwitching;

    string publicMuzzle = "Muzzle";
    
    private void Awake()
    {
        inputHandling = GetComponentInParent<PlayerInputHandling>();
        cam = GetComponentInParent<Camera>();
        weaponSwitching = GetComponentInParent<WeaponSwitching>();
        recoil = GetComponentInParent<Recoil>();

        playerMain = transform.root;
        gunValues.ammoLeft = gunValues.magSize;
        
        publicMuzzlePoint = GameObject.Find(gunValues.name + publicMuzzle);
        
        //Debug.Log(gunValues.name + publicMuzzle);
        GameObject flash = Instantiate(gunValues.muzzleFlash, muzzlePoint.transform) as GameObject;
        flash.layer = muzzlePoint.layer;
        
        GameObject flash2 = Instantiate(gunValues.muzzleFlash, publicMuzzlePoint.transform) as GameObject;
        flash2.layer = publicMuzzlePoint.layer;
        
        Switch();
    }

    public void Switch()
    {
        SetWeapon();
    }

    public void SetWeapon()
    {
        muzzlePoint.GetComponentInChildren<ParticleSystem>().Stop();
        publicMuzzlePoint.GetComponentInChildren<ParticleSystem>().Stop();
        
        gunValues.isReloading = false;
        recoil.snappiness = gunValues.snappiness;
        recoil.returnSpeed = gunValues.returnSpeed;

        for (int i = 0; i < weaponSwitching.weaponTypes.Length; i++)
        {
            weaponSwitching.isActive[i] = false;
            if (weaponSwitching.weaponTypes[i] == gunValues.type)
            {
                weaponSwitching.isActive[i] = true;
            }
        }

        weaponSwitching.HandleAnimations();
    }

    private void Update()
    {
        if (gunValues.Automatic)
        {
            if (inputHandling.canShoot)
            {
                Shoot();
            }
        }
        else
        {
            if (!inputHandling.canShoot)
            {
                alreadyShot = false;
            }
            if (inputHandling.canShoot && alreadyShot == false)
            {
                alreadyShot = true;
                Shoot();
            }
        }

        if (inputHandling.shouldReload && gunValues.ammoLeft < gunValues.magSize && !gunValues.isReloading)
        {
            StartCoroutine(Reload());
        }

        timeSinceLastShot += 1f * Time.deltaTime;
    }

 
    private void Shoot()
    {
        if (!gunValues.isReloading && gunValues.ammoLeft > 0 && timeSinceLastShot > 1f / (gunValues.fireRate / 60f))
        {
            recoil.FireRecoil(gunValues.recoilX, gunValues.recoilY, gunValues.recoilZ);

            for (int i = 0; i < gunValues.bulletsPerShot; i++)
            {
                //Spread
                float x = Random.Range(-gunValues.spread, gunValues.spread);
                float y = Random.Range(-gunValues.spread, gunValues.spread);

                //Calculate Direction with Spread
                Vector3 direction = cam.transform.forward + new Vector3(x, y, 0);

                if (gunValues.hitscan)
                {
                    if (Physics.Raycast(cam.transform.position, direction, out rayHit, gunValues.range))
                    {
                        if (rayHit.transform == playerMain)
                            continue;

                        IDamageable damageable = rayHit.transform.GetComponent<IDamageable>();
                        if (damageable != null)
                        {
                            damageable.DamageRPC(gunValues.damage, gunValues.armorPiercing);

                            GameObject impact = Instantiate(gunValues.hitEffect, rayHit.point, Quaternion.identity) as GameObject;
                            impact.transform.forward = rayHit.normal;

                            Destroy(impact, 1.5f);
                        }
                        else
                        {
                            GameObject impact = Instantiate(gunValues.impactEffect, rayHit.point, Quaternion.identity) as GameObject;
                            impact.transform.forward = rayHit.normal;

                            Destroy(impact, 1.5f);
                        }
                            
                    }
                }
                else
                {
                    var projectile = Instantiate(gunValues.projectile, muzzlePoint.transform.position, muzzlePoint.transform.rotation);
                    projectile.GetComponent<Rigidbody>().AddForce(direction.normalized * gunValues.bulletVelocityForce, ForceMode.Impulse);
                }
            }

            //ShakeCamera

            //Graphics
            gunValues.ammoLeft--;
            timeSinceLastShot = 0f;
            OnGunFire();
        }     
    }
    private IEnumerator Reload()
    {
        gunValues.isReloading = true;

        yield return new WaitForSeconds(gunValues.reloadTime);
        gunValues.ammoLeft = gunValues.magSize;
        gunValues.isReloading = false;
    }

    void OnGunFire()
    {
        source.Stop();
        source.PlayOneShot(gunValues.fireSound);
        muzzlePoint.GetComponentInChildren<ParticleSystem>().Play();
        publicMuzzlePoint.GetComponentInChildren<ParticleSystem>().Play();
        // + whatever logic you need here
    }
}
