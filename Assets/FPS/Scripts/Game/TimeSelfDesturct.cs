using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// TimeSelfDestruct 부착한 게임 오브젝트는 지정된 시간에 파괴
    /// </summary>
    public class TimeSelfDesturct : MonoBehaviour
    {
        #region Variables
        public float lifeTime = 1f;
        private float spawnTime; // 생성될 때의 시간
        #endregion

        private void Awake()
        {
            spawnTime = Time.time;
        }
    
        private void Update()
        {
            if(spawnTime + lifeTime <= Time.time)
            {
                Destroy(gameObject);
            }
        }

        // private void OnEnable()
        // {
        //     Destroy(this.gameObject, 5f);
        // }
    }
}
