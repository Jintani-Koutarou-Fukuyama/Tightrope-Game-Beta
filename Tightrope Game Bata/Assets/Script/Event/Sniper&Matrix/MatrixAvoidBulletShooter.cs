using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/* マトリックス回避フェーズで、4発の弾を同時に飛ばすための発射管理です。 */
public class MatrixAvoidBulletShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform[] bulletSpawnPoints = new Transform[4];
    [SerializeField] private Transform passCheckTarget;
    [SerializeField] private Transform slowTarget;

    [Header("Bullet Movement")]
    /* 右から左へ飛ばす想定なので初期値は左方向です。Sceneに合わせてInspectorで調整できます。 */
    [SerializeField] private Vector3 moveDirection = Vector3.left;
    [SerializeField] private float normalSpeed = 12f;
    [SerializeField] private float slowSpeed = 2f;
    [SerializeField] private float slowStartDistance = 4f;
    [SerializeField] private float passDistance = 1f;
    [SerializeField] private float bulletLifeTime = 6f;

    [Header("Matrix Shot Sound")]
    [SerializeField] private AudioSource matrixShotAudioSource;
    [SerializeField] private AudioClip matrixShotSound;
    [SerializeField] private float matrixShotSoundVolume = 1.0f;

    [Header("Events")]
    [SerializeField] private UnityEvent onAllMatrixBulletsPassed;

    private readonly List<GameObject> spawnedBullets = new List<GameObject>();
    private int firedBulletCount;
    private int passedBulletCount;
    private bool isShooting;

    public event Action AllMatrixBulletsPassed;

    public void StartShooting()
    {
        FireMatrixBullets();
    }

    public void FireMatrixBullets()
    {
        StopShooting();

        if (bulletPrefab == null)
        {
            Debug.LogWarning("MatrixAvoidBulletShooter: Bullet Prefabが設定されていません。マトリックス弾を発射しません。", this);
            return;
        }

        if (passCheckTarget == null)
        {
            Debug.LogWarning("MatrixAvoidBulletShooter: Pass Check Targetが設定されていません。弾の通過判定ができないため発射しません。", this);
            return;
        }

        if (slowTarget == null)
        {
            Debug.LogWarning("MatrixAvoidBulletShooter: Slow Targetが設定されていません。弾は通常速度のまま飛びます。", this);
        }

        if (bulletSpawnPoints == null || bulletSpawnPoints.Length == 0)
        {
            Debug.LogWarning("MatrixAvoidBulletShooter: Bullet Spawn Pointsが設定されていません。", this);
            return;
        }

        Vector3 direction = moveDirection.sqrMagnitude > 0.0001f ? moveDirection.normalized : Vector3.left;
        isShooting = true;
        firedBulletCount = 0;
        passedBulletCount = 0;

        for (int i = 0; i < bulletSpawnPoints.Length; i++)
        {
            Transform spawnPoint = bulletSpawnPoints[i];
            if (spawnPoint == null)
            {
                Debug.LogWarning($"MatrixAvoidBulletShooter: Bullet Spawn Points の Element {i} が未設定です。", this);
                continue;
            }

            GameObject bulletObject = Instantiate(bulletPrefab, spawnPoint.position, spawnPoint.rotation);
            MatrixAvoidBullet bullet = bulletObject.GetComponent<MatrixAvoidBullet>();
            if (bullet == null)
            {
                bullet = bulletObject.AddComponent<MatrixAvoidBullet>();
            }

            spawnedBullets.Add(bulletObject);
            firedBulletCount++;
            bullet.Initialize(direction, normalSpeed, slowSpeed, slowStartDistance, passDistance, bulletLifeTime, slowTarget, passCheckTarget, OnBulletPassed);
        }

        RemoveDestroyedBulletReferences();

        if (firedBulletCount == 0)
        {
            isShooting = false;
            Debug.LogWarning("MatrixAvoidBulletShooter: 有効なSpawn Pointがないため、弾を発射できませんでした。", this);
            return;
        }

        PlayMatrixShotSound();
        Debug.Log($"MatrixAvoidBulletShooter: マトリックス弾を{firedBulletCount}発同時に発射しました。", this);
    }

    private void PlayMatrixShotSound()
    {
        bool hasMissingAudioSetting = false;

        if (matrixShotAudioSource == null)
        {
            Debug.LogWarning("MatrixAvoidBulletShooter: matrixShotAudioSource not assigned.", this);
            hasMissingAudioSetting = true;
        }

        if (matrixShotSound == null)
        {
            Debug.LogWarning("MatrixAvoidBulletShooter: matrixShotSound not assigned.", this);
            hasMissingAudioSetting = true;
        }

        if (hasMissingAudioSetting)
        {
            return;
        }

        matrixShotAudioSource.PlayOneShot(matrixShotSound, matrixShotSoundVolume);
        Debug.Log("MatrixAvoidBulletShooter: Matrix avoid shot sound played.", this);
    }

    public void StopShooting()
    {
        DestroySpawnedBullets();
        firedBulletCount = 0;
        passedBulletCount = 0;
        isShooting = false;
    }

    private void OnBulletPassed(MatrixAvoidBullet bullet)
    {
        if (bullet != null)
        {
            spawnedBullets.Remove(bullet.gameObject);
        }

        if (!isShooting)
        {
            return;
        }

        passedBulletCount++;
        Debug.Log($"MatrixAvoidBulletShooter: 弾が通過しました。{passedBulletCount}/{firedBulletCount}", this);

        if (passedBulletCount >= firedBulletCount)
        {
            NotifyAllBulletsPassed();
        }
    }

    private void NotifyAllBulletsPassed()
    {
        if (!isShooting)
        {
            return;
        }

        isShooting = false;
        RemoveDestroyedBulletReferences();
        Debug.Log("MatrixAvoidBulletShooter: すべてのマトリックス弾が通過しました。", this);
        onAllMatrixBulletsPassed?.Invoke();
        AllMatrixBulletsPassed?.Invoke();
    }

    private void DestroySpawnedBullets()
    {
        for (int i = spawnedBullets.Count - 1; i >= 0; i--)
        {
            if (spawnedBullets[i] != null)
            {
                Destroy(spawnedBullets[i]);
            }
        }

        spawnedBullets.Clear();
    }

    private void RemoveDestroyedBulletReferences()
    {
        for (int i = spawnedBullets.Count - 1; i >= 0; i--)
        {
            if (spawnedBullets[i] == null)
            {
                spawnedBullets.RemoveAt(i);
            }
        }
    }
}
