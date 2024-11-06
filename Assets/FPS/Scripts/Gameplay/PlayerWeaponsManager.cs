using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.FPS.Game;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// 무기 교체 상태
    /// </summary>
    public enum WeaponSwitchState
    {
        Up,
        Down,
        PutDownPrvious,
        PutUpNew
    }

    /// <summary>
    /// 플레이어가 가진 무기(WeaponController)들을 관리하는 클래스
    /// </summary>
    public class PlayerWeaponsManager : MonoBehaviour
    {
        #region Variables
        // 무기 지급 - 게임을 시작할 때 처음 유저에게 지급되는 무기 리스트
        public List<WeaponController> startingWeapons = new List<WeaponController>();

        // 무기 장착
        // 무기를 장착하는 오브젝트
        public Transform weaponParentSocket;

        // 플레이어가 게임중에 들고 다니는 무기 리스트
        private WeaponController[] weaponSlots = new WeaponController[9];

        // 무기 리스트(슬롯)중 활성화된 무기를 관리하는 인덱스
        public int ActiveWeaponIndex {get; private set;} 

        // 무기 교체
        public UnityAction<WeaponController> OnSwitchToWeapon;  // 무기 교체시 등록된 함수 호출

        private WeaponSwitchState weaponSwitchState;        // 무기 교체시 상태

        private PlayerInputHandler playerInputHandler;

        //무기 교체시 계산되는 최종 위치
        private Vector3 weaponMainLocalPosition;

        public Transform defaultWeaponPostion;
        public Transform downWeaponPostion;
        public Transform aimWeaponPostion;

        private int weaponSwitchNewIndex;       //새로 바뀌는 무기 인덱스

        private float weaponSwitchTimeStarted = 0f;
        [SerializeField] private float weaponSwitchDelay = 1f;

        // 적 포착
        public bool IsPointingAtEnemy {get; private set;}    // 적 포착 여부
        public Camera weaponCamera;                          // weaponCamera에서 Ray로 적 포착

        // 조준
        // 카메라 세팅
        private PlayerCharacterController playerCharacterController;
        public float defaultFov = 60f;          // 기본 카메라 시야
        public float weaponFovMultiplier = 1f; // 무기 조준 시 카메라 시야 조절 계수

        public bool IsAiming {get; private set;}
        [SerializeField] private float aimingAnimationSpeed = 10f;

        // 흔들림
        [SerializeField] private float bobFrequency = 10f;
        [SerializeField] private float bobSharpness = 10f;
        [SerializeField] private float defaultBobAmount = 0.05f; // 평상시 흔들림 량
        [SerializeField] private float aimingBobAmount = 0.02f; // 조준시 흔들림 량

        private float weaponBobFactor;  // 흔들림 계수
        private Vector3 lastCharacterPosition;  // 현재 프레임에서의 이동속도를 구하기 위한 변수

        private Vector3 weaponBobLocalPosition;  // 이동시 흔들린 량 최종 계산값, 이동하지 않으면 0

        #endregion

        void Start()
        {
            // 참조
            playerInputHandler = GetComponent<PlayerInputHandler>();
            playerCharacterController = GetComponent<PlayerCharacterController>();

            // 초기화
            ActiveWeaponIndex = -1;
            weaponSwitchState = WeaponSwitchState.Down;

            // 액티브 무기 show 함수 등록
            OnSwitchToWeapon += OnWeaponSwitched;

            // FOV 초기화
            SetFov(defaultFov);

            // 지급 받은 무기 장착
            foreach (var weapon in startingWeapons)
            {
                AddWeapon(weapon);
            }
            SwitchWeapon(true);
        }

        void Update()
        {
            // 현재 액티브 무기
            WeaponController activeWeapon = GetActiveWeapon();

            // 조준 입력값 처리
            IsAiming = playerInputHandler.GetAimInputHeld();

            if(!IsAiming && (weaponSwitchState == WeaponSwitchState.Up || weaponSwitchState == WeaponSwitchState.Down))
            {
                int switchWeaponInput = playerInputHandler.GetSwitchWeaponInput();
                if(switchWeaponInput != 0f)
                {
                    bool switchUp = switchWeaponInput > 0f;
                    SwitchWeapon(switchUp);
                }
            }

            // 적 포착
            IsPointingAtEnemy = false;
            if(activeWeapon)
            {
                RaycastHit hit;
                if(Physics.Raycast(weaponCamera.transform.position, weaponCamera.transform.forward, out hit, 300f))
                {
                    Health health = hit.collider.GetComponent<Health>();
                    if(health)
                    {
                        IsPointingAtEnemy = true;
                    }
                }
            }
        }

        void LateUpdate()
        {
            UpdateWeaponBob();
            UpdateWeaponAiming();
            UpdateWeaponSwitching();

            // 무기 최종 위치
            weaponParentSocket.localPosition = weaponMainLocalPosition + weaponBobLocalPosition;
        }

        // 카메라 FOV 값 설정 줌 인, 줌 아웃
        private void SetFov(float fov)
        {
            playerCharacterController.PlayerCamera.fieldOfView = fov;
            weaponCamera.fieldOfView = fov * weaponFovMultiplier;
        }

        // 무기 조준에 따른 연출
        void UpdateWeaponAiming()
        {
            // 무기를 들고 있을 때만 조준
            if(weaponSwitchState == WeaponSwitchState.Up)
            {
                WeaponController activeWeapon = GetActiveWeapon();

                if(IsAiming && activeWeapon) // 조준시: 디폴트 -> Aiming 위치로 이동
                {
                    weaponMainLocalPosition = Vector3.Lerp(weaponMainLocalPosition, 
                        aimWeaponPostion.localPosition + activeWeapon.aimPositionOffset, 
                        aimingAnimationSpeed * Time.deltaTime);
                    float fov = Mathf.Lerp(playerCharacterController.PlayerCamera.fieldOfView, defaultFov * activeWeapon.aimZoomRatio, aimingAnimationSpeed * Time.deltaTime);
                    SetFov(fov);
                }
                else // 조준 아닐시: Aiming 위치 -> 디폴트 위치로 이동
                {
                    weaponMainLocalPosition = Vector3.Lerp(weaponMainLocalPosition, 
                        defaultWeaponPostion.localPosition, 
                        aimingAnimationSpeed * Time.deltaTime);

                    float fov = Mathf.Lerp(playerCharacterController.PlayerCamera.fieldOfView, defaultFov, aimingAnimationSpeed * Time.deltaTime);
                    SetFov(fov);
                }
            }
        }

        // 이동에 의한 무기 흔들린 값 구하기
        void UpdateWeaponBob()
        {
            if(Time.timeScale > 0f)
            {
                // 플레이어가 한 프레임동안 이동한 거리
                // playerCharacterController.transform.position - lastCharacterPosition
                // 현재 프레임에서 플레이어 이동 속도
                Vector3 playerCharacterVelocity = (playerCharacterController.transform.position - lastCharacterPosition) / Time.deltaTime;

                // 흔들림 계수
                float charactorMovementFactor = 0f;
                if(playerCharacterController.IsGrounded)
                {
                    charactorMovementFactor = Mathf.Clamp01(playerCharacterVelocity.magnitude / 
                        (playerCharacterController.MaxSpeedOnGround * playerCharacterController.SprintSpeedModifier));
                }

                //속도에 의한 흔들림 계수
                weaponBobFactor = Mathf.Lerp(weaponBobFactor, charactorMovementFactor, bobSharpness * Time.deltaTime);

                // 흔들림 량(조준시, 평상시)
                float bobAmout = IsAiming ? aimingBobAmount : defaultBobAmount;
                float frequency = bobFrequency;
                // 좌우 흔들림
                float vBobValue = Mathf.Sin(frequency * Time.time) * bobAmout * weaponBobFactor;
                float hBobValue = ((Mathf.Sin(frequency * Time.time) * 0.5f) + 0.5f) * bobAmout * weaponBobFactor;

                // 흔들림 최종 계산값
                weaponBobLocalPosition.x = hBobValue;
                weaponBobLocalPosition.y = Mathf.Abs(vBobValue);

                // 플레이어의 현재 프레임의 마지막 위치를 저장
                lastCharacterPosition = playerCharacterController.transform.position;
            }
        }

        // 상태에 따른 무기 연출
        void UpdateWeaponSwitching()
        {
            //Lerp 변수
            float switchingTimeFactor = 0f;
            if(weaponSwitchDelay == 0f)
            {
                switchingTimeFactor = 1f;
            }
            else
            {
                switchingTimeFactor = Mathf.Clamp01((Time.time - weaponSwitchTimeStarted) / weaponSwitchDelay);
            }

            // 지연시간 이후 무기 상태 바꾸기 연출
            if(switchingTimeFactor >= 1f)
            {
                if(weaponSwitchState == WeaponSwitchState.PutDownPrvious)
                {
                    // 현재 무기 false, 새로운 무기 true
                    WeaponController oldWeapon = GetActiveWeapon();
                    if(oldWeapon != null)
                    {
                        oldWeapon.ShowWeapon(false);
                    }

                    ActiveWeaponIndex = weaponSwitchNewIndex;
                    WeaponController newWeapon = GetActiveWeapon();
                    OnSwitchToWeapon?.Invoke(newWeapon);
                    // newWeapon.ShowWeapon(true);


                    switchingTimeFactor = 0f;
                    if(newWeapon != null)
                    {
                        weaponSwitchTimeStarted = Time.time;
                        weaponSwitchState = WeaponSwitchState.PutUpNew;
                    }
                    else
                    {
                        weaponSwitchState = WeaponSwitchState.Down;
                    }
                }
                else if(weaponSwitchState == WeaponSwitchState.PutUpNew)
                {
                    weaponSwitchState = WeaponSwitchState.Up;
                }
            }

            // 지연 시간동안 무기 교체 연출
            if(weaponSwitchState == WeaponSwitchState.PutDownPrvious)
            {
                weaponMainLocalPosition = Vector3.Lerp(defaultWeaponPostion.localPosition, downWeaponPostion.localPosition, switchingTimeFactor);
            }
            else if (weaponSwitchState == WeaponSwitchState.PutUpNew)
            {
                weaponMainLocalPosition = Vector3.Lerp(downWeaponPostion.localPosition, defaultWeaponPostion.localPosition, switchingTimeFactor);
            }
        }

        // weaponSlots에 무기 프리팹으로 생성한 WeaponController 오브젝트 추가
        public bool AddWeapon(WeaponController weaponPrefab)
        {
            // 추가하는 무기 소지 여부 제크 - 중복 검사
            if(HasWeapon(weaponPrefab) != null)
            {
                Debug.Log("Has Same weapon");
                return false;
            }

            for(int i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i] == null)
                {
                    WeaponController weaponInstance = Instantiate(weaponPrefab, weaponParentSocket);
                    weaponInstance.transform.localPosition = Vector3.zero;
                    weaponInstance.transform.localRotation = Quaternion.identity;

                    weaponInstance.Owner = this.gameObject;
                    weaponInstance.SourcePrefab = weaponPrefab.gameObject;
                    weaponInstance.ShowWeapon(false);

                    weaponSlots[i] = weaponInstance;

                    return true;
                }
            }
            Debug.Log("weaponSlots full");
            return false;
        }

        // 매개변수로 들어온 무기와 슬롯의 무기를 비교하여 중복되지 않도록 하는 함수
        private WeaponController HasWeapon(WeaponController weaponPrefab)
        {
            for(int i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i] != null && weaponSlots[i].SourcePrefab == weaponPrefab)
                {
                    return weaponSlots[i];
                }
            }
            return null;
        }

        // 현재 활성화된 무기를 구하는 함수
        public WeaponController GetActiveWeapon()
        {
            return GetWeaponAtSlotIndex(ActiveWeaponIndex);
        }

        //지정된 슬롯에 무기가 있는지 여부
        public WeaponController GetWeaponAtSlotIndex(int index)
        {
            if(index >= 0 && index < weaponSlots.Length)
            {
                return weaponSlots[index];
            }
            return null;
        }

        // 0 ~ 9 - 0, 1, 2
        // 무기 바꾸기 - 현재 들고 있는 무기 false, 새로운 무기 true
        public void SwitchWeapon(bool ascendingOrder)
        {
            int newWeaponIndex = -1;    // 새로 액티브할 무기 인덱스
            int closestSoltDistance = weaponSlots.Length;
            for(int i = 0; i < weaponSlots.Length; i++)
            {
                if(i != ActiveWeaponIndex && GetWeaponAtSlotIndex(i) != null)
                {
                    int distanceToActiveIndex = GetDistanceBetweenWeaponSlot(ActiveWeaponIndex, i, ascendingOrder);
                    if(distanceToActiveIndex < closestSoltDistance)
                    {
                        closestSoltDistance = distanceToActiveIndex;
                        newWeaponIndex = i;
                    }
                }
            }

            // 새로 액티브할 무기 인덱스로 무기 교체
            SwitchToWeaponIndex(newWeaponIndex);
        }

        // 새로 액티브할 무기 인덱스로 무기 교체 함수
        private void SwitchToWeaponIndex(int newWeaponIndex)
        {
            //newWeaponIndex 값 체크
            if(newWeaponIndex >= 0 && newWeaponIndex != ActiveWeaponIndex)
            {
                weaponSwitchNewIndex = newWeaponIndex;
                weaponSwitchTimeStarted = Time.time;

                // 현재 Active한 무기가 있는지 체크
                if(GetActiveWeapon() == null)
                {
                    weaponMainLocalPosition = downWeaponPostion.position;
                    weaponSwitchState = WeaponSwitchState.PutUpNew;
                    ActiveWeaponIndex = newWeaponIndex;

                    WeaponController weaponController = GetWeaponAtSlotIndex(newWeaponIndex);
                    OnSwitchToWeapon?.Invoke(weaponController);
                    // weaponController.ShowWeapon(true);
                }
                else
                {
                    weaponSwitchState = WeaponSwitchState.PutDownPrvious;
                }

                // =========================================================
                // 삭제한 코드
                // if(ActiveWeaponIndex >= 0)
                // {
                //     WeaponController nowWeapon = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                //     nowWeapon.ShowWeapon(false);
                // }

                // WeaponController newWeapon = GetWeaponAtSlotIndex(newWeaponIndex);
                // newWeapon.ShowWeapon(true);
                // ActiveWeaponIndex = newWeaponIndex;
                // =========================================================

            }
        }

        // 슬롯간 거리
        private int GetDistanceBetweenWeaponSlot(int fromSlotIndex, int toSlotIndex, bool ascendingOrder)
        {
            int distanceBetweenSlot = 0;

            if(ascendingOrder)
            {
                distanceBetweenSlot = toSlotIndex - fromSlotIndex;
            }
            else
            {
                distanceBetweenSlot = fromSlotIndex - toSlotIndex;
            }

            if(distanceBetweenSlot < 0)
            {
                distanceBetweenSlot = distanceBetweenSlot + weaponSlots.Length;
            }

            return distanceBetweenSlot;
        }

        void OnWeaponSwitched(WeaponController newWeapon)
        {
            if(newWeapon != null)
            {
                newWeapon.ShowWeapon(true);
            }
        }
    }
}
