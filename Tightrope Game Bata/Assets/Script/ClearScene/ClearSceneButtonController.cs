using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClearSceneButtonController : MonoBehaviour
{
    private static readonly string[] ResetTargetTypeNames =
    {
        "PlayerGameFeedback",
        "PlayerGameFeedbackController",
        "GameManager",
        "BalanceManager"
    };

    private static readonly string[] ResetMethodNames =
    {
        "ResetDamage",
        "ResetBalance",
        "ResetMissCount",
        "ResetMistakeCount",
        "ResetGameState"
    };

    [Header("Scene Names")]
    [SerializeField] private string retrySceneName = "SampleScene";
    [SerializeField] private string titleSceneName = "TitleScene";

    [Header("Optional Retry Reset Targets")]
    [SerializeField] private MonoBehaviour[] retryResetTargets;

    public void OnRetryButtonPressed()
    {
        Debug.Log($"{nameof(ClearSceneButtonController)}: RetryButton pressed.", this);
        LoadRetryScene();
    }

    public void OnTitleButtonPressed()
    {
        Debug.Log($"{nameof(ClearSceneButtonController)}: TitleButton pressed.", this);
        Time.timeScale = 1f;
        LoadScene(titleSceneName, "TitleButton");
    }

    private void LoadRetryScene()
    {
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(retrySceneName))
        {
            Debug.LogWarning($"{nameof(ClearSceneButtonController)}: Scene name for RetryButton is empty.", this);
            return;
        }

        SceneManager.sceneLoaded += OnRetrySceneLoaded;
        DontDestroyOnLoad(gameObject);
        SceneManager.LoadScene(retrySceneName);
    }

    private void OnRetrySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != retrySceneName)
        {
            return;
        }

        SceneManager.sceneLoaded -= OnRetrySceneLoaded;
        ResetRunStateForRetry();
        Destroy(gameObject);
    }

    private void ResetRunStateForRetry()
    {
        Time.timeScale = 1f;

        bool resetApplied = TryResetAssignedTargets();

        if (!resetApplied)
        {
            resetApplied = TryResetFoundTargets();
        }

        if (!resetApplied)
        {
            Debug.LogWarning($"{nameof(ClearSceneButtonController)}: reset target not found.", this);
        }
    }

    private bool TryResetAssignedTargets()
    {
        if (retryResetTargets == null || retryResetTargets.Length == 0)
        {
            return false;
        }

        bool resetApplied = false;

        foreach (MonoBehaviour resetTarget in retryResetTargets)
        {
            if (resetTarget == null)
            {
                continue;
            }

            resetApplied |= TryInvokeResetMethod(resetTarget);
        }

        return resetApplied;
    }

    private bool TryResetFoundTargets()
    {
        bool resetApplied = false;
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || !IsResetTargetType(behaviour.GetType()))
            {
                continue;
            }

            resetApplied |= TryInvokeResetMethod(behaviour);
        }

        return resetApplied;
    }

    private bool IsResetTargetType(System.Type type)
    {
        string typeName = type.Name;

        foreach (string resetTargetTypeName in ResetTargetTypeNames)
        {
            if (typeName == resetTargetTypeName)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryInvokeResetMethod(MonoBehaviour resetTarget)
    {
        System.Type targetType = resetTarget.GetType();

        foreach (string resetMethodName in ResetMethodNames)
        {
            MethodInfo method = targetType.GetMethod(
                resetMethodName,
                BindingFlags.Instance | BindingFlags.Public,
                null,
                System.Type.EmptyTypes,
                null);

            if (method == null)
            {
                continue;
            }

            try
            {
                method.Invoke(resetTarget, null);
                Debug.Log($"{nameof(ClearSceneButtonController)}: Invoked {targetType.Name}.{resetMethodName}().", resetTarget);
                return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"{nameof(ClearSceneButtonController)}: Failed to invoke {targetType.Name}.{resetMethodName}(). {exception.Message}", resetTarget);
            }
        }

        return false;
    }

    private void LoadScene(string sceneName, string buttonName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"{nameof(ClearSceneButtonController)}: Scene name for {buttonName} is empty.", this);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
