using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// ???? ??? ????
    /// </summary>
    public enum WeaponSwithState
    {
        Up,
        Down,
        PutDownPrvious,
        PutUpNew,
    }

    /// <summary>
    /// ?°¿???? ???? ????(WeaponController)???? ??????? ?????
    /// </summary>
    public class PlayerWeaponsManager : MonoBehaviour
    {
        #region Variables
        //???? ???? - ?????? ??????? ??? ???????? ?????? ???? ?????(?•Í???)
        public List<WeaponController> startingWeapons = new List<WeaponController>();

        //???? ????
        //???? ??????? ???????
        public Transform weaponParentSocket;

        //?°¿???? ??????? ??? ???? ???? ?????
        private WeaponController[] weaponSlots = new WeaponController[9];
        //???? ?????(????)?? ?????? ???? ??????? ?•Â???
        public int ActiveWeaponIndex { get; private set; }

        //???? ???
        public UnityAction<WeaponController> OnSwitchToWeapon;  //???? ?????????? ???? ??? ???
        public UnityAction<WeaponController, int> OnAddedWeapon;    //???? ?????????? ???? ??? ???
        public UnityAction<WeaponController, int> OnRemoveWeapon;   //?????? ???? ??????????? ???? ??? ???

        private WeaponSwithState weaponSwithState;          //???? ????? ????

        private PlayerInputHandler playerInputHandler;

        //???? ????? ????? ???? ???
        private Vector3 weaponMainLocalPosition;

        public Transform defaultWeaponPostion;
        public Transform downWeaponPostion;
        public Transform aimingWeaponPosition;

        private int weaponSwitchNewIndex;           //???? ???? ???? ?•Â???

        private float weaponSwitchTimeStarted = 0f;
        [SerializeField] private float weaponSwitchDelay = 1f;

        //?? ????
        public bool IsPointingAtEnemy { get; private set; }         //?? ???? ????
        public Camera weaponCamera;                                 //weaponCamera???? Ray?? ?? ???

        //????
        //???? ????
        private PlayerCharacterController playerCharacterController;
        [SerializeField] private float defaultFov = 60f;          //???? ?? FOV ??
        [SerializeField] private float weaponFovMultiplier = 1f;       //FOV ???? ???

        public bool IsAiming { get; private set; }                      //???? ???? ????
        [SerializeField] private float aimingAnimationSpeed = 10f;      //???? ???,Fov ???? Lerp???

        //???
        [SerializeField] private float bobFrequency = 10f;
        [SerializeField] private float bobSharpness = 10f;
        [SerializeField] private float defaultBobAmount = 0.05f;         //???? ??? ??
        [SerializeField] private float aimingBobAmount = 0.02f;          //?????? ??? ??

        private float weaponBobFactor;          //??? ???
        private Vector3 lastCharacterPosition;  //???? ??????????? ???????? ????? ???? ????

        private Vector3 weaponBobLocalPosition; //????? ??? ?? ???? ??ÅU, ??????? ?????? 0

        //???
        [SerializeField] private float recoilSharpness = 50f;       //??? ?¨⁄??? ??? ???
        [SerializeField] private float maxRecoilDistance = 0.5f;    //????? ??? ?¨⁄??? ??? ?????
        private float recolieRepositionSharpness = 10f;             //??????? ??????? ???
        private Vector3 accumulateRecoil;                           //????? ??? ?¨⁄??? ??

        private Vector3 weaponRecoilLocalPosition;      //????? ????? ???? ??ÅU, ????? ??????? ??????? 0

        //???? ???
        private bool isScopeOn = false;
        [SerializeField] private float distanceOnScope = 0.1f;

        public UnityAction OnScopedWeapon;              //???? ??? ????? ???? ??? ???
        public UnityAction OffScopedWeapon;             //???? ??? ?????? ???? ??? ???
        #endregion

        private void Start()
        {
            //????
            playerInputHandler = GetComponent<PlayerInputHandler>();
            playerCharacterController = GetComponent<PlayerCharacterController>();

            //????
            ActiveWeaponIndex = -1;
            weaponSwithState = WeaponSwithState.Down;

            //????? ???? show ??? ???
            OnSwitchToWeapon += OnWeaponSwitched;

            //???? ??? ??? ???
            OnScopedWeapon += OnScope;
            OffScopedWeapon += OffScope;

            //Fov ??? ????
            SetFov(defaultFov);

            //???? ???? ???? ????
            foreach (var weapon in startingWeapons)
            {
                AddWeapon(weapon);
            }
            SwitchWeapon(true);
        }

        private void Update()
        {
            //???? ????? ????
            WeaponController activeWeapon = GetActiveWeapon();

            if(weaponSwithState == WeaponSwithState.Up)
            {
                //???? ??°∆? ???
                IsAiming = playerInputHandler.GetAimInputHeld();

                //???? ??? ???
                if(activeWeapon.shootType == WeaponShootType.Sniper)
                {
                    if(playerInputHandler.GetAimInputDown())
                    {
                        //???? ??? ????
                        isScopeOn = true;
                        //OnScopedWeapon?.Invoke();
                    }
                    if(playerInputHandler.GetAimInputUp())
                    {
                        //???? ??? ??
                        OffScopedWeapon?.Invoke();
                    }
                }

                //?? ???
                bool isFire = activeWeapon.HandleShootInputs(
                    playerInputHandler.GetFireInputDown(),
                    playerInputHandler.GetFireInputHeld(),
                    playerInputHandler.GetFireInputUp());

                if (isFire)
                {
                    //??? ???
                    accumulateRecoil += Vector3.back * activeWeapon.recoilForce;
                    accumulateRecoil = Vector3.ClampMagnitude(accumulateRecoil, maxRecoilDistance);
                }
            }

            if (!IsAiming && (weaponSwithState == WeaponSwithState.Up || weaponSwithState == WeaponSwithState.Down))
            {
                int switchWeaponInput = playerInputHandler.GetSwitchWeaponInput();
                if (switchWeaponInput != 0)
                {
                    bool switchUp = switchWeaponInput > 0;
                    SwitchWeapon(switchUp);
                }
            }

            //?? ????
            IsPointingAtEnemy = false;
            if (activeWeapon)
            {
                RaycastHit hit;
                if (Physics.Raycast(weaponCamera.transform.position, weaponCamera.transform.forward, out hit, 300f))
                {
                    //?????? ?? - ??(Damageable)
                    Damageable damageable = hit.collider.GetComponent<Damageable>();
                    if (damageable)
                    {
                        IsPointingAtEnemy = true;
                    }
                }
            }
        }

        private void LateUpdate()
        {
            UpdateWeaponBob();
            UpdateWeaponRecoil();
            UpdateWeaponAiming();
            UpdateWeaponSwitching();

            //???? ???? ???
            weaponParentSocket.localPosition = weaponMainLocalPosition + weaponBobLocalPosition + weaponRecoilLocalPosition;
        }

        //???
        void UpdateWeaponRecoil()
        {
            if(weaponRecoilLocalPosition.z >= accumulateRecoil.z * 0.99f)
            {
                weaponRecoilLocalPosition = Vector3.Lerp(weaponRecoilLocalPosition, accumulateRecoil,
                    recoilSharpness * Time.deltaTime);
            }
            else
            {
                weaponRecoilLocalPosition = Vector3.Lerp(weaponRecoilLocalPosition, Vector3.zero,
                    recolieRepositionSharpness * Time.deltaTime);
                accumulateRecoil = weaponRecoilLocalPosition;
            }
        }

        //???? Fov ?? ????: ????, ????
        private void SetFov(float fov)
        {
            playerCharacterController.PlayerCamera.fieldOfView = fov;
            weaponCamera.fieldOfView = fov * weaponFovMultiplier;
        }

        //???? ????? ???? ????: ??????? ????, Fov?? ????
        void UpdateWeaponAiming()
        {
            //???? ??? ???????? ???? ????
            if (weaponSwithState == WeaponSwithState.Up)
            {
                WeaponController activeWeapon = GetActiveWeapon();

                if (IsAiming && activeWeapon)    //?????: ????? -> Aiming ????? ???, fov: ????? -> aimZoomRatio
                {
                    weaponMainLocalPosition = Vector3.Lerp(weaponMainLocalPosition,
                        aimingWeaponPosition.localPosition + activeWeapon.aimOffset,
                        aimingAnimationSpeed * Time.deltaTime);

                    //???? ??? ????
                    if(isScopeOn)
                    {
                        //weaponMainLocalPosition, ????????????? ????? ?????
                        float dist = Vector3.Distance(weaponMainLocalPosition, aimingWeaponPosition.localPosition + activeWeapon.aimOffset);
                        if(dist < distanceOnScope)
                        {
                            OnScopedWeapon?.Invoke();
                            isScopeOn = false;
                        }
                    }
                    else
                    {
                        float fov = Mathf.Lerp(playerCharacterController.PlayerCamera.fieldOfView,
                            activeWeapon.aimZoomRatio * defaultFov, aimingAnimationSpeed * Time.deltaTime);
                        SetFov(fov);
                    }
                }
                else            //?????? ???????: Aiming ??? -> ????? ????? ??? fov: aimZoomRatio -> default
                {
                    weaponMainLocalPosition = Vector3.Lerp(weaponMainLocalPosition,
                        defaultWeaponPostion.localPosition,
                        aimingAnimationSpeed * Time.deltaTime);
                    float fov = Mathf.Lerp(playerCharacterController.PlayerCamera.fieldOfView,
                        defaultFov, aimingAnimationSpeed * Time.deltaTime);
                    SetFov(fov);
                }
            }
        }

        //????? ???? ???? ??? ?? ?????
        void UpdateWeaponBob()
        {
            if(Time.deltaTime > 0)
            {
                //?°¿???? ?? ????????? ????? ???
                //playerCharacterController.transform.position - lastCharacterPosition
                //???? ????????? ?°¿???? ??? ???
                Vector3 playerCharacterVelocity =
                    (playerCharacterController.transform.position - lastCharacterPosition)/Time.deltaTime;

                float charactorMovementFactor = 0f;
                if(playerCharacterController.IsGrounded)
                {
                    charactorMovementFactor = Mathf.Clamp01(playerCharacterVelocity.magnitude /
                        (playerCharacterController.MaxSpeedOnGround * playerCharacterController.SprintSpeedModifier));
                }

                //????? ???? ??? ???
                weaponBobFactor = Mathf.Lerp(weaponBobFactor, charactorMovementFactor, bobSharpness * Time.deltaTime);

                //?????(?????, ????)
                float bobAmount = IsAiming ? aimingBobAmount : defaultBobAmount;
                float frequency = bobFrequency;
                //?¢Ø? ???
                float hBobValue = Mathf.Sin(Time.time * frequency) * bobAmount * weaponBobFactor;
                //????? ??? (?¢Ø? ????? ????)
                float vBobValue = ((Mathf.Sin(Time.time * frequency) * 0.5f) + 0.5f) * bobAmount * weaponBobFactor;

                //??? ???? ?????? ????
                weaponBobLocalPosition.x = hBobValue;
                weaponBobLocalPosition.y = Mathf.Abs(vBobValue);
                //Debug.Log($"weaponBobLocalPosition: {weaponBobLocalPosition}");

                //?°¿?????? ???? ???????? ?????? ????? ????
                lastCharacterPosition = playerCharacterController.transform.position;
            }
        }

        //???¢Ø? ???? ???? ????
        void UpdateWeaponSwitching()
        {
            //Lerp ????
            float switchingTimeFactor = 0f;
            if (weaponSwitchDelay == 0f)
            {
                switchingTimeFactor = 1f;
            }
            else
            {
                switchingTimeFactor = Mathf.Clamp01((Time.time - weaponSwitchTimeStarted) / weaponSwitchDelay);
            }

            //?????©£????? ???? ???? ????
            if (switchingTimeFactor >= 1f)
            {
                if (weaponSwithState == WeaponSwithState.PutDownPrvious)
                {
                    //???Õ®?? false, ???•Ô? ???? true
                    WeaponController oldWeapon = GetActiveWeapon();
                    if (oldWeapon != null)
                    {
                        oldWeapon.ShowWeapon(false);
                    }

                    ActiveWeaponIndex = weaponSwitchNewIndex;
                    WeaponController newWeapon = GetActiveWeapon();
                    OnSwitchToWeapon?.Invoke(newWeapon);

                    switchingTimeFactor = 0f;
                    if (newWeapon != null)
                    {
                        weaponSwitchTimeStarted = Time.time;
                        weaponSwithState = WeaponSwithState.PutUpNew;
                    }
                    else
                    {
                        weaponSwithState = WeaponSwithState.Down;
                    }
                }
                else if (weaponSwithState == WeaponSwithState.PutUpNew)
                {
                    weaponSwithState = WeaponSwithState.Up;
                }
            }

            //?????©£????? ?????? ??? ???
            if (weaponSwithState == WeaponSwithState.PutDownPrvious)
            {
                weaponMainLocalPosition = Vector3.Lerp(defaultWeaponPostion.localPosition, downWeaponPostion.localPosition, switchingTimeFactor);
            }
            else if (weaponSwithState == WeaponSwithState.PutUpNew)
            {
                weaponMainLocalPosition = Vector3.Lerp(downWeaponPostion.localPosition, defaultWeaponPostion.localPosition, switchingTimeFactor);
            }
        }

        //weaponSlots?? ???? ?????????? ?????? WeaponController ??????? ???
        public bool AddWeapon(WeaponController weaponPrefab)
        {
            //?????? ???? ???? ???? ?? - ??????
            if (HasWeapon(weaponPrefab) != null)
            {
                Debug.Log("Has Same Weapon");
                return false;
            }

            for (int i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i] == null)
                {
                    WeaponController weaponInstance = Instantiate(weaponPrefab, weaponParentSocket);
                    weaponInstance.transform.localPosition = Vector3.zero;
                    weaponInstance.transform.localRotation = Quaternion.identity;

                    weaponInstance.Owner = this.gameObject;
                    weaponInstance.SourcePrefab = weaponPrefab.gameObject;
                    weaponInstance.ShowWeapon(false);

                    //????????
                    OnAddedWeapon?.Invoke(weaponInstance, i);

                    weaponSlots[i] = weaponInstance;
                    return true;
                }
            }

            Debug.Log("weaponSlots full");
            return false;
        }

        //weaponSlots?? ?????? ???? ????
        public bool RemoveWeapon(WeaponController oldWeapon)
        {
            for (int i = 0;  i < weaponSlots.Length; i++)
            {
                //???? ???? ???? ????
                if (weaponSlots[i] == oldWeapon)
                {
                    //????
                    weaponSlots[i] = null;

                    OnRemoveWeapon?.Invoke(oldWeapon, i);

                    Destroy(oldWeapon.gameObject);

                    //???? ????? ???? ???????? ???•Ô? ????? ???? ??¢•?
                    if(i == ActiveWeaponIndex)
                    {
                        SwitchWeapon(true);
                    }
                    return true;
                }
            }

            return false;
        }


        //????????? ???? ?????n???? ???? ???? ????? ??
        private WeaponController HasWeapon(WeaponController weaponPrefab)
        {
            for (int i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i] != null && weaponSlots[i].SourcePrefab == weaponPrefab)
                {
                    return weaponSlots[i];
                }
            }

            return null;
        }

        public WeaponController GetActiveWeapon()
        {
            return GetWeaponAtSlotIndex(ActiveWeaponIndex);
        }

        //?????? ????? ???? ????? ????
        public WeaponController GetWeaponAtSlotIndex(int index)
        {
            if (index >= 0 && index < weaponSlots.Length)
            {
                return weaponSlots[index];
            }

            return null;
        }

        //0~9  
        //???? ????, ???? ??? ??? ???? false, ???•Ô? ???? true
        public void SwitchWeapon(bool ascendingOrder)
        {
            int newWeaponIndex = -1;    //???? ??????? ???? ?•Â???
            int closestSlotDistance = weaponSlots.Length;
            for (int i = 0; i < weaponSlots.Length; i++)
            {
                if (i != ActiveWeaponIndex && GetWeaponAtSlotIndex(i) != null)
                {
                    int distanceToActiveIndex = GetDistanceBetweenWeaponSlot(ActiveWeaponIndex, i, ascendingOrder);
                    if (distanceToActiveIndex < closestSlotDistance)
                    {
                        closestSlotDistance = distanceToActiveIndex;
                        newWeaponIndex = i;
                    }
                }
            }

            //???? ??????? ???? ?•Â????? ???? ???
            SwitchToWeaponIndex(newWeaponIndex);
        }

        private void SwitchToWeaponIndex(int newWeaponIndex)
        {
            //newWeaponIndex ?? ??
            if (newWeaponIndex >= 0 && newWeaponIndex != ActiveWeaponIndex)
            {
                weaponSwitchNewIndex = newWeaponIndex;
                weaponSwitchTimeStarted = Time.time;

                //???? ??????? ???? ??????
                if (GetActiveWeapon() == null)
                {
                    weaponMainLocalPosition = downWeaponPostion.position;
                    weaponSwithState = WeaponSwithState.PutUpNew;
                    ActiveWeaponIndex = newWeaponIndex;

                    WeaponController weaponController = GetWeaponAtSlotIndex(newWeaponIndex);
                    OnSwitchToWeapon?.Invoke(weaponController);
                }
                else
                {
                    weaponSwithState = WeaponSwithState.PutDownPrvious;
                }
            }
        }

        //????? ???
        private int GetDistanceBetweenWeaponSlot(int fromSlotIndex, int toSlotIndex, bool ascendingOrder)
        {
            int distanceBetweenSlots = 0;

            if (ascendingOrder)
            {
                distanceBetweenSlots = toSlotIndex - fromSlotIndex;
            }
            else
            {
                distanceBetweenSlots = fromSlotIndex - toSlotIndex;
            }

            if (distanceBetweenSlots < 0)
            {
                distanceBetweenSlots = distanceBetweenSlots + weaponSlots.Length;
            }

            return distanceBetweenSlots;
        }

        void OnWeaponSwitched(WeaponController newWeapon)
        {
            if (newWeapon != null)
            {
                newWeapon.ShowWeapon(true);
            }
        }

        void OnScope()
        {
            weaponCamera.enabled = false;
        }

        void OffScope()
        {
            weaponCamera.enabled = true;
        }
    }
}