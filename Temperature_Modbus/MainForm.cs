using Microsoft.Data.SqlClient;
using System.Buffers.Binary;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;


namespace Temperature_Modbus
{
    public partial class MainForm : Form
    {

        // HttpClient를 전역으로 선언하여 재사용 (Kakao API 호출용)
        private static readonly HttpClient kakaoHttpClient = new();

        // Kakao API 호출을 취소할 수 있도록 CancellationTokenSource 사용
        private CancellationTokenSource? kakaoWorkerCts;

        // TcpClient 객체를 전역 변수로 선언하여 연결 상태를 유지
        private TcpClient? deviceClient;

        // 연결 시도를 취소할 수 있도록 CancellationTokenSource 사용
        private CancellationTokenSource? connectCts;

        // NetworkStream 객체를 전역 변수로 선언하여 장치와의 데이터 스트림을 유지
        private NetworkStream? deviceStream;

        // 연결 상태를 나타내는 플래그
        private bool isClosing = false;

        // ScottPlot 5에서 한글 표시를 위해 글꼴을 지정할 수 있음. 기본 글꼴은 한글을 지원하지 않음.
        private const string ChartFontName = "TemperatureKorean";

        // 온도 데이터를 저장할 리스트 (시간, 값) - 그래프에 표시하기 위해 사용
        private readonly List<double> temperatureTimes = new();
        private readonly List<double> temperatureValues = new();


        // 보안상 위험할듯. DB 접근 정보 유출될 수도 있음.
        //private const string DbConnectionString = @"Server=.\SQLEXPRESS;Database=......;" + "Integrated Security=True;Encrypt=True;" + "TrustServerCertificate=True;Connect Timeout=5;";

        // 사용자 비밀에서 DB 연결 문자열을 한 번 읽어 재사용
        private static readonly string DbConnectionString = new ConfigurationBuilder()
                .AddUserSecrets<MainForm>(optional: true)
                .Build()["ConnectionStrings:TemperatureMonitor"]?? throw new InvalidOperationException(
                    "사용자 암호 관리에 ConnectionStrings:TemperatureMonitor를 설정하세요.");

        // DB 연결 확인 및 장치 연결 상태를 관리하기 위한 CancellationTokenSource
        private CancellationTokenSource? dbCheckCts;

        // 시계 갱신을 위한 CancellationTokenSource (1초마다 현재 시각을 갱신)
        private CancellationTokenSource? clockCts;

        // Modbus 프로토콜에서 장치 ID는 1바이트로 표현되며, 일반적으로 1~247 사이의 값을 사용.
        // 여기서는 장치 ID를 1로 설정.
        private const byte ModbusUnitId = 1;

        // Modbus TCP 통신에서 트랜잭션 ID는 요청과 응답을 매칭시키기 위해 사용되는 2바이트 값.
        // ushort란 0~65535 범위의 부호 없는 정수형으로, Modbus TCP에서 트랜잭션 ID를 나타내기에 적합.
        private ushort transactionId;

        // 온도 데이터를 주기적으로 읽어오기 위한 CancellationTokenSource
        private CancellationTokenSource? pollingCts;

        // 그래프 시작 시각을 기록하여 10분 범위의 데이터를 표시하기 위해 사용
        private DateTime? graphStartedAt;

        // 온도 알람 관련 상수 및 변수
        // HighTemperatureLimit: 온도가 이 값 이상이면 고온 알람 발생
        private const double HighTemperatureLimit = 80.0;
        // RecoveryTemperatureLimit: 온도가 이 값 이하로 떨어지면 알람 해제
        private const double RecoveryTemperatureLimit = 40.0;
        // RequiredSamples: 고온 알람 발생을 위해 연속으로 측정해야 하는 샘플 수
        private const int RequiredSamples = 3;


        // 현재 장치 ID를 저장하는 변수 (장치 식별용)
        private int currentDeviceId;
        // 현재 장치의 마지막 온도 알람 상태를 저장하는 변수 (알람 발생 여부 추적)
        private bool savedHighTemperatureAlarm;

        // 온도 알람 관련 변수
        private int highTemperatureCount;
        private int recoveryCount;
        private bool isHighTemperatureAlarm;
        // FC04로 입력 레지스터 한 개를 읽는다.
        private async Task<ushort> ReadInputRegisterAsync(
            NetworkStream stream,
            ushort registerAddress,
            CancellationToken token)
        {
            // 트랜잭션 ID를 증가시키면서 16비트 범위를 벗어나면 0으로 되돌아가도록 처리
            ushort requestId = unchecked(++transactionId);

            // Modbus TCP 요청 패킷 구성 (12바이트)
            byte[] request = new byte[12];

            // Transaction ID (2바이트),
            // Protocol ID (2바이트),
            // Length (2바이트),
            // Unit ID (1바이트),
            // Function Code (1바이트),
            // Starting Address (2바이트),
            // Quantity of Registers (2바이트)
            BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(0, 2), requestId);
            // Protocol ID는 기본값 0
            BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(4, 2), 6);

            // Length는 Unit ID(1바이트) +
            // Function Code(1바이트) +
            // Starting Address(2바이트) +
            // Quantity of Registers(2바이트) = 6바이트
            request[6] = ModbusUnitId;

            // Function Code 0x04 (Read Input Registers)를 설정
            request[7] = 0x04;

            // Starting Address와 Quantity of Registers를 Big Endian 형식으로 작성
            BinaryPrimitives.WriteUInt16BigEndian(
                request.AsSpan(8, 2), registerAddress);

            // Quantity of Registers를 1로 설정 (한 개의 레지스터 읽기)
            BinaryPrimitives.WriteUInt16BigEndian(
                request.AsSpan(10, 2), 1);

            // TCP 요청 전송
            await stream.WriteAsync(request.AsMemory(), token);

            // TCP 응답은 나뉘어 도착할 수 있으므로 필요한 길이만큼 읽는다.
            byte[] header = new byte[7];
            await stream.ReadExactlyAsync(header.AsMemory(), token);

