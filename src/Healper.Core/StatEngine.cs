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

            // 하루에 인정하는 세션 수를 제한한다. 나머지는 기록은 남되 보상은 없다.
            int counted = Math.Min(workouts.Count, _b.MaxWorkoutsPerDay);

            for (int i = 0; i < counted; i++)
            {
                WorkoutRecord w = workouts[i];
                float trust = _b.TrustFactor(w.Trust);

                if (w.Kind == WorkoutKind.Strength)
                {
                    // 웨이트는 코인을 볼륨에 비례해 주지 않는다(비대칭 설계).
                    // 중량은 어떤 센서로도 검증할 수 없어, 게임 경제와 분리해 둔다.
                    // 타당성 상한을 먼저 씌운다 - 비율 감액만으로는 무제한 입력을 못 막는다.
                    float volume = Math.Min(w.Volume, _b.MaxVolumePerSession);
                    rawCoins += _b.CoinPerStrengthSession * trust;
                    outcome.Strength += (volume / _b.VolumePerStrength) * trust;
                }
                else
                {
                    float load = Math.Min((float)w.Duration.TotalMinutes * w.Intensity,
                                          _b.MaxCardioLoadPerSession);
                    rawCoins += load * _b.CoinPerCardioMinute * trust;
                    outcome.Endurance += (load / _b.CardioLoadPerEndurance) * trust;
                }

                outcome.HasWorkout = true;
            }

            // 검증 수단이 없는 근력은 하루 단위로 상승폭을 가둔다.
            if (outcome.Strength > _b.StrengthStatDailyCap)
                outcome.Strength = _b.StrengthStatDailyCap;

            // 볼륨과 무관하게, 그날 운동했다는 사실에 주는 몫(§7.3 Tier0-2).
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
