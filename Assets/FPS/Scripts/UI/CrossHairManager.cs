using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    public class CrossHairManager : MonoBehaviour
    {
        #region Variables
        public Image crossHairImage;                        // 크로스헤어 이미지
        public Sprite defaultCrossHairSprite;               // 액티브한 무기가 없을 때 크로스헤어 이미지

        private RectTransform crossHairRectTransform;

        private CrossHairData crossHairDefault;             // 평상시 크로스헤어 데이터
        private CrossHairData crossHairTarget;              // 타겟팅 크로스헤어 데이터 

        private CrossHairData crossHairCurrent;             // 실제 크로스헤어 데이터
        [SerializeField] private float crossHairUpdateSharpness = 5.0f;      // Lerp 변수

        private PlayerWeaponsManager playerWeaponsManager;

        private bool wasPointingAtEnemy;
        #endregion

        void Start()
        {
            //참조
            playerWeaponsManager = GameObject.FindObjectOfType<PlayerWeaponsManager>();
            // 액티브한 무기 크로스 헤어 보이기
            OnWeaponChanged(playerWeaponsManager.GetActiveWeapon());
            // 무기 바뀔 때 마다 크로스헤어 이미지 업데이트 이벤트 등록
            playerWeaponsManager.OnSwitchToWeapon += OnWeaponChanged;
        }

        void Update()
        {
            UpdateCrossHairPointingAtEnemy(false);

            wasPointingAtEnemy = playerWeaponsManager.IsPointingAtEnemy;
        }

        // 크로스 헤어 그리기
        void UpdateCrossHairPointingAtEnemy(bool force)
        {
            if(crossHairDefault.CrossHairSprite == null)
                return;

            // 평상시?, 타겟팅?
            if((force || wasPointingAtEnemy == false) && playerWeaponsManager.IsPointingAtEnemy == true) // 적 포착 시작
            {
                crossHairCurrent = crossHairTarget;
                crossHairImage.sprite = crossHairCurrent.CrossHairSprite;
                crossHairRectTransform.sizeDelta = crossHairCurrent.CrossHairSize * Vector2.one;
            }
            else if((force || wasPointingAtEnemy == true) && playerWeaponsManager.IsPointingAtEnemy == false) // 적 포착 중 적 포착 끊어짐
            {
                crossHairCurrent = crossHairDefault;
                crossHairImage.sprite = crossHairCurrent.CrossHairSprite;
                crossHairRectTransform.sizeDelta = crossHairCurrent.CrossHairSize * Vector2.one;
            }

            crossHairImage.color = Color.Lerp(crossHairImage.color, crossHairCurrent.CrossHairColor, 
                crossHairUpdateSharpness * Time.deltaTime);

            crossHairRectTransform.sizeDelta = Mathf.Lerp(crossHairRectTransform.sizeDelta.x, crossHairCurrent.CrossHairSize,
                crossHairUpdateSharpness * Time.deltaTime) * Vector2.one;
        }

        // 무기가 바뀔 때 마다 crosshairImage를 각각의 무기 CrossHair이미지로 바꾸기
        public void OnWeaponChanged(WeaponController newWeapon)
        {
            if(newWeapon)
            {
                // 크로스헤어 이미지 업데이트
                crossHairImage.enabled = true;
                crossHairRectTransform = crossHairImage.GetComponent<RectTransform>();

                // 크로스헤어 데이터 업데이트
                crossHairDefault = newWeapon.crossHairDefault;
                crossHairTarget = newWeapon.crossHairTargetInSight;
            }
            else
            {
                if(defaultCrossHairSprite)
                {
                    crossHairImage.sprite = defaultCrossHairSprite;
                }
                else
                {
                    crossHairImage.enabled = false;
                }
            }

            UpdateCrossHairPointingAtEnemy(true);
        }
    }
}
