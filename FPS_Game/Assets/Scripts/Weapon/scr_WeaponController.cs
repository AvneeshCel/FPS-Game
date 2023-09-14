using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using static Models;
using UnityEngine.Pool;
using Unity.VisualScripting;

public class scr_WeaponController : MonoBehaviour
{
    public scr_CharacterController characterController;

    [Header("References")]
    public Animator weaponAnimator;

    [Header("Settings")]
    public WeaponSettings settings;

    bool isInitialized;

    Vector3 newWeaponRotation;
    Vector3 newWeaponRotationVelocity;
    
    Vector3 targetWeaponRotation;
    Vector3 targetWeaponRotationVelocity;
    
    Vector3 newWeaponMovementRotation;
    Vector3 newWeaponMovementRotationVelocity;
    
    Vector3 targetWeaponMovementRotation;
    Vector3 targetWeaponMovementRotationVelocity;

    public bool isGroundedTrigger;

    private float fallingDelay;

    [Header("Weapon Sway")]
    public Transform weaponSwayObject;

    public float swayAmountA = 1f;
    public float swayAmountB = 2f;
    public float swayScale = 600f;
    public float swayLerpSpeed = 14f;

    public float swayTime;
    public Vector3 swayPosition;


    [Header("Sights")]
    public Transform sightTarget;
    public float sightOffset;
    public float aimingInTime;
    private Vector3 weaponSwayPosition;
    private Vector3 weaponSwayPositionVelocity;

    [HideInInspector]
    public bool isAimingIn;
    public bool aimedOut;


    [Header("Shooting")]
    public float fireRate;
    private float currentFireRate;
    public List<WeaponFireType> allowedFireTypes;
    public WeaponFireType currentFireType;
    [HideInInspector]
    public bool isShooting;


    [Header("Weapon Stats")]
    public GameObject bulletPrefab;
    [SerializeField]
    private float shootforce, upwardForce;
    [SerializeField]
    private float currentTimeBetweenShooting, spread, reloadTime, timeBetweenShots;
    [SerializeField]
    public int magazineSize, bulletsPerTap;
    public bool allowButtonHold;

    public int bulletsLeft, bulletsShot;
    public bool shooting, readyToShoot, reloading;
    public Transform bulletSpawn;
    public LayerMask bulletMask;


    private ObjectPool<scr_Bullet> bulletPool;


    public scr_Bullet prefab2;

    //bug fixing
    public bool allowInvoke = true;

    //Recoil
    private Vector3 rcl_CurRot;
    private Vector3 rcl_TargetRot;

    [Header("Recoil")] 
    // Hipfire Recoil
    [SerializeField] private float recoilX;
    [SerializeField] private float recoilY;
    [SerializeField] private float recoilZ;

    //Settings 
    [SerializeField] private float rcl_snappiness;
    [SerializeField] private float rcl_returnSpeed;

    #region Awake / Start / Update

    private void Awake()
    {
        bulletPool = new ObjectPool<scr_Bullet>(() =>
        {
            return Instantiate(prefab2);

        }, bullet =>
        {
            bullet.transform.position = bulletSpawn.position;
            bullet.transform.rotation = Quaternion.LookRotation(bulletSpawn.transform.forward);
            bullet.gameObject.SetActive(true);
        }, bullet =>
        {
            bullet.gameObject.SetActive(false);
           // bullet.gameObject.transform.position = bulletSpawn.position;
            bullet.gameObject.GetComponent<TrailRenderer>().Clear();
            //bullet.force = Vector3.zero;
        }, bullet =>
        {
            Destroy(bullet.gameObject);
        }, false, 50, 100);
    }

    private void Start()
    {
        newWeaponRotation = transform.localRotation.eulerAngles;
        currentFireType = allowedFireTypes[0];
        WeaponInit();
        Application.targetFrameRate = 60;
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        CalculateWeaponRotation();
        SetWeaponAnimations();
        CalculateWeaponSway();
        CalculateAimingIn();
        CalculateShooting();
        RecoilUpdate();
    }

    #endregion


    #region Object Pooling

    private void DestroyBullet(scr_Bullet bullet)
    {
        bulletPool.Release(bullet);
    }

    #endregion

    #region Shooting

    private void WeaponInit()
    {
        bulletsLeft = magazineSize;
        readyToShoot = true;
    }

    private void CalculateShooting()
    {
        if (isShooting && readyToShoot && !reloading && bulletsLeft > 0)
        {
            bulletsShot = 0;

            Shoot();

            if (currentFireType == WeaponFireType.SemiAuto)
            {
                isShooting = false;
            }

            
        }
        
        if (readyToShoot && isShooting && !reloading && bulletsLeft <= 0) Reload();
    }

