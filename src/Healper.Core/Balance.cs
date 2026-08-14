namespace Healper.Core
{
    /// <summary>
    /// 밸런싱 상수 전부. 숫자는 여기에만 존재한다.
    ///
    /// §11 난이도 함정 1번: "경제 밸런싱이 코딩보다 훨씬 어렵다. 코드로 밸런싱하면
    /// 못 고친다." 그래서 계산식은 StatEngine 에, 숫자는 전부 이 클래스에 둔다.
    /// 값을 바꿔가며 sim 을 돌리는 것이 스프레드시트 시뮬레이션을 대신한다.
    ///
    /// 아래 초기값은 근거 있는 수치가 아니라 출발점이다. Q2(§10)가 열려 있는 이유.
    /// </summary>
    public sealed class Balance
    {
        // --- 코인 획득 ---------------------------------------------------

        /// <summary>이만큼의 볼륨(kg x 회 x 세트)당 코인 1개.</summary>
        public float VolumePerCoin = 500f;

        /// <summary>유산소 1분 x 강도 1.0 당 코인.</summary>
        public float CoinPerCardioMinute = 0.5f;

        public int CoinPerMealLogged = 3;
        public int CoinProteinGoalBonus = 8;
        public int CoinRegularityBonus = 5;

        /// <summary>하루 코인 상한(§7.3 Tier0-1). 몰아서 입력해도 이득이 없게 한다.</summary>
        public int DailyCoinCap = 80;

        // --- 기록 신뢰도(§7.2) -------------------------------------------

        public float TrustManual = 0.30f;
        public float TrustAutomatic = 1.00f;
        public float TrustActivelyRecorded = 1.00f;

        // --- 스탯 상승 ---------------------------------------------------

        /// <summary>이만큼의 볼륨당 근력 1.0.</summary>
        public float VolumePerStrength = 2000f;

        /// <summary>(분 x 강도)가 이만큼 쌓일 때마다 지구력 1.0.</summary>
        public float CardioLoadPerEndurance = 60f;

        public float ConditionPerMeal = 0.15f;
        public float ConditionProteinBonus = 0.4f;
        public float ConditionRegularityBonus = 0.3f;

        // --- 오프라인 진행(§4.4) -----------------------------------------

        /// <summary>주간 목표 운동 횟수. 이행률 계산의 분모.</summary>
        public int WeeklyWorkoutTarget = 4;

        /// <summary>주간 목표 식단 기록 일수.</summary>
        public int WeeklyDietLogTarget = 7;

        /// <summary>방치해도 이만큼은 쌓인다. 0배는 금지 - 진행 상실은 이탈 최대 원인.</summary>
        public float OfflineMultiplierMin = 0.2f;

        /// <summary>꾸준한 유저의 상한. Min 과 10배 차이가 나도록 잡았다.</summary>
        public float OfflineMultiplierMax = 2.0f;

        /// <summary>이행률에서 운동이 차지하는 비중(나머지는 식단).</summary>
        public float ComplianceWorkoutWeight = 0.6f;

        /// <summary>오프라인 시간당 기본 코인.</summary>
        public float OfflineCoinPerHour = 4f;

        /// <summary>오프라인 누적 상한 시간. 이 이상 비워둬도 더 안 쌓인다.</summary>
        public float OfflineCapHours = 12f;

        // --- 스테이지 / 전투(§6.1) ---------------------------------------

        public float StageBaseHp = 100f;

        /// <summary>스테이지마다 HP가 몇 배씩 오르는가. 방치형 진행 곡선의 핵심 값.</summary>
        public float StageHpGrowth = 1.18f;

        public float BaseAttack = 10f;
        public float AttackPerStrength = 2.0f;

        public float BaseActions = 3f;
        public float ActionsPerEndurance = 0.5f;

        /// <summary>컨디션 1당 행동 횟수 몇 % 증가(생존 -> 더 오래 때린다).</summary>
        public float ActionsPerCondition = 0.05f;

        public float StageBaseCoin = 8f;
        public float StageCoinGrowth = 1.12f;

        public int GemEveryNStages = 5;
        public int GemPerMilestone = 1;

        /// <summary>한 번에 밀 수 있는 스테이지 상한. 폭주 진행을 막는 안전장치.</summary>
        public int MaxStagesPerTick = 50;

        // --- 코인 싱크(§5) -----------------------------------------------

        public float GearBaseCost = 50f;
        public float GearCostGrowth = 1.25f;

        /// <summary>장비 1레벨당 공격력 증가율.</summary>
        public float GearAttackBonus = 0.08f;

        public float TrustFactor(RecordTrust trust)
        {
            switch (trust)
            {
                case RecordTrust.Manual: return TrustManual;
                case RecordTrust.Automatic: return TrustAutomatic;
                case RecordTrust.ActivelyRecorded: return TrustActivelyRecorded;
                default: return TrustManual;
            }
        }
    }
}
