using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.AI
{
    /// <summary>
    /// Enemy ????  
    /// </summary>
    public enum AIState
    {
        Patrol,
        Follow,
        Attack
    }

    /// <summary>
    /// ?????? Enemy?? ??????? ??????? ?????
    /// </summary>
    public class EnemyMobile : MonoBehaviour
    {
        #region Variables
        public Animator animator;
        private EnemyController enemyController;

        public AIState AiState { get; private set; }

        //???
        public AudioClip movementSound;
        public MinMaxFloat pitchMovenemtSpeed;

        private AudioSource audioSource;

        //?????? - ?????
        public ParticleSystem[] randomHitSparks;

        //Detected
        public ParticleSystem[] detectedVfxs;
        public AudioClip detectedSfx;

        //attack
        [Range(0f, 1f)]
        public float attackSkipDistanceRatio = 0.5f;

        //animation parameter
        const string k_AnimAttackParameter = "Attack";
        const string k_AnimMoveSpeedParameter = "MoveSpeed";
        const string k_AnimAlertedParameter = "Alerted";
        const string k_AnimOnDamagedParameter = "OnDamaged";
        const string k_AnimDeathParameter = "Death";
        #endregion

        private void Start()
        {
            //????            
            enemyController = GetComponent<EnemyController>();
            enemyController.Damaged += OnDamaged;
            enemyController.OnDetectedTarget += OnDetected;
            enemyController.OnLostTarget += OnLost;
            enemyController.OnAttack += OnAttacked;

            audioSource = GetComponent<AudioSource>();
            audioSource.clip = movementSound;
            audioSource.Play();

            //????
            AiState = AIState.Patrol;
        }

        private void OnAttacked()
        {
            //애니
            animator.SetTrigger(k_AnimAttackParameter);
        }


        private void Update()
        {
            //???? ????/????
            UpdateAiStateTransition();
            UpdateCurrentAiState();

            //????? ???? ???/???? ???
            float moveSpeed = enemyController.Agent.velocity.magnitude;
            animator.SetFloat(k_AnimMoveSpeedParameter, moveSpeed);         //???
            audioSource.pitch = pitchMovenemtSpeed.GetValueFromRatio(moveSpeed/enemyController.Agent.speed);
        }

        //???¿? ???? Enemy ????
        private void UpdateCurrentAiState()
        {
            switch(AiState)
            {
                case AIState.Patrol:
                    enemyController.UpdatePathDestination(true);
                    enemyController.SetNavDestination(enemyController.GetDestinationOnPath());
                    break;
                case AIState.Follow:
                    enemyController.SetNavDestination(enemyController.KnonwDetectedTarget.transform.position);
                    enemyController.OrientToward(enemyController.KnonwDetectedTarget.transform.position);
                    enemyController.OrientWeaponsToward(enemyController.KnonwDetectedTarget.transform.position);
                    break;
                case AIState.Attack:
                    //일정거리까지는 이동하면서 공격
                    float distance = Vector3.Distance(enemyController.KnonwDetectedTarget.transform.position,
                        enemyController.DetectionModule.detectionSourcePoint.position);
                    
                    if(distance >= enemyController.DetectionModule.attackRange * attackSkipDistanceRatio)
                    {
                        enemyController.SetNavDestination(enemyController.KnonwDetectedTarget.transform.position);
                    }
                    else
                    {
                        enemyController.SetNavDestination(transform.position); // 제자리에 서기
                    }

                    enemyController.OrientToward(enemyController.KnonwDetectedTarget.transform.position);
                    enemyController.OrientWeaponsToward(enemyController.KnonwDetectedTarget.transform.position);
                    enemyController.TryAttack(enemyController.KnonwDetectedTarget.transform.position);
                    break;
            }
        }

        //???? ???濡 ???? ????
        private void UpdateAiStateTransition()
        {
            switch (AiState)
            {
                case AIState.Patrol: 
                    break;
                case AIState.Follow:
                    if(enemyController.IsSeeingTarget && enemyController.IsTargetInAttackRange)
                    {
                        AiState = AIState.Attack;
                        enemyController.SetNavDestination(transform.position);  //????
                    }
                    break;
                case AIState.Attack:
                    if (enemyController.IsTargetInAttackRange == false)
                    {
                        AiState = AIState.Follow;
                    }
                    break;
            }
        }

        private void OnDamaged()
        {
            //????? ???? - ??????? ??? ??????? ?÷???
            if(randomHitSparks.Length > 0)
            {
                int randNum = Random.Range(0, randomHitSparks.Length);
                randomHitSparks[randNum].Play();
            }

            //?????? ???
            animator.SetTrigger(k_AnimOnDamagedParameter);
        }

        private void OnDetected()
        {
            //???? ????
            if(AiState == AIState.Patrol)
            {
                AiState = AIState.Follow;
            }

            //Vfx
            for (int i = 0; i < detectedVfxs.Length; i++)
            {
                detectedVfxs[i].Play();
            }

            //Sfx
            if(detectedSfx)
            {
                AudioUtility.CreateSfx(detectedSfx, this.transform.position, 1f);
            }

            //anim
            animator.SetBool(k_AnimAlertedParameter, true);
        }

        private void OnLost()
        {
            // 상태 변경
            if(AiState == AIState.Follow || AiState == AIState.Attack)
            {
                AiState = AIState.Patrol;
            }

            //Vfx
            for (int i = 0; i < detectedVfxs.Length; i++)
            {
                detectedVfxs[i].Stop();
            }

            //anim
            animator.SetBool(k_AnimAlertedParameter, false);
        }

    }
}
