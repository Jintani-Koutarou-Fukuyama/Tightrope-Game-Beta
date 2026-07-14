using System;
using UnityEngine;

// スナイパーの弾をまっすぐ移動させるだけのスクリプトです。
// 今回は当たり判定やダメージ処理は行いません。
public class SniperBullet : MonoBehaviour
{
    private Vector3 targetPosition;
    private Vector3 moveDirection;
    private float moveSpeed;
    private float remainingLifeTime;
    private bool isInitialized;
    private bool hasFinished;
    private Action<SniperBullet, bool> onFinished;

    public void Initialize(Vector3 startPosition, Vector3 targetPosition, float speed, float lifeTime, Action<SniperBullet, bool> onFinished = null)
    {
        transform.position = startPosition;

        this.onFinished = onFinished;
        this.targetPosition = targetPosition;
        moveSpeed = Mathf.Max(0f, speed);
        remainingLifeTime = Mathf.Max(0.01f, lifeTime);

        Vector3 direction = targetPosition - startPosition;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            FinishBullet(false);
            return;
        }

        moveDirection = direction.normalized;
        transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        transform.position += moveDirection * moveSpeed * Time.deltaTime;
        remainingLifeTime -= Time.deltaTime;

        if (HasReachedTarget())
        {
            FinishBullet(true);
            return;
        }

        if (remainingLifeTime <= 0f)
        {
            FinishBullet(false);
        }
    }

    private bool HasReachedTarget()
    {
        Vector3 bulletToTarget = targetPosition - transform.position;
        return Vector3.Dot(bulletToTarget, moveDirection) <= 0f;
    }

    // 到達通知は1回だけ出します。寿命切れの場合はreachedTarget=falseです。
    private void FinishBullet(bool reachedTarget)
    {
        if (hasFinished)
        {
            return;
        }

        hasFinished = true;
        isInitialized = false;
        onFinished?.Invoke(this, reachedTarget);
        Destroy(gameObject);
    }
}
