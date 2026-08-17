using System;
using System.Collections.Generic;

namespace CityRunner.Core
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
        /// 느리게, 대신 오래 뛰는 유저. Consistent 와 같은 빈도로 움직인다.
        /// 근력이 속도에서 나오는 설계(§4.2.1)의 전제 - "빠르게 뛰지 못해도
        /// 완주할 수 있다" - 가 지켜지는지 확인하는 대조군이다.
        /// </summary>
        SlowJogger,

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
                    float speed = PickSpeed(rng, profile);
                    int minutes = PickMinutes(rng, profile);

                    _workouts.Add(WorkoutRecord.Cardio(
                        day.AddHours(7), TimeSpan.FromMinutes(minutes),
                        speed * minutes / 60f, IntensityOf(speed), PickTrust(rng, profile)));
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
            // 2시간 동안 시속 30km 로 뛰었다고 적는다. 속도 상한(§7.3)이 실제로
            // 이 입력을 잘라내는지 확인하는 것이 이 프로필의 목적이다.
            _workouts.Add(WorkoutRecord.Cardio(
                day.AddHours(19), TimeSpan.FromMinutes(90), 45f, 2.0f, RecordTrust.Manual));
            _workouts.Add(WorkoutRecord.Cardio(
                day.AddHours(7), TimeSpan.FromMinutes(120), 60f, 2.0f, RecordTrust.Manual));

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

        /// <summary>
        /// 세션 평균 속도(km/h). SlowJogger 는 걷기보다는 빠르지만 근력이 거의
        /// 붙지 않는 구간(약 7)에 머문다.
        /// </summary>
        private static float PickSpeed(Random rng, MockProfile p)
        {
            if (p == MockProfile.SlowJogger) return 6.5f + (float)rng.NextDouble();
            return 9f + (float)rng.NextDouble() * 2f;
        }

        /// <summary>SlowJogger 는 속도를 시간으로 벌충한다.</summary>
        private static int PickMinutes(Random rng, MockProfile p)
        {
            if (p == MockProfile.SlowJogger) return 40 + rng.Next(20);
            return 25 + rng.Next(20);
        }

        /// <summary>
        /// 강도를 속도에서 끌어낸다. §4.2.2 폴백 2순위가 속도이므로 실제 구현도
        /// 이렇게 움직인다. 결과적으로 지구력은 거리(양), 근력은 속도(질)를 본다.
        /// </summary>
        private static float IntensityOf(float speedKmh)
        {
            float i = speedKmh / 7f;
            if (i < 1f) return 1f;
            return i > 2f ? 2f : i;
        }

        private static float BaseWorkoutChance(MockProfile p)
        {
            switch (p)
            {
                case MockProfile.Consistent:
                case MockProfile.SlowJogger: return 0.65f;
                case MockProfile.Erratic: return 0.22f;
                default: return 0.60f; // Lapsed 는 decay 가 깎아 내린다
            }
        }

        private static float BaseDietChance(MockProfile p)
        {
            switch (p)
            {
                case MockProfile.Consistent:
                case MockProfile.SlowJogger: return 0.90f;
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
            // 워치 없이 폰만 쓰는 유저. 폰이 알아서 기록하므로 사후 입력이 거의 없다.
            if (p == MockProfile.SlowJogger) return RecordTrust.Automatic;

            double auto = p == MockProfile.Consistent ? 0.75 : 0.35;
            return rng.NextDouble() < auto ? RecordTrust.ActivelyRecorded : RecordTrust.Manual;
        }
    }
}
