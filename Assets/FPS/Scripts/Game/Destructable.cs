using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// 죽었을 때 Health를 가진 오브젝트를 킬하는 클래스
    /// </summary>
    public class Destructable : MonoBehaviour
    {
        #region Variables
        private Health health;
        #endregion

        void Start()
        {
            health = GetComponent<Health>();
            DebugUtility.HandleErrorIfNullGetComponent<Health, Destructable>(health, this, gameObject);

            // UnityAction 함수 등록
            health.OnDie += OnDie;
            health.OnDamaged += OnDamaged;
        }

        void OnDamaged(float damage, GameObject damageSource)
        {
            //ToDo: 데미지 효과 구현 
        }

        // 죽었을 때 호출되는 함수
        void OnDie()
        {
            Destroy(gameObject);
        }
    }
}
