using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.AI
{
    /// <summary>
    /// Enemy 리스트를 관리하는 클래스
    /// </summary>
    public class EnemyManager : MonoBehaviour
    {
        #region Variables
        public List<EnemyController> Enemies { get; private set; }
        public int NumberOfEnemiesTotal { get; private set; }
        public int NumberOfEnemiesRemaining => Enemies.Count;   //현재 살아있는 

        #endregion
        void Awake()
        {
            Enemies = new List<EnemyController>();
        }

        //등록
        public void RegisterEnemy(EnemyController newEnemy)
        {
            Enemies.Add(newEnemy);

            NumberOfEnemiesTotal++;
        }

        //제거
        public void RemoveEnemy(EnemyController killedEnemy)
        {
            Enemies.Remove(killedEnemy);
        }
    }
}
