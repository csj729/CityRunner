using System;
using System.Collections.Generic;

namespace Healper.Core
{
    /// <summary>
    /// 오프라인 진행(§4.4).
    ///
    ///   오프라인 획득량 = 기본치 x 최근7일이행률(0.2 ~ 2.0) x 시설배수
    ///
    /// 방치형의 "안 해도 쌓인다"와 이 게임의 "운동해야 쌓인다"가 충돌하는 지점이라,
    /// 배수의 하한을 0이 아니라 0.2로 두는 것이 핵심이다. 0으로 만들면 복귀 유인이
    /// 사라지고, 진행 상실은 이탈의 최대 원인이 된다.
    /// </summary>
    public static class OfflineProgress
    {
        /// <summary>최근 7일 이행률(0 ~ 1). 운동 횟수와 식단 기록일을 가중 평균한다.</summary>
        public static float Compliance(IReadOnlyList<DailyOutcome> last7Days, Balance b)
        {
            int workoutDays = 0;
            int dietDays = 0;

            for (int i = 0; i < last7Days.Count; i++)
            {
                if (last7Days[i].HasWorkout) workoutDays++;
                if (last7Days[i].HasDietLog) dietDays++;
            }

            float workoutRate = Clamp01((float)workoutDays / b.WeeklyWorkoutTarget);
            float dietRate = Clamp01((float)dietDays / b.WeeklyDietLogTarget);

            return workoutRate * b.ComplianceWorkoutWeight
                 + dietRate * (1f - b.ComplianceWorkoutWeight);
        }

        /// <summary>이행률 -> 오프라인 배수.</summary>
        public static float Multiplier(float compliance, Balance b)
        {
            return b.OfflineMultiplierMin
                 + (b.OfflineMultiplierMax - b.OfflineMultiplierMin) * Clamp01(compliance);
        }

        /// <summary>최근 7일의 활동 코인 일평균. 오프라인 기본치의 근거가 된다.</summary>
        public static float RecentDailyActivityCoins(IReadOnlyList<DailyOutcome> last7Days)
        {
            if (last7Days.Count == 0) return 0f;

            int sum = 0;
            for (int i = 0; i < last7Days.Count; i++) sum += last7Days[i].Coins;
            return (float)sum / last7Days.Count;
        }

        /// <summary>
        /// 자리를 비운 동안 쌓인 코인.
        ///
        ///   한 사이클 = 최근 활동 일평균 x OfflineShareOfActivity x 이행률배수 x 시설배수
        ///
        /// 기본치를 활동에서 유도하는 것이 핵심이다. 절대 상수로 두면 운동과 무관하게
        /// 재화가 쌓여, 이 게임의 전제(재화원은 현실 운동)가 무너진다.
        /// </summary>
        public static int Accrue(TimeSpan away, IReadOnlyList<DailyOutcome> last7Days, Balance b,
                                 float facilityMultiplier = 1f)
        {
            float perCycle = RecentDailyActivityCoins(last7Days) * b.OfflineShareOfActivity;
            if (perCycle < b.OfflineMinPerCycle) perCycle = b.OfflineMinPerCycle;

            float multiplier = Multiplier(Compliance(last7Days, b), b);

            // 사이클을 얼마나 채웠는지. 상한을 넘긴 시간은 버린다.
            double filled = Math.Min(away.TotalHours, b.OfflineCapHours) / b.OfflineCapHours;

            return (int)Math.Round(perCycle * filled * multiplier * facilityMultiplier);
        }

        private static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }
}
