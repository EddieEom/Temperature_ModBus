# 개발 과정

[프로젝트 홈](../README.md) · [설계 과정](design.md) · [시험 기록](verification.md)

## 진행 흐름

실제 장비 연결과 온도 수신을 출발점으로 기능을 확장했습니다. 아래 단계는 조회 가능한 개발 대화와 현재 소스의 기능 의존성을 바탕으로 정리한 순서입니다. 세부 작업별 소요 시간은 측정 기록이 없습니다.

| 단계 | 작업 | 확인 기준 |
| --- | --- | --- |
| 요구사항 정리 | 실시간 온도·그래프·알람·이력·개인 메시지 범위 정리 | 필요한 입력·상태·출력 정의 |
| 연결 | IP·포트 입력, TCP 연결·해제, 시간 제한 | 실제 장비에 TCP 연결 |
| 단건 수신 | Modbus 요청 생성, 응답 검사, 온도 변환 | 정상 수신 25.0℃ 확인 |
| 주기 수집 | 약 1초 루프, 센서 상태 선조회 | 화면·그래프에 갱신 |
| 상태 판정 | 연속 샘플과 발생·복귀 임계값 | 판정 횟수와 고온 상태 표시 |
| DB 연동 | 장비·알람·발송 테이블과 저장 프로시저 | 발생·확인·복귀 이력 대조 |
| 카카오 연동 | 발송 대기 처리, HTTP 호출, 결과 기록 | 메시지와 DB 발송 상태 대조 |
| 조회·배포 준비 | 이력 화면, 라이선스, 사용자 비밀, Git | 다른 PC의 설정 절차 정리 |

## 연결 객체와 수명

연결 성공 전 TcpClient는 지역 변수 pendingClient로 관리합니다. DB 장비 조회와 TCP 연결을 모두 통과한 뒤 전역 deviceClient와 deviceStream으로 넘깁니다. 실패한 연결 객체는 finally에서 정리합니다.

연결 중 버튼을 비활성화하고 connectCts로 중복 시도를 막습니다. 연결이 성립하면 입력란을 잠그고 버튼을 통신 해제로 바꿉니다. 사용자가 해제하거나 수신 오류가 발생하면 스트림·클라이언트를 정리하고 다시 연결할 수 있는 상태로 전환합니다.

연결 전 RestoreAlarmStateAsync에서 입력 IP·포트와 일치하는 장비를 확인합니다. 여러 행이 매칭되면 예외로 처리하고, 미복귀 고온 이력이 있으면 메모리 상태를 복원합니다.

## 주기 수집과 UI

PollTemperatureAsync는 센서 상태 레지스터를 조회하고, 정상일 때 온도 레지스터를 읽습니다. 정상값은 온도 표시, 그래프 추가, 고온 판정, 상태 저장으로 이어집니다. 센서 오류이면 온도 자리에 대시를 표시하고 그래프에 NaN을 넣어 관측 공백을 남깁니다.

PeriodicTimer와 비동기 I/O를 사용합니다. 카카오 작업도 별도 비동기 루프로 구성했습니다. 현재 코드에는 전용 스레드를 생성하는 구조가 없으며, async 작업들이 대기 구간에서 실행을 양보합니다. DB 저장 지연은 해당 수집 루프의 다음 처리 시점에 영향을 줄 수 있습니다.

폼 종료 시 시계·카카오 작업·수집·DB 확인·연결 시도의 취소를 요청하고 네트워크 객체를 정리합니다. 이전 연결 작업이 새로운 연결 화면을 덮어쓰는 것을 줄이기 위해 일부 경로에서 ownerCts 참조와 isClosing·IsDisposed를 검사합니다.

## 그래프 구현

| 항목 | 현재 처리 |
| --- | --- |
| 데이터 | 시각과 온도 List를 Scatter에 연결 |
| 시작 | 첫 유효 온도 수신 시 시작 시각 확정 |
| X축 | 처음 10분은 시작부터 채우고 이후 최근 10분 표시 |
| 오래된 데이터 | 표시 창보다 이전인 값을 리스트에서 제거 |
| Y축 | 20~100℃ 고정 |
| 시각 표기 | HH:mm:ss 24시간 형식 |
| 기준선 | 고온 80℃ 빨간 점선, 복귀 40℃ 녹색 점선 |
| 수신 공백 | NaN을 추가해 선을 끊음 |
| 한글 | 배포 폰트를 FileFontResolver에 등록 |

Y축 범위는 화면 표시 설정입니다. 장비 측정 범위 전체를 그래프가 자동 확대해 보여주는 구조는 현재 구현에 없습니다. 주석 일부의 수치와 실행 코드가 다를 수 있어, 문서에는 SetLimitsY(20, 100)을 기준으로 기록했습니다.

## DB 저장과 화면 반영

C#에서 판정한 상태와 DB에 마지막으로 저장한 상태를 각각 관리합니다. 두 상태가 다를 때 SaveAlarmStateAsync가 저장 프로시저를 호출하고, 성공한 뒤 저장 완료 상태를 갱신합니다. SQL 오류일 때는 저장 완료 상태를 갱신하지 않아 이후 정상 수신에서 다시 저장을 시도합니다.

