using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    public class FeedBackFlashHUD : MonoBehaviour
    {
        #region Variables
        private Health playerHealth;

        public Image flashImage;
        public CanvasGroup flashCanvasGroup;

        public Color DamageFlashColor;
        public Color HealFlashColor;

        [SerializeField] private float flashDuration = 1f;
        [SerializeField] private float flashMaxAlpha = 1f;

        private bool flashActive = false;
        private float lastTimeFlashStarted = Mathf.NegativeInfinity;
        #endregion

        private void Start()
        {
            PlayerCharacterController playerCharacterController 
                = GameObject.FindObjectOfType<PlayerCharacterController>();

            playerHealth = playerCharacterController.GetComponent<Health>();
            playerHealth.OnDamaged += OnDamaged;
            playerHealth.OnHeal += OnHeal;

        }

        private void Update()
        {
            if(flashActive)
            {
                float normalizedTimeSinceDamage = (Time.time - lastTimeFlashStarted) / flashDuration;
                if(normalizedTimeSinceDamage < 1f)
                {
                    float flashAmount = flashMaxAlpha * (1f - normalizedTimeSinceDamage);
                    flashCanvasGroup.alpha = flashAmount;
                }
                else
                {
                    flashCanvasGroup.gameObject.SetActive(false);
                    flashActive = false;
                }
            }
        }

        void ResetFlash()
        {
            flashActive = true;
            lastTimeFlashStarted = Time.time;
            flashCanvasGroup.alpha = 0f;
            flashCanvasGroup.gameObject.SetActive(true);
            
        }

        private void OnDamaged(float damage, GameObject damageSource)
        {
            ResetFlash();
            flashImage.color = DamageFlashColor;
        }

        //힐 할때 힐 플래시 시작
        private void OnHeal(float amount)
        {
            ResetFlash();
            flashImage.color = HealFlashColor;
        }

    }
}
