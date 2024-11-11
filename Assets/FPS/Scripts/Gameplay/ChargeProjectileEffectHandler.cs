using UnityEngine;
using Unity.FPS.Game;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// 충전용 발사체를 발사할 때 충전량에 발사체의 게임오브젝트 크기 결정
    /// </summary>
    public class ChargeProjectileEffectHandler : MonoBehaviour
    {
        #region Variables
        ProjectileBase projectileBase;
        
        public GameObject chargeObject;
        public MinMaxVector3 scale;
        #endregion

        void OnEnable()
        {
            projectileBase = GetComponent<ProjectileBase>();
            projectileBase.OnShoot += OnShoot;
        }

        void OnShoot()
        {
            chargeObject.transform.localScale = scale.GetValueFromRatio(projectileBase.InitialCharge);
        }
    }
}
