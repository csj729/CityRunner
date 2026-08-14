using System;

namespace Healper.Core
{
    /// <summary>유저의 영속 상태. 저장/불러오기 대상이 되는 값들.</summary>
    public sealed class PlayerState
    {
        public float Strength;
        public float Endurance;
        public float Condition;

        public int Coins;
        public int Gems;

        /// <summary>코인의 주 소비처(§5). 올릴수록 공격력 배수가 붙는다.</summary>
        public int GearLevel;

        /// <summary>마지막으로 깬 스테이지. 0이면 아직 하나도 못 깼다.</summary>
        public int StageCleared;
    }

    /// <summary>
    /// 하루 단위로 굴러가는 진행 루프.
    ///
    ///   활동 -> 코인·스탯 -> 장비 강화 -> 스테이지 클리어 -> 코인·결정 -> 다시 강화
    ///
    /// 코인이 남아도는지(§11 P3 성공 기준)를 보려면 소비까지 모사해야 해서,
    /// "살 수 있으면 산다"는 단순 탐욕 정책으로 유저 행동을 대신한다.
    /// </summary>
    public sealed class Progression
    {
        private readonly Balance _b;

        public PlayerState State { get; private set; }

        /// <summary>스테이지 보상으로 들어온 코인 누적. 활동 보상과 비중을 비교하기 위함.</summary>
        public int CoinsFromStages { get; private set; }

        public Progression(Balance balance)
        {
            _b = balance;
            State = new PlayerState();
        }

        public void ApplyDay(DailyOutcome outcome)
        {
            State.Coins += outcome.Coins;
            State.Strength += outcome.Strength;
            State.Endurance += outcome.Endurance;
            State.Condition += outcome.Condition;
        }

        public void AddOfflineCoins(int coins)
        {
            State.Coins += coins;
        }

        /// <summary>깰 수 있는 만큼 스테이지를 밀고, 보상을 받는다.</summary>
        public int PushStages()
        {
            int gained = 0;
            while (gained < _b.MaxStagesPerTick)
            {
                int next = State.StageCleared + 1;
                if (!Stage.CanClear(State, next, _b)) break;

                State.StageCleared = next;
                int reward = Stage.CoinReward(next, _b);
                State.Coins += reward;
                CoinsFromStages += reward;
                State.Gems += Stage.GemReward(next, _b);
                gained++;
            }
            return gained;
        }

        /// <summary>살 수 있는 만큼 장비를 올린다. 코인 싱크가 충분한지 보는 장치.</summary>
        public int AutoUpgrade()
        {
            int bought = 0;
            while (true)
            {
                int cost = GearCost(State.GearLevel);
                if (State.Coins < cost) break;
                State.Coins -= cost;
                State.GearLevel++;
                bought++;
            }
            return bought;
        }

        public int GearCost(int level)
        {
            return (int)Math.Round(_b.GearBaseCost * Math.Pow(_b.GearCostGrowth, level));
        }
    }
}
