-- 이 프로시저의 역할은 
-- 같은 장비에서 같은 오류가 계속되는 동안에는 
-- 한 건만 기록하고, 오류가 해소되면 그 행에 복귀 시각을 기록


-- 프로시저를 생성하거나 수정할 데이터베이스 선택
USE TemperatureMonitorDB;
GO

-- 프로시저가 없으면 생성하고, 이미 있으면 기존 정의를 수정
CREATE OR ALTER PROCEDURE dbo.SaveDeviceFault
    @DeviceId  int,            -- 오류가 발생하거나 해소된 장비의 ID
    @AlarmType varchar(20),    -- 오류 종류: SENSOR_ERROR(센서 오류), COMM_LOST(통신 끊김)
    @IsActive  bit,            -- 현재 오류 상태: 1 = 오류 발생 또는 지속, 0 = 오류 해소
    @Message   nvarchar(250)   -- 오류를 새로 등록할 때 저장할 메시지
AS
BEGIN
    -- 각 SQL 문이 끝날 때 클라이언트에 영향받은 행 수를 별도로 전송하지 않음
    SET NOCOUNT ON;

    -- 실행 중 오류가 발생하면 트랜잭션 전체를 롤백하도록 설정
    SET XACT_ABORT ON;

    -- 이 프로시저가 처리하는 오류 종류인지 확인
    -- 예를 들어 HIGH_TEMP가 들어오면 아래 저장 작업을 시작하기 전에 예외 발생
    IF @AlarmType NOT IN ('SENSOR_ERROR', 'COMM_LOST')
        THROW 50001, N'지원하지 않는 오류 종류입니다.', 1;

    BEGIN TRY
        -- 활성 오류 조회부터 등록 또는 복귀 처리까지 하나의 작업으로 묶음
        BEGIN TRANSACTION;

        -- 기존에 발생한 오류의 ID를 담을 변수. 초기값은 NULL
        DECLARE @AlarmId bigint;

        -- 같은 장비에서 발생한 같은 종류의 오류 중 아직 복귀하지 않은 건을 찾음
        -- UPDLOCK: 조회할 때부터 갱신용 잠금을 요청
        -- HOLDLOCK: 트랜잭션이 끝날 때까지 조회 조건에 해당하는 범위를 보호
        -- 두 요청이 동시에 들어와 둘 다 "기존 오류 없음"으로 판단하는 상황을 방지
        SELECT @AlarmId = AlarmId
        FROM dbo.AlarmEvent WITH (UPDLOCK, HOLDLOCK)
        WHERE DeviceId = @DeviceId
          AND AlarmType = @AlarmType
          AND RecoveredAt IS NULL;   -- 복귀 시각이 비어 있으면 현재 활성 상태

        -- 현재 오류 상태이고, 아직 등록된 활성 오류가 없으면 새 이력 한 건 추가
        -- 이미 활성 오류가 있으면 이 조건을 건너뛰므로 같은 오류가 반복 저장되지 않음
        IF @IsActive = 1 AND @AlarmId IS NULL
        BEGIN
            INSERT INTO dbo.AlarmEvent
                (DeviceId, AlarmType, Message)
            VALUES
                (@DeviceId, @AlarmType, @Message);
        END;

        -- 오류가 해소됐고, 복귀 처리할 활성 오류가 있으면 해당 행을 갱신
        -- 기존 발생 이력은 보존하고 복귀 시각만 UTC 기준으로 기록
        IF @IsActive = 0 AND @AlarmId IS NOT NULL
        BEGIN
            UPDATE dbo.AlarmEvent
            SET RecoveredAt = SYSUTCDATETIME()
            WHERE AlarmId = @AlarmId;
        END;

        -- 오류 없이 끝났으면 이번 트랜잭션의 변경 사항을 확정
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        -- XACT_STATE()가 0이면 열린 트랜잭션이 없음
        -- 1 또는 -1이면 아직 트랜잭션이 남아 있으므로 변경 사항을 취소
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        -- 원래 발생한 예외를 호출한 쪽(C# 프로그램 등)에 다시 전달
        THROW;
    END CATCH;
END;
GO