using System;
using UnityEngine;

/* マトリックス回避フェーズ専用の弾です。Time.timeScaleは使わず、この弾の移動速度だけを遅くします。 */
public class MatrixAvoidBullet : MonoBehaviour
{
    private Transform slowTarget;
    private Transform passCheckTarget;
    private Vector3 moveDirection = Vector3.left;
    private float normalSpeed;
    private float slowSpeed;
    private float slowStartDistance;
    private float passDistance;
    private float lifeTime;
    private float elapsedTime;
    private bool isInitialized;
    private bool hasPassed;
    private bool hasEnteredSlowArea;
    private Action<MatrixAvoidBullet> onPassed;

    public void Initialize(
        Vector3 direction,
        float normalMoveSpeed,
        float slowMoveSpeed,
        float slowAreaDistance,
        float passCheckDistance,
        float bulletLifeTime,
        Transform slowCheckTarget,
        Transform passTarget,
        Action<MatrixAvoidBullet> passedCallback)
    {
        moveDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.left;
        normalSpeed = Mathf.Max(0f, normalMoveSpeed);
        slowSpeed = Mathf.Max(0f, slowMoveSpeed);
        slowStartDistance = Mathf.Max(0f, slowAreaDistance);
        passDistance = Mathf.Max(0f, passCheckDistance);
        lifeTime = Mathf.Max(0.1f, bulletLifeTime);
        slowTarget = slowCheckTarget;
        passCheckTarget = passTarget;
        onPassed = passedCallback;
        elapsedTime = 0f;
        hasPassed = false;
        hasEnteredSlowArea = false;
        isInitialized = true;

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        }
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        transform.position += moveDirection * GetCurrentSpeed() * Time.deltaTime;

        if (!hasPassed && HasPassedTarget())
        {
            NotifyPassed();
            return;
        }

        if (elapsedTime >= lifeTime)
        {
            Debug.LogWarning("MatrixAvoidBullet: LifeTimeを超えたため弾を削除します。Pass Check TargetやLife Timeを確認してください。", this);
            Destroy(gameObject);
        }
    }

    private float GetCurrentSpeed()
    {
        if (slowTarget == null || slowStartDistance <= 0f)
        {
            return normalSpeed;
        }

        float distance = Vector3.Distance(transform.position, slowTarget.position);
        if (distance > slowStartDistance)
        {
            return normalSpeed;
        }

        if (!hasEnteredSlowArea)
        {
            hasEnteredSlowArea = true;
            Debug.Log("MatrixAvoidBullet: 弾がスロー範囲に入りました。", this);
        }

        float rate = Mathf.Clamp01(distance / slowStartDistance);
        return Mathf.Lerp(slowSpeed, normalSpeed, rate);
    }

    private bool HasPassedTarget()
    {
        if (passCheckTarget == null)
        {
            return false;
        }

        /* 弾が移動方向へ、判定TargetよりpassDistance分だけ進んだら「通過」とします。 */
        Vector3 targetToBullet = transform.position - passCheckTarget.position;
        return Vector3.Dot(targetToBullet, moveDirection) >= passDistance;
    }

    private void NotifyPassed()
    {
        hasPassed = true;
        onPassed?.Invoke(this);
        Destroy(gameObject);
    }
}