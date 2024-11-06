using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// 크로스 헤어 데이터
    /// </summary>
    [System.Serializable]
    public struct CrossHairData
    {
        public Sprite CrossHairSprite;
        public float CrossHairSize;
        public Color CrossHairColor;
    }

    /// <summary>
    /// 무기(총기)를 관리하는 클래스
    /// </summary>
    public class WeaponController : MonoBehaviour
    {   
        #region Variables
        // 무기 활성화, 비활성화
        public GameObject weaponRoot;

        public GameObject Owner { get; set; }               // 무기의 주인
        public GameObject SourcePrefab { get; set; }        // 무기를 생성한 오리지널 프리팹
        public bool IsWeaponActive { get; private set; }    // 무기 활성화 상태 여부

        private AudioSource shootAudioSource;
        public AudioClip switchWeaponSfx;

        //CrossHair
        public CrossHairData crossHairDefault;              // 기본, 평상시
        public CrossHairData crossHairTargetInSight;        // 적을 포착했을 때, 타겟팅

        // 조준
        public float aimZoomRatio = 1f;                     // 조준시 줌인 설정값
        public Vector3 aimPositionOffset;                   // 조준시 위치 오프셋
        #endregion

        void Awake()
        {
            //참조
            shootAudioSource = this.GetComponent<AudioSource>();
        }
    
        // 무기 활성화, 비활성화
        public void ShowWeapon(bool show)
        {
            weaponRoot.SetActive(show);

            // this 무기로 변경
            if(show == true && switchWeaponSfx != null)
            {
                // 무기 변경 효과음 플레이
                shootAudioSource.PlayOneShot(switchWeaponSfx);
            }

            IsWeaponActive = show;
        }
    }
}
