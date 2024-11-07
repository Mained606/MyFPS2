using UnityEngine;
using UnityEngine.Events;
using Unity.FPS.Gameplay;

namespace Unity.FPS.UI
{
    public class ScopeUIManager : MonoBehaviour
    {
        #region Variables
        public GameObject scopeUI;
        private PlayerWeaponsManager playerWeaponsManager;
        #endregion

        void Start()
        {
            playerWeaponsManager = FindObjectOfType<PlayerWeaponsManager>();

            playerWeaponsManager.OnScopedWeapon += ShowScopeUI;
            playerWeaponsManager.OffScopedWeapon += HideScopeUI;
        }

        public void ShowScopeUI()
        {
            scopeUI.SetActive(true);
        }

        public void HideScopeUI()
        {
            scopeUI.SetActive(false);
        }
    }
}
