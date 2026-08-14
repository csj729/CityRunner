using System;

namespace Healper.Core
{
    /// <summary>운동 종류. 스탯 3종(§4.2) 중 근력/지구력에 각각 대응한다.</summary>
    public enum WorkoutKind
    {
        Strength, // 웨이트·저항운동 -> 근력
        Cardio,   // 유산소         -> 지구력
    }

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

    /// <summary>운동 1건.</summary>
    public struct WorkoutRecord
    {
        public DateTime Start;
        public TimeSpan Duration;
        public WorkoutKind Kind;

        /// <summary>Strength 전용: 총 볼륨(중량 x 횟수 x 세트). Cardio 는 0.</summary>
        public float Volume;

        /// <summary>Cardio 전용: 강도 배수(1.0 저강도 ~ 2.0 고강도). Strength 는 0.</summary>
        public float Intensity;

        public RecordTrust Trust;

        public static WorkoutRecord Strength(DateTime start, TimeSpan dur, float volume, RecordTrust trust)
        {
            return new WorkoutRecord
            {
                Start = start, Duration = dur, Kind = WorkoutKind.Strength,
                Volume = volume, Intensity = 0f, Trust = trust,
            };
        }

        public static WorkoutRecord Cardio(DateTime start, TimeSpan dur, float intensity, RecordTrust trust)
        {
            return new WorkoutRecord
            {
                Start = start, Duration = dur, Kind = WorkoutKind.Cardio,
                Volume = 0f, Intensity = intensity, Trust = trust,
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
