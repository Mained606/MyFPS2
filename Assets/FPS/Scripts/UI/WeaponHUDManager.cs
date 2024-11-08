using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;

namespace Unity.FPS.UI
{
    public class WeaponHUDManager : MonoBehaviour
    {
        #region Variables
        public RectTransform ammoPanel;
        public GameObject ammoCountPrefab;

        private PlayerWeaponsManager playerWeaponsManager;
        #endregion

        void Awake()
        {
            //참조
            playerWeaponsManager = GameObject.FindObjectOfType<PlayerWeaponsManager>();
            //


            playerWeaponsManager.OnAddedWeapon += AddWeapon;
            playerWeaponsManager.OnRemoveWeapon += RemoveWeapon;
            playerWeaponsManager.OnSwitchToWeapon += SwitchWeapon;
        }

        // 무기 추가하면 ammoUI 하나 추가
        void AddWeapon(WeaponController newWeapon, int weaponIndex)
        {
            GameObject ammoCountGo = Instantiate(ammoCountPrefab, ammoPanel);
            AmmoCount amooCount = ammoCountGo.GetComponent<AmmoCount>();
            amooCount.Initialzie(newWeapon, weaponIndex);
        }

        // 무기 제거하면 ammoUI 하나 제거

        void RemoveWeapon(WeaponController oldWeapon, int weaponIndex)
        {
            
        }

        //
        void SwitchWeapon(WeaponController weapon)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(ammoPanel);
        }
    }
}