이 방식은 현재 상태 전환을 재시도합니다. DB 장애 중 발생했다가 복구 전에 이미 해소된 모든 중간 이벤트를 보존하는 내구성 큐는 현재 구현에서 확인되지 않습니다.

활성 알람 그리드의 각 행 Tag에는 AlarmId를 보관합니다. 확인 버튼을 누르면 해당 장비·알람의 AcknowledgedAt이 비어 있는 경우만 갱신합니다. 이력 조회는 시작일 0시부터 종료일 다음 날 0시 미만을 한국 시간에서 UTC로 변환해 검색합니다.

## 카카오 연동

고온 알람 등록 시 발송 대기 행을 함께 생성합니다. RunKakaoWorkerAsync는 약 2초마다 현재 장비의 대기 건을 조회합니다. HttpClient는 재사용하고, 액세스 토큰은 사용자 비밀에서 읽습니다.

발송 선점에는 UPDLOCK·READPAST·ROWLOCK을 사용하고, 선택과 시도 횟수 증가를 하나의 UPDATE 문으로 처리합니다. API 응답을 확인한 뒤 SendStatus·SentAt·LastError를 기록합니다. HTTP 취소 뒤에도 발송 결과를 저장하도록 별도 5초 취소 제한을 사용합니다.

처음 연동 과정에서는 talk_message 권한 부족에 따른 403/-402 응답을 확인하고, 권한·토큰을 점검한 뒤 200 OK와 실제 시험 메시지를 확인한 기록이 있습니다. 현재의 재시도·중단 제약은 [트러블슈팅](troubleshooting.md#카카오-발송과-상태-추적)에 남겼습니다.

## 라이선스와 실행 설정

LicenseForm은 licenses.json의 패키지·버전·라이선스를 읽습니다. Licenses 폴더에서 패키지명__버전 형식의 HTML·TXT 원문을 찾아 실제 존재하는 파일을 엽니다. 누락 파일 개수도 화면에 표시합니다.

DB 연결 문자열과 카카오 토큰은 .NET 사용자 비밀에 보관합니다. Git에는 소스와 SQL·폰트·라이선스를 등록하고, Visual Studio 임시 파일과 빌드 산출물을 제외했습니다. 새 PC 설정 순서는 [실행 및 설정](setup.md)에 있습니다.

## 코드 탐색

| 보고 싶은 흐름 | 진입점과 이어서 볼 메서드 |
| --- | --- |
| 연결 실패 | btnConnect_Click → RestoreAlarmStateAsync → TcpClient.ConnectAsync |
| 값 수신·변환 | PollTemperatureAsync → ReadInputRegisterAsync |
| 그래프·한글 | RegisterChartFont → InitializeTemperatureGraph → AppendTemperaturePoint |
| 고온 판정 | EvaluateTemperatureAlarm → SaveAlarmStateAsync |
| 센서·통신 오류 | PollTemperatureAsync → SaveDeviceFaultAsync |
| 확인 버튼 | dgvActiveAlarms_CellContentClick → AlarmEvent UPDATE |
| 카카오 대기·발송 | RunKakaoWorkerAsync → SendPendingKakaoAlarmAsync |
| 과거 이력 | AlarmHistoryForm.btnSearch_Click |
| 종료 | OnFormClosing → 각 작업 Cancel 및 네트워크 Dispose |
| 원문 라이선스 | LicenseForm_Shown → btnOpenLicense_Click |

소스 위치는 [MainForm](../Temperature_Modbus/MainForm.cs), [AlarmHistoryForm](../Temperature_Modbus/AlarmHistoryForm.cs), [LicenseForm](../Temperature_Modbus/LicenseForm.cs)입니다.

## AI 활용

개발 순서를 정하고 반복 작업을 줄이는 데 AI를 활용해 생산성을 높였습니다.

| 활용 영역 | 적용 내용 | 확인 방식 |
| --- | --- | --- |
| 개발 순서 | 연결·단건 수신·주기 수집·알람·저장·발송 단계 정리 | 단계별로 실제 동작을 확인하며 다음 기능 추가 |
| 반복 구현 | UI 속성, SQL 매개변수, 유사한 상태 표시와 예외 처리 초안 | 현재 코드의 호출 경로와 화면 결과 대조 |
| 문제 분석 보조 | 오류 메시지와 네트워크·DB·API 확인 항목 정리 | 배선·DHCP·실제 장비 수신·응답값 확인 |
| 문서화 | 주석, 시험 항목, 개발·트러블슈팅 기록 초안 | 시험 기록과 소스의 임계값·동작 비교 |

배선 확인과 실제 주소 식별, 장비 시험, 알람 발생·확인·복귀와 메시지 결과 대조를 통해 적용 결과를 확인했습니다. 생산성 향상률이나 절감 시간을 수치로 측정한 기록은 없습니다.
