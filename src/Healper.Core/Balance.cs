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

        /// <summary>
        /// 그날 운동을 했다는 사실 자체에 주는 코인(§7.3 Tier0-2 "누적량이 아니라 연속성").
        /// 볼륨은 손으로 부풀릴 수 있지만 "며칠 했는가"는 부풀리기 어렵다.
        /// 그래서 코인의 주된 몫을 빈도에 싣고, 볼륨 비례분은 보조로 둔다.
        /// </summary>
        public int CoinPerWorkoutDay = 12;

        /// <summary>
        /// 유산소 1분 x 강도 1.0 당 코인.
        ///
        /// 비대칭 설계: 게임 진행의 연료는 유산소와 식단이다. 이 둘은 폰만으로
        /// 자동 기록되고 GPS/케이던스/칼로리로 교차검증되기 때문이다.
        /// 웨이트는 볼륨 비례 코인을 주지 않는다 - 중량은 Health Connect 에 아예
        /// 없는 데이터라 어떤 센서로도 검증할 수 없고, 부정확한 입력이 게임 경제를
        /// 오염시키는 통로가 된다. 웨이트는 근력 스탯만 올린다.
        /// </summary>
        public float CoinPerCardioMinute = 0.2f;

        /// <summary>
        /// 웨이트 세션 1회당 고정 코인. 볼륨과 무관하다.
        ///
        /// 유산소 한 세션의 평균 코인과 비슷하게 맞춘다. 비대칭 설계의 목적은
        /// "웨이트 없이도 완주 가능"이지 "웨이트를 하면 손해"가 아니다.
        /// 웨이트와 유산소의 차이는 받는 양이 아니라 오르는 스탯이어야 한다.
        /// 고정값이므로 볼륨을 부풀려도 코인은 늘지 않는다.
        /// </summary>
        public float CoinPerStrengthSession = 10f;

        public int CoinPerMealLogged = 3;
        public int CoinProteinGoalBonus = 8;
        public int CoinRegularityBonus = 5;

        /// <summary>하루 코인 상한(§7.3 Tier0-1). 몰아서 입력해도 이득이 없게 한다.</summary>
        public int DailyCoinCap = 45;

        // --- 입력 타당성 상한(§7.3) ---------------------------------------
        //
        // 비율 감액(수동 30%)만으로는 무제한 입력을 막지 못한다는 것이 시뮬레이션에서
        // 확인됐다. 30%를 곱해도 볼륨을 5배로 적으면 그만이기 때문이다.
        // 그래서 레코드 단위로 생리학적 상한을 둔다. 초중급 유저의 상단을 넉넉히
        // 잡았으므로 정직한 기록은 걸리지 않는다.

        /// <summary>한 세션에서 인정하는 최대 볼륨. 초중급 상단이 1만 안팎.</summary>
        public float MaxVolumePerSession = 15000f;

        /// <summary>한 세션에서 인정하는 최대 유산소 부하(분 x 강도).</summary>
        public float MaxCardioLoadPerSession = 90f;

        /// <summary>하루에 보상으로 인정하는 최대 운동 세션 수.</summary>
        public int MaxWorkoutsPerDay = 2;

        // --- 기록 신뢰도(§7.2) -------------------------------------------

        public float TrustManual = 0.30f;
        public float TrustAutomatic = 1.00f;
        public float TrustActivelyRecorded = 1.00f;

        // --- 스탯 상승 ---------------------------------------------------

        /// <summary>이만큼의 볼륨당 근력 1.0.</summary>
        public float VolumePerStrength = 2000f;

        /// <summary>
        /// 하루에 오를 수 있는 근력 상한. 웨이트는 검증 수단이 없으므로
        /// 영향 범위를 하루 단위로 격리한다.
        /// </summary>
        public float StrengthStatDailyCap = 4f;

        /// <summary>
        /// (분 x 강도)가 이만큼 쌓일 때마다 지구력 1.0.
        /// 60 이었을 때 근력 40.8 대 지구력 5.3 으로 지구력 스탯이 사실상 죽어 있었다.
        /// </summary>
        public float CardioLoadPerEndurance = 12f;

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

        /// <summary>
        /// 오프라인 한 사이클이 최근 활동 수급의 몇 %인가.
        ///
        /// 여기가 절대 상수였을 때 오프라인 수급이 활동 수급을 5배 압도했다(§3 구조 충돌).
        /// 활동 수급에 비례시키면, 운동을 안 할 때 오프라인 수급도 같이 말라붙는다.
        /// 1.0 을 넘기면 "운동보다 방치가 이득"으로 되돌아가므로 넘기지 말 것.
        /// </summary>
        public float OfflineShareOfActivity = 0.5f;

        /// <summary>
        /// 활동이 0이어도 이만큼은 보장한다. 0으로 두면 이탈한 유저의 복귀 유인이
        /// 사라진다 - 진행 상실은 이탈의 최대 원인이다.
        /// </summary>
        public float OfflineMinPerCycle = 5f;

        /// <summary>오프라인 누적 상한 시간. 이 이상 비워둬도 더 안 쌓인다.</summary>
        public float OfflineCapHours = 12f;

        // --- 스테이지 / 전투(§6.1) ---------------------------------------

        public float StageBaseHp = 100f;

        /// <summary>스테이지마다 HP가 몇 배씩 오르는가. 방치형 진행 곡선의 핵심 값.</summary>
        public float StageHpGrowth = 1.18f;

        public float BaseAttack = 10f;

        /// <summary>
        /// 지구력이 공격력에 기여하는 몫. 웨이트를 전혀 하지 않아도 완주할 수 있도록
        /// 바닥을 깔아주는 역할이다.
        ///
        /// 작게 유지해야 한다. 지구력은 이미 행동 횟수를 지배하므로, 공격력까지 크게
        /// 잡으면 제곱으로 작용해 유산소만 하는 쪽이 압도한다(실제로 그렇게 됐었다).
        /// </summary>
        public float AttackPerEndurance = 0.3f;

        /// <summary>근력이 공격력에 기여하는 몫. 공격력의 주축이다.</summary>
        public float AttackPerStrength = 2.0f;

        /// <summary>
        /// 공격력에 반영되는 근력의 상한. 웨이트를 많이 한다고 무한히 세지지 않는다.
        /// 검증 불가능한 입력이 진행을 지배하지 못하게 막는 마지막 방벽.
        /// </summary>
        public float StrengthAttackCap = 40f;

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
