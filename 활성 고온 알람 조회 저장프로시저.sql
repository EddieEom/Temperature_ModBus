--이 프로시저는 C#에서 판정한 고온 상태를 DB에 기록함.
-- @IsActive가 1이면 고온 발생, 0이면 정상 복귀를 뜻해. 
-- 80.0℃ 이상 3회 연속인지 계산하는 곳은 C#이고, 
-- SQL의 @Temperature는 알람이 처음 발생한 순간의 온도를 저장하는 데 사용


-- 프로시저를 생성하거나 수정할 데이터베이스 선택
USE TemperatureMonitorDB;
GO

-- 프로시저가 없으면 생성하고, 이미 있으면 정의를 수정
CREATE OR ALTER PROCEDURE dbo.SaveHighTemperatureAlarm
    @DeviceId    int,           -- 고온 상태를 기록할 장비의 ID
    @IsActive    bit,           -- C#에서 판정한 상태: 1 = 고온, 0 = 정상 복귀
    @Temperature decimal(5, 1)  -- 이번 측정 온도. 새 알람을 등록할 때 발생 온도로 저장
AS
BEGIN
    -- 각 SQL 문을 실행할 때 영향받은 행 수를 클라이언트에 별도로 전송하지 않음
    SET NOCOUNT ON;

    -- 실행 오류가 발생하면 트랜잭션 전체를 롤백하도록 설정
    SET XACT_ABORT ON;

    BEGIN TRY
        -- 활성 알람 조회부터 발생 등록 또는 복귀 처리까지 한 트랜잭션으로 묶음
        BEGIN TRANSACTION;

        -- 기존 활성 알람을 찾거나 새 알람을 등록했을 때 그 번호를 보관
        -- 선언 직후 값은 NULL
        DECLARE @AlarmId bigint;

        -- 해당 장비에서 발생한 고온 알람 중 아직 복귀하지 않은 건을 찾음
        -- UPDLOCK: 조회한 데이터를 갱신할 수 있도록 갱신용 잠금을 요청
        -- HOLDLOCK: 트랜잭션이 끝날 때까지 조회 범위를 보호
        -- 같은 장비의 고온 요청 두 건이 동시에 "활성 알람 없음"으로 판단하는 것을 방지
        SELECT @AlarmId = AlarmId
        FROM dbo.AlarmEvent WITH (UPDLOCK, HOLDLOCK)
        WHERE DeviceId = @DeviceId
          AND AlarmType = 'HIGH_TEMP'
          AND RecoveredAt IS NULL;  -- 복귀 시각이 비어 있으면 활성 알람

        -- 1. 고온으로 판정됐고 기존 활성 알람이 없으면 새 알람을 등록
        -- 이미 활성 고온 알람이 있으면 이 블록을 건너뛰므로 중복 등록되지 않음
        IF @IsActive = 1 AND @AlarmId IS NULL
        BEGIN
            -- 고온 판정이 성립한 순간의 온도를 저장
            -- OccurredAt은 테이블의 기본값에 의해 현재 UTC 시각으로 기록됨
            INSERT INTO dbo.AlarmEvent
                (DeviceId, AlarmType, Message, TriggerTemperature)
            VALUES
                (@DeviceId, 'HIGH_TEMP',
                 N'80.0 ℃ 이상 3회 연속 수신', @Temperature);

            -- 바로 위 INSERT에서 자동 생성된 AlarmId를 가져옴
            -- SCOPE_IDENTITY()는 현재 세션의 같은 범위에서 생성된 마지막 IDENTITY 값을 반환
            -- 반환값을 AlarmId 열과 같은 bigint 형식으로 변환해 변수에 저장
            SET @AlarmId = CONVERT(bigint, SCOPE_IDENTITY());

            -- 같은 AlarmId로 카카오톡 발송 대기 건을 등록
            -- 테이블 기본값에 따라 SendStatus = 'PENDING', AttemptCount = 0으로 시작
            -- 실제 메시지는 별도의 C# 발송 작업이 이 대기 건을 조회해 전송
            INSERT INTO dbo.AlarmNotification (AlarmId)
            VALUES (@AlarmId);
        END;

        -- 2. 정상 복귀로 판정됐고 기존 활성 알람이 있으면 해당 건을 복귀 처리
        -- 활성 알람이 없는 상태에서 정상 값이 들어오면 갱신할 행이 없으므로 건너뜀
        IF @IsActive = 0 AND @AlarmId IS NOT NULL
        BEGIN
            -- 기존 발생 온도와 발생 시각은 보존하고, 같은 행에 UTC 복귀 시각을 기록
            UPDATE dbo.AlarmEvent
            SET RecoveredAt = SYSUTCDATETIME()
            WHERE AlarmId = @AlarmId;
        END;

        -- 지금까지의 변경을 확정
        -- 새 고온 알람인 경우 AlarmEvent와 AlarmNotification 두 행이 함께 확정됨
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        -- 오류 발생 후에도 트랜잭션이 남아 있으면 이번 변경을 전부 취소
        -- 예: 알람 등록 후 발송 대기 등록에 실패하면 앞서 등록한 알람도 함께 취소
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        -- 원래 오류를 호출한 쪽에 다시 전달해 실패 원인을 확인할 수 있게 함
        THROW;
    END CATCH;
END;
GO



-- 로직 예시 : 고온 판정이 성립한 순간의 온도가 93.6℃라면 이 온도가 AlarmEvent.TriggerTemperature에 저장되고, 
-- AlarmNotification에도 같은 AlarmId = 19로 발송 대기 행이 생김. 고온이 지속되는 동안에는 93.6℃가 최신 온도로 덮어써지지 않음. 
-- 정상 복귀가 확인되면 19번 행의 RecoveredAt이 채워지고, 이후 다시 고온이 발생하면 새 알람 번호로 기록