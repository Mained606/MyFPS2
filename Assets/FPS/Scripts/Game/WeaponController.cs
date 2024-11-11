using System;
using System.Runtime.CompilerServices;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.FPS.Game
{
    /// <summary>
    /// 크로스 헤어 데이터
    /// </summary>
    [System.Serializable]
    public struct CrossHairData
    {
        public Sprite CrossHairSprite;
        public float CrossHairSize;
        public Color CrossHairColor;
    }

    /// <summary>
    /// 무기 슛 타입
    /// </summary>
    public enum WeaponShootType
    {
        Manual,
        Automatic,
        Charge,
        Sniper
    }

    /// <summary>
    /// 무기(총기)를 관리하는 클래스
    /// </summary>
    public class WeaponController : MonoBehaviour
    {   
        #region Variables
        // 무기 활성화, 비활성화
        public GameObject weaponRoot;

        public GameObject Owner { get; set; }               // 무기의 주인
        public GameObject SourcePrefab { get; set; }        // 무기를 생성한 오리지널 프리팹
        public bool IsWeaponActive { get; private set; }    // 무기 활성화 상태 여부

        private AudioSource shootAudioSource;
        public AudioClip switchWeaponSfx;

        //shooting
        public WeaponShootType shootType;

        [SerializeField] private float maxAmmo = 8f;    // 장전할 수 있는 최대 총알 갯수
        private float currentAmmo;                         // 현재 총알 갯수

        [SerializeField] private float delayBetweenShots = 0.5f; // 총알 발사 간격
        [SerializeField] private float lastTimeShot;       // 마지막 총알 발사 시간

        // Vfx, Sfx
        public Transform weaponMuzzle;
        public GameObject muzzleFlashPrefab;
        public AudioClip shootSfx;

        //CrossHair
        public CrossHairData crossHairDefault;              // 기본, 평상시
        public CrossHairData crossHairTargetInSight;        // 적을 포착했을 때, 타겟팅

        // 조준
        public float aimZoomRatio = 1f;                     // 조준시 줌인 설정값
        public Vector3 aimPositionOffset;                   // 조준시 위치 오프셋

        // 반동
        public float recoilForce = 0.5f;

        //Projectile
        public ProjectileBase projectilePrefab;
        public Vector3 MuzzleWorldVelocity { get; private set; }        // 현재 프레임에서의 구동 속도
        private Vector3 lastMuzzlePosition;


        [SerializeField] private int bulletsPerShot = 1; // 한 번 슛하는 데 발사되는 탄환 수
        [SerializeField] private float bulletSpreadAngle = 0f;   //불렛이 퍼저나가는 각도

        //Charge : 발사 버튼을 누르고 있으면 발사체의 데미지, 속도가 일정값까지 커진다.
        public float CurrentCharge { get; private set; }            // 값이 0 ~ 1
        public bool IsCharging { get; private set; }

        [SerializeField] private float ammoUseOnStartCharge = 1f;    // 충전 시작 버튼을 누르기 위해 필요한 최소 ammo량
        [SerializeField] private float ammoUsageRateWhileCharging = 1f;  //차지하고 있는 동안 소비되는 ammo량
        private float maxChargeDuration = 2f;       //충전 시간 MAX

        public float lastChargeTriggerTimestamp;    // 충전 시작 시간

        public float CurrentAmmoRatio => currentAmmo / maxAmmo;
        #endregion

        void Awake()
        {
            //참조
            shootAudioSource = this.GetComponent<AudioSource>();
        }

        void Start()
        {
            // 초기화
            currentAmmo = maxAmmo;
            lastTimeShot = Time.time;
            lastMuzzlePosition = weaponMuzzle.position;
        }

        void Update()
        {
            // 충전
            UpdateCharge();

            //MuzzleWorldVelocity
            if(Time.deltaTime > 0)
            {
                MuzzleWorldVelocity = (weaponMuzzle.position - lastMuzzlePosition) / Time.deltaTime;

                lastMuzzlePosition = weaponMuzzle.position;
            }

        }

        // 충전
        private void UpdateCharge()
        {
            if(IsCharging)
            {
                if(CurrentCharge < 1f)
                {
                    // 현재 남아있는 충전량
                    float chargeLeft = 1f - CurrentCharge;
                    
                    float chargeAdd = 0f;       // 이번 프레임에 충전할 량
                    if(maxChargeDuration <= 0f)
                    {
                        chargeAdd = chargeLeft; // 한 번에 풀 충전
                    }
                    else
                    {
                        chargeAdd = (1f / maxChargeDuration) * Time.deltaTime;
                    }
                    chargeAdd = Mathf.Clamp(chargeAdd, 0f, chargeLeft);     // 남아있는 충전량보다 작아야 한다.

                    //chargeAdd 만큼 Ammo 소비량을 구한다.
                    float ammoThisChargeRequire = chargeAdd * ammoUsageRateWhileCharging;
                    if(ammoThisChargeRequire <= currentAmmo)
                    {
                        UseAmmo(ammoThisChargeRequire);
                        CurrentCharge = Mathf.Clamp01(CurrentCharge + chargeAdd);
                    }
                }
            }
        }

        // 무기 활성화, 비활성화
        public void ShowWeapon(bool show)
        {
            weaponRoot.SetActive(show);

            // this 무기로 변경
            if(show == true && switchWeaponSfx != null)
            {
                // 무기 변경 효과음 플레이
                shootAudioSource.PlayOneShot(switchWeaponSfx);
            }

            IsWeaponActive = show;
        }

        // 키 입력에 따른 슛 구현
        public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
        {
            switch(shootType)
            {
                case WeaponShootType.Manual:
                    if(inputDown)
                    {
                        return TryShoot();
                    }
                    break;
                case WeaponShootType.Automatic:
                    if(inputHeld)
                    {
                        return TryShoot();
                    }
                    break;
                case WeaponShootType.Charge:
                    if(inputHeld)
                    {
                        // 충전 시작
                        TryBeginCharge();
                    }

                    if(inputUp)
                    {
                        // 충전 끝 발사
                        return TryReleaseCharge();
                    }
                    break;

                case WeaponShootType.Sniper:
                    if(inputDown)
                    {
                        return TryShoot();
                    }
                    break;
            }

            return false;
        }

        // 충전 시작
        void TryBeginCharge()
        {
            if(!IsCharging && currentAmmo >= ammoUseOnStartCharge 
                && (lastTimeShot + delayBetweenShots) < Time.time)
            {
                UseAmmo(ammoUseOnStartCharge);

                lastChargeTriggerTimestamp = Time.time;
                IsCharging = true;

            }
        }

        // 발사
        bool TryReleaseCharge()
        {
            if(IsCharging)
            {
                // 슛
                HandleShoot();

                // 초기화
                CurrentCharge = 0f;
                IsCharging = false;
                return true;
            }

            return false;
        }

        void UseAmmo(float amount)
        {
            currentAmmo = Mathf.Clamp(currentAmmo - amount, 0f, maxAmmo);
            lastTimeShot = Time.time;
        }

        private bool TryShoot()
        {
            if(lastTimeShot + delayBetweenShots < Time.time && currentAmmo >= 1f)
            {
                currentAmmo -= 1f;
                Debug.Log(currentAmmo);

                HandleShoot();
                return true;
            }

            return false;
        }

        // 슛 연출
        void HandleShoot()
        {

            //project tile 생성
            for(int i = 0; i < bulletsPerShot; i++)
            {
                Vector3 shotDirection = GetShotDirectionWithinSpread(weaponMuzzle);
                
                ProjectileBase projectileInstance = Instantiate(projectilePrefab, weaponMuzzle.position, Quaternion.LookRotation(shotDirection));
                projectileInstance.Shoot(this);
                
            }
            //Vfx
            if(muzzleFlashPrefab)
            {
                GameObject effectGo = Instantiate(muzzleFlashPrefab, weaponMuzzle.position, weaponMuzzle.rotation, weaponMuzzle);
                Destroy(effectGo, 2f);
            }

            //Sfx
            if(shootSfx)
            {
                shootAudioSource.PlayOneShot(shootSfx);
            }

            lastTimeShot = Time.time;
        }
        //project tile 방향
        Vector3 GetShotDirectionWithinSpread(Transform shootTransform)
        {
            float spreadAngleRatio = bulletSpreadAngle / 180f; 
            return Vector3.Lerp(shootTransform.forward, UnityEngine.Random.insideUnitSphere, spreadAngleRatio);
        }
    }
}
