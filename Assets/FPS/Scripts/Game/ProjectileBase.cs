using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Game
{
    /// <summary>
    /// 발사체의 기본이 되는 부모 클래스
    /// </summary>
    public abstract class ProjectileBase : MonoBehaviour
    {
        #region Variables
        public GameObject Owner { get; private set; } // 발사한 주체
        public Vector3 InitialPosition { get; private set; } // 발사체의 초기 위치
        public Vector3 InitialDirection { get; private set; } // 발사체의 초기 방향값
        public Vector3 InheritedMuzzleVelocity { get; private set; } // 발사체에 상속된 총구 속도
        public float InitialCharge { get; private set; } // 발사체의 초기 충전량

        public UnityEvent OnShoot; // 발사 이벤트
        #endregion

        public void Shoot(WeaponController controller)
        {
            Owner = controller.Owner;
            InitialPosition = this.transform.position;
            InitialDirection = this.transform.forward;
            InheritedMuzzleVelocity = controller.MuzzleWorldVelocity;
            InitialCharge = controller.CurrentCharge;

            OnShoot?.Invoke();
        }
    }

}
