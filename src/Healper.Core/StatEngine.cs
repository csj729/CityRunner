using System;
using System.Collections.Generic;

namespace Healper.Core
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

            for (int i = 0; i < workouts.Count; i++)
            {
                WorkoutRecord w = workouts[i];
                float trust = _b.TrustFactor(w.Trust);

                if (w.Kind == WorkoutKind.Strength)
                {
                    rawCoins += (w.Volume / _b.VolumePerCoin) * trust;
                    outcome.Strength += (w.Volume / _b.VolumePerStrength) * trust;
                }
                else
                {
                    float load = (float)w.Duration.TotalMinutes * w.Intensity;
                    rawCoins += load * _b.CoinPerCardioMinute * trust;
                    outcome.Endurance += (load / _b.CardioLoadPerEndurance) * trust;
                }

                outcome.HasWorkout = true;
            }

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
