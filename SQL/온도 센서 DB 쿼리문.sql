-- SQL Server의 시스템 데이터베이스인 master에서 시작
USE master;
GO

-- DB_ID()는 데이터베이스의 ID를 반환함
-- TemperatureMonitorDB가 아직 없으면 새로 생성
IF DB_ID(N'TemperatureMonitorDB') IS NULL
BEGIN
    CREATE DATABASE TemperatureMonitorDB;
END;
GO

-- 이후 테이블과 인덱스를 생성할 데이터베이스 선택
USE TemperatureMonitorDB;
GO

-- 트랜잭션 처리 중 실행 오류가 발생하면 전체 작업을 롤백하도록 설정
SET XACT_ABORT ON;

BEGIN TRY
    -- 아래 세 테이블과 인덱스를 하나의 트랜잭션으로 생성
    -- 위에서 실행한 CREATE DATABASE는 이 트랜잭션의 범위 밖에 있음
    BEGIN TRANSACTION;

    -- ============================================================
    -- 1. 장비 접속 정보
    -- 장비 한 대당 한 행을 저장하고, DeviceId로 알람 이력과 연결
    -- ============================================================
    CREATE TABLE dbo.Device
    (
        -- 장비 고유 번호. 첫 번째 값은 1이고 새 행마다 1씩 증가
        DeviceId    int IDENTITY(1, 1) NOT NULL,

        -- 화면에 표시할 장비 이름. nvarchar는 한글 이름도 저장 가능
        DeviceName  nvarchar(50)       NOT NULL,

        -- 장비의 MAC 주소. 구분자를 포함한 17자리 형식을 저장하기 위한 열
        MacAddress  char(17)           NOT NULL,

        -- 마지막으로 확인한 장비 IP. 아직 주소를 모르면 NULL 허용
        LastKnownIp varchar(45)        NULL,

        -- 장비에 TCP로 연결할 포트. INSERT할 때 생략하면 502가 입력됨
        TcpPort     int                NOT NULL
            CONSTRAINT DF_Device_TcpPort DEFAULT (502),

        -- 기본 키(PK): DeviceId로 장비 한 대를 식별
        CONSTRAINT PK_Device
            PRIMARY KEY (DeviceId),

        -- 고유 제약조건(UQ): 같은 MAC 주소 값으로 장비가 중복 등록되는 것을 방지
        CONSTRAINT UQ_Device_MacAddress
            UNIQUE (MacAddress),

        -- 검사 제약조건(CK): 접속 포트는 1~65535 범위의 정수만 허용
        CONSTRAINT CK_Device_TcpPort
            CHECK (TcpPort BETWEEN 1 AND 65535)
    );

    -- ============================================================
    -- 2. 알람 발생·확인·복귀 이력
    --    알람 한 건당 한 행을 저장하고, 확인·복귀 시각을 같은 행에 기록
    -- ============================================================
    CREATE TABLE dbo.AlarmEvent
    (
        -- 알람 고유 번호. 같은 장비에서 다시 발생한 알람도 개별 식별
        AlarmId bigint IDENTITY(1, 1) NOT NULL,

        -- 이 알람이 어느 장비에서 발생했는지 나타내는 번호
        DeviceId int NOT NULL,

        -- 알람 종류: 고온, 센서 오류, 통신 끊김
        AlarmType varchar(20) NOT NULL,

        -- 화면과 이력에 표시할 알람 내용
        Message nvarchar(250) NOT NULL,

        -- 알람이 발생했을 때의 온도. 예: 93.6℃
        -- decimal(5, 1)은 전체 다섯 자리 중 소수점 아래 한 자리까지 저장
        -- 통신 끊김처럼 발생 온도를 알 수 없는 알람은 NULL 허용
        TriggerTemperature decimal(5, 1) NULL,

        -- 알람 발생 시각. INSERT할 때 생략하면 SQL Server의 현재 UTC 시각을 저장
        -- datetime2(3)은 초의 소수 부분을 세 자리(밀리초)까지 기록
        OccurredAt datetime2(3) NOT NULL
            CONSTRAINT DF_AlarmEvent_OccurredAt DEFAULT (SYSUTCDATETIME()),

        -- 작업자가 알람을 확인한 시각. 아직 확인하지 않았으면 NULL
        AcknowledgedAt datetime2(3) NULL,

        -- 장비가 정상 상태로 복귀한 시각. 아직 복귀하지 않았으면 NULL
        RecoveredAt datetime2(3) NULL,

        -- 기본 키: AlarmId로 알람 한 건을 식별
        CONSTRAINT PK_AlarmEvent PRIMARY KEY (AlarmId),

        -- 외래 키(FK): Device 테이블에 등록된 장비의 알람만 저장 가능
        -- 한 장비에서 여러 번 알람이 발생할 수 있으므로 장비와 알람은 1:N 관계
        CONSTRAINT FK_AlarmEvent_Device
            FOREIGN KEY (DeviceId)
            REFERENCES dbo.Device (DeviceId),

        -- 정의한 세 가지 알람 코드만 허용
        CONSTRAINT CK_AlarmEvent_AlarmType CHECK (AlarmType IN ('HIGH_TEMP', 'SENSOR_ERROR', 'COMM_LOST')),

        -- 확인 전에는 NULL 허용. 확인 시각을 기록했다면 발생 시각과 같거나 이후여야 함
        CONSTRAINT CK_AlarmEvent_AcknowledgedAt CHECK (AcknowledgedAt IS NULL OR AcknowledgedAt >= OccurredAt),

        -- 복귀 전에는 NULL 허용. 복귀 시각을 기록했다면 발생 시각과 같거나 이후여야 함
        -- 작업자가 확인하기 전에 장비가 먼저 정상 복귀하는 경우도 허용
        CONSTRAINT CK_AlarmEvent_RecoveredAt CHECK (RecoveredAt IS NULL OR RecoveredAt >= OccurredAt)
    );

    -- 필터링된 고유 인덱스: 아직 복귀하지 않은 행에만 중복 방지 규칙 적용
    -- 예: 한 장비의 COMM_LOST가 지속되는 동안에는 활성 COMM_LOST를 한 건만 허용
    -- 작업자가 확인 버튼을 눌러도 RecoveredAt이 NULL이면 여전히 활성 상태
    -- 복귀 시각이 기록되면 기존 행은 이력으로 남고, 다음 통신 끊김을 새 행으로 등록 가능
    CREATE UNIQUE INDEX UX_AlarmEvent_Active_Device_Type
        ON dbo.AlarmEvent (DeviceId, AlarmType)
        WHERE RecoveredAt IS NULL;

    -- ============================================================
    -- 3. 카카오톡 발송 상태
    -- 발송 대상 알람에 대해 현재 상태와 누적 시도 횟수를 한 행에 저장
    -- ============================================================
    CREATE TABLE dbo.AlarmNotification
    (
        -- 발송의 원인이 된 알람 번호. AlarmEvent의 AlarmId를 그대로 사용
        AlarmId bigint NOT NULL,

        -- 발송 상태. 새 행을 추가할 때 생략하면 PENDING(대기)으로 시작
        SendStatus varchar(10) NOT NULL
            CONSTRAINT DF_AlarmNotification_SendStatus DEFAULT ('PENDING'),

        -- 지금까지 발송을 시도한 횟수. 기본값 0은 아직 시도하지 않았다는 의미
        AttemptCount  int           NOT NULL
            CONSTRAINT DF_AlarmNotification_AttemptCount DEFAULT (0),

        -- 가장 최근에 발송을 시도한 시각
        LastAttemptAt datetime2(3)  NULL,

        -- 발송에 성공한 시각. 대기 또는 실패 상태에서는 NULL
        SentAt datetime2(3)  NULL,

        -- 최근 발송 오류 내용. 기록할 오류가 없으면 NULL
        LastError nvarchar(500) NULL,

        -- AlarmId 자체가 기본 키이므로 같은 알람에 대한 발송 상태 행은 최대 한 개
        -- 여러 번 시도하더라도 시도별 행을 추가하지 않고 기존 행의 횟수와 상태를 갱신
        CONSTRAINT PK_AlarmNotification
            PRIMARY KEY (AlarmId),

        -- 실제로 존재하는 알람에 대해서만 발송 상태를 등록할 수 있음
        CONSTRAINT FK_AlarmNotification_AlarmEvent
            FOREIGN KEY (AlarmId)
            REFERENCES dbo.AlarmEvent (AlarmId),

        -- 발송 상태는 대기(PENDING), 성공(SENT), 실패(FAILED) 중 하나만 허용
        CONSTRAINT CK_AlarmNotification_SendStatus
            CHECK (SendStatus IN ('PENDING', 'SENT', 'FAILED')),

        -- 발송 시도 횟수에 음수가 입력되는 것을 방지
        CONSTRAINT CK_AlarmNotification_AttemptCount
            CHECK (AttemptCount >= 0),

        -- 발송 상태와 성공 시각이 서로 맞는지 검사
        -- SENT이면 SentAt이 반드시 있어야 하고, PENDING 또는 FAILED이면 NULL이어야 함
        CONSTRAINT CK_AlarmNotification_SentAt
            CHECK (
                (SendStatus = 'SENT' AND SentAt IS NOT NULL)
                OR
                (SendStatus IN ('PENDING', 'FAILED') AND SentAt IS NULL)
            )
    );

    -- 테이블과 인덱스가 모두 정상적으로 생성되면 변경 사항을 확정
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    -- XACT_STATE()가 0이면 열린 트랜잭션이 없음
    -- 1 또는 -1이면 아직 트랜잭션이 남아 있으므로 이번 생성 작업을 취소
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    -- 오류 번호와 메시지를 호출한 쪽에 다시 전달해 SSMS에서 원인을 확인할 수 있게 함
    THROW;
END CATCH;
GO