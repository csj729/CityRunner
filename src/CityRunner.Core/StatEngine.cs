using System;
using System.Collections.Generic;

namespace CityRunner.Core
{
    /// <summary>하루치 활동을 환산한 결과.</summary>
    public struct DailyOutcome
    {
        public DateTime Date;
        public int Coins;
        public float Strength;
        public float Endurance;
        public float Condition;

        /// <summary>일일 상한에 걸려 잘려나갔는가. 캡이 너무 낮은지 판단하는 신호.</summary>
        public bool CapHit;

        public bool HasWorkout;
        public bool HasDietLog;
    }

    /// <summary>
    /// 활동 -> 코인 + 3스탯 환산(§4). 계산식만 담고 숫자는 Balance 에서 온다.
    /// </summary>
    public sealed class StatEngine
    {
        private readonly Balance _b;

        public StatEngine(Balance balance)
        {
            _b = balance;
        }

        public DailyOutcome EvaluateDay(DateTime day, IReadOnlyList<WorkoutRecord> workouts, DietDay? diet)
        {
            var outcome = new DailyOutcome { Date = day.Date };
            float rawCoins = 0f;

            // 근력은 그날 가장 빠른 세션 하나만 본다. 속도는 그날의 최고 출력을
            // 대표하는 값이라 세션 수만큼 더할 성질이 아니고, 더하게 두면
            // "세션을 여러 개로 쪼개 적기"가 곧바로 최적 전략이 된다.
            float bestSpeed = 0f;
            float bestSpeedTrust = 0f;

            // 하루치 유산소 부하 예산. 세션들이 나눠 쓰고, 다 쓰면 남은 세션은
            // 기록만 남고 보상이 없다(§7.3 하루 하드캡).
            float loadBudget = _b.MaxCardioLoadPerDay;

            // 하루에 인정하는 세션 수를 제한한다. 나머지는 기록은 남되 보상은 없다.
            int counted = Math.Min(workouts.Count, _b.MaxWorkoutsPerDay);

            for (int i = 0; i < counted; i++)
            {
                WorkoutRecord w = workouts[i];
                float trust = _b.TrustFactor(w.Trust);
                float minutes = (float)w.Duration.TotalMinutes;

                // 지구력은 얼마나 오래·세게 움직였는가(양).
                float load = Math.Min(minutes * w.Intensity, loadBudget);
                loadBudget -= load;
                rawCoins += load * _b.CoinPerCardioMinute * trust;
                outcome.Endurance += (load / _b.CardioLoadPerEndurance) * trust;

                // 근력은 얼마나 빨랐는가(질). 시간을 곱하지 않는 것이 핵심이다 -
                // 곱하면 지구력과 같은 것을 두 번 재게 된다(§4.2.1).
                // 대신 짧은 전력질주 반복이 최적해가 되지 않도록 최소 시간을 요구한다.
                if (minutes >= _b.MinStrengthSessionMinutes)
                {
                    // 타당성 상한을 먼저 씌운다 - 비율 감액만으로는 무제한 입력을 못 막는다.
                    float speed = Math.Min(w.SpeedKmh, _b.MaxSpeedKmh);
                    if (speed > bestSpeed)
                    {
                        bestSpeed = speed;
                        bestSpeedTrust = trust;
                    }
                }

                outcome.HasWorkout = true;
            }

            float over = bestSpeed - _b.StrengthSpeedFloor;
            if (over > 0f)
                outcome.Strength = (over / _b.SpeedPerStrength) * bestSpeedTrust;

            // 부하와 무관하게, 그날 운동했다는 사실에 주는 몫(§7.3 Tier0-2).
            if (outcome.HasWorkout)
                rawCoins += _b.CoinPerWorkoutDay * _b.TrustFactor(workouts[0].Trust);

            if (diet.HasValue)
            {
                DietDay d = diet.Value;
                float trust = _b.TrustFactor(d.Trust);

                // 무엇을 먹었든 기록 자체에 보상한다(§7.1).
                rawCoins += d.MealsLogged * _b.CoinPerMealLogged * trust;
                outcome.Condition += d.MealsLogged * _b.ConditionPerMeal * trust;

                if (d.ProteinGoalMet)
                {
                    rawCoins += _b.CoinProteinGoalBonus * trust;
                    outcome.Condition += _b.ConditionProteinBonus * trust;
                }

                if (d.AteAllMainMeals)
                {
                    rawCoins += _b.CoinRegularityBonus * trust;
                    outcome.Condition += _b.ConditionRegularityBonus * trust;
                }

                outcome.HasDietLog = d.MealsLogged > 0;
            }

            int capped = (int)Math.Round(rawCoins);
            if (capped > _b.DailyCoinCap)
            {
                capped = _b.DailyCoinCap;
                outcome.CapHit = true;
            }

            outcome.Coins = capped;
            return outcome;
        }
    }
}
