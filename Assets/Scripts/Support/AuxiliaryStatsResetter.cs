using UnityEngine;

public class AuxiliaryStatsResetter : MonoBehaviour
{
    [SerializeField] private AchievementSystem achievementSystem;
    [SerializeField] private DailyRewardSystem dailyRewardSystem;
    [SerializeField] private PlayerMetaProgress playerMetaProgress;
    [SerializeField] private FeedbackSystem feedbackSystem;

    public void ResetAllStats()
    {
        playerMetaProgress?.ResetProgress();
        achievementSystem?.ResetProgress();
        dailyRewardSystem?.ResetProgress();

        if (achievementSystem != null)
        {
            achievementSystem.NotifyAllAchievementsChanged();
        }

        feedbackSystem?.ShowStatus("Статистика и прогресс сброшены.");
        Debug.Log("Auxiliary stats reset completed.");
    }
}
