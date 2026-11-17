# Windows에서 실행하기

1. .NET 8 SDK를 설치한 Windows PC에서 솔루션을 열고 NuGet 패키지를 복원합니다.
2. 빈 DB 환경에서는 SSMS에서 [온도 센서 DB 쿼리문](../SQL/온도%20센서%20DB%20쿼리문.sql)을 한 번 실행합니다. 이 파일이 TemperatureMonitorDB와 세 테이블을 생성합니다. 같은 폴더의 ‘DB 테이블 생성 쿼리문’은 테이블 정의가 겹치므로 둘 중 하나만 사용합니다.
3. [오류 알람 저장 프로시저](../SQL/오류%20알람%20저장%20프로시저.sql)와 [고온 알람 프로시저](../SQL/활성%20고온%20알람%20조회%20저장프로시저.sql)를 실행합니다.
4. dbo.Device에 DeviceName, MacAddress, LastKnownIp, TcpPort를 등록합니다. 장비에서 확인한 IP와 포트를 사용합니다. 프로그램은 연결 전에 이 두 값으로 장비를 찾습니다.
5. Visual Studio에서 Temperature_Modbus 프로젝트를 우클릭해 **사용자 비밀 관리**를 엽니다. 아래 예시에서 서버·계정·암호를 해당 환경의 값으로 바꿉니다. 카카오 메시지를 시험할 때는 talk_message 권한이 있는 사용자 토큰을 입력합니다.

```json
{
  "ConnectionStrings:TemperatureMonitor": "Server=<서버>;Database=TemperatureMonitorDB;User ID=<로그인>;Password=<암호>;Encrypt=True;TrustServerCertificate=True;Connect Timeout=5;",
  "Kakao:AccessToken": "<사용자 액세스 토큰>"
}
```

예시는 로컬 실습 서버 인증서를 신뢰하는 설정입니다. 다른 PC에서 기존 DB에 접속할 때는 Server에 해당 SQL Server 주소를 입력합니다.

6. F5로 실행한 뒤 DB 상태와 장비 수를 확인합니다. spFinder에서 확인한 IP·포트로 **통신 연결**을 누르고 현재 온도·그래프가 갱신되는지 확인합니다.

## 패키지 변경 후 라이선스 갱신

솔루션 폴더에서 실행합니다. 최초 설치 명령은 `dotnet tool install --global nuget-license`입니다.

```powershell
dotnet restore
nuget-license -i .\Temperature_Modbus\Temperature_Modbus.csproj -t -d .\Temperature_Modbus\Licenses -o JsonPretty -fo .\Temperature_Modbus\licenses.json
```

앱의 라이선스 화면에서 패키지 버전과 원문 열기 결과를 확인할 수 있습니다.
