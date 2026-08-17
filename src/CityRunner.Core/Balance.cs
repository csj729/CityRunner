namespace CityRunner.Core
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
        /// 시간·거리는 손으로 부풀릴 수 있지만 "며칠 했는가"는 부풀리기 어렵다.
        /// 그래서 코인의 주된 몫을 빈도에 싣고, 부하 비례분은 보조로 둔다.
        /// </summary>
        public int CoinPerWorkoutDay = 12;

        /// <summary>
        /// 유산소 1분 x 강도 1.0 당 코인.
        ///
        /// 게임 진행의 연료는 유산소와 식단이다. 이 둘은 폰만으로 자동 기록되고
        /// GPS/케이던스/칼로리로 교차검증되기 때문이다. 센서로 검증할 수 없는
        /// 인풋은 애초에 게임에 들이지 않는다(§4.2.1).
        ///
        /// 0.2 였을 때 활동 코인이 부하에만 달려 있어, 느리게 오래 뛰는 쪽이 빠른
        /// 쪽보다 더 벌었다. 절반을 CoinPerSpeedOver 로 옮겼다.
        /// </summary>
        public float CoinPerCardioMinute = 0.08f;

        /// <summary>
        /// 바닥 속도(StrengthSpeedFloor) 초과분 1km/h 당 코인.
        ///
        /// 속도를 코인에 연결하는 유일한 통로다. 값은 근력이 쓰는 것과 정확히 같다 -
        /// 상한(MaxSpeedKmh)·하루 1세션·최소 세션 시간을 이미 통과한 뒤의 수치이므로,
        /// 코인에 연결해도 §7.3 조작 방어를 다시 설계할 필요가 없다.
        /// </summary>
        public float CoinPerSpeedOver = 3.5f;

        public int CoinPerMealLogged = 3;
        public int CoinProteinGoalBonus = 8;
        public int CoinRegularityBonus = 5;

        /// <summary>하루 코인 상한(§7.3 Tier0-1). 몰아서 입력해도 이득이 없게 한다.</summary>
        public int DailyCoinCap = 45;

        // --- 입력 타당성 상한(§7.3) ---------------------------------------
        //
        // 비율 감액(수동 30%)만으로는 무제한 입력을 막지 못한다는 것이 시뮬레이션에서
        // 확인됐다. 30%를 곱해도 수치를 5배로 적으면 그만이기 때문이다.
        // 그래서 레코드 단위로 생리학적 상한을 둔다. 초중급 유저의 상단을 넉넉히
        // 잡았으므로 정직한 기록은 걸리지 않는다.

        /// <summary>
        /// 근력에 반영하는 최대 속도(km/h). §2 가 타깃을 초중급으로 잡았고,
        /// 그 상단이 14 안팎(4'20"/km)이다. §7.3 "비현실적 속도 이상치 탐지"의
        /// 하드 상한 역할도 겸한다.
        /// </summary>
        public float MaxSpeedKmh = 14f;

        /// <summary>
        /// 하루에 인정하는 최대 유산소 부하(분 x 강도). 세션당이 아니라 하루 단위인
        /// 이유: 세션당 상한은 "세션을 여러 개 적기"로 그냥 우회된다. 하루 90은
        /// 고강도 한 세션(45분 x 2.0)에 해당하고, 정직한 초중급 기록은 닿지 않는다.
        /// </summary>
        public float MaxCardioLoadPerDay = 90f;

        /// <summary>하루에 보상으로 인정하는 최대 운동 세션 수.</summary>
        public int MaxWorkoutsPerDay = 2;

        // --- 기록 신뢰도(§7.2) -------------------------------------------

        public float TrustManual = 0.30f;
        public float TrustAutomatic = 1.00f;
        public float TrustActivelyRecorded = 1.00f;

        // --- 스탯 상승 ---------------------------------------------------

        /// <summary>
        /// 근력이 붙기 시작하는 속도(km/h). 이 아래는 근력 0이다.
        /// 걷기(약 5)를 제외해 "빠르면 많이, 느리면 적게"의 바닥을 만든다.
        /// </summary>
        public float StrengthSpeedFloor = 5f;

        /// <summary>바닥 초과분이 이만큼일 때마다 근력 1.0.</summary>
        public float SpeedPerStrength = 1.6f;

        /// <summary>
        /// 근력을 인정하는 최소 세션 시간(분).
        ///
        /// 근력은 속도만 보고 시간을 곱하지 않는다(§4.2.1). 그래서 가드가 없으면
        /// 2분 전력질주를 매일 반복하는 것이 최적 전략이 된다. 그건 건강 앱이
        /// 권해서는 안 되는 행동이다.
        /// </summary>
        public float MinStrengthSessionMinutes = 10f;

        /// <summary>
        /// (분 x 강도)가 이만큼 쌓일 때마다 지구력 1.0.
        /// 60 이었을 때 지구력이 근력에 밀려 사실상 죽어 있었다.
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
        /// 지구력이 공격력에 기여하는 몫. 작게 유지해야 한다 - 지구력은 이미 행동
        /// 횟수를 지배하므로, 공격력까지 크게 잡으면 제곱으로 작용해 오래 뛰는 쪽이
        /// 압도한다(0.3 이었을 때 느린 러너가 빠른 러너를 앞질렀다).
        ///
        /// 느린 러너의 완주는 이 값이 아니라 행동 횟수가 보장한다.
        /// </summary>
        public float AttackPerEndurance = 0.15f;

        /// <summary>
        /// 근력이 공격력에 기여하는 몫. 공격력의 주축이다 - 속도 차이가 진행에
        /// 드러나는 것은 이 계수를 통해서다(§4.2.1 선 2번).
        /// </summary>
        public float AttackPerStrength = 4.0f;

        /// <summary>
        /// 공격력에 반영되는 근력의 상한. 빠르게 뛴다고 무한히 세지지 않는다.
        /// 28일 기준으로는 아무도 닿지 않는다 - 장기 플레이용 방벽이다.
        /// </summary>
        public float StrengthAttackCap = 60f;

        public float BaseActions = 3f;
        public float ActionsPerEndurance = 0.5f;

        /// <summary>컨디션 1당 행동 횟수 몇 % 증가(생존 -> 더 오래 때린다).</summary>
        public float ActionsPerCondition = 0.05f;

        /// <summary>
        /// 스테이지 1 클리어 보상. 8 이었을 때 30스테이지 누적 등비합이 2,400 이 되어
        /// 스테이지 보상이 코인 수급의 75% 를 차지했다 - 게임 내 루프가 현실 활동보다
        /// 많은 코인을 만드는 상태라 §3 원칙과 어긋난다.
        ///
        /// 기울기(StageCoinGrowth)가 아니라 이 시작값을 낮춰야 한다. 기울기를 낮추면
        /// 스테이지 보상이 같이 눌리는데, **속도 우위가 코인으로 환산되는 통로가
        /// 스테이지 보상뿐**이라 속도 차이까지 사라진다(아래 참조).
        /// </summary>
        public float StageBaseCoin = 5f;

        /// <summary>
        /// 스테이지마다 보상이 몇 배씩 오르는가. HP 성장(1.18)보다 낮게 두어 뒤로
        /// 갈수록 난이도 대비 보상이 줄게 한다.
        ///
        /// 1.08 까지 낮춰봤더니 빠른 러너와 느린 러너의 총획득 차이가 1.18배에서
        /// 1.03배로 무너졌다. 활동 코인은 부하(시간 x 강도) 기반이라 느리게 오래
        /// 뛰는 쪽이 오히려 더 벌기 때문이다(519 대 445). 그래서 이 값은 유지한다.
        /// </summary>
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
