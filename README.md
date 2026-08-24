# IIDXTierTable

beatmania IIDX의 플레이 데이터와 서열표를 비교해 곡별 정보를 확인할 수 있는 웹 애플리케이션입니다.

## 주요 기능

- SP 서열표 데이터 조회
- Normal/Hard 서열표 보기
- 버전, 난이도, 타입, 티어별 필터링
- IIDX 플레이 데이터 CSV 가져오기
- 플레이 데이터와 서열표 곡 매칭
- 매칭된 곡의 점수, DJ LEVEL, 클리어 타입, 미스 카운트 확인
- 브라우저의 `localStorage`를 이용한 기기 내 데이터 저장
- PWA 및 배포 환경의 오프라인 앱 셸 지원

## 기술 스택

- .NET 10
- Blazor WebAssembly
- Azure Functions Isolated Worker
- Bootstrap
- Azure Monitor OpenTelemetry Exporter

## 프로젝트 구조

```text
IIDXTierTable/
├── IIDXTierTable/       # Blazor WebAssembly 프론트엔드
└── IIDXTierTable.Api/   # Azure Functions API
```

## 실행 환경

- .NET SDK 10 이상
- Visual Studio 2022 또는 Visual Studio Code
- 로컬 API를 실행할 경우 Azure Functions Core Tools

## 로컬 실행

### 1. API 실행

프로젝트 루트에서 다음 명령을 실행합니다.

```powershell
Push-Location .\IIDXTierTable.Api
func start
Pop-Location
```

API의 기본 로컬 주소는 Azure Functions 실행 설정에 따라 확인해야 합니다. 프론트엔드의 개발용 설정인 `IIDXTierTable/wwwroot/appsettings.Development.json`의 `ApiBaseUrl`과 API 주소가 일치해야 합니다.

### 2. 프론트엔드 실행

별도 터미널에서 다음 명령을 실행합니다.

```powershell
dotnet run --project .\IIDXTierTable\IIDXTierTable.csproj
```

실행 후 터미널에 표시된 로컬 URL로 접속합니다.

### 3. 빌드

프론트엔드 빌드:

```powershell
dotnet build .\IIDXTierTable\IIDXTierTable.csproj
```

API 빌드:

```powershell
dotnet build .\IIDXTierTable.Api\IIDXTierTable.Api.csproj
```

솔루션 전체 빌드:

```powershell
dotnet build .\IIDXTierTable.slnx
```

## API

API의 기본 경로는 `/api/`입니다.

| 메서드 | 경로 | 설명 |
|---|---|---|
| GET | `/api/tier-table` | SP12 서열표 데이터 반환 |
| GET | `/api/rank-points` | 랭크 포인트 데이터 반환 |

응답에는 캐시 효율을 높이기 위한 `ETag`와 데이터 버전을 나타내는 `X-Data-Version` 헤더가 포함됩니다.

## 플레이 데이터 가져오기

1. e-amusement의 [플레이 데이터 CSV 다운로드 페이지](https://p.eagate.573.jp/game/2dx/33/djdata/score_download.html)를 엽니다.
2. 로그인 후 플레이 데이터를 CSV로 저장하거나 CSV 텍스트를 복사합니다.
3. 애플리케이션의 플레이 데이터 가져오기 페이지에서 다음 중 하나를 선택합니다.
	- CSV 파일 업로드
	- 텍스트 붙여넣기
4. 저장 버튼을 클릭합니다.

파일 또는 텍스트 입력은 최대 1MiB까지 지원합니다. CSV 파싱에 성공하면 변환된 JSON 데이터가 브라우저에 저장되며, 저장 완료 메시지에 실제 JSON 크기가 표시됩니다.

## 지원 CSV 형식

현재는 e-amusement에서 다운로드한 고정 형식의 CSV를 지원합니다.

- 필드 구분자: 쉼표(`,`)
- 필수 열 수: 41개
- 첫 번째 행: 지정된 일본어 헤더
- 지원 난이도: `BEGINNER`, `NORMAL`, `HYPER`, `ANOTHER`, `LEGGENDARIA`
- 날짜 형식: `yyyy-MM-dd HH:mm`
- 미스 카운트: 원본 CSV의 텍스트를 그대로 저장

현재 CSV 계약에는 필드 내부의 쉼표나 줄바꿈이 포함되지 않는 것으로 가정합니다. 곡명이나 아티스트명에 포함된 큰따옴표는 일반 문자로 보존됩니다.

## 데이터 저장 및 개인정보

- 플레이 데이터는 서버 데이터베이스에 저장하지 않습니다.
- 가져온 데이터는 현재 사용 중인 브라우저의 `localStorage`에 저장됩니다.
- 다른 기기나 다른 브라우저와 자동으로 동기화되지 않습니다.
- 브라우저의 사이트 데이터 삭제, 시크릿 모드 종료, 저장소 초기화 등에 따라 데이터가 삭제될 수 있습니다.
- 저장된 플레이 데이터에는 게임 플레이 기록이 포함될 수 있으므로 공용 기기에서는 사용 후 사이트 데이터를 확인하세요.

## 배포

Blazor WebAssembly 앱은 다음과 같이 Release publish할 수 있습니다.

```powershell
dotnet publish .\IIDXTierTable\IIDXTierTable.csproj -c Release
```

Azure Functions API는 다음과 같이 publish할 수 있습니다.

```powershell
dotnet publish .\IIDXTierTable.Api\IIDXTierTable.Api.csproj -c Release
```

배포 시 `IIDXTierTable/wwwroot/appsettings.json`의 `ApiBaseUrl`이 실제 API 주소를 가리키는지 확인해야 합니다. 배포용 PWA Service Worker는 정적 리소스 캐시와 API 네트워크 우선 캐시를 사용합니다.

## 알려진 제한 사항

- 현재 CSV 형식은 특정 e-amusement 다운로드 형식에 맞춰져 있습니다.
- CSV 파일과 붙여넣기 텍스트의 최대 크기는 1MiB입니다.
- 플레이 데이터는 브라우저별·기기별로 저장되며 계정 간 동기화를 제공하지 않습니다.
- 곡 매칭은 서열표의 제목과 `MatchTitle` 규칙에 의존합니다.
- 운영 API 주소와 CORS 설정은 배포 환경에 맞게 별도로 구성해야 합니다.