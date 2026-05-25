using UnityEngine;

public class AuxiliarySystemsBootstrap : MonoBehaviour
{
    [SerializeField] private AchievementSystem achievementSystem;
    [SerializeField] private DailyRewardSystem dailyRewardSystem;

    private void Awake()
    {
        achievementSystem?.Initialize();
        dailyRewardSystem?.Initialize();
    }

    private void Update()
    {
        GameEvents.UpdatePlayTime(Time.unscaledDeltaTime);
    }

    private void OnDestroy()
    {
        achievementSystem?.Shutdown();
    }
}
