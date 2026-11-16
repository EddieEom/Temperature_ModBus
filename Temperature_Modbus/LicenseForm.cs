// NuGet 패키지의 라이선스 목록과 원문 파일을 수집하는 도구
//dotnet tool install --global nuget-license

// 프로젝트 최상위 폴더에서 실행.
// dotnet restore
// nuget-license -i .\Temperature_Modbus\Temperature_Modbus.csproj -t -d .\Temperature_Modbus\Licenses -o JsonPretty -fo .\Temperature_Modbus\licenses.json


using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

namespace Temperature_Modbus
{
    public partial class LicenseForm : Form
    {
        private readonly string licenseFolder =
            Path.Combine(AppContext.BaseDirectory, "Licenses");

        public LicenseForm()
        {
            InitializeComponent();

            // icenseGrid를 초기화.
            InitializeLicenseGrid();

            // SelectionChanged 이벤트를 구독하여 선택된 행이 변경될 때 버튼 상태를 업데이트.
            dgvLicenses.SelectionChanged += dgvLicenses_SelectionChanged;
        }

        // 데이터그리드뷰를 초기화 하는 메서드 - 속성창 방식이 아닌 코드로 설정함.
        private void InitializeLicenseGrid()
        {
            // 기존 열을 모두 제거하고 새 열을 추가.
            dgvLicenses.Columns.Clear();

            // 데이터그리드뷰의 속성을 설정.
            dgvLicenses.ReadOnly = true;
            dgvLicenses.AllowUserToAddRows = false;
            dgvLicenses.AllowUserToDeleteRows = false;
            dgvLicenses.MultiSelect = false;
            dgvLicenses.RowHeadersVisible = false;

            // 행 선택 모드를 전체 행 선택으로 설정.
            dgvLicenses.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;

            // 열 너비를 자동으로 조정하도록 설정.
            dgvLicenses.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;

            // 열을 추가하고 각 열의 FillWeight를 설정하여 너비 비율을 조정.
            dgvLicenses.Columns.Add("colPackage", "패키지");
            dgvLicenses.Columns.Add("colVersion", "버전");
            dgvLicenses.Columns.Add("colLicense", "라이선스");
            dgvLicenses.Columns.Add("colFileStatus", "원문 파일");

            // FillWeight를 설정하여 각 열의 너비 비율을 조정.
            dgvLicenses.Columns["colPackage"].FillWeight = 240;
            dgvLicenses.Columns["colVersion"].FillWeight = 90;
            dgvLicenses.Columns["colLicense"].FillWeight = 100;
            dgvLicenses.Columns["colFileStatus"].FillWeight = 100;

            btnOpenLicense.Enabled = false;
        }

