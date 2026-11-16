using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Temperature_Modbus
{
    public partial class AlarmHistoryForm : Form
    {
        private readonly string connectionString = string.Empty;
        private CancellationTokenSource? searchCts;
        private bool isClosing;

        private static readonly TimeZoneInfo KoreaTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");

        // 디자이너에서 사용하는 생성자
        public AlarmHistoryForm()
        {
            InitializeComponent();

            // AutoGenerateColumns를 false로 설정해야 컬럼 순서가 디자이너에서 설정한 순서대로 표시된다.
            dgvAlarmHistory.AutoGenerateColumns = false;

            dtpFromDate.Value = DateTime.Today;
            dtpToDate.Value = DateTime.Today;

            // Items는 디자이너에서 넣은
            // 전체, 고온, 센서 오류, 통신 끊김 순서를 사용한다.
            cmbAlarmType.SelectedIndex = 0;
        }

        // 메인폼에서 DB 연결 문자열을 전달받는다.
        public AlarmHistoryForm(string dbConnectionString) : this()
        {
            // DB 연결 문자열이 전달되지 않으면 조회 버튼 클릭 시 메시지 박스를 띄운다.
            connectionString = dbConnectionString;
        }

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            // 조회 중이거나 폼이 닫히는 중이면 조회를 시작하지 않는다.
            if (isClosing || searchCts != null)
                return;

            // DB 연결 문자열이 전달되지 않으면 조회 버튼 클릭 시 메시지 박스를 띄운다.
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                MessageBox.Show(this, "DB 연결 문자열이 전달되지 않았습니다.");
                return;
            }

            // 선택한 날짜를 한국 시간대의 날짜로 해석하도록 지정한다.
            DateTime fromDate = DateTime.SpecifyKind(
                dtpFromDate.Value.Date,
                DateTimeKind.Unspecified);

            DateTime toDate = DateTime.SpecifyKind(
                dtpToDate.Value.Date,
                DateTimeKind.Unspecified);

            if (fromDate > toDate)
            {
                MessageBox.Show(this, "시작일은 종료일보다 늦을 수 없습니다.");
                dtpFromDate.Focus();
                return;
            }

            // 종료일 다음 날 0시 미만으로 조회해서 종료일 전체를 포함한다.
            DateTime fromUtc =
                TimeZoneInfo.ConvertTimeToUtc(fromDate, KoreaTimeZone);

            DateTime toUtc =
                TimeZoneInfo.ConvertTimeToUtc(
                    toDate.AddDays(1), KoreaTimeZone);

            // 콤보박스에서 선택한 알람 유형을 DB 조회 조건에 맞게 변환한다.
            string? alarmType = cmbAlarmType.SelectedIndex switch
            {
                1 => "HIGH_TEMP",
                2 => "SENSOR_ERROR",
                3 => "COMM_LOST",
                _ => null
            };

            // 조회 시간 초과를 방지하기 위해 15초 후 취소되는 CancellationTokenSource를 생성한다.
            using var cts =
                new CancellationTokenSource(TimeSpan.FromSeconds(15));

            searchCts = cts;
            btnSearch.Enabled = false;
            lblHistoryCount.Text = "조회 중...";

            // 조회 작업을 비동기적으로 수행한다.
            try
            {
                // SqlConnection과 SqlCommand를 using으로 감싸서 사용 후 자동으로 해제되도록 한다.
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cts.Token);

                // SqlCommand를 생성하고, CommandTimeout을 10초로 설정한다.
                using var command = connection.CreateCommand();
                command.CommandTimeout = 10;

                // SQL 쿼리를 작성한다. 알람 유형과 상태를 한글로 변환하고, 발생일시 기준으로 내림차순 정렬한다.
                command.CommandText = """
                    SELECT
                        a.AlarmId,
                        d.DeviceName,
                        CASE a.AlarmType
                            WHEN 'HIGH_TEMP' THEN N'고온'
                            WHEN 'SENSOR_ERROR' THEN N'센서 오류'
                            WHEN 'COMM_LOST' THEN N'통신 끊김'
                            ELSE a.AlarmType
                        END AS Category,
                        a.Message,
                        a.TriggerTemperature,
                        a.OccurredAt,
                        a.AcknowledgedAt,
                        a.RecoveredAt,
                        CASE n.SendStatus
                            WHEN 'PENDING' THEN N'대기'
                            WHEN 'SENT' THEN N'발송 성공'
                            WHEN 'FAILED' THEN N'발송 실패'
                            ELSE N'대상 아님'
                        END AS KakaoStatus
                    FROM dbo.AlarmEvent AS a
                    INNER JOIN dbo.Device AS d
                        ON d.DeviceId = a.DeviceId
                    LEFT JOIN dbo.AlarmNotification AS n
                        ON n.AlarmId = a.AlarmId
                    WHERE a.OccurredAt >= @FromUtc
                      AND a.OccurredAt < @ToUtc
                      AND (@AlarmType IS NULL OR a.AlarmType = @AlarmType)
                    ORDER BY a.OccurredAt DESC, a.AlarmId DESC;
                    """;

                // SQL 쿼리의 매개변수를 설정한다. 알람 유형이 null이면 DBNull.Value를 사용한다.
                command.Parameters.Add("@FromUtc", SqlDbType.DateTime2).Value =
                    fromUtc;

                command.Parameters.Add("@ToUtc", SqlDbType.DateTime2).Value =
                    toUtc;

                command.Parameters.Add("@AlarmType", SqlDbType.VarChar, 20).Value =
                    (object?)alarmType ?? DBNull.Value;

                // 조회 결과를 저장할 리스트를 생성한다.
                var rows = new List<object[]>();

                // SqlDataReader를 사용하여 조회 결과를 읽는다. CancellationToken을 전달하여 취소 가능하게 한다.
                using var reader =
                    await command.ExecuteReaderAsync(cts.Token);

                // 조회 결과를 한 행씩 읽어와서 리스트에 추가한다. DBNull 값은 적절히 처리한다.
                while (await reader.ReadAsync(cts.Token))
                {
                    rows.Add(new object[]
                    {
                        reader.GetInt64(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetString(3),
                        reader.IsDBNull(4)
                            ? "—"
                            : reader.GetDecimal(4).ToString("F1"),
                        FormatKoreaTime(reader.GetDateTime(5)),
                        reader.IsDBNull(6)
                            ? "미확인"
                            : FormatKoreaTime(reader.GetDateTime(6)),
                        reader.IsDBNull(7)
                            ? "미복귀"
                            : FormatKoreaTime(reader.GetDateTime(7)),
                        reader.GetString(8)
                    });
                }

                // 조회 결과를 DataGridView에 표시한다. 폼이 닫히는 중이거나 Dispose된 경우에는 표시하지 않는다.
                if (isClosing || IsDisposed)
                    return;

                // 조회 결과를 표시하기 전에 DataGridView의 기존 행을 모두 제거한다.
                dgvAlarmHistory.Rows.Clear();

                // 조회 결과를 DataGridView에 추가한다.
                foreach (object[] row in rows)
                    dgvAlarmHistory.Rows.Add(row);

                lblHistoryCount.Text = $"조회 결과 {rows.Count:N0}건";
            }
            catch (OperationCanceledException)
            {
                if (!isClosing && !IsDisposed)
                    lblHistoryCount.Text = "조회 시간 초과 · 다시 조회하세요";
            }
            catch (SqlException ex)
            {
                if (!isClosing && !IsDisposed)
                {
                    lblHistoryCount.Text = "조회 실패";

                    MessageBox.Show(
                        this,
                        $"SQL 오류 번호: {ex.Number}\n\n{ex.Message}",
                        "알람 이력 조회 실패",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            finally
            {
                searchCts = null;

                if (!isClosing && !IsDisposed)
                    btnSearch.Enabled = true;
            }
        }

        // 한국 시간으로 변환하여 "yyyy-MM-dd HH:mm:ss" 형식으로 반환하는 메서드
        private static string FormatKoreaTime(DateTime value)
        {
            DateTime utc = DateTime.SpecifyKind(value, DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(utc, KoreaTimeZone)
                .ToString("yyyy-MM-dd HH:mm:ss");
        }

        // 닫기 버튼 클릭 시 폼을 닫는다.
        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        // 폼이 닫히는 중이면 조회 작업을 취소한다.
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (e.Cancel)
                return;

            isClosing = true;
            searchCts?.Cancel();
        }
    }
}