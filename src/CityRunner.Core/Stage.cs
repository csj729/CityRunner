using System;

namespace CityRunner.Core
{
    /// <summary>
    /// 스테이지 테이블과 전투 판정(§6.1 방치형 오토배틀러).
    ///
    /// 전투력을 스칼라 하나로 뭉치지 않는 이유: §4.2에서 스탯 3종에 서로 다른
    /// 역할을 줬기 때문이다. 하나로 합치면 유저가 가장 쉬운 활동만 반복하게 되고,
    /// 스탯을 나눈 의미가 사라진다.
    ///
    ///   근력   -> 공격력      (한 번 때릴 때의 피해)
    ///   지구력 -> 행동 횟수   (한 판에서 몇 번 때리는가)
    ///   컨디션 -> 생존        (버티는 만큼 행동 횟수가 늘어난다)
    /// </summary>
    public static class Stage
    {
        public static float Hp(int stage, Balance b)
        {
            return b.StageBaseHp * (float)Math.Pow(b.StageHpGrowth, stage - 1);
        }

        public static float AttackPower(PlayerState p, Balance b)
        {
            // 근력(속도)이 공격력의 주축. 상한은 장기 플레이용 방벽이다(§4.2.1).
            float fromStrength = Math.Min(p.Strength, b.StrengthAttackCap) * b.AttackPerStrength;

            return (b.BaseAttack + p.Endurance * b.AttackPerEndurance + fromStrength)
                 * (1f + p.GearLevel * b.GearAttackBonus);
        }

        public static float Actions(PlayerState p, Balance b)
        {
            return (b.BaseActions + p.Endurance * b.ActionsPerEndurance)
                 * (1f + p.Condition * b.ActionsPerCondition);
        }

        /// <summary>한 판에 낼 수 있는 총 피해. 이것이 스테이지 HP 이상이면 클리어.</summary>
        public static float TotalDamage(PlayerState p, Balance b)
        {
            return AttackPower(p, b) * Actions(p, b);
        }

        public static bool CanClear(PlayerState p, int stage, Balance b)
        {
            return TotalDamage(p, b) >= Hp(stage, b);
        }

        public static int CoinReward(int stage, Balance b)
        {
            return (int)Math.Round(b.StageBaseCoin * Math.Pow(b.StageCoinGrowth, stage - 1));
        }

        /// <summary>결정(하드 재화)은 최초 클리어에만, 그것도 몇 스테이지마다 한 번(§5).</summary>
        public static int GemReward(int stage, Balance b)
        {
            return stage % b.GemEveryNStages == 0 ? b.GemPerMilestone : 0;
        }
    }
}
