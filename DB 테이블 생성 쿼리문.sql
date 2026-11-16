
-- ============================================================
-- TemperatureMonitorDB 데이터베이스 테이블 생성 스크립트
-- ============================================================
-- 목적:
-- 1. 온도 센서 장비의 접속 정보 관리
-- 2. 온도 이상 및 장비 오류 발생 이력 관리
-- 3. 카카오톡 알림 발송 상태 및 결과 관리

-- 테이블 관계:
-- Device (1) ─── (N) AlarmEvent
-- AlarmEvent (1) ─── (0..1) AlarmNotification

-- ============================================================


-- ============================================================
-- 0. 데이터베이스 선택 및 트랜잭션 설정
-- ============================================================

-- TemperatureMonitorDB 데이터베이스를 사용한다.
USE TemperatureMonitorDB;
GO

-- SQL 실행 중 런타임 오류가 발생하면 트랜잭션을
-- 자동으로 롤백하도록 설정한다.
SET XACT_ABORT ON;

-- TRY: 정상 실행할 SQL 구문
-- CATCH: 오류 발생 시 실행할 SQL 구문
BEGIN TRY

    -- 트랜잭션 시작
    -- 테이블 생성 작업을 하나의 작업 단위로 묶는다.
    BEGIN TRANSACTION;


    -- ========================================================
    -- 1. Device 테이블
    -- ========================================================
    -- 목적:
    -- 온도 센서 장비의 기본 정보와 접속 정보를 저장한다.
    --
    -- 저장 정보:
    -- DeviceId    : 장비 고유 식별번호
    -- DeviceName  : 장비 이름
    -- MacAddress  : 장비 MAC 주소
    -- LastKnownIp : 마지막으로 확인된 IP 주소
    -- TcpPort     : TCP 통신 포트 번호
    -- ========================================================

    CREATE TABLE dbo.Device
    (
        -- 장비 고유번호
        -- IDENTITY(1, 1):
        -- 첫 번째 데이터는 1부터 시작하며
        -- 새로운 데이터가 추가될 때마다 1씩 증가한다.
        -- NOT NULL: NULL 값 저장 불가
        DeviceId int IDENTITY(1, 1) NOT NULL,

        -- 장비 이름
        -- nvarchar(50): 유니코드 문자열 최대 50자
        DeviceName nvarchar(50) NOT NULL,

        -- 장비 MAC 주소
        -- char(17): 고정 길이 17자리 문자열
        -- 예: AA:BB:CC:DD:EE:FF
        MacAddress char(17) NOT NULL,

        -- 장비의 마지막 확인 IP 주소
        -- varchar(45): IPv4 및 IPv6 주소 저장 가능
        -- NULL 허용: IP 주소를 아직 모를 수 있음
        LastKnownIp varchar(45) NULL,

        -- 장비 TCP 포트 번호
        -- 기본값 502: Modbus/TCP 기본 포트
        TcpPort int NOT NULL
            CONSTRAINT DF_Device_TcpPort DEFAULT (502),

        -- PRIMARY KEY (기본키)
        -- DeviceId를 각 장비의 고유 식별자로 지정한다.
        -- 중복 값과 NULL 값을 허용하지 않는다.
        CONSTRAINT PK_Device
            PRIMARY KEY (DeviceId),

        -- UNIQUE (고유 제약조건)
        -- 동일한 MAC 주소를 가진 장비가
        -- 중복 등록되지 않도록 제한한다.
        CONSTRAINT UQ_Device_MacAddress
            UNIQUE (MacAddress),

        -- CHECK (검사 제약조건)
        -- TCP 포트 번호가 1~65535 범위에
        -- 포함되는 경우에만 저장하도록 제한한다.
        CONSTRAINT CK_Device_TcpPort
            CHECK (TcpPort BETWEEN 1 AND 65535)
    );


    -- ========================================================
    -- 2. AlarmEvent 테이블
    -- ========================================================
    -- 목적:
    -- 장비에서 발생한 알람의 전체 생명주기를 기록한다.
    --
    -- 알람 처리 흐름:
    -- 발생(OccurredAt)
    --       ↓
    -- 확인(AcknowledgedAt)
    --       ↓
    -- 복귀(RecoveredAt)
    --
    -- 주의:
    -- 확인과 복귀는 반드시 위 순서로 이루어지는 것은 아니다.
    -- 확인하지 않은 알람도 정상 상태로 복귀할 수 있다.
    --
    -- 알람 유형:
    -- HIGH_TEMP    : 온도 임계치 초과
    -- SENSOR_ERROR : 센서 이상
    -- COMM_LOST    : 통신 연결 끊김
    -- ========================================================

    CREATE TABLE dbo.AlarmEvent
    (
        -- 알람 고유번호
        -- bigint: int보다 큰 범위의 정수 저장 가능
        -- IDENTITY: 알람 등록 시 번호 자동 증가
        AlarmId bigint IDENTITY(1, 1) NOT NULL,

        -- 알람이 발생한 장비의 고유번호
        -- Device 테이블의 DeviceId와 연결된다.
        DeviceId int NOT NULL,

        -- 알람 종류
        -- varchar(20): 최대 20바이트 문자열
        -- CHECK 제약조건으로 허용할 종류를 제한한다.
        AlarmType varchar(20) NOT NULL,

        -- 알람 상세 메시지
        -- 예: 현재 온도가 50℃를 초과했습니다.
        Message nvarchar(250) NOT NULL,

        -- 알람 발생 시점의 온도
        -- decimal(5, 1): 전체 5자리, 소수점 이하 1자리
        -- 예: 125.5, -20.0
        -- 통신 오류 등 온도값을 알 수 없는 경우 NULL 허용
        TriggerTemperature decimal(5, 1) NULL,

        -- 알람이 발생한 시간
        -- datetime2(3): 밀리초 단위까지 기록
        -- SYSUTCDATETIME(): 현재 UTC 시간 반환
        -- 별도 입력하지 않으면 현재 UTC 시간이 자동 저장된다.
        OccurredAt datetime2(3) NOT NULL
            CONSTRAINT DF_AlarmEvent_OccurredAt
            DEFAULT (SYSUTCDATETIME()),

        -- 사용자가 알람을 확인한 시간
        -- NULL: 아직 확인하지 않은 상태
        AcknowledgedAt datetime2(3) NULL,

        -- 알람 원인이 해소되어 정상 복귀한 시간
        -- NULL: 아직 복귀하지 않은 활성 알람
        RecoveredAt datetime2(3) NULL,

        -- 알람 고유번호를 기본키로 지정한다.
        CONSTRAINT PK_AlarmEvent
            PRIMARY KEY (AlarmId),

        -- FOREIGN KEY (외래키)
        -- AlarmEvent.DeviceId가
        -- Device.DeviceId를 참조하도록 설정한다.
        --
        -- Device 테이블에 존재하지 않는 장비번호로는
        -- 알람을 등록할 수 없다.
        CONSTRAINT FK_AlarmEvent_Device
            FOREIGN KEY (DeviceId)
            REFERENCES dbo.Device (DeviceId),

        -- 허용 가능한 알람 유형을 3개로 제한한다.
        -- 이외의 문자열이 입력되면 오류가 발생한다.
        CONSTRAINT CK_AlarmEvent_AlarmType
            CHECK (
                AlarmType IN (
                    'HIGH_TEMP',
                    'SENSOR_ERROR',
                    'COMM_LOST'
                )
            ),

        -- 알람 확인시간 검증
        -- 확인하지 않은 경우 NULL 허용
        -- 확인한 경우 발생시간보다 빠를 수 없다.
        CONSTRAINT CK_AlarmEvent_AcknowledgedAt
            CHECK (
                AcknowledgedAt IS NULL
                OR AcknowledgedAt >= OccurredAt
            ),

        -- 정상 복귀시간 검증
        -- 미복귀 상태인 경우 NULL 허용
        -- 복귀한 경우 발생시간보다 빠를 수 없다.
        CONSTRAINT CK_AlarmEvent_RecoveredAt
            CHECK (
                RecoveredAt IS NULL
                OR RecoveredAt >= OccurredAt
            )
    );


    -- ========================================================
    -- 2-1. 활성 알람 중복 방지 인덱스
    -- ========================================================
    -- 목적:
    -- 같은 장비에서 동일한 종류의 활성 알람이
    -- 중복 생성되는 것을 방지한다.
    --
    -- UNIQUE INDEX:
    -- 인덱스에 지정된 컬럼 조합의 중복을 허용하지 않는다.
    --
    -- (DeviceId, AlarmType):
    -- 장비번호 + 알람종류 조합을 기준으로 중복 검사
    --
    -- WHERE RecoveredAt IS NULL:
    -- 아직 정상 복귀하지 않은 활성 알람만 검사한다.
    --
    -- 예시:
    -- 장비 1, HIGH_TEMP, 미복귀 → 등록 가능
    -- 장비 1, HIGH_TEMP, 미복귀 → 중복 등록 불가
    -- 장비 1, SENSOR_ERROR    → 등록 가능
    --
    -- 기존 HIGH_TEMP 알람이 정상 복귀하면
    -- 새로운 HIGH_TEMP 알람을 다시 등록할 수 있다.
    -- ========================================================

    CREATE UNIQUE INDEX UX_AlarmEvent_Active_Device_Type
        ON dbo.AlarmEvent (DeviceId, AlarmType)
        WHERE RecoveredAt IS NULL;


    -- ========================================================
    -- 3. AlarmNotification 테이블
    -- ========================================================
    -- 목적:
    -- 알람 발생 후 카카오톡 알림의 발송 상태를 기록한다.
    --
    -- 알림 상태:
    -- PENDING : 발송 대기
    -- SENT    : 발송 성공
    -- FAILED  : 발송 실패
    --
    -- 관리 정보:
    -- 발송 시도 횟수
    -- 마지막 발송 시도 시간
    -- 발송 성공 시간
    -- 마지막 오류 메시지
    --
    -- 구조:
    -- AlarmId를 기본키이자 외래키로 사용하므로
    -- 알람 한 건당 알림 기록은 최대 한 건만 저장한다.
    -- ========================================================

    CREATE TABLE dbo.AlarmNotification
    (
        -- 발송 대상 알람의 고유번호
        -- AlarmEvent 테이블의 AlarmId를 참조한다.
        AlarmId bigint NOT NULL,

        -- 카카오톡 알림 발송 상태
        -- 기본값 PENDING: 최초 등록 시 발송 대기
        SendStatus varchar(10) NOT NULL
            CONSTRAINT DF_AlarmNotification_SendStatus
            DEFAULT ('PENDING'),

        -- 알림 발송 시도 횟수
        -- 기본값 0: 아직 발송을 시도하지 않은 상태
        -- 실패 후 재시도할 때 증가시키는 용도
        AttemptCount int NOT NULL
            CONSTRAINT DF_AlarmNotification_AttemptCount
            DEFAULT (0),

        -- 마지막으로 알림 발송을 시도한 시간
        -- NULL: 아직 발송을 시도하지 않음
        LastAttemptAt datetime2(3) NULL,

        -- 알림 발송에 성공한 시간
        -- NULL: 아직 발송 성공 기록이 없음
        SentAt datetime2(3) NULL,

        -- 마지막 알림 발송 오류 메시지
        -- 예: API 응답 오류, 인증 토큰 만료 등
        -- NULL: 기록된 오류 메시지가 없음
        LastError nvarchar(500) NULL,

        -- PRIMARY KEY
        -- AlarmId 중복을 허용하지 않는다.
        -- 따라서 알람 하나에 알림 결과를 최대 한 건 저장한다.
        CONSTRAINT PK_AlarmNotification
            PRIMARY KEY (AlarmId),

        -- FOREIGN KEY
        -- AlarmEvent 테이블에 존재하는 알람에 대해서만
        -- 알림 기록을 생성할 수 있도록 제한한다.
        CONSTRAINT FK_AlarmNotification_AlarmEvent
            FOREIGN KEY (AlarmId)
            REFERENCES dbo.AlarmEvent (AlarmId),

        -- 발송 상태값 제한
        -- PENDING, SENT, FAILED만 입력 가능하다.
        CONSTRAINT CK_AlarmNotification_SendStatus
            CHECK (
                SendStatus IN ('PENDING', 'SENT', 'FAILED')
            ),

        -- 발송 시도 횟수는 음수가 될 수 없다.
        CONSTRAINT CK_AlarmNotification_AttemptCount
            CHECK (AttemptCount >= 0),

        -- 발송 상태와 성공 시간의 일관성을 검사한다.
        --
        -- 조건 1:
        -- SENT 상태라면 SentAt에 발송 성공 시간이 있어야 한다.
        --
        -- 조건 2:
        -- PENDING 또는 FAILED 상태라면
        -- SentAt은 NULL이어야 한다.
        --
        -- 주의:
        -- 실제 카카오톡 수신 여부가 아니라
        -- 애플리케이션에서 기록한 발송 성공 상태를 의미한다.
        CONSTRAINT CK_AlarmNotification_SentAt
            CHECK (
                (SendStatus = 'SENT' AND SentAt IS NOT NULL)
                OR
                (
                    SendStatus IN ('PENDING', 'FAILED')
                    AND SentAt IS NULL
                )
            )
    );


    -- ========================================================
    -- 4. 트랜잭션 정상 종료
    -- ========================================================

    -- 앞의 모든 SQL 구문이 오류 없이 실행되었다면
    -- COMMIT으로 변경 사항을 최종 확정한다.
    COMMIT TRANSACTION;

END TRY

BEGIN CATCH

    -- ========================================================
    -- 5. 오류 발생 시 롤백 처리
    -- ========================================================

    -- XACT_STATE():
    --  1 : 트랜잭션이 활성 상태이며 커밋 가능
    -- -1 : 트랜잭션이 활성 상태이지만 커밋 불가능
    --  0 : 활성 트랜잭션 없음
    --
    -- 트랜잭션이 남아 있다면 모든 변경 사항을 취소한다.
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    -- 발생한 오류를 다시 호출자에게 전달한다.
    -- 오류 번호, 메시지, 발생 위치 등을 유지한다.
    THROW;

END CATCH;
GO
