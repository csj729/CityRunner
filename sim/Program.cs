using System;
using System.Collections.Generic;
using Healper.Core;

namespace Healper.Sim
{
    /// <summary>
    /// 코어 루프를 기기 없이 돌려보는 밸런싱 시뮬레이터.
    ///
    ///   dotnet run --project sim
    ///
    /// Balance.cs 의 숫자를 바꿔가며 다시 돌리는 것이, 문서(§11)에서 말한
    /// "코드로 밸런싱하기 전에 스프레드시트로 먼저" 단계를 대신한다.
    /// </summary>
    internal static class Program
    {
        private const int Days = 28;

        /// <summary>
        /// 시드 하나로 낸 결론은 우연일 수 있다. 여러 시드를 돌려 평균을 봐야
        /// 밸런싱 판단이 선다.
        /// </summary>
        private const int Seeds = 30;

        private static void Main()
        {
            var balance = new Balance();
            var start = new DateTime(2026, 1, 1);

            Console.WriteLine("Healper 코어 루프 시뮬레이션 - {0}일 x 시드 {1}개 평균", Days, Seeds);
            Console.WriteLine();

            var totals = new Dictionary<MockProfile, Summary>();
            foreach (MockProfile profile in Enum.GetValues(typeof(MockProfile)))
            {
                Summary s = RunAveraged(profile, balance, start);
                totals[profile] = s;
                Report(profile, s);
            }

            Console.WriteLine("=== 유저 격차 (§4.4 목표: 약 10배) ===");
            float best = totals[MockProfile.Consistent].OfflinePerDay;
            float worst = totals[MockProfile.Lapsed].OfflinePerDay;
            Console.WriteLine("  꾸준함 {0:F1} 코인/일  vs  방치 {1:F1} 코인/일  ->  {2:F1}배",
                best, worst, worst > 0 ? best / worst : 0f);
            Console.WriteLine();
        }

        private static Summary RunAveraged(MockProfile profile, Balance balance, DateTime start)
        {
            var avg = new Summary();
            for (int seed = 0; seed < Seeds; seed++)
            {
                Summary s = Run(profile, balance, start, seed);
                avg.Coins += s.Coins;
                avg.Strength += s.Strength;
                avg.Endurance += s.Endurance;
                avg.Condition += s.Condition;
                avg.WorkoutDays += s.WorkoutDays;
                avg.DietDays += s.DietDays;
                avg.CapHitDays += s.CapHitDays;
                avg.FinalCompliance += s.FinalCompliance;
                avg.OfflineMultiplier += s.OfflineMultiplier;
                avg.OfflinePerDay += s.OfflinePerDay;
            }

            avg.Coins /= Seeds;
            avg.Strength /= Seeds;
            avg.Endurance /= Seeds;
            avg.Condition /= Seeds;
            avg.WorkoutDays /= Seeds;
            avg.DietDays /= Seeds;
            avg.CapHitDays /= Seeds;
            avg.FinalCompliance /= Seeds;
            avg.OfflineMultiplier /= Seeds;
            avg.OfflinePerDay /= Seeds;
            return avg;
        }

        private static Summary Run(MockProfile profile, Balance balance, DateTime start, int seed)
        {
            var source = new MockActivitySource(profile, start, Days, seed);
            var engine = new StatEngine(balance);
            var history = new List<DailyOutcome>();
            var s = new Summary();

            for (int d = 0; d < Days; d++)
            {
                DateTime day = start.AddDays(d);
                var workouts = source.GetWorkouts(day, day.AddDays(1));

                DietDay? diet = null;
                var dietDays = source.GetDietDays(day, day.AddDays(1));
                if (dietDays.Count > 0) diet = dietDays[0];

                DailyOutcome outcome = engine.EvaluateDay(day, workouts, diet);
                history.Add(outcome);

                s.Coins += outcome.Coins;
                s.Strength += outcome.Strength;
                s.Endurance += outcome.Endurance;
                s.Condition += outcome.Condition;
                if (outcome.CapHit) s.CapHitDays++;
                if (outcome.HasWorkout) s.WorkoutDays++;
                if (outcome.HasDietLog) s.DietDays++;

                // 이행률은 항상 최근 7일 기준이다.
                var window = history.GetRange(Math.Max(0, history.Count - 7),
                                              Math.Min(7, history.Count));
                s.FinalCompliance = OfflineProgress.Compliance(window, balance);
            }

            s.OfflineMultiplier = OfflineProgress.Multiplier(s.FinalCompliance, balance);
            s.OfflinePerDay = OfflineProgress.Accrue(TimeSpan.FromHours(24), s.OfflineMultiplier, balance);
            return s;
        }

        private static void Report(MockProfile profile, Summary s)
        {
            Console.WriteLine("--- {0} ---", profile);
            Console.WriteLine("  운동한 날      {0,5:F1}/{1}일", s.WorkoutDays, Days);
            Console.WriteLine("  식단 기록      {0,5:F1}/{1}일", s.DietDays, Days);
            Console.WriteLine("  누적 코인      {0,5:F0}", s.Coins);
            Console.WriteLine("  스탯           근력 {0:F1} / 지구력 {1:F1} / 컨디션 {2:F1}",
                s.Strength, s.Endurance, s.Condition);
            Console.WriteLine("  최근 이행률    {0:P0}  ->  오프라인 {1:F2}배 ({2:F1} 코인/일)",
                s.FinalCompliance, s.OfflineMultiplier, s.OfflinePerDay);

            if (s.CapHitDays > 0.5f)
                Console.WriteLine("  ! 일일 캡에 평균 {0:F1}일 걸림 - 캡이 낮거나 획득량이 과하다", s.CapHitDays);

            Console.WriteLine();
        }

        // 시드 평균을 내므로 카운트도 실수로 둔다.
        private sealed class Summary
        {
            public float Coins;
            public float Strength, Endurance, Condition;
            public float WorkoutDays, DietDays, CapHitDays;
            public float FinalCompliance, OfflineMultiplier, OfflinePerDay;
        }
    }
}
