using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// ũ�ν��� �׸��� ���� ������
    /// </summary>
    [System.Serializable]
    public struct CrossHairData
    {
        public Sprite CrossHairSprite;
        public float CrossHairSize;
        public Color CrossHairColor;
    }

    /// <summary>
    /// ���� �� Ÿ��
    /// </summary>
    public enum WeaponShootType
    {
        Manual,
        Automatic,
        Charge,
        Sniper,
    }

    /// <summary>
    /// ����(�ѱ�)�� �����ϴ� Ŭ����
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        #region Variables
        //���� Ȱ��ȭ, ��Ȱ��ȭ
        public GameObject weaponRoot;

        public GameObject Owner { get; set; }           //������ ����
        public GameObject SourcePrefab { get; set; }    //���⸦ ������ �������� ������
        public bool IsWeaponActive { get; private set; }    //���� Ȱ��ȭ ����

        private AudioSource shootAudioSource;
        public AudioClip switchWeaponSfx;

        //Shooting
        public WeaponShootType shootType;

        [SerializeField] private float maxAmmo = 8f;            //�����Ҽ� �ִ� �ִ� �Ѿ� ����
        private float currentAmmo;

        [SerializeField] private float delayBetweenShots = 0.5f;    //�� ����
        private float lastTimeShot;                                 //���������� ���� �ð�

        //Vfx, Sfx
        public Transform weaponMuzzle;                              //�ѱ� ��ġ
        public GameObject muzzleFlashPrefab;                        //�ѱ� �߻� ����Ʈ ȿ��
        public AudioClip shootSfx;                                  //�� �߻� ����

        //CrossHair
        public CrossHairData crosshairDefault;              //�⺻, ����
        public CrossHairData crosshairTargetInSight;        //���� ����������, Ÿ���� �Ǿ�����

        //����
        public float aimZoomRatio = 1f;             //���ؽ� ���� ������
        public Vector3 aimOffset;                   //���ؽ� ���� ��ġ ������

        //�ݵ�
        public float recoilForce = 0.5f;

        //Projectile
        public ProjectileBase projectilePrefab;

        public Vector3 MuzzleWorldVelocity { get; private set; }            //���� �����ӿ����� �ѱ� �ӵ�
        private Vector3 lastMuzzlePosition;
        
        [SerializeField] private int bulletsPerShot = 1;                     //�ѹ� ���ϴµ� �߻�Ǵ� źȯ�� ����
        [SerializeField] private float bulletSpreadAngle = 0f;               //�淿�� ���� ������ ����

        //Charge : �߻� ��ư�� ������ ������ �߻�ü�� ������, �ӵ��� ���������� Ŀ����
        public float CurrentCharge { get; private set; }            //0 ~ 1
        public bool IsCharging { get; private set; }

        [SerializeField] private float ammoUseOnStartCharge = 1f; //���� ���� ��ư�� ������ ���� �ʿ��� ammo��
        [SerializeField] private float ammoUsageRateWhileCharging = 1f; //�����ϰ� �ִµ��� �Һ�Ǵ� ammo��
        private float maxChargeDuration = 2f;                           //���� �ð� Max

        public float lastChargeTriggerTimeStamp;                        //���� ���� �ð�

        //Reload: ������
        [SerializeField] private float ammoReloadRate = 1f;             //�ʴ� �������Ǵ� ��
        [SerializeField] private float ammoReloadDelay = 2f;            //���� ���� ammoReloadDelay�� �����Ŀ� ������ ����

        [SerializeField] private bool automaticReload = true;   //�ڵ�, ���� ����
        #endregion

        public float CurrentAmmoRatio => currentAmmo / maxAmmo;

        private void Awake()
        {
            //����
            shootAudioSource = this.GetComponent<AudioSource>();
        }

        private void Start()
        {
            //�ʱ�ȭ
            currentAmmo = maxAmmo;
            lastTimeShot = Time.time;
            lastMuzzlePosition = weaponMuzzle.position;
        }

        private void Update()
        {            
            UpdateCharge();     //����
            UpdateAmmo();

            //MuzzleWorldVelocity
            if (Time.deltaTime > 0f)
            {
                MuzzleWorldVelocity = (weaponMuzzle.position - lastMuzzlePosition) / Time.deltaTime;
                lastMuzzlePosition = weaponMuzzle.position;
            }
        }

        //Reload - Auto
        private void UpdateAmmo()
        {
            //������
            if(automaticReload && currentAmmo < maxAmmo && IsCharging == false
               && lastTimeShot + ammoReloadDelay < Time.time )
            {
                currentAmmo += ammoReloadRate * Time.deltaTime; //�ʴ� ammoReloadRate�� ������
                currentAmmo = Mathf.Clamp(currentAmmo, 0, maxAmmo);
            }
        }

        //Reload - ����
        public void Reload()
        {
            if(automaticReload || currentAmmo >= maxAmmo || IsCharging)
            {
                return;
            }

            currentAmmo = maxAmmo;
        }

        //����
        void UpdateCharge()
        {
            if(IsCharging)
            {
                if(CurrentCharge < 1f)
                {
                    //���� �����ִ� ������
                    float chargeLeft = 1f - CurrentCharge;

                    float chargeAdd = 0f;           //�̹� �����ӿ� ������ ��
                    if(maxChargeDuration <= 0f)
                    {
                        chargeAdd = chargeLeft;     //�ѹ��� Ǯ ����
                    }
                    else
                    {
                        chargeAdd = (1f / maxChargeDuration) * Time.deltaTime;
                    }
                    chargeAdd = Mathf.Clamp(chargeAdd, 0f, chargeLeft);         //�����ִ� ���������� �۾ƾ� �Ѵ�

                    //chargeAdd ��ŭ Ammo �Һ��� ���Ѵ�
                    float ammoThisChargeRequire = chargeAdd * ammoUsageRateWhileCharging;
                    if(ammoThisChargeRequire <= currentAmmo)
                    {
                        UseAmmo(ammoThisChargeRequire);
                        CurrentCharge = Mathf.Clamp01(CurrentCharge + chargeAdd);
                    }
                }
            }
        }

        //���� Ȱ��ȭ, ��Ȱ��ȭ
        public void ShowWeapon(bool show)
        {
            weaponRoot.SetActive(show);

            //this ����� ����
            if (show == true && switchWeaponSfx != null)
            {
                //���� ���� ȿ���� �÷���
                shootAudioSource.PlayOneShot(switchWeaponSfx);
            }

            IsWeaponActive = show;
        }

        //Ű �Է¿� ���� �� Ÿ�� ����
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
                        //���� ����
                        TryBeginCharge();
                    }
                    if(inputUp)
                    {
                        //���� �� �߻�
                        return TryReleaseCharge();
                    }
                    break;
                case WeaponShootType.Sniper:
                    if (inputDown)
                    {
                        return TryShoot();
                    }
                    break;
            }

            return false;
        }

        //���� ����
        void TryBeginCharge()
        {
            if(IsCharging == false && currentAmmo >= ammoUseOnStartCharge
                && (lastTimeShot + delayBetweenShots) < Time.time)
            {
                UseAmmo(ammoUseOnStartCharge);

                lastChargeTriggerTimeStamp = Time.time;
                IsCharging = true;
            }
        }

        //���� �� - �߻�
        bool TryReleaseCharge()
        {
            if(IsCharging)
            {
                //��
                HandleShoot();

                //�ʱ�ȭ
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

        bool TryShoot()
        {
            if(currentAmmo >= 1f && (lastTimeShot + delayBetweenShots) < Time.time)
            {
                currentAmmo -= 1f;

                HandleShoot();
                return true;
            }

            return false;
        }

        //�� ����
        void HandleShoot()
        {
            //projectile ����
            for (int i = 0; i < bulletsPerShot; i++)
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

            //���� �ð� ����
            lastTimeShot = Time.time;
        }

        //projectile ���ư��� ����
        Vector3 GetShotDirectionWithinSpread(Transform shootTransfrom)
        {
            float spreadAngleRatio = bulletSpreadAngle / 180f;
            return Vector3.Lerp(shootTransfrom.forward, UnityEngine.Random.insideUnitSphere, spreadAngleRatio);
        }

    }
}