    private void Shoot()
    {
        readyToShoot = false;

        Ray ray = characterController.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.5f));
        RaycastHit hit;

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit, bulletMask))
        {
            targetPoint = hit.point;
        } else { targetPoint = ray.GetPoint(75); }


        Vector3 directionWithoutSpread = targetPoint - bulletSpawn.position;

        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);

        Vector3 directionWithSpread = directionWithoutSpread + new Vector3(x, y, 0);
        //GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity);
        var bullet = bulletPool.Get();
       // bullet.transform.position = bulletSpawn.position;
         //bullet.transform.forward = directionWithoutSpread.normalized;

        //bullet.GetComponent<scr_Bullet>().force = directionWithoutSpread.normalized * shootforce/2;
        //bullet.GetComponent<scr_Bullet>().force = directionWithoutSpread.normalized * 4f;
        bullet.GetComponent<Rigidbody>().AddForce(bulletSpawn.forward * shootforce);
        bullet.Init(DestroyBullet);
        //bullet.GetComponent<Rigidbody>().AddForce(characterController.camera.transform.up * upwardForce, ForceMode.Impulse);

        bulletsLeft--;
        bulletsShot++;

        if (allowInvoke)
        {
            Invoke("ResetShot", currentTimeBetweenShooting);
            allowInvoke = false;

            if (bulletsShot < bulletsPerTap && bulletsLeft > 0)
            {
                Invoke("Shoot", timeBetweenShots);
            }

        }

        RecoilFire();

        // Load bullet settings
    }

    private void ResetShot()
    {
        readyToShoot = true;
        allowInvoke = true;
    }

    public void Reload()
    {
        reloading = true;
        Invoke("ReloadFinished", reloadTime);
    }

    private void ReloadFinished()
    {
        bulletsLeft = magazineSize;
        reloading = false;
    }

    #endregion

    #region Initialize
    public void Initialize(scr_CharacterController CharacterController)
    {
        characterController = CharacterController;
        isInitialized = true;
    }


    #endregion

    #region Aiming In

    bool aimingBool;
    private void CalculateAimingIn()
    {
        var targetPosition = transform.position;

        if (isAimingIn)
        {
            targetPosition = characterController.camera.transform.position + (weaponSwayObject.transform.position - sightTarget.position) + (characterController.camera.transform.forward * sightOffset);
            aimedOut = false;
            aimingBool = false;
            Debug.Log("false");
        }

        if (!isAimingIn)
        {

            if (!aimingBool)
            {
                Debug.Log("true");
                Invoke("SetAimingBool", 0.3f);
                aimingBool = true;
            }
        }
        
      

        
        weaponSwayPosition = weaponSwayObject.transform.position;
        weaponSwayPosition = Vector3.SmoothDamp(weaponSwayPosition, targetPosition, ref weaponSwayPositionVelocity, aimingInTime);
        weaponSwayObject.transform.position = weaponSwayPosition;

       
    }

    private void SetAimingBool()
    {
        aimedOut = true;
    }

    #endregion

    #region Jumping
    public void TriggerJump()
    {
        isGroundedTrigger = false;
        weaponAnimator.SetTrigger("Jump");
    }

    #endregion

    #region Rotation
    private void CalculateWeaponRotation()
    {
        targetWeaponRotation.x += (isAimingIn ? settings.SwayAmount / 3 : settings.SwayAmount) * (settings.SwayYInverted ? -characterController.input_View.y : characterController.input_View.y) * Time.deltaTime;
        targetWeaponRotation.y += (isAimingIn ? settings.SwayAmount / 3 : settings.SwayAmount) * (settings.SwayXInverted ? -characterController.input_View.x : characterController.input_View.x) * Time.deltaTime;

        targetWeaponRotation.x = Mathf.Clamp(targetWeaponRotation.x, -settings.SwayClampX, settings.SwayClampX);
        targetWeaponRotation.y = Mathf.Clamp(targetWeaponRotation.y, -settings.SwayClampY, settings.SwayClampY);
        targetWeaponRotation.z = isAimingIn ? 0 : targetWeaponRotation.y;

        targetWeaponRotation = Vector3.SmoothDamp(targetWeaponRotation, Vector3.zero, ref targetWeaponRotationVelocity, settings.SwayResetSmoothing);
        newWeaponRotation = Vector3.SmoothDamp(newWeaponRotation, targetWeaponRotation, ref targetWeaponRotationVelocity, settings.SwaySmoothing);

        targetWeaponMovementRotation.z = (isAimingIn ? settings.MovementSwayX / 3 : settings.MovementSwayX) * (settings.MovementSwayXInverted ? -characterController.input_Movement.x : characterController.input_Movement.x);
        targetWeaponMovementRotation.x = (isAimingIn ? settings.MovementSwayY / 3 : settings.MovementSwayY) * (settings.MovementSwayYInverted ? -characterController.input_Movement.y : characterController.input_Movement.y);

        targetWeaponMovementRotation = Vector3.SmoothDamp(targetWeaponRotation, Vector3.zero, ref targetWeaponMovementRotationVelocity, settings.MovementSwaySmoothing);
        newWeaponMovementRotation = Vector3.SmoothDamp(newWeaponRotation, targetWeaponMovementRotation, ref newWeaponMovementRotationVelocity, settings.MovementSwaySmoothing);

        transform.localRotation = Quaternion.Euler(newWeaponRotation + newWeaponMovementRotation);

    }

    #endregion

    #region Animations
    private void SetWeaponAnimations()
    {
        if (isGroundedTrigger)
        {
            fallingDelay = 0f;
        }
        else
        {
            fallingDelay += Time.deltaTime;
        }

        if (characterController.isGrounded && !isGroundedTrigger && fallingDelay > 0.1f)
        {
            weaponAnimator.SetTrigger("Land");
            isGroundedTrigger = true;
        }

        else if (!characterController.isGrounded && isGroundedTrigger)
        {
            weaponAnimator.SetTrigger("Falling");
            isGroundedTrigger = false;
        }

        weaponAnimator.SetBool("isSprinting", characterController.isSprinting);
        weaponAnimator.SetFloat("WeaponAnimationSpeed", characterController.weaponAnimationSpeed);
    }

    #endregion

    #region Sway

    float timer = 0.0f; 
    private void CalculateWeaponSway()
    {

        var targetPosition = LissajousCurve(swayTime, swayAmountA, swayAmountB) / (isAimingIn ? swayScale * 3 : swayScale);

        //if (!isAimingIn && aimedOut) weaponSwayObject.SetLocalPositionAndRotation(swayPosition, weaponSwayObject.localRotation);

        swayPosition = Vector3.Lerp(swayPosition, targetPosition, Time.smoothDeltaTime * swayLerpSpeed);
        swayTime += Time.deltaTime;


        if (swayTime > 6.3f)
        {
            swayTime = 0f;
        }

        //if (!isAimingIn && aimedOut) weaponSwayObject.localPosition = swayPosition;

        /*
        if (!isAimingIn && aimedOut)
         {
            weaponSwayObject.localPosition = Vector3.Lerp(weaponSwayObject.localPosition, swayPosition, Time.smoothDeltaTime * timer);
            timer += Time.deltaTime;

            if (timer > 0.99f) { timer = 0f; }
        }
        */

         if (!isAimingIn && aimedOut) weaponSwayObject.localPosition = Vector3.SmoothDamp(weaponSwayObject.localPosition, swayPosition, ref swayPosition, 0.4f);
        

    }

    private Vector3 LissajousCurve(float time, float a, float b)
    {
        return new Vector3(Mathf.Sin(time), a * Mathf.Sin(b * time + Mathf.PI));
    }

    private void OnDrawGizmos()
    {
        Ray ray = characterController.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.5f));
        RaycastHit hit;

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit, bulletMask))
        {
            targetPoint = hit.point;
        }
        else { targetPoint = ray.GetPoint(75); }

        Gizmos.DrawSphere(targetPoint, 3f);
    }
    #endregion

    #region Recoil

    private void RecoilUpdate()
    {
        rcl_TargetRot = Vector3.Lerp(rcl_TargetRot, Vector3.zero, rcl_returnSpeed * Time.deltaTime);
        rcl_CurRot = Vector3.Slerp(rcl_CurRot, rcl_TargetRot, rcl_snappiness * Time.fixedDeltaTime);
        characterController.camera.transform.localRotation = Quaternion.Euler(rcl_CurRot);
        characterController.cameraHolder.transform.localRotation = Quaternion.Euler(rcl_CurRot); ;
    }

    public void RecoilFire()
    {
        rcl_TargetRot += new Vector3(recoilX, Random.Range(-recoilY, recoilY), Random.Range(-recoilZ, recoilZ));
    }

    #endregion
}
