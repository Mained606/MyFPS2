using UnityEngine;
using Unity.FPS.Gameplay;
using Unity.FPS.Game;
using TMPro;
using UnityEngine.UI;


namespace Unity.FPS.UI
{
    /// <summary>
    /// Weapon 무기의 탄약 표시 UI
    /// </summary>
    public class AmmoCount : MonoBehaviour
    {
        #region Variables
        private PlayerWeaponsManager playerWeaponsManager;

        private WeaponController weaponController;
        private int weaponIndex;
        
        //Ui
        public TextMeshProUGUI weaponIndexText;

        public Image ammoFillImage;                             // ammo Rate에 따른 게이지

        [SerializeField] private float ammoFillSharpness = 10f; // 게이지 채우는(비우는) 속도
        private float weaponSwitchSharpness = 10f;              // 무기 교체시 UI가 바뀌는 속도

        public CanvasGroup canvasGroup;
        [SerializeField][Range (0,1)] private float unSelectedOpacity = 0.5f;
        private Vector3 unSelectedScale = Vector3.one * 0.8f;

        // 게이지바 색 변경
        public FillBarColorChange fillBarColorChange;
        #endregion

        //AmmoCount값 초기화
        public void Initialzie(WeaponController weapon, int _weaponIndex)
        {
            weaponController = weapon;
            weaponIndex = _weaponIndex;

            // 무기 인덱스
            weaponIndexText.text = (weaponIndex +1).ToString();

            // 게이지 색 값 초기화
            fillBarColorChange.Initialize(1f, 0.1f);

            //참조
            playerWeaponsManager = GameObject.FindObjectOfType<PlayerWeaponsManager>();
        }

        void Update()
        {
            float currentFillRate = weaponController.CurrentAmmoRatio;
            ammoFillImage.fillAmount = Mathf.Lerp(ammoFillImage.fillAmount, currentFillRate, ammoFillSharpness * Time.deltaTime);

            // 무기 교체에 따른 액티브
            bool isActiveWeapon = (weaponController == playerWeaponsManager.GetActiveWeapon());

            float currentOpacity = isActiveWeapon ? 1.0f : unSelectedOpacity;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, currentOpacity, 
                weaponSwitchSharpness * Time.deltaTime);
            Vector3 currentScale = isActiveWeapon ? Vector3.one : unSelectedScale;
            transform.localScale = Vector3.Lerp(transform.localScale, currentScale,
                weaponSwitchSharpness * Time.deltaTime);

            // 배경색 변경
            fillBarColorChange.UpdateVisual(currentFillRate);
        }

    }
}