        // 폼이 표시될 때 라이선스 목록을 비동기적으로 불러오는 이벤트 핸들러.
        private async void LicenseForm_Shown(object? sender, EventArgs e)
        {
            // licenses.json 파일 경로를 설정.
            string jsonPath = Path.Combine(
                AppContext.BaseDirectory,
                "licenses.json");

            lblLicenseStatus.Text = "라이선스 목록을 불러오는 중...";

            // 비동기적으로 라이선스 목록을 불러오는 메서드를 호출.
            try
            {
                // licenses.json 파일을 비동기적으로 읽어옴.
                string json = await File.ReadAllTextAsync(jsonPath);

                // 폼이 이미 닫혔거나 Dispose 중이면 작업을 중단.
                // dispose 상태를 확인하여 예외를 방지.
                if (IsDisposed || Disposing)
                    return;

                // JSON 문서를 파싱하고 최상위 요소가 배열인지 확인.
                using JsonDocument document = JsonDocument.Parse(json);

                // 최상위 요소가 배열이 아닌 경우 예외를 발생시킴.
                // 이 로직의 목적은 licenses.json 파일의 형식이 올바른지 확인하는 것.
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    // licenses.json의 최상위 형식이 배열이 아닌 경우 예외를 발생시킴.
                    throw new JsonException(
                        "licenses.json의 최상위 형식은 배열이어야 합니다.");
                }

                // 파일 경로는 실제 폴더에서 읽은 파일만 사용한다.
                string[] files = Directory.Exists(licenseFolder)
                    ? Directory.GetFiles(licenseFolder)
                    : Array.Empty<string>();

                dgvLicenses.Rows.Clear();

                // 원문 파일이 없는 패키지 수를 세기 위한 변수 초기화.
                int missingCount = 0;

                // licenses.json의 각 항목을 순회하며 데이터그리드뷰에 추가.
                foreach (JsonElement item in document.RootElement.EnumerateArray())
                {
                    // licenses.json의 각 항목에서 PackageId, PackageVersion, License를 추출.
                    string packageId = GetText(item, "PackageId");
                    string version = GetText(item, "PackageVersion");
                    string license = GetText(item, "License");

                    string expectedName = $"{packageId}__{version}";

                    // 원문 파일 경로를 찾기 위해 files 배열에서 조건에 맞는 첫 번째 파일을 검색.
                    string? filePath = files.FirstOrDefault(path =>
                        string.Equals(
                            Path.GetFileNameWithoutExtension(path),
                            expectedName,
                            StringComparison.OrdinalIgnoreCase)
                        &&
                        (string.Equals(
                            Path.GetExtension(path),
                            ".html",
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        string.Equals(
                            Path.GetExtension(path),
                            ".txt",
                            StringComparison.OrdinalIgnoreCase)));

                    if (filePath == null)
                        missingCount++;

                    // 데이터그리드뷰에 새 행을 추가하고 각 셀에 값을 설정.
                    int rowIndex = dgvLicenses.Rows.Add(
                        packageId,
                        version,
                        string.IsNullOrWhiteSpace(license)
                            ? "정보 없음"
                            : license,
                        filePath == null ? "파일 없음" : "열기 가능");

                    // 추가된 행을 가져와서 Tag 속성에 원문 파일 경로를 저장.
                    DataGridViewRow row = dgvLicenses.Rows[rowIndex];

                    // 선택한 행에서 열 원문 파일 경로
                    row.Tag = filePath;

                    if (filePath == null)
                    {
                        row.Cells["colFileStatus"].Style.ForeColor =
                            Color.Firebrick;
                    }
                }

                lblLicenseStatus.Text =
                    $"패키지 {dgvLicenses.Rows.Count}개 / 원문 파일 없음 {missingCount}개";

                // 선택된 행이 변경될 때 버튼 상태를 업데이트.
                UpdateOpenButton();
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is JsonException)
            {
                if (IsDisposed || Disposing)
                    return;

                lblLicenseStatus.Text = "라이선스 목록 불러오기 실패";

                MessageBox.Show(
                    this,
                    $"licenses.json과 Licenses 폴더를 확인하세요.\n\n{ex.Message}",
                    "라이선스 조회",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        // JsonElement에서 지정된 속성 이름의 문자열 값을 안전하게 가져오는 메서드.
        private static string GetText(JsonElement item, string propertyName)
        {
            return item.TryGetProperty(propertyName, out JsonElement value)
                && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? ""
                : "";
        }

        // 데이터그리드뷰의 선택된 행이 변경될 때 호출되는 이벤트 핸들러.
        private void dgvLicenses_SelectionChanged(object? sender, EventArgs e)
        {
            UpdateOpenButton();
        }

        // 선택된 행의 원문 파일 경로에 따라 "열기" 버튼의 활성화 상태를 업데이트하는 메서드.

        private void UpdateOpenButton()
        {
            btnOpenLicense.Enabled =
                dgvLicenses.CurrentRow?.Tag is string;
        }

        // "열기" 버튼 클릭 시 선택된 라이선스 원문 파일을 여는 이벤트 핸들러.

        private void btnOpenLicense_Click(object? sender, EventArgs e)
        {
            if (dgvLicenses.CurrentRow?.Tag is not string filePath)
                return;

            if (!File.Exists(filePath))
            {
                MessageBox.Show(
                    this,
                    "선택한 라이선스 원문 파일을 찾을 수 없습니다.",
                    "라이선스 원문",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            try
            {
                // HTML은 기본 브라우저, TXT는 연결된 프로그램으로 연다.
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }

            // 특정 예외를 필터링하여 사용자에게 경고 메시지를 표시.
            catch (Exception ex) when (

                // Win32Exception은 프로세스 시작 실패 시 발생할 수 있음.
                ex is Win32Exception ||
                // IOException은 파일 접근 문제 시 발생할 수 있음.
                ex is IOException ||
                // UnauthorizedAccessException은 권한 문제 시 발생할 수 있음.
                ex is UnauthorizedAccessException ||
                // InvalidOperationException은 잘못된 프로세스 시작 시 발생할 수 있음.
                ex is InvalidOperationException)
            {
                MessageBox.Show(
                    this,
                    $"라이선스 원문을 열지 못했습니다.\n\n{ex.Message}",
                    "라이선스 원문",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
} 