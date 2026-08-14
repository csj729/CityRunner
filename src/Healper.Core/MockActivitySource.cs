using System;
using System.Collections.Generic;

namespace Healper.Core
{
    /// <summary>
    /// 밸런싱용 유저 유형. 꾸준한 유저와 방치 유저의 격차가 실제로 벌어지는지
    /// 확인하려면 최소 이 셋을 나란히 돌려봐야 한다(§4.4 목표: 약 10배).
    /// </summary>
    public enum MockProfile
    {
        Consistent, // 주 4~5회, 식단 거의 매일
        Erratic,    // 주 1~2회, 식단 띄엄띄엄
        Lapsed,     // 초반만 하고 방치

        /// <summary>
        /// 운동하지 않고 매일 부풀린 수치를 손으로 입력하는 유저.
        /// 일일 캡과 수동 입력 감액(§7.3)이 실제로 먹히는지 확인하는 대조군이다.
        /// 이 프로필이 Consistent 를 앞지르면 신뢰 설계가 뚫린 것이다.
        /// </summary>
        Cheater,
    }

    /// <summary>
    /// 기기 없이 코어 루프를 돌리기 위한 가짜 데이터 소스.
    /// 시드를 고정하므로 같은 프로필은 항상 같은 히스토리를 만든다.
    /// </summary>
    public sealed class MockActivitySource : IActivitySource
    {
        private readonly List<WorkoutRecord> _workouts = new List<WorkoutRecord>();
        private readonly List<DietDay> _diet = new List<DietDay>();

        public MockActivitySource(MockProfile profile, DateTime start, int days, int seed = 42)
        {
            var rng = new Random(seed);

            for (int d = 0; d < days; d++)
            {
                DateTime day = start.Date.AddDays(d);

                if (profile == MockProfile.Cheater)
                {
                    AddCheatDay(day);
                    continue;
                }

                float decay = profile == MockProfile.Lapsed ? Math.Max(0f, 1f - d / 10f) : 1f;

                float workoutChance = BaseWorkoutChance(profile) * decay;
                float dietChance = BaseDietChance(profile) * decay;

                if (rng.NextDouble() < workoutChance)
                {
                    // 웨이트와 유산소를 섞는다. 실제 유저도 한쪽만 하지는 않는다.
                    if (rng.NextDouble() < 0.6)
                    {
                        float volume = 6000f + (float)rng.NextDouble() * 6000f;
                        _workouts.Add(WorkoutRecord.Strength(
                            day.AddHours(19), TimeSpan.FromMinutes(50), volume, PickTrust(rng, profile)));
                    }
                    else
                    {
                        float intensity = 1.0f + (float)rng.NextDouble();
                        int minutes = 25 + rng.Next(20);
                        _workouts.Add(WorkoutRecord.Cardio(
                            day.AddHours(7), TimeSpan.FromMinutes(minutes), intensity, PickTrust(rng, profile)));
                    }
                }

                if (rng.NextDouble() < dietChance)
                {
                    _diet.Add(new DietDay
                    {
                        Date = day,
                        MealsLogged = 2 + rng.Next(2),
                        ProteinGoalMet = rng.NextDouble() < 0.5,
                        AteAllMainMeals = rng.NextDouble() < 0.8,
                        Trust = RecordTrust.Manual, // 식단은 대체로 수동 입력
                    });
                }
            }
        }

        /// <summary>매일, 말도 안 되는 수치를, 전부 수동으로 입력한다.</summary>
        private void AddCheatDay(DateTime day)
        {
            _workouts.Add(WorkoutRecord.Strength(
                day.AddHours(19), TimeSpan.FromMinutes(90), 25000f, RecordTrust.Manual));
            _workouts.Add(WorkoutRecord.Cardio(
                day.AddHours(7), TimeSpan.FromMinutes(120), 2.0f, RecordTrust.Manual));

            _diet.Add(new DietDay
            {
                Date = day,
                MealsLogged = 3,
                ProteinGoalMet = true,
                AteAllMainMeals = true,
                Trust = RecordTrust.Manual,
            });
        }

        public IReadOnlyList<WorkoutRecord> GetWorkouts(DateTime from, DateTime to)
        {
            var result = new List<WorkoutRecord>();
            for (int i = 0; i < _workouts.Count; i++)
                if (_workouts[i].Start >= from && _workouts[i].Start < to)
                    result.Add(_workouts[i]);
            return result;
        }

        public IReadOnlyList<DietDay> GetDietDays(DateTime from, DateTime to)
        {
            var result = new List<DietDay>();
            for (int i = 0; i < _diet.Count; i++)
                if (_diet[i].Date >= from.Date && _diet[i].Date < to.Date)
                    result.Add(_diet[i]);
            return result;
        }

        private static float BaseWorkoutChance(MockProfile p)
        {
            switch (p)
            {
                case MockProfile.Consistent: return 0.65f;
                case MockProfile.Erratic: return 0.22f;
                default: return 0.60f; // Lapsed 는 decay 가 깎아 내린다
            }
        }

        private static float BaseDietChance(MockProfile p)
        {
            switch (p)
            {
                case MockProfile.Consistent: return 0.90f;
                case MockProfile.Erratic: return 0.35f;
                default: return 0.85f;
            }
        }

        /// <summary>
        /// 꾸준한 유저일수록 워치·앱 자동 기록 비중이 높다는 가정.
        /// 수동 입력 지급률(§7.2)이 실제로 얼마나 체감되는지 보려면 이 분포가 중요하다.
        /// </summary>
        private static RecordTrust PickTrust(Random rng, MockProfile p)
        {
            double auto = p == MockProfile.Consistent ? 0.75 : 0.35;
            return rng.NextDouble() < auto ? RecordTrust.ActivelyRecorded : RecordTrust.Manual;
        }
    }
}
