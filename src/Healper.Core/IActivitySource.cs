using System;
using System.Collections.Generic;

namespace Healper.Core
{
    /// <summary>
    /// 게임이 활동 데이터를 읽는 유일한 통로. 구현체는 두 개다.
    ///
    ///   MockActivitySource      - 가짜 데이터. 기기 없이 밸런싱을 돌린다.
    ///   HealthConnectSource     - 실제 연동. Track A(P0)가 끝나면 갈아끼운다.
    ///
    /// 동기 API인 이유: Health Connect 읽기는 비동기지만, 그 결과는 어차피
    /// 로컬 DB에 적재한 뒤(§9.3 로컬 온리) 게임이 DB를 읽는 구조가 된다.
    /// 비동기는 "동기화" 단계의 문제이지 이 인터페이스의 문제가 아니다.
    /// </summary>
    public interface IActivitySource
    {
        IReadOnlyList<WorkoutRecord> GetWorkouts(DateTime from, DateTime to);

        IReadOnlyList<DietDay> GetDietDays(DateTime from, DateTime to);
    }
}
