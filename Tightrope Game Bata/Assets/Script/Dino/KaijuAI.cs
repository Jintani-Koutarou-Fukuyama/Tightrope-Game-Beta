using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.AI;

public class KaijuAI : MonoBehaviour
{
    [Header("Move / Rotate")]
    public float moveSpeed = 1.0f;
    public float rotateSpeed = 5.0f;

    [Header("Attack")]
    public float attackDistance = 4.5f;
    public float attackCooldown = 2f;

    [Header("Effects")]
    public AudioSource asiato;
    public ParticleSystem footSmokeLeft;
    public ParticleSystem footSmokeRight;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    [SerializeField] Transform player;//プレイヤーの座標を検知

    Animator anim;
    NavMeshAgent agent;

    GameObject targetBuilding;
    BreakBuilding targetBreakScript;

    float attackTimer = 0f;
    bool isAttacking = false;
    int patrolIndex = 0;

    void Start()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        agent.speed = moveSpeed;
        agent.angularSpeed = rotateSpeed * 100f;
        agent.acceleration = 20f;
        agent.stoppingDistance = attackDistance * 0.65f;

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("[KaijuAI] NavMesh 上にいません。", this);
        }

        FindNewBuilding();
    }

    void Update()
    {
        transform.Rotate(0, 50 * Time.deltaTime, 0);

        anim.speed = 0.7f;

        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;

        if (targetBuilding == null)
        {
            FindNewBuilding();
            if (targetBuilding == null)
            {
                Patrol();
                return;
            }
        }

        Vector3 toTarget = targetBuilding.transform.position - transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        // 攻撃開始
        if (!isAttacking && dist <= attackDistance && attackTimer <= 0f)
        {
            StartAttack();
            return;
        }

        // 攻撃中は停止
        if (isAttacking)
        {
            agent.ResetPath();
            anim.SetFloat("Speed", 0f);
            return;
        }

        // 通常移動
        if (agent.isOnNavMesh)
            agent.SetDestination(targetBuilding.transform.position);

        float v = agent.velocity.magnitude;
        if (v < 0.05f) v = 0.05f;

        anim.SetFloat("Speed", v);

    }

    void StartAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;

        if (agent.isOnNavMesh)
            agent.ResetPath();

        anim.SetTrigger("Attack");
        anim.SetFloat("Speed", 0f);
    }

    void FindNewBuilding()
    {
        var buildings = GameObject.FindGameObjectsWithTag("Building")
            .Where(b => b != null && b.activeInHierarchy)
            .ToArray();

        if (buildings.Length == 0)
        {
            targetBuilding = null;
            targetBreakScript = null;
            return;
        }

        //プレイヤーの近くの建物をセットする
        targetBuilding = buildings
            .OrderBy(b => Vector3.Distance(player.position, b.transform.position))
            .FirstOrDefault();

        targetBreakScript = targetBuilding?.GetComponent<BreakBuilding>();
    }

    public void PunchHit()
    {
        StartCoroutine(PunchHitRoutine());
    }

    IEnumerator PunchHitRoutine()
    {
        if (agent.isOnNavMesh)
            agent.ResetPath();

        if (targetBreakScript != null)
        {
            targetBreakScript.Break(transform.position);

            if (targetBuilding != null)
                targetBuilding.SetActive(false);

            targetBuilding = null;
            targetBreakScript = null;

            attackTimer = 0f;
            FindNewBuilding();
        }

        //硬直中は待機状態にする
        isAttacking = true;
        anim.SetFloat("Speed", 0f);

        float stunTime = 1.2f;
        yield return new WaitForSeconds(stunTime);

        isAttacking = false;

        //移動させる
        if (targetBuilding != null && agent.isOnNavMesh)
            agent.SetDestination(targetBuilding.transform.position);

       
    }



    public void FootStepLeft()
    {
        if (footSmokeLeft != null) footSmokeLeft.Play();
        if (asiato != null) asiato.Play();
    }

    public void FootStepRight()
    {
        if (footSmokeRight != null) footSmokeRight.Play();
        if (asiato != null) asiato.Play();
    }

    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            anim.SetFloat("Speed", 0f);
            if (agent.isOnNavMesh)
                agent.ResetPath();
            return;
        }

        Transform target = patrolPoints[patrolIndex];

        if (agent.isOnNavMesh)
            agent.SetDestination(target.position);

        float v = agent.velocity.magnitude;
        if (v < 0.05f) v = 0.05f;

        anim.SetFloat("Speed", v);

        if (Vector3.Distance(transform.position, target.position) < 2.0f)
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }



    private void OnCollisionEnter(Collision collision)
    {
        // ロープに当たった時
        if (collision.gameObject.tag == "RopeParts")
        {
            //Apply Root Motionを無効にする
            anim.applyRootMotion = false;

            // ロープをくぐるアニメーション発生
            anim.SetTrigger("UnderTheRope");
            agent.speed = 0.0f;
            
        }
    }

    //アニメーションイベントから呼び出される
    public void ExitUnderTheRope()
    {
        //Apply Root Motionを有効にする
        anim.applyRootMotion = true;
        // 歩くようにする
        agent.speed = moveSpeed;
    }
}
