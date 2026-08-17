using System;

namespace CityRunner.Core
{
    /// <summary>
    /// 기록이 어떻게 만들어졌는지. §7.2 재화 지급률 차등의 근거이자
    /// Health Connect / HealthKit 의 recordingMethod 를 그대로 옮긴 값이다.
    /// </summary>
    public enum RecordTrust
    {
        Manual,           // 손으로 입력. 정직한 유저도 쓰므로 0%는 금지
        Automatic,        // 앱·워치가 사후 기록
        ActivelyRecorded, // 세션을 실시간으로 측정
    }

    /// <summary>
    /// 운동 1건. 종류 구분이 없는 것은 누락이 아니라 설계다(§4.2.1).
    /// 센서로 검증할 수 있는 활동만 게임에 들이므로, 남는 것은 유산소 세션뿐이다.
    /// </summary>
    public struct WorkoutRecord
    {
        public DateTime Start;
        public TimeSpan Duration;

        /// <summary>
        /// 이동 거리. 속도(= 거리 / 시간)를 통해 근력 스탯의 인풋이 된다(§4.2.1).
        /// Health Connect 의 DistanceRecord 에 대응한다.
        /// </summary>
        public float DistanceKm;

        /// <summary>강도 배수(1.0 저강도 ~ 2.0 고강도). 지구력 스탯의 인풋(§4.2.2).</summary>
        public float Intensity;

        public RecordTrust Trust;

        /// <summary>
        /// 평균 속도(km/h). 근력은 이 값만 본다 - 시간은 곱하지 않는다(§4.2.1).
        /// 시간을 곱하면 지구력과 같은 것을 두 번 재게 된다.
        /// </summary>
        public float SpeedKmh
        {
            get
            {
                double hours = Duration.TotalHours;
                return hours <= 0d ? 0f : (float)(DistanceKm / hours);
            }
        }

        public static WorkoutRecord Cardio(
            DateTime start, TimeSpan dur, float distanceKm, float intensity, RecordTrust trust)
        {
            return new WorkoutRecord
            {
                Start = start, Duration = dur, DistanceKm = distanceKm,
                Intensity = intensity, Trust = trust,
            };
        }
    }

    /// <summary>
    /// 하루치 식단 요약.
    ///
    /// 칼로리 필드가 없는 것은 누락이 아니라 설계다(§7.1). 칼로리를 재화와 연결하면
    /// "덜 먹을수록 이득"이 되어 섭식장애 행동을 강화한다. 그래서 이 구조체는
    /// 빼는 목표가 아니라 채우는 목표(기록했는가 / 단백질 / 규칙성)만 담는다.
    /// </summary>
    public struct DietDay
    {
        public DateTime Date;

        /// <summary>기록한 끼니 수. 무엇을 먹었든 기록 자체에 보상한다.</summary>
        public int MealsLogged;

        /// <summary>단백질 목표 달성 여부(채우는 목표).</summary>
        public bool ProteinGoalMet;

        /// <summary>끼니를 거르지 않았는가(규칙성). 과소섭취는 오히려 감점.</summary>
        public bool AteAllMainMeals;

        public RecordTrust Trust;
    }
}
