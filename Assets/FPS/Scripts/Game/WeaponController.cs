using System;
using Unity.FPS.Gameplay;
using UnityEngine;

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
        public Vector3 MuzzleWorldVelocity { get; private set; }
        private Vector3 lastMuzzlePosition;
        public float CurrentCharge { get; private set; }
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
    }
}
