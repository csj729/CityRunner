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

            Summary top = totals[MockProfile.Consistent];
            Summary bottom = totals[MockProfile.Lapsed];

            Console.WriteLine("=== 유저 격차 (§4.4 목표: 약 10배) ===");
            Console.WriteLine("  오프라인 배수  {0:F1} vs {1:F1} 코인/일  ->  {2:F1}배",
                top.OfflinePerDay, bottom.OfflinePerDay, Ratio(top.OfflinePerDay, bottom.OfflinePerDay));

            // 배수만 보면 착시가 생긴다. 실제로 유저가 체감하는 건 총 획득과 도달 스테이지다.
            float topEarned = top.ActivityCoins + top.OfflineCoins + top.StageCoins;
            float bottomEarned = bottom.ActivityCoins + bottom.OfflineCoins + bottom.StageCoins;
            Console.WriteLine("  총 획득 코인   {0:F0} vs {1:F0}  ->  {2:F1}배",
                topEarned, bottomEarned, Ratio(topEarned, bottomEarned));
            Console.WriteLine("  도달 스테이지  {0:F1} vs {1:F1}  ->  {2:F1}배",
                top.Stage, bottom.Stage, Ratio(top.Stage, bottom.Stage));
            Console.WriteLine();

            // 이 게임의 재화원은 현실 운동이어야 한다(§3). 방치 수급이 활동 수급을
            // 압도하면 운동할 이유가 사라지므로, 비율을 항상 눈에 보이게 둔다.
            Console.WriteLine("=== 수급 구조 점검 (§3: 방치 수급이 활동을 압도하면 안 됨) ===");
            foreach (MockProfile profile in Enum.GetValues(typeof(MockProfile)))
            {
                Summary s = totals[profile];
                float total = s.ActivityCoins + s.OfflineCoins + s.StageCoins;
                float offlineShare = total > 0f ? s.OfflineCoins / total : 0f;

                // 활동이 거의 없는 유저는 최소 보장(OfflineMinPerCycle)이 비중을 채운다.
                // 이건 복귀 유인이라는 의도된 동작이므로 구조 문제와 구분해서 표시한다.
                float activityPerDay = s.ActivityCoins / Days;
                string note = "";
                if (offlineShare > 0.5f)
                {
                    note = activityPerDay >= balance.OfflineMinPerCycle
                        ? "   <- 운동보다 방치가 더 이득 (구조 문제)"
                        : "   (활동이 거의 없어 최소 보장이 지배 - 의도된 복귀 유인)";
                }

                Console.WriteLine("  {0,-11} 오프라인 비중 {1:P0}{2}", profile, offlineShare, note);
            }
            Console.WriteLine();

            // 매일 부풀린 수치를 손으로 넣는 유저가 정직한 유저를 앞지르면
            // §7.3 신뢰 설계가 뚫린 것이다. 매번 눈으로 찾지 않도록 지표로 둔다.
            Summary cheat = totals[MockProfile.Cheater];
            Console.WriteLine("=== 치팅 방어 점검 (§7.3) ===");
            Compare("활동 코인", cheat.ActivityCoins, top.ActivityCoins);
            Compare("근력", cheat.Strength, top.Strength);
            Compare("지구력", cheat.Endurance, top.Endurance);
            Compare("도달 스테이지", cheat.Stage, top.Stage);
            Console.WriteLine();
        }

        private static Summary RunAveraged(MockProfile profile, Balance balance, DateTime start)
        {
            var avg = new Summary();
            for (int seed = 0; seed < Seeds; seed++)
            {
                Summary s = Run(profile, balance, start, seed);
                avg.ActivityCoins += s.ActivityCoins;
                avg.OfflineCoins += s.OfflineCoins;
                avg.StageCoins += s.StageCoins;
                avg.CoinsLeft += s.CoinsLeft;
                avg.Strength += s.Strength;
                avg.Endurance += s.Endurance;
                avg.Condition += s.Condition;
                avg.WorkoutDays += s.WorkoutDays;
                avg.DietDays += s.DietDays;
                avg.CapHitDays += s.CapHitDays;
                avg.FinalCompliance += s.FinalCompliance;
                avg.OfflineMultiplier += s.OfflineMultiplier;
                avg.OfflinePerDay += s.OfflinePerDay;
                avg.Stage += s.Stage;
                avg.GearLevel += s.GearLevel;
                avg.Gems += s.Gems;
                avg.NextGearCost += s.NextGearCost;
            }

            avg.ActivityCoins /= Seeds;
            avg.OfflineCoins /= Seeds;
            avg.StageCoins /= Seeds;
            avg.CoinsLeft /= Seeds;
            avg.Strength /= Seeds;
            avg.Endurance /= Seeds;
            avg.Condition /= Seeds;
            avg.WorkoutDays /= Seeds;
            avg.DietDays /= Seeds;
            avg.CapHitDays /= Seeds;
            avg.FinalCompliance /= Seeds;
            avg.OfflineMultiplier /= Seeds;
            avg.OfflinePerDay /= Seeds;
            avg.Stage /= Seeds;
            avg.GearLevel /= Seeds;
            avg.Gems /= Seeds;
            avg.NextGearCost /= Seeds;
            return avg;
        }

        private static Summary Run(MockProfile profile, Balance balance, DateTime start, int seed)
        {
            var source = new MockActivitySource(profile, start, Days, seed);
            var engine = new StatEngine(balance);
            var progression = new Progression(balance);
            var history = new List<DailyOutcome>();
            var s = new Summary();

            for (int d = 0; d < Days; d++)
            {
                DateTime day = start.AddDays(d);

                // 자리를 비운 동안의 수급은 어제까지의 활동과 이행률로 정해진다(§4.4).
                if (history.Count > 0)
                {
                    int offline = OfflineProgress.Accrue(TimeSpan.FromHours(24), Window(history), balance);
                    progression.AddOfflineCoins(offline);
                    s.OfflineCoins += offline;
                }

                var workouts = source.GetWorkouts(day, day.AddDays(1));

                DietDay? diet = null;
                var dietDays = source.GetDietDays(day, day.AddDays(1));
                if (dietDays.Count > 0) diet = dietDays[0];

                DailyOutcome outcome = engine.EvaluateDay(day, workouts, diet);
                history.Add(outcome);
                progression.ApplyDay(outcome);

                // 강화 -> 스테이지 -> 보상으로 다시 강화. 이게 코어 루프 한 바퀴다.
                progression.AutoUpgrade();
                progression.PushStages();
                progression.AutoUpgrade();

                s.ActivityCoins += outcome.Coins;
                s.Strength += outcome.Strength;
                s.Endurance += outcome.Endurance;
                s.Condition += outcome.Condition;
                if (outcome.CapHit) s.CapHitDays++;
                if (outcome.HasWorkout) s.WorkoutDays++;
                if (outcome.HasDietLog) s.DietDays++;

                s.FinalCompliance = OfflineProgress.Compliance(Window(history), balance);
            }

            s.OfflineMultiplier = OfflineProgress.Multiplier(s.FinalCompliance, balance);
            s.OfflinePerDay = OfflineProgress.Accrue(TimeSpan.FromHours(24), Window(history), balance);

            s.Stage = progression.State.StageCleared;
            s.GearLevel = progression.State.GearLevel;
            s.Gems = progression.State.Gems;
            s.CoinsLeft = progression.State.Coins;
            s.StageCoins = progression.CoinsFromStages;
            s.NextGearCost = progression.GearCost(progression.State.GearLevel);
            return s;
        }

        private static void Report(MockProfile profile, Summary s)
        {
            float earned = s.ActivityCoins + s.OfflineCoins + s.StageCoins;

            Console.WriteLine("--- {0} ---", profile);
            Console.WriteLine("  운동한 날      {0,5:F1}/{1}일", s.WorkoutDays, Days);
            Console.WriteLine("  식단 기록      {0,5:F1}/{1}일", s.DietDays, Days);
            Console.WriteLine("  스탯           근력 {0:F1} / 지구력 {1:F1} / 컨디션 {2:F1}",
                s.Strength, s.Endurance, s.Condition);
            Console.WriteLine("  최근 이행률    {0:P0}  ->  오프라인 {1:F2}배 ({2:F1} 코인/일)",
                s.FinalCompliance, s.OfflineMultiplier, s.OfflinePerDay);
            Console.WriteLine("  도달 스테이지  {0,5:F1}      장비 Lv {1:F1}      결정 {2:F1}",
                s.Stage, s.GearLevel, s.Gems);
            Console.WriteLine("  코인 출처      활동 {0:F0} / 오프라인 {1:F0} / 스테이지 {2:F0}  (합 {3:F0})",
                s.ActivityCoins, s.OfflineCoins, s.StageCoins, earned);
            Console.WriteLine("  코인 잔고      {0,5:F0}   (다음 장비 {1:F0})", s.CoinsLeft, s.NextGearCost);

            if (s.CapHitDays > 0.5f)
                Console.WriteLine("  ! 일일 캡에 평균 {0:F1}일 걸림 - 캡이 낮거나 획득량이 과하다", s.CapHitDays);

            // §11 P3 성공 기준: 코인이 남아돌지 않는가.
            if (earned > 0f && s.CoinsLeft > earned * 0.25f)
                Console.WriteLine("  ! 잔고가 총획득의 {0:P0} - 싱크 부족(살 게 없다)", s.CoinsLeft / earned);

            Console.WriteLine();
        }

        private static void Compare(string label, float cheater, float honest)
        {
            float ratio = honest > 0f ? cheater / honest : 0f;
            string verdict = ratio > 1.05f ? "  <- 치터가 앞선다" : "  ok";
            Console.WriteLine("  {0,-13} 치터 {1,6:F1}  vs  정직 {2,6:F1}   ({3:F2}배){4}",
                label, cheater, honest, ratio, verdict);
        }

        private static float Ratio(float top, float bottom)
        {
            return bottom > 0f ? top / bottom : 0f;
        }

        /// <summary>이행률은 항상 최근 7일 기준이다.</summary>
        private static List<DailyOutcome> Window(List<DailyOutcome> history)
        {
            return history.GetRange(Math.Max(0, history.Count - 7), Math.Min(7, history.Count));
        }

        // 시드 평균을 내므로 카운트도 실수로 둔다.
        private sealed class Summary
        {
            public float ActivityCoins, OfflineCoins, StageCoins, CoinsLeft;
            public float Strength, Endurance, Condition;
            public float WorkoutDays, DietDays, CapHitDays;
            public float FinalCompliance, OfflineMultiplier, OfflinePerDay;
            public float Stage, GearLevel, Gems, NextGearCost;
        }
    }
}