            // 응답 헤더에서 트랜잭션 ID, 프로토콜 ID, 길이를 읽어와서 요청과 비교하여 유효성을 확인
            ushort responseId =
                BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(0, 2));

            // Modbus TCP 프로토콜 ID는 항상 0이어야 함
            ushort protocolId =
                BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(2, 2));

            // 응답 길이 (Unit ID + Function Code + Byte Count + Data Length)를 읽어옴
            ushort length =
                BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(4, 2));

            // 응답 유효성 검사: 트랜잭션 ID, 프로토콜 ID, Unit ID, 길이 확인
            if (responseId != requestId ||
                protocolId != 0 ||
                header[6] != ModbusUnitId ||
                length < 2 ||
                length > 254)
            {
                throw new InvalidDataException("Modbus 응답 헤더가 올바르지 않습니다.");
            }

            // Length에는 이미 읽은 Unit ID 1바이트가 포함된다.
            byte[] body = new byte[length - 1];
            await stream.ReadExactlyAsync(body.AsMemory(), token);

            if (body[0] == 0x84 && body.Length == 2)
            {
                throw new IOException(
                    $"Modbus 예외 응답. 오류 코드 0x{body[1]:X2}");
            }

            if (body.Length != 4 || body[0] != 0x04 || body[1] != 2)
                throw new InvalidDataException("레지스터 응답 형식이 올바르지 않습니다.");

            return BinaryPrimitives.ReadUInt16BigEndian(body.AsSpan(2, 2));
        }


        // 시계 메서드
        private async Task RunClockAsync(CancellationTokenSource ownerCts)
        {
            CancellationToken token = ownerCts.Token;

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

            try
            {
                do
                {
                    token.ThrowIfCancellationRequested();

                    if (isClosing || IsDisposed)
                        return;

                    lblCurrentTime.Text =
                        $"현재 시각: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                } while (await timer.WaitForNextTickAsync(token));
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // 폼 종료로 시계 갱신을 마친다.
            }
            finally
            {
                if (ReferenceEquals(clockCts, ownerCts))
                    clockCts = null;

                ownerCts.Dispose();
            }
        }

        // 장치에서 온도와 센서 상태를 한 번 읽어오는 메서드
        private async Task PollTemperatureAsync(
            NetworkStream stream,
            CancellationTokenSource ownerCts)
        {

            int pollingDeviceId = currentDeviceId;

            // 이번 연결에서 DB에 반영한 센서 상태
            bool? savedSensorFault = null;

            // 재연결 후 통신 복귀를 DB에 반영했는지
            bool communicationRecovered = false;

            // PeriodicTimer를 사용하여 1초마다 온도를 읽어오는 루프를 실행
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

            // pollingCts를 ownerCts로 설정하여 다른 메서드에서 취소할 수 있도록 함
            try
            {
                // pollingCts를 ownerCts로 설정하여 다른 메서드에서 취소할 수 있도록 함
                do
                {
                    // 현재 조회에만 3초 제한을 적용한다.
                    using var requestCts =
                        CancellationTokenSource.CreateLinkedTokenSource(ownerCts.Token);

                    // 3초 후에 요청을 취소하도록 설정 - 이유 : 장치가 응답하지 않으면 무한 대기 상태가 될 수 있으므로 제한을 둠
                    requestCts.CancelAfter(TimeSpan.FromSeconds(3));

                    // 센서 오류 상태를 읽어온다. (레지스터 주소 8)
                    ushort sensorError =
                        await ReadInputRegisterAsync(stream, 8, requestCts.Token);

                    // 센서 오류가 없으면 온도를 읽어오고, 있으면 오류 상태를 표시한다.
                    if (sensorError == 0)
                    {
                        // 온도 레지스터 주소 0에서 값을 읽어온다.
                        ushort raw = await ReadInputRegisterAsync(stream, 0, requestCts.Token);

                        // Modbus에서 읽어온 값은 16비트 부호 없는 정수이므로, 이를 부호 있는 short로 변환하여 온도를 계산한다.
                        short signedValue = unchecked((short)raw);
                        double temperature = signedValue / 10.0;

                        // 폼이 닫히거나 연결이 해제되었는지 확인하여 UI 업데이트를 중단한다.
                        ownerCts.Token.ThrowIfCancellationRequested();

                        if (isClosing || IsDisposed)
                            return;

                        lblTemperature.Text = $"{temperature:F1} ℃";

                        // 정상 응답을 받은 시각과 온도를 그래프에 추가한다.
                        AppendTemperaturePoint(DateTime.Now, temperature);

                        // 온도 알람 상태를 평가하고 UI를 업데이트한다.
                        EvaluateTemperatureAlarm(temperature);

                        // 온도 알람 상태를 DB에 저장한다. (발생 또는 복귀 시에만 저장)
                        await SaveAlarmStateAsync(temperature, ownerCts.Token);
                    }
                    // 센서 오류가 있으면 온도를 표시하지 않고 오류 상태를 표시한다.
                    else
                    {
                        ownerCts.Token.ThrowIfCancellationRequested();

                        if (isClosing || IsDisposed)
                            return;

                        lblTemperature.Text = "— ℃";

                        // 센서 오류 구간에서 그래프 선을 끊는다.
                        MarkTemperatureGap();

                        // 고온·복귀 연속 확인 횟수를 초기화한다.
                        ResetTemperatureConfirmation();

                        lblDeviceStatus.Text =
                            $"장비 통신: 연결됨 / 센서 오류 {sensorError}";
                    }


                    if (sensorError == 0)
                        lblDeviceStatus.Text = "장비 통신: TCP 연결됨";


                    ownerCts.Token.ThrowIfCancellationRequested();

                    // 센서 오류 응답도 Modbus 통신 자체는 성공한 상태다.
                    if (!communicationRecovered)
                    {
                        communicationRecovered = await SaveDeviceFaultAsync(
                            pollingDeviceId,
                            "COMM_LOST",
                            false,
                            "Modbus 응답 수신 재개");
                    }

                    ownerCts.Token.ThrowIfCancellationRequested();

                    bool sensorFault = sensorError != 0;

                    if (savedSensorFault != sensorFault)
                    {
                        bool saved = await SaveDeviceFaultAsync(
                            pollingDeviceId,
                            "SENSOR_ERROR",
                            sensorFault,
                            $"센서 오류 코드 {sensorError}");

                        if (saved)
                            savedSensorFault = sensorFault;
                    }

                    ownerCts.Token.ThrowIfCancellationRequested();
                }
                // PeriodicTimer를 사용하여 1초마다 온도를 읽어오는 루프를 실행하며,
                // ownerCts.Token이 취소되면 OperationCanceledException이 발생하여 루프 종료
                while (await timer.WaitForNextTickAsync(ownerCts.Token));
            }
            catch (OperationCanceledException) when (ownerCts.IsCancellationRequested)
            {
                // 사용자가 통신을 해제하거나 폼을 종료했다.
            }
            catch (Exception ex) when (
                ex is OperationCanceledException ||
                ex is IOException ||
                ex is SocketException ||
                ex is ObjectDisposedException)
            {
                if (isClosing || IsDisposed ||
                    !ReferenceEquals(pollingCts, ownerCts))
                    return;

                string faultMessage = ex is OperationCanceledException
                    ? "Modbus 응답 시간 초과"
                    : "Modbus 수신 실패";

                btnConnect.Enabled = false;

                try
                {
                    // 장치와의 연결을 끊고 UI를 업데이트한다.
                    DisconnectDevice();

                    lblDeviceStatus.Text = $"장비 통신: {faultMessage}";
                    lblDeviceStatus.ForeColor = Color.Firebrick;

                    // 통신 끊김 발생을 DB에 기록한다.
                    await SaveDeviceFaultAsync(
                        pollingDeviceId,
                        "COMM_LOST",
                        true,
                        faultMessage);
                }
                
                finally
                {
                    if (!isClosing && !IsDisposed)
                        btnConnect.Enabled = true;
                }
            }
        }

        // DB에서 장치의 알람 상태를 복원하는 메서드
        private async Task RestoreAlarmStateAsync(string ipAddress,
            int port,
            CancellationToken token)
        {
            // DB 연결을 열고 장치 ID와 알람 상태를 조회한다.
            using var connection = new SqlConnection(DbConnectionString);
            await connection.OpenAsync(token);

            using var command = connection.CreateCommand();
            // DB에서 장치 ID와 활성 알람 상태를 조회하는 SQL 쿼리
            // 저장프로시저를 만들지 않은 이유 : 단순 조회이므로 쿼리문으로 충분하며, 저장프로시저를 만들면 유지보수가 번거로워짐.
            command.CommandText = """
               SELECT
                    d.DeviceId,
                    CAST(CASE WHEN EXISTS (
                        SELECT 1
                        FROM dbo.AlarmEvent AS a
                        WHERE a.DeviceId = d.DeviceId
                          AND a.AlarmType = 'HIGH_TEMP'
                          AND a.RecoveredAt IS NULL
                    ) THEN 1 ELSE 0 END AS bit) AS HasActiveAlarm
                FROM dbo.Device AS d
                WHERE d.LastKnownIp = @IpAddress
                  AND d.TcpPort = @Port;
               """;

            command.Parameters.Add("@IpAddress", SqlDbType.VarChar, 45).Value =
                ipAddress;

            command.Parameters.Add("@Port", SqlDbType.Int).Value =
                port;

            using var reader = await command.ExecuteReaderAsync(token);

            if (!await reader.ReadAsync(token))
            {
                throw new InvalidOperationException(
                    "입력한 IP와 포트에 해당하는 장비를 DB에 등록하세요.");
            }

            int loadedDeviceId = reader.GetInt32(0);
            bool loadedAlarmState = reader.GetBoolean(1);

            // 같은 접속 정보로 여러 장비가 등록되어 있으면 잘못된 장비에 저장하지 않는다.
            if (await reader.ReadAsync(token))
            {
                throw new InvalidOperationException(
                    "같은 IP와 포트로 여러 장비가 등록되어 있습니다. DB 등록 정보를 확인하세요.");
            }

            token.ThrowIfCancellationRequested();

            if (isClosing || IsDisposed)
                return;

            currentDeviceId = loadedDeviceId;
            isHighTemperatureAlarm = loadedAlarmState;
            savedHighTemperatureAlarm = loadedAlarmState;

            // UI를 초기화하고 온도 알람 상태를 표시한다.
            ResetTemperatureConfirmation();
        }

        // 온도 알람 상태를 DB에 저장하는 메서드
        private async Task SaveAlarmStateAsync(
            double temperature,
            CancellationToken token)
        {
            // 발생 또는 복귀로 상태가 바뀌었을 때 저장한다.
            if (isHighTemperatureAlarm == savedHighTemperatureAlarm)
                return;

            // 현재 알람 상태를 저장할 변수에 복사하여 DB에 저장한다.
            bool stateToSave = isHighTemperatureAlarm;

            // DB에 저장할 때는 현재 장치 ID와 알람 상태를 함께 저장한다.
            try
            {
                // DB 연결을 열고 저장 프로시저를 호출하여 알람 상태를 저장한다.
                using var connection = new SqlConnection(DbConnectionString);
                await connection.OpenAsync(token);

                // DB에 저장할 때는 현재 장치 ID와 알람 상태를 함께 저장한다.
                using var command = connection.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "dbo.SaveHighTemperatureAlarm";

                command.Parameters.Add("@DeviceId", SqlDbType.Int).Value =
                    currentDeviceId;

                command.Parameters.Add("@IsActive", SqlDbType.Bit).Value =
                    stateToSave;

                // 온도 값을 소수점 한 자리까지 저장하기 위해 SqlDbType.Decimal을 사용하고, Precision과 Scale을 설정한다.
                var temperatureParameter =
                    command.Parameters.Add("@Temperature", SqlDbType.Decimal);

                temperatureParameter.Precision = 5;
                temperatureParameter.Scale = 1;
                temperatureParameter.Value = (decimal)temperature;

                // DB에 저장할 때는 현재 시각을 함께 저장한다.
                await command.ExecuteNonQueryAsync(token);

                savedHighTemperatureAlarm = stateToSave;

                // DB에 저장 후 활성 알람 상태를 다시 조회하여 UI를 업데이트한다.
                await LoadActiveAlarmsAsync();

                if (!isClosing && !IsDisposed)
                {
                    lblDbStatus.Text = stateToSave
                        ? "DB: 알람 발생 저장"
                        : "DB: 정상";

                    lblDbStatus.ForeColor = Color.SeaGreen;
                }

                if (!isClosing && !IsDisposed)
                    await LoadActiveAlarmsAsync();
            }
            catch (SqlException ex)
            {
                if (isClosing || IsDisposed)
                    return;

                lblDbStatus.Text = $"DB: 알람 저장 실패 ({ex.Number})";
                lblDbStatus.ForeColor = Color.Firebrick;

                // 저장 상태를 바꾸지 않아 다음 정상 수신 때 재시도한다.
            }
        }

        // 온도 알람 상태를 초기화하고 UI를 업데이트하는 메서드
        private void ResetTemperatureConfirmation()
        {
            highTemperatureCount = 0;
            recoveryCount = 0;

            lblTemperatureState.Text = "대기";
            lblTemperatureState.ForeColor = Color.DarkOrange;

            lblAlarmSummary.Text = isHighTemperatureAlarm
                ? "고온 알람 유지 · 정상 수신 후 복귀 판정"
                : "정상 온도 수신 대기";

            lblAlarmSummary.ForeColor = Color.DarkOrange;
        }

        // 온도 알람 상태를 평가하고 UI를 업데이트하는 메서드
        private void EvaluateTemperatureAlarm(double temperature)
        {
            if (!isHighTemperatureAlarm)
            {

                recoveryCount = 0;

                // 온도가 HighTemperatureLimit 이상이면 고온 알람 카운트를 증가시키고, 그렇지 않으면 초기화한다.
                if (temperature >= HighTemperatureLimit)
                    highTemperatureCount++;
                else
                    highTemperatureCount = 0;

                // 연속으로 RequiredSamples 이상 고온이 감지되면 고온 알람 상태로 전환한다.
                if (highTemperatureCount >= RequiredSamples)
                {
                    isHighTemperatureAlarm = true;
                    highTemperatureCount = 0;
                }
            }
            else
            {
                highTemperatureCount = 0;

                // 온도가 RecoveryTemperatureLimit 이하이면 복귀 카운트를 증가시키고, 그렇지 않으면 초기화한다.
                if (temperature <= RecoveryTemperatureLimit)
                    recoveryCount++;
                else
                    recoveryCount = 0;

                if (recoveryCount >= RequiredSamples)
                {
                    isHighTemperatureAlarm = false;
                    recoveryCount = 0;
                }
            }

            // UI를 업데이트하여 현재 온도 상태와 알람 요약을 표시한다.
            if (isHighTemperatureAlarm)
            {
                lblTemperatureState.Text = "고온";
                lblTemperatureState.ForeColor = Color.Firebrick;

                lblAlarmSummary.Text = recoveryCount > 0
                    ? $"고온 알람 유지 · 복귀 확인 {recoveryCount}/{RequiredSamples}"
                    : "활성 고온 알람 발생";

                lblAlarmSummary.ForeColor = Color.Firebrick;
            }
            else
            {
                lblTemperatureState.Text = "정상";
                lblTemperatureState.ForeColor = Color.SeaGreen;

                lblAlarmSummary.Text = highTemperatureCount > 0
                    ? $"고온 확인 {highTemperatureCount}/{RequiredSamples}"
                    : "활성 고온 알람 없음";

                lblAlarmSummary.ForeColor = Color.Black;
            }
        }

        // 폼이 표시될 때 DB 연결 확인 및 장치 연결 상태 초기화
        protected override async void OnShown(EventArgs e)
        {
            // 폼이 표시될 때 기본 동작 수행
            base.OnShown(e);

            if (clockCts == null)
            {
                var newClockCts = new CancellationTokenSource();
                clockCts = newClockCts;

                _ = RunClockAsync(newClockCts);
            }

            // Kakao API 호출을 위한 백그라운드 작업을 시작하여 UI가 멈추지 않도록 함
            if (kakaoWorkerCts == null)
            {
                var workerCts = new CancellationTokenSource();
                kakaoWorkerCts = workerCts;

                _ = RunKakaoWorkerAsync(workerCts);
            }

            // DB 연결 확인
            using var cts = new CancellationTokenSource();
            dbCheckCts = cts;

            // DB 연결 상태 표시: 확인 중 (주황색)
            lblDbStatus.Text = "DB: 확인 중";
            lblDbStatus.ForeColor = Color.DarkOrange;

            // DB 연결 확인을 비동기적으로 수행하여 UI가 멈추지 않도록 함
            try
            {
                // SqlConnection 객체를 생성하고 연결 시도
                using var connection = new SqlConnection(DbConnectionString);
                await connection.OpenAsync(cts.Token);

                // SQL 쿼리를 실행하여 장치 수를 조회
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM dbo.Device;";

                // 커맨드 타임아웃을 5초로 설정하여 쿼리 실행이 오래 걸릴 경우 취소되도록 함
                // 이유 : DB 연결이 정상이어도 쿼리 실행이 오래 걸릴 수 있음
                command.CommandTimeout = 5;

                // ExecuteScalarAsync() 메서드를 사용하여 쿼리 결과를 비동기적으로 가져옴
                object? result = await command.ExecuteScalarAsync(cts.Token);
                // 쿼리 결과가 null이거나 DBNull인 경우 예외 처리
                int deviceCount = Convert.ToInt32(result);

                // DB 연결 확인 후 UI 상태 변경: 정상 (녹색)
                if (isClosing || IsDisposed)
                    return;

                lblDbStatus.Text = $"DB: 정상 ({deviceCount}대)";
                lblDbStatus.ForeColor = Color.SeaGreen;

                // 장치의 마지막 알려진 IP와 포트를 조회하여 입력란에 표시
                using var deviceCommand = connection.CreateCommand();

                // SQL 쿼리를 작성하여 MacAddress가 "00-30-F9-18-7B-FF"인 장치의 LastKnownIp와 TcpPort를 조회
                deviceCommand.CommandText =
                """
                    SELECT LastKnownIp, TcpPort
                    FROM dbo.Device
                    WHERE MacAddress = @MacAddress;
                """;

                // SQL 파라미터를 사용하여 MacAddress 값을 안전하게 전달
                deviceCommand.Parameters.Add("@MacAddress", SqlDbType.Char, 17).Value =
                    "00-30-F9-18-7B-FF";

                // ExecuteReaderAsync() 메서드를 사용하여 쿼리 결과를 비동기적으로 가져옴
                using var reader = await deviceCommand.ExecuteReaderAsync(cts.Token);

                // ReadAsync() 메서드를 사용하여 결과를 읽고, 장치의 마지막 알려진 IP와 포트를 입력란에 표시
                if (await reader.ReadAsync(cts.Token))
                {
                    // 조회 중에 폼을 닫거나 사용자가 연결을 시작했으면 입력란을 건드리지 않는다.
                    if (isClosing || IsDisposed || connectCts != null || deviceClient != null)
                        return;

                    // 조회를 기다리는 동안 사용자가 직접 입력한 값은 보존한다.
                    if (!txtDeviceIp.Modified && !reader.IsDBNull(0))
                        txtDeviceIp.Text = reader.GetString(0);

                    // 조회 중에 사용자가 포트 입력란을 수정하지 않았다면 DB에서 가져온 값을 표시한다.
                    if (!txtDevicePort.Modified)
                        txtDevicePort.Text = reader.GetInt32(1).ToString();
                }
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                // 폼을 닫아서 취소한 경우에는 오류 창을 띄우지 않는다.
            }
            catch (SqlException ex)
            {
                if (isClosing || IsDisposed)
                    return;

                // DB 연결 실패 시 UI 상태 변경: 실패 (빨간색) 및 오류 메시지 표시
                lblDbStatus.Text = "DB: 조회 실패";
                lblDbStatus.ForeColor = Color.Firebrick;

                MessageBox.Show(
                    this,
                    $"SQL 오류 번호: {ex.Number}\n\n{ex.Message}",
                    "DB 연결 확인",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                dbCheckCts = null;
            }
        }

        // ScottPlot 5에서 한글 표시를 위해 글꼴을 등록하는 메서드
        private static void RegisterChartFont()
        {
            // ScottPlot 5에서 한글 표시를 위해 글꼴을 등록하는 메서드
            string fontPath = Path.Combine(
                AppContext.BaseDirectory,
                "Fonts",
                "NotoSansCJKkr-Regular.otf");

            foreach (var weight in new[]
            {
                ScottPlot.FontWeight.Normal,
                ScottPlot.FontWeight.Bold
            })
            {
                var resolver = new ScottPlot.FontResolvers.FileFontResolver(
                    ChartFontName,
                    fontPath,
                    weight,
                    ScottPlot.FontSlant.Upright,
                    ScottPlot.FontSpacing.Normal);

                ScottPlot.Fonts.FontResolvers.Insert(0, resolver);
            }
        }

        // 온도 데이터를 저장할 그래프용 리스트
        private void InitializeTemperatureGraph()
        {
            // ScottPlot 컨트롤의 Plot 객체 가져오기
            var plot = plotTemperature.Plot;

            // 그래프 초기화
            plot.Clear();

            // 그래프에 저장된 수신 시각 초기화
            temperatureTimes.Clear();

            // 그래프에 표시할 온도 값 리스트 초기화
            temperatureValues.Clear();

            // 같은 리스트에 값을 추가하면 그래프에도 반영된다.
            var temperatureLine = plot.Add.Scatter(
                temperatureTimes,
                temperatureValues);

            // 그래프 선 스타일 설정 (파란색, 두께 2, 마커 크기 3)
            temperatureLine.Color = ScottPlot.Colors.Blue;
            temperatureLine.LineWidth = 2;
            temperatureLine.MarkerSize = 3;

            // 그래프 제목과 축 레이블 설정
            plot.XLabel("시간");
            plot.YLabel("온도 (℃)");

            // 시간축 생성
            var bottomAxis = plot.Axes.DateTimeTicksBottom();

            // 날짜·오전/오후 대신 24시간 형식으로 표시
            // 시간축의 ‘오전/오후' 한글이 기본 글꼴에서 표시되지 않는 것을 방지하기 위해
            // ScottPlot 5에서는 글꼴 지정과 시간축 표시 형식 변경을 지원함.

            // plot.Axes.SetLimitsX(now.AddMinutes(-10).ToOADate(),now.ToOADate());
            var tickGenerator = (ScottPlot.TickGenerators.DateTimeAutomatic)bottomAxis.TickGenerator;

            // 시간축의 표시 형식 변경 (24시간 형식)
            tickGenerator.LabelFormatter = dateTime => dateTime.ToString("HH:mm:ss");

            // 한글 표시용 글꼴
            plot.Font.Set(ChartFontName);

            // 그래프 시작 시각 초기화
            graphStartedAt = null;

            DateTime now = DateTime.Now;

            // 처음에는 왼쪽이 현재 시각, 오른쪽이 10분 뒤다.
            plot.Axes.SetLimitsX(
                now.ToOADate(),
                now.AddMinutes(10).ToOADate());

            // Y축 범위를 20~60℃로 설정(-200~850℃ 범위까지 측정 가능하지만 온도 범위가 너무 넓어서 그래프가 작게 표시됨)
            plot.Axes.SetLimitsY(20, 100);

            // 축 이름이 들어갈 여백 확보
            plot.Layout.Fixed(new ScottPlot.PixelPadding(
                left: 75,
                right: 20,
                bottom: 55,
                top: 20));

            // 온도 상한선을 표시하기 위해 수평선을 추가한다.
            var highLimit = plot.Add.HorizontalLine(HighTemperatureLimit);

            // 상한선 스타일 설정 (빨간색, 두께 2, 점선)
            highLimit.Color = ScottPlot.Colors.Red;
            highLimit.LineWidth = 2;
            highLimit.LinePattern = ScottPlot.LinePattern.Dashed;


            // 정상 복귀 기준선 (40 ℃)
            var recoveryLimit = plot.Add.HorizontalLine(RecoveryTemperatureLimit);

            // 복귀 기준선 스타일 설정 (녹색, 두께 2, 점선)
            recoveryLimit.Color = ScottPlot.Colors.Green;
            recoveryLimit.LineWidth = 2;
            recoveryLimit.LinePattern = ScottPlot.LinePattern.Dashed;

            plotTemperature.Refresh();
        }

        private void AppendTemperaturePoint(
            DateTime receivedAt,
            double temperature)
        {
            // 그래프 시작 시각이 null이면 정상 온도를 처음 받았을 때 그래프를 시작한다.
            if (graphStartedAt == null)
            {
                // 정상 온도를 처음 받았을 때 그래프를 시작한다.
                if (!double.IsFinite(temperature))
                    return;

                graphStartedAt = receivedAt;
            }

            DateTime startedAt = graphStartedAt.Value;

            // 처음 10분 동안은 시작 시각부터 오른쪽으로 채운다.
            DateTime right = startedAt.AddMinutes(10);

            // 10분이 지나면 최근 10분을 표시한다.
            if (receivedAt > right)
                right = receivedAt;

            // 그래프에 표시할 왼쪽 시각은 오른쪽 시각에서 10분 전으로 설정한다.
            DateTime left = right.AddMinutes(-10);

            // 그래프에 수신 시각과 온도 값을 추가한다.
            temperatureTimes.Add(receivedAt.ToOADate());
            temperatureValues.Add(temperature);

            // 그래프에 표시할 범위에서 벗어난 오래된 데이터를 제거한다.
            while (temperatureTimes.Count > 0 &&
                   temperatureTimes[0] < left.ToOADate())
            {
                temperatureTimes.RemoveAt(0);
                temperatureValues.RemoveAt(0);
            }

            // 그래프의 X축 범위를 왼쪽 시각과 오른쪽 시각으로 설정한다.
            plotTemperature.Plot.Axes.SetLimitsX(
                left.ToOADate(),
                right.ToOADate());

            // 그래프의 Y축 범위를 20~100℃로 설정한다.
            plotTemperature.Plot.Axes.SetLimitsY(20, 100);

            plotTemperature.Refresh();
        }

        private void MarkTemperatureGap()
        {
            // 그래프에 NaN 값을 추가하여 연결이 끊긴 구간을 표시한다.
            AppendTemperaturePoint(DateTime.Now, double.NaN);
        }

        public MainForm()
        {
            InitializeComponent();

            // ScottPlot 5에서 한글 표시를 위해 글꼴을 등록
            RegisterChartFont();

            // 온도 그래프 초기화
            InitializeTemperatureGraph();

            dgvActiveAlarms.AutoGenerateColumns = false;
        }

        // 종료 버튼 클릭 이벤트 핸들러
        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        // TCP 연결 버튼 클릭 이벤트 핸들러
        private void DisconnectDevice()
        {
            // 
            pollingCts?.Cancel();
            pollingCts = null;

            lblTemperature.Text = "— ℃";

            // 그래프 초기화
            MarkTemperatureGap();

            // 온도 알람 상태 초기화 및 UI 업데이트
            ResetTemperatureConfirmation();

            // 연결 해제 시 CancellationTokenSource 취소
            deviceStream?.Dispose();
            deviceStream = null;

            // TcpClient 객체를 Dispose()하여 연결 해제
            deviceClient?.Dispose();
            deviceClient = null;

            // UI 상태 변경: 연결 버튼 활성화, 포트 입력란 활성화
            txtDevicePort.Enabled = true;

            // 연결 해제 후 UI 상태 초기화
            lblDeviceStatus.Text = "장비 통신: 미연결";
            lblDeviceStatus.ForeColor = Color.Black;

            // 연결 버튼 텍스트를 "통신 연결"로 변경하고 IP 입력란을 활성화
            btnConnect.Text = "통신 연결";
            txtDeviceIp.Enabled = true;
        }


        // TCP 연결 버튼 클릭 이벤트 핸들러
        private async void btnConnect_Click(object sender, EventArgs e)
        {
            // 종료 중이거나 이미 연결을 시도하는 중이면 중복 실행 방지
            if (isClosing || connectCts != null)
                return;

            // 이미 연결한 상태라면 연결 해제
            if (deviceClient != null)
            {
                // 연결 해제 시 CancellationTokenSource 취소
                DisconnectDevice();
                return;
            }

            // IP 입력 확인
            string ipText = txtDeviceIp.Text.Trim();

            // IP 주소 유효성 검사
            // IPAddress.TryParse() 메서드를 사용하여 IP 주소 형식이 올바른지 확인
            if (!IPAddress.TryParse(ipText, out IPAddress? address))
            {
                MessageBox.Show(this, "spFinder에서 확인한 올바른 장비 IP를 입력하세요.",
                    "IP 확인",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // IP 입력란에 포커스 설정
                txtDeviceIp.Focus();
                return;
            }

            // 포트 입력 확인: 포트는 1~65535 사이의 숫자로 입력해야 함
            if (!int.TryParse(txtDevicePort.Text.Trim(), out int port) ||
                port < 1 || port > 65535)
            {
                MessageBox.Show(this, "포트는 1~65535 사이의 숫자로 입력하세요.",
                    "포트 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // 포트 입력란에 포커스 설정 및 전체 선택
                txtDevicePort.Focus();
                txtDevicePort.SelectAll();
                return;
            }


            // UI 상태 변경: 연결 버튼 비활성화, 텍스트 변경, IP 입력란 비활성화
            btnConnect.Enabled = false;
            btnConnect.Text = "연결 중...";
            txtDevicePort.Enabled = false;
            txtDeviceIp.Enabled = false;

            // UI 상태 변경 : 장비 통신 상태 표시
            lblDeviceStatus.Text = "장비 통신: 연결 중";
            lblDeviceStatus.ForeColor = Color.DarkOrange;

            // 이번 연결 시도에만 적용하는 5초 제한
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // connectCts를 전역 변수로 설정하여 다른 메서드에서 취소할 수 있도록 함
            connectCts = timeoutCts;

            // 성공하기 전까지는 지역변수로 관리
            TcpClient? pendingClient = null;

            try
            {
                // 입력한 IP와 포트로 등록된 장비와 활성 알람을 조회한다.
                await RestoreAlarmStateAsync(address.ToString(), port, timeoutCts.Token);

                // TcpClient 객체 생성 및 연결 시도
                pendingClient = new TcpClient(address.AddressFamily);

                // TcpClient.ConnectAsync() 메서드를 사용하여 비동기적으로 연결 시도
                await pendingClient.ConnectAsync(address, port, timeoutCts.Token);

                // 연결 성공 시 CancellationTokenSource를 통해 취소 요청이 들어오면 예외 발생
                timeoutCts.Token.ThrowIfCancellationRequested();

                // 연결 성공 시 UI 상태 변경
                if (isClosing)
                    return;




                // TcpClient 객체를 전역 변수로 설정하여 연결 상태를 유지
                deviceClient = pendingClient;
                pendingClient = null;
                deviceStream = deviceClient.GetStream();

                // UI 상태 변경: 장비 통신 상태 표시, 연결 버튼 텍스트 변경
                lblDeviceStatus.Text = "장비 통신: TCP 연결됨";
                lblDeviceStatus.ForeColor = Color.SeaGreen;

                // 연결 버튼 텍스트를 "통신 해제"로 변경하여 연결 해제 기능 제공
                btnConnect.Text = "통신 해제";

                // UI 상태 변경: 포트 입력란 비활성화, IP 입력란 비활성화
                var newPollingCts = new CancellationTokenSource();
                pollingCts = newPollingCts;

                // 내부에서 수신 오류를 처리하며 연결 해제 때까지 반복한다.
                _ = PollTemperatureAsync(deviceStream!, newPollingCts);
            }
            // 연결 시도 중 취소되거나 시간 초과된 경우 예외 처리
            catch (OperationCanceledException)
            {
                // 연결 시도 중 취소되거나 시간 초과된 경우 UI 상태 변경
                if (!isClosing)
                {
                    lblDeviceStatus.Text = "장비 통신: 연결 시간 초과";
                    lblDeviceStatus.ForeColor = Color.Firebrick;
                }
            }
            // SocketException 발생 시 예외 처리 (연결 실패)
            catch (SocketException ex)
            {
                // 연결 실패 시 UI 상태 변경 및 오류 메시지 표시
                if (!isClosing)
                {
                    lblDeviceStatus.Text = "장비 통신: 연결 실패";
                    lblDeviceStatus.ForeColor = Color.Firebrick;

                    MessageBox.Show(
                        this,
                        $"IP·전원·LAN·포트 설정을 확인하세요.\n\n" +
                        $"오류: {ex.SocketErrorCode}",
                        "연결 실패",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            finally
            {
                // 실패하거나 종료 중인 연결 객체 정리
                pendingClient?.Dispose();
                connectCts = null;

                // UI 상태 변경: 연결 버튼 활성화, IP 입력란 활성화, 연결 버튼 텍스트 초기화
                if (!isClosing && !IsDisposed)
                {
                    btnConnect.Enabled = true;
                    txtDeviceIp.Enabled = deviceClient == null;

                    if (deviceClient == null)
                        btnConnect.Text = "통신 연결";

                }
            }
        }

        // 온도 알람 발생/복귀 상태를 DB에 저장하는 메서드
        private async Task<bool> SaveDeviceFaultAsync(
            int deviceId,
            string alarmType,
            bool isActive,
            string message)
        {
            // DB 저장 시 5초 제한을 두어 오래 걸리면 취소한다.
            using var cts =
                new CancellationTokenSource(TimeSpan.FromSeconds(5));

            
            try
            {
                // DB 연결을 열고 저장 프로시저를 호출하여 알람 상태를 저장한다.
                using var connection = new SqlConnection(DbConnectionString);
                await connection.OpenAsync(cts.Token);

                // DB에 저장할 때는 현재 장치 ID와 알람 상태를 함께 저장한다.
                using var command = connection.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "dbo.SaveDeviceFault";
                command.CommandTimeout = 5;

                command.Parameters.Add("@DeviceId", SqlDbType.Int).Value =
                    deviceId;

                command.Parameters.Add("@AlarmType", SqlDbType.VarChar, 20).Value =
                    alarmType;

                command.Parameters.Add("@IsActive", SqlDbType.Bit).Value =
                    isActive;

                command.Parameters.Add("@Message", SqlDbType.NVarChar, 250).Value =
                    message;

                // DB에 저장할 때는 현재 시각을 함께 저장한다.
                await command.ExecuteNonQueryAsync(cts.Token);

                if (!isClosing && !IsDisposed && currentDeviceId == deviceId)
                    await LoadActiveAlarmsAsync();

                return true;
            }
            catch (Exception ex) when (
                ex is SqlException ||
                ex is OperationCanceledException)
            {
                if (!isClosing && !IsDisposed && currentDeviceId == deviceId)
                {
                    lblDbStatus.Text = "DB: 오류 알람 저장 실패";
                    lblDbStatus.ForeColor = Color.Firebrick;
                }

                return false;
            }
        }
        // 활성 알람을 DB에서 조회하여 DataGridView에 표시하는 메서드
        private async Task LoadActiveAlarmsAsync()
        {
            int deviceId = currentDeviceId;

            if (deviceId <= 0 || isClosing || IsDisposed)
                return;

            try
            {
                using var connection = new SqlConnection(DbConnectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandTimeout = 5;

                // 활성 알람을 조회하는 SQL 쿼리 작성
                // 저장프로시저 만들 필요가 없는 이유 : 단순 조회이므로 SQL 쿼리로 충분하며, 저장 프로시저를 만들면 유지보수가 어려워질 수 있음
                command.CommandText = """
                    SELECT
                        a.AlarmId,
                        a.OccurredAt,
                        CASE a.AlarmType
                            WHEN 'HIGH_TEMP' THEN N'고온'
                            WHEN 'SENSOR_ERROR' THEN N'센서 오류'
                            WHEN 'COMM_LOST' THEN N'통신 끊김'
                        END AS Category,
                            CONCAT(
                        CASE
                            WHEN a.RecoveredAt IS NOT NULL THEN N'[복귀 완료] '
                            ELSE N''
                        END,
                        a.Message,
                        CASE
                            WHEN a.TriggerTemperature IS NOT NULL
                                THEN CONCAT(
                                    N' · 발생 온도 ',
                                    CONVERT(nvarchar(20), a.TriggerTemperature),
                                    N' ℃'
                                )
                            ELSE N''
                        END
                    ) AS Message,
                        a.AcknowledgedAt,
                        CASE n.SendStatus
                            WHEN 'PENDING' THEN N'대기'
                            WHEN 'SENT' THEN N'발송 성공'
                            WHEN 'FAILED' THEN N'발송 실패'
                            ELSE N'대상 아님'
                        END AS KakaoStatus
                    FROM dbo.AlarmEvent AS a
                    LEFT JOIN dbo.AlarmNotification AS n
                        ON n.AlarmId = a.AlarmId
                    WHERE a.DeviceId = @DeviceId
                    AND (
                        a.RecoveredAt IS NULL
                        OR a.AcknowledgedAt IS NULL
                    )
                    ORDER BY a.OccurredAt DESC, a.AlarmId DESC;
                    """;

                // SQL 파라미터를 사용하여 DeviceId 값을 안전하게 전달
                command.Parameters.Add("@DeviceId", SqlDbType.Int).Value =
                    deviceId;

                // ExecuteReaderAsync() 메서드를 사용하여 쿼리 결과를 비동기적으로 가져옴
                var rows = new List<(long AlarmId, object[] Cells)>();

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    DateTime occurredUtc = DateTime.SpecifyKind(
                        reader.GetDateTime(1),
                        DateTimeKind.Utc);

                    rows.Add((
                        reader.GetInt64(0),
                        new object[]
                        {
                    occurredUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.IsDBNull(4) ? "확인" : "확인 완료",
                    reader.GetString(5)
                        }));
                }

                if (isClosing || IsDisposed || currentDeviceId != deviceId)
                    return;

                dgvActiveAlarms.Rows.Clear();

                foreach (var item in rows)
                {
                    int index = dgvActiveAlarms.Rows.Add(item.Cells);
                    dgvActiveAlarms.Rows[index].Tag = item.AlarmId;
                }
            }
            catch (SqlException ex)
            {
                if (isClosing || IsDisposed)
                    return;

                lblDbStatus.Text = $"DB: 알람 조회 실패 ({ex.Number})";
                lblDbStatus.ForeColor = Color.Firebrick;
            }
        }

        // DataGridView의 셀 클릭 이벤트 처리: 알람 확인 버튼 클릭 시 DB에 저장
        private bool isAcknowledgingAlarm;

        // DataGridView의 셀 클릭 이벤트 처리: 알람 확인 버튼 클릭 시 DB에 저장
        private async void dgvActiveAlarms_CellContentClick(
            object? sender,
            DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
                isClosing || isAcknowledgingAlarm)
                return;

            // "colAcknowledge" 열이 아닌 경우에는 처리하지 않는다.
            if (dgvActiveAlarms.Columns[e.ColumnIndex].Name != "colAcknowledge")
                return;

            var row = dgvActiveAlarms.Rows[e.RowIndex];

            if (row.Tag is not long alarmId ||
                Convert.ToString(row.Cells["colAcknowledge"].Value) != "확인")
                return;

            int deviceId = currentDeviceId;
            isAcknowledgingAlarm = true;

            try
            {
                using var connection = new SqlConnection(DbConnectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandTimeout = 5;

                // 활성 알람을 확인 처리하는 SQL 쿼리 작성
                command.CommandText = """
                    UPDATE dbo.AlarmEvent
                    SET AcknowledgedAt = SYSUTCDATETIME()
                    WHERE AlarmId = @AlarmId
                      AND DeviceId = @DeviceId
                      AND AcknowledgedAt IS NULL;
                 """;

                command.Parameters.Add("@AlarmId", SqlDbType.BigInt).Value =
                    alarmId;

                command.Parameters.Add("@DeviceId", SqlDbType.Int).Value =
                    deviceId;

                await command.ExecuteNonQueryAsync();

                if (!isClosing && !IsDisposed)
                    await LoadActiveAlarmsAsync();
            }
            catch (SqlException ex)
            {
                if (!isClosing && !IsDisposed)
                {
                    MessageBox.Show(
                        this,
                        $"알람 확인 저장에 실패했습니다.\nSQL 오류 번호: {ex.Number}",
                        "알람 확인",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            finally
            {
                isAcknowledgingAlarm = false;
            }
        }

        // 카카오 알람 발송을 위한 백그라운드 작업
        private async Task RunKakaoWorkerAsync(
            CancellationTokenSource ownerCts)
        {
            CancellationToken token = ownerCts.Token;

            // PeriodicTimer를 사용하여 2초마다 반복 작업 수행
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));

            try
            {
                // 카카오 알람 발송 작업을 반복 수행
                do
                {
                    // 작업 취소 요청이 들어오면 OperationCanceledException 예외를 발생시켜 반복 작업을 종료한다.
                    token.ThrowIfCancellationRequested();

                    int deviceId = currentDeviceId;

                    if (deviceId <= 0)
                        continue;

                    // 카카오 알람 발송 작업 수행
                    try
                    {
                        // SendPendingKakaoAlarmAsync() 메서드를 호출하여 대기 중인 카카오 알람을 발송하고, 발송 결과를 반환받는다.
                        bool processed =
                            await SendPendingKakaoAlarmAsync(deviceId, token);

                        // 처리 결과가 생겼을 때만 그리드를 갱신한다.
                        if (processed &&
                            !isClosing &&
                            !IsDisposed &&
                            currentDeviceId == deviceId)
                        {
                            await LoadActiveAlarmsAsync();
                        }
                    }
                    catch (OperationCanceledException)
                        when (token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (isClosing || IsDisposed)
                            break;

                        lblDbStatus.Text = "카카오: 처리 오류";
                        lblDbStatus.ForeColor = Color.Firebrick;

                        // 토큰을 출력하지 않고 예외 종류만 기록한다.
                        System.Diagnostics.Debug.WriteLine(
                            $"카카오 작업 오류: {ex.GetType().Name}");
                    }

                } while (await timer.WaitForNextTickAsync(token));
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // 폼 종료로 반복 작업을 마친다.
            }
            finally
            {
                if (ReferenceEquals(kakaoWorkerCts, ownerCts))
                    kakaoWorkerCts = null;

                ownerCts.Dispose();
            }
        }

        // 대기 중인 카카오 알람을 발송하는 메서드
        private async Task<bool> SendPendingKakaoAlarmAsync(
            int deviceId,
            CancellationToken token)
        {
            // 카카오 알람 발송을 위해 사용자 비밀에서 토큰을 읽어온다.
            var configuration = new ConfigurationBuilder()
            .AddUserSecrets<MainForm>(optional: true)
            .Build();

            string? accessToken = configuration["Kakao:AccessToken"];

            // 사용자 비밀에 토큰이 없으면 발송하지 않고 상태 표시만 한다.
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                if (!isClosing && !IsDisposed)
                    lblDbStatus.Text = "카카오: 사용자 비밀의 토큰 확인 필요";

                return false;
            }

            long alarmId;
            string message;
            // DB에서 발송 대기 중인 알람을 가져와서 발송 처리한다.
            using (var connection = new SqlConnection(DbConnectionString))
            {
                await connection.OpenAsync(token);

                using var command = connection.CreateCommand();
                command.CommandTimeout = 5;

                // 발송 대기 중인 알람을 가져오는 SQL 쿼리 작성 (UPDLOCK, READPAST, ROWLOCK 힌트 사용)
                // 저장 프로시저 만들 필요가 없는 이유 : 단순 조회 및 업데이트.
                // 저장 프로시저를 만들면 유지보수가 어려워질 수 있음.
                command.CommandText = """
                    ;WITH Target AS
                    (
                        SELECT TOP (1) n.*
                        FROM dbo.AlarmNotification AS n
                            WITH (UPDLOCK, READPAST, ROWLOCK)
                        WHERE n.SendStatus = 'PENDING'
                          AND n.AttemptCount = 0
                          AND EXISTS
                          (
                              SELECT 1
                              FROM dbo.AlarmEvent AS a
                              WHERE a.AlarmId = n.AlarmId
                                AND a.DeviceId = @DeviceId
                                AND a.AlarmType = 'HIGH_TEMP'
                                AND a.RecoveredAt IS NULL
                          )
                        ORDER BY n.AlarmId
                    )
                    UPDATE Target
                    SET AttemptCount = AttemptCount + 1,
                        LastAttemptAt = SYSUTCDATETIME(),
                        LastError = N'발송 처리 중'
                    OUTPUT INSERTED.AlarmId;
                    """;

                command.Parameters.Add("@DeviceId", SqlDbType.Int).Value = deviceId;

                // 
                object? result = await command.ExecuteScalarAsync(token);

                if (result == null || result == DBNull.Value)
                    return false;

                alarmId = Convert.ToInt64(result);

                using var detailCommand = connection.CreateCommand();

                // 발송할 알람의 상세 정보를 가져오는 SQL 쿼리 작성
                detailCommand.CommandText = """
                SELECT
                    d.DeviceName,
                    a.TriggerTemperature,
                    a.OccurredAt
                FROM dbo.AlarmEvent AS a
                INNER JOIN dbo.Device AS d ON d.DeviceId = a.DeviceId
                WHERE a.AlarmId = @AlarmId;
                """;

                detailCommand.Parameters.Add("@AlarmId", SqlDbType.BigInt).Value =
                    alarmId;

                using var reader = await detailCommand.ExecuteReaderAsync(token);

                if (!await reader.ReadAsync(token))
                    throw new InvalidOperationException("발송할 알람을 찾지 못했습니다.");

                DateTime occurredAt = DateTime.SpecifyKind(
                    reader.GetDateTime(2), DateTimeKind.Utc).ToLocalTime();

                // 카카오 알람 메시지 작성 (장비 이름, 발생 온도, 발생 시각 포함)
                message =
                    $"[고온 알람 #{alarmId}]\n" +
                    $"장비: {reader.GetString(0)}\n" +
                    $"발생 온도: {reader.GetDecimal(1):F1} ℃\n" +
                    $"발생 시각: {occurredAt:yyyy-MM-dd HH:mm:ss}";
            }

            bool succeeded = false;
            string? error = null;

            try
            {
                // 카카오톡 메시지 발송을 위한 HttpClient를 생성하고, 10초 제한을 두어 발송 요청을 수행한다.
                using var sendCts =
                    CancellationTokenSource.CreateLinkedTokenSource(token);

                sendCts.CancelAfter(TimeSpan.FromSeconds(10));

                // 카카오톡 메시지 템플릿 JSON 생성 (텍스트 메시지, 링크 포함)
                string templateJson = JsonSerializer.Serialize(new
                {
                    object_type = "text",
                    text = message,
                    link = new
                    {
                        web_url = "https://developers.kakao.com",
                        mobile_web_url = "https://developers.kakao.com"
                    },
                    button_title = "개발자 사이트"
                });

                // HttpRequestMessage를 생성하여 카카오톡 메시지 발송 API에 POST 요청을 보낸다.
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://kapi.kakao.com/v2/api/talk/memo/default/send");

                // Authorization 헤더에 Bearer 토큰을 설정하여 카카오톡 API 인증을 수행한다.
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken.Trim());

                // 폼 URL 인코딩된 콘텐츠를 요청 본문에 설정하여 템플릿 JSON을 전송한다.
                request.Content = new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["template_object"] = templateJson
                    });

                // HttpClient를 사용하여 비동기적으로 요청을 보내고, 응답을 받는다.
                using var response =
                    await kakaoHttpClient.SendAsync(request, sendCts.Token);

                string body =
                    await response.Content.ReadAsStringAsync(sendCts.Token);

                // 응답이 성공적인 경우, JSON 응답을 파싱하여 result_code가 0인지 확인하여 발송 성공 여부를 판단한다.
                if (response.IsSuccessStatusCode)
                {
                    using var json = JsonDocument.Parse(body);

                    succeeded =
                        json.RootElement.TryGetProperty("result_code", out var code) &&
                        code.TryGetInt32(out int value) &&
                        value == 0;
                }

                if (!succeeded)
                    error = $"HTTP {(int)response.StatusCode}: {body}";
            }
            catch (OperationCanceledException)
            {
                error = "요청 취소 또는 시간 초과. 실제 도착 여부 확인 필요";
            }
            catch (HttpRequestException ex)
            {
                error = ex.Message;
            }
            catch (JsonException)
            {
                error = "응답 해석 실패. 실제 도착 여부 확인 필요";
            }

            // 전송 취소 후에도 결과 기록은 별도 제한 시간으로 시도한다.
            using var saveCts =
                new CancellationTokenSource(TimeSpan.FromSeconds(5));

            using var saveConnection = new SqlConnection(DbConnectionString);
            await saveConnection.OpenAsync(saveCts.Token);

            using var saveCommand = saveConnection.CreateCommand();

            // 발송 결과를 DB에 기록하는 SQL 쿼리 작성 (발송 성공 시 SentAt에 현재 시각 기록, 실패 시 LastError에 오류 메시지 기록)
            saveCommand.CommandText = """
            UPDATE dbo.AlarmNotification
            SET SendStatus = @Status,
                SentAt = CASE WHEN @Status = 'SENT'
                              THEN SYSUTCDATETIME() ELSE NULL END,
                LastError = @Error
            WHERE AlarmId = @AlarmId;
            """;

            saveCommand.Parameters.Add("@AlarmId", SqlDbType.BigInt).Value =
                alarmId;

            saveCommand.Parameters.Add("@Status", SqlDbType.VarChar, 10).Value =
                succeeded ? "SENT" : "FAILED";

            saveCommand.Parameters.Add("@Error", SqlDbType.NVarChar, 500).Value =
                error == null
                    ? DBNull.Value
                    : error[..Math.Min(error.Length, 500)];

            await saveCommand.ExecuteNonQueryAsync(saveCts.Token);

            // 성공·실패 결과를 기록했으므로 화면을 다시 조회한다.
            return true;
        }

        // 폼 종료 이벤트 처리: 연결 해제 및 리소스 정리
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 폼 종료 이벤트 처리 전에 기본 동작 수행
            base.OnFormClosing(e);

            if (e.Cancel)
                return;

            // 종료 중임을 나타내는 플래그 설정
            isClosing = true;

            // 시계 CancellationTokenSource 취소
            clockCts?.Cancel();

            // kakaoWorker CancellationTokenSource 취소
            kakaoWorkerCts?.Cancel();

            // polling CancellationTokenSource 취소
            pollingCts?.Cancel();

            // DB 연결 확인 시 CancellationTokenSource 취소
            dbCheckCts?.Cancel();

            // 연결 해제 시 CancellationTokenSource 취소
            connectCts?.Cancel();

            // NetworkStream과 TcpClient 객체를 Dispose()하여 연결 해제 및 리소스 정리
            deviceStream?.Dispose();
            deviceStream = null;

            deviceClient?.Dispose();
            deviceClient = null;
        }

        // 알람 이력 버튼 클릭 이벤트 처리: 알람 이력 폼을 모달로 표시
        private void btnAlarmHistory_Click(object sender, EventArgs e)
        {
            using var historyForm = new AlarmHistoryForm(DbConnectionString);
            historyForm.ShowDialog(this);
        }

        // 라이선스 버튼 클릭 이벤트 처리: 라이선스 파일을 열어 표시
        private void btnLicenses_Click(object sender, EventArgs e)
        {
            using var form = new LicenseForm();
            form.ShowDialog(this);
        }
    }
}
