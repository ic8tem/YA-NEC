[Italiano](README.md) · [English](README.en.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Português](README.pt.md) · [日本語](README.ja.md) · **한국어**

# YA-NEC

**NEC-2 기반 와이어 안테나 시뮬레이터** (Windows, Linux, macOS 지원. C#, .NET 8, Avalonia).
제작: Costantino Cerrotta **IC8TEM**.

> *이 프로그램의 역사는 여러분이 만들어 갑니다. 여러분의 프로젝트, 아이디어, 재능, 발견, 그리고 확신으로.*

계산 엔진은 **nec2c**(로렌스 리버모어 국립연구소의 NEC-2)를 C#으로 옮긴 것이며, 매 릴리스마다 nec2c 및 EZNEC와 비교합니다. **NEC (.nec)**, **MMANA (.maa)**, **AutoEZ (.weq)**, **EZNEC (.ez)**, **K6STI의 YO/AO (.yag, .ant)** 모델과 자체 **.yanec** 형식을 열 수 있습니다.

주요 기능: 임피던스, SWR, 이득, 2D·3D 방사 패턴, 주파수 스윕과 스미스 차트; 텔레스코픽(단차 직경) 파이프에 대한 Leeson 보정(EZNEC와 동일); 대지(완전 도체, 반사 계수, Sommerfeld, 래디얼); 변수, 수식, 최적화 도구; 40개가 넘는 계산기(DL6WU 롱 야기, 쿼기, LPDA, Moxon, 헥스빔, 멀티밴드 다이폴과 야기, 포스퀘어, 수직 안테나, 파라볼라와 급전부, 엘리먼트 고정 브래킷 등); 수신 안테나와 RDF; 잡음, G/T, 스택; 동축 케이블 표, 정합 선로, 발룬; 입력 데이터 검사. 모든 계산 방식과 체크박스는 [매뉴얼](doc/Manuale_d_uso.pdf)에 설명되어 있으며 프로그램 안(매뉴얼 탭)에서도 볼 수 있습니다. 자세한 내용은 [Wiki](https://github.com/ic8tem/YA-NEC/wiki)를 참고하세요.

**언어:** 화면은 37개 언어를 지원합니다(오른쪽 위 언어 메뉴). 위키도 마찬가지입니다. 매뉴얼은 이탈리아어입니다.

## 화면

<table><tr><td width="50%" align="center"><a href="docs/screenshots/struttura-3d.png"><img src="docs/screenshots/struttura-3d.png" alt="전류와 로브가 있는 3D 구조"></a><br><sub>전류와 로브가 있는 3D 구조</sub></td><td width="50%" align="center"><a href="docs/screenshots/terreno-reale.png"><img src="docs/screenshots/terreno-reale.png" alt="DEM 단면 위의 HFTA 방식 실제 지형"></a><br><sub>DEM 단면 위의 HFTA 방식 실제 지형</sub></td></tr><tr><td width="50%" align="center"><a href="docs/screenshots/ricezione-rdf.png"><img src="docs/screenshots/ricezione-rdf.png" alt="수신: RDF, DMF와 잡음"></a><br><sub>수신: RDF, DMF와 잡음</sub></td><td width="50%" align="center"><a href="docs/screenshots/tolleranze.png"><img src="docs/screenshots/tolleranze.png" alt="제작 공차 (몬테카를로)"></a><br><sub>제작 공차 (몬테카를로)</sub></td></tr><tr><td width="50%" align="center"><a href="docs/screenshots/cavo-balun.png"><img src="docs/screenshots/cavo-balun.png" alt="케이블과 발룬: 공통 모드 전류"></a><br><sub>케이블과 발룬: 공통 모드 전류</sub></td><td width="50%" align="center"><a href="docs/screenshots/yagi-incrociate.png"><img src="docs/screenshots/yagi-incrociate.png" alt="교차 야기: RHCP, LHCP, 축비"></a><br><sub>교차 야기: RHCP, LHCP, 축비</sub></td></tr><tr><td width="50%" align="center"><a href="docs/screenshots/vento.png"><img src="docs/screenshots/vento.png" alt="기계 계산: 바람 (NTC 2018)"></a><br><sub>기계 계산: 바람 (NTC 2018)</sub></td><td width="50%" align="center"><a href="docs/screenshots/divisori.png"><img src="docs/screenshots/divisori.png" alt="전력 분배기와 위상 케이블"></a><br><sub>전력 분배기와 위상 케이블</sub></td></tr><tr><td width="50%" align="center"><a href="docs/screenshots/carichi-hf.png"><img src="docs/screenshots/carichi-hf.png" alt="HF 부하: 트랩과 코일"></a><br><sub>HF 부하: 트랩과 코일</sub></td><td width="50%" align="center"><a href="docs/screenshots/smith.png"><img src="docs/screenshots/smith.png" alt="스미스 차트"></a><br><sub>스미스 차트</sub></td></tr></table>

## 1.5.1의 새 기능

- **최적화기**: "+ 변수 추가"로 모델에서 변수를 만듭니다(각 소자의 길이 또는 둘레, 위치, 반지름, 루프 포함; 안테나 높이). 황토색 미리보기.
- **변수**: 오른쪽 3D 구조에 선택한 변수가 작용하는 곳을 황토색으로 표시.
- 수정 사항과 영어 번역 완료.

## 1.5의 새 기능

- **계산기**: 기계 계산이 계산기로 이동; 짧은 다이폴의 리액턴스, 주파수에 따른 폴디드 다이폴, 여러 턴과 용량 결합의 마그네틱 루프 (Antenna Engineering Handbook 4판). **HF 스택 간격**을 별도 항목으로.
- **AutoEZ, MultiNEC, EZNEC 모델**: "mark out" 급전원, 가상 와이어, 변압기, 계산 셀; 저장된 .nec는 전류원을 계산된 전압으로 기록합니다.

## 1.4.2의 새 기능

- **계산기**: NVIS(최적 높이와 지면 스크린, Cebik 기준), 헤어핀(베타) 정합, Cebik 방법과 VHF에서 NEC 공진 탐색을 쓰는 Quad, Slim Jim이 추가된 J-pole.
- **최적화**: 제작 공차를 반영한 몬테카를로. **스택**: 두 주파수에서 간격 평가.
- **설정**: 업데이트 강제, 문제 신고.

## 1.4의 새 기능

- **기술 문서**: 사용 설명서, 새 계산 및 출처 설명서, 번호가 매겨진 인용 문헌(각각 목차와 PDF, 세 문서 통합 검색, 문서는 이탈리아어).
- **스택 간격**: 액티브 SWR과 실현 이득; 액티브 SWR이 높은 간격은 "선택하지 말 것"으로 표시.
- **종료**: 감사 인사가 있는 확인 창.

## 1.3의 새 기능

- **수신 안테나**: 계산기에서 **낮은 다이폴**, **K9AY**("F/B 최적화" 종단 저항), **4-square RX**, 그리고 수직 2·3·8개의 **Hi-Z** 어레이가 활성화되었습니다. **행 사이 위상**과 "위상 최적화 (RDF)" 버튼이 있습니다. 각 항목은 NEC 모델을 만들고 RDF, DMF, F/B와 순위를 보여 줍니다. 노란 경고는 실제 지면에 연결된 선에서 NEC-2의 한계를 알려 줍니다.
- **수신 (RDF)**: 원형 8개 수직은 220도 위상을 쓰며 로브가 앞쪽을 향합니다.
- **동영상 강좌**: 22강(실제 지형), 23강(수신 안테나), 24강(VHF 도구), 25강(기계 계산과 HF 부하)을 이탈리아어와 영어 음성으로 제공합니다.
- **언어**: 아직 번역되지 않은 문장은 이탈리아어 대신 영어로 표시됩니다.
- **수정**: HF 부하에서 "로딩 코일"로 바꿔도 더 이상 닫히지 않습니다. 필드에 커서가 있을 때 다른 항목을 클릭해도 프로그램이 닫히지 않습니다. 수신 안테나 경고가 더 이상 쌓이지 않습니다. 넓은 창이 더 이상 오른쪽으로 넘치지 않습니다.
- 모든 내용은 설명서 24장에 출처와 함께 있습니다(K9AY, Hi-Z Antennas, IV3PRK, ON4UN, W7EL).

## 1.2의 새 기능

- **실제 지형** (N6BV의 HFTA와 같은 방식): 실제 지형 단면 위의 앙각 패턴. DEM **TINITALY** (이탈리아, 10 m) 또는 **SRTM1** (전 세계), HorizonProfiler 파일, 또는 평지에서. 구간별 반사를 프레넬 영역으로 가중하고 능선 회절, 바다, 지구 곡률을 반영. 로케이터 (최대 12자)에서 교신 방향, 항공 사진·OpenStreetMap·로케이터 격자가 있는 **지도**. VHF에서는 전체 보기와 0-15도 확대.
- **수신 (RDF)**: Beverage, BOG, K9AY, Flag, EWE, 4-square, 원형 8 버티컬의 RDF, DMF, 평균 F/R, 안테나 잡음 (ITU-R P.372), 수신기 여유.
- **VNA 측정**: .s1p와 CSV를 계산 위에 표시. 케이블 제거 (디임베딩), 스미스 차트, 보정 제안.
- **제작 공차**: 반복 가능한 몬테카를로, 엘리먼트별 민감도, 허용 공차가 있는 절단 목록.
- **기계 계산** (새 탭): NTC 2018, CNR-DT 207, EN 1991-1-4에 따른 **바람** (로케이터로 지역 결정, 횡류)과 마스트의 **균형**. 힘은 N과 kgf.
- 1.8 ~ 1296 MHz **케이블과 발룬** (실드가 NEC 와이어가 됨), 스택의 **H 프레임과 마스트**, 1:1 도면이 있는 λ/4와 Wilkinson **분배기**, **교차 야기** (RHCP, LHCP, X-pol), **HF 부하** (W8NX 동축 트랩, 코일, 리니어 로딩), 최적화기에서도 쓰는 **평균 F/R**, 자동 **AGT 테스트**.
- 모든 내용은 매뉴얼 23장에서 출처와 함께 설명. 새 항목은 37개 언어로 번역.

## 1.1의 새 기능

> **저자의 말.** 이번 버전에서는 잡음 온도에 대해 오래 고민했습니다. 지향성 안테나의 스택에 관한 MATLAB 연구를 소개받았고, 바로 시도해 보고 싶었습니다. YA-NEC 사용자가 DL6WU의 스택 간격(삼각함수 공식)을 넘어서, 원한다면 자신만의 스택 간격을 고를 수 있도록 돕는 것입니다. 이와 함께 사용자 인터페이스를 크게 손보았습니다. — *Costantino IC8TEM*

- **실제 하늘의 잡음.** 상위 탭 **잡음/GT/스택**의 새 페이지: 안테나나 스택의 패턴을 실제 하늘 지도(Haslam의 408 MHz 탐사를 Remazeilles가 재처리하고 G8ONH가 144·432 MHz로 환산) 위에 겹칩니다. 자신의 QTH(로케이터 또는 좌표)에서, 고른 시각에, 원하는 방향이나 달, 가장 차가운 하늘로 향할 수 있습니다. G/T 두 개를 나란히 보여 줍니다: 균일한 하늘(VE7BQH 표와 비교 가능, 앙각 30도, DL6WU 간격 2 x 2 스택)과 실제 하늘(24시간 안테나 온도 곡선과 최소·최대). 계산은 **John C. Sager G8ONH의 skynoise**(MIT 라이선스)를 C#으로 옮긴 것입니다.
- **NEC로 고르는 스택 간격.** **스택 간격** 페이지(계산기에서 옮겨 옴)에서 DL6WU 간격 주변의 여러 간격에 대해 스택을 실제로 계산합니다: 이득, 최대 사이드로브, G/T, 포트 간 결합 행렬(S11, S21...). 기준(최대 이득, 최저 로브, 최적 G/T, 로브가 임계값 아래일 때의 이득)으로 제안 간격을 고르고, 버튼으로 그 간격의 스택을 만듭니다. **HF**에서는 DL6WU 공식이 맞지 않습니다: 지상 스택은 원하는 발사각으로 고르며, 동상(BIP), 단독, 역상(BOP)의 앙각 패턴을 보여 줍니다.
- **계열별 계산기.** 탭 아래 막대가 계산기를 **HF**, **VHF**, **수신 안테나**(이제 별도 탭이 아님), **도구**, **파라볼라와 급전기**, **2,4 GHz**로 나누고, 왼쪽 목록은 고른 계열만 보여 줍니다.
- **패턴 측정.** -3 dB 빔폭에 더해 첫 널 빔폭(FNBW), 최대 사이드로브(SLL), 클릭으로 놓는 커서 C1·C2(레벨, 각도, 둘의 차이).
- **축 방향 F/B** 수정: 빔이 Z축을 향하는 자유 공간 모델(MathWorks Antenna Toolbox의 야기 등).
- **새 계산기**: 바이쿼드와 더블 바이쿼드(파라볼라 급전기), Big Wheel과 다이폴 3개, 수평 멀티밴드 루프(Skywire, 대역별 급전선 손실 포함).
- **번역**: 새 항목 모두 37개 언어로. **비디오 강좌**에 새 주제의 강의를 추가.

## 다운로드

**[Releases](https://github.com/ic8tem/YA-NEC/releases)** 페이지에서 사용하는 플랫폼의 zip을 내려받아 **쓰기 가능한 폴더**(예: `C:\YA-NEC`, `~/YA-NEC`)에 압축을 풀고 `YA-NEC.exe`(Linux와 macOS는 `YA-NEC`)를 실행합니다. .NET을 설치할 필요가 없습니다. 설정과 데이터는 프로그램 옆 `data` 폴더에 저장되며 AppData나 레지스트리는 사용하지 않습니다.

| 시스템 | 패키지 |
|---|---|
| Windows 10/11 64비트 | `YA-NEC-<버전>-windows-x64-portatile.zip` |
| Windows 10/11 32비트 | `YA-NEC-<버전>-windows-x86-portatile.zip` |
| ARM용 Windows(Snapdragon) | `YA-NEC-<버전>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1(.NET 6) | `YA-NEC-<버전>-windows7-x64-portatile.zip`, `-windows7-x86-portatile.zip`(업데이트 KB2533623, KB2999226 필요) |
| Linux x86_64 | `YA-NEC-<버전>-linux-x64.tar.gz` |
| Linux ARM64(Raspberry Pi OS 64비트, 기타 aarch64) | `YA-NEC-<버전>-linux-arm64.tar.gz` |
| Linux ARM 32비트(Raspberry Pi OS 32비트, armv7l) | `YA-NEC-<버전>-linux-armv7l.tar.gz` |
| Alpine 등 musl 기반 Linux | `YA-NEC-<버전>-alpine-x64.tar.gz`, `-alpine-arm64.tar.gz` |
| macOS Apple Silicon(M1~M4) | `YA-NEC-<버전>-mac-apple-silicon-app.zip` |
| macOS Intel | `YA-NEC-<버전>-mac-intel-app.zip` |

각 zip에는 `.sha256` 체크섬이 함께 있습니다.

- **Linux**: 압축을 푼 뒤 필요하면 `chmod +x YA-NEC`. `libfontconfig1`과 `libicu`가 필요합니다(데스크톱 배포판에는 보통 설치되어 있음).
- **macOS**: Apple 서명이 없습니다. 압축을 푼 뒤 터미널에서 `xattr -dr com.apple.quarantine ~/YA-NEC`(압축을 푼 폴더)를 실행하고 `~/YA-NEC/YA-NEC`로 시작합니다.

## 자동 업데이트

프로그램은 30분마다 이곳에 새 버전이 있는지 조용히 확인합니다. 새 버전이 있으면 해당 플랫폼의 zip을 내려받고 SHA-256 체크섬을 확인한 뒤 설치합니다. 새 버전은 **다음 실행 시** 적용되며 상태 표시줄에 메시지가 나타납니다. 이전 버전은 `data/aggiornamenti/precedente-<버전>`에 남고, 설정(`data/config.ini`)과 사용자 모델은 건드리지 않습니다. 체크섬이 없거나 다르면 아무것도 설치하지 않습니다. **설정 > 업데이트**에서 끌 수 있습니다.

**예외: Debian/Ubuntu(.deb) 및 Fedora/openSUSE(.rpm) 패키지.** 제공될 경우 관리자 권한으로 시스템 폴더에 설치되므로 프로그램이 스스로를 바꿀 수 없고 바꿔서도 안 됩니다. 패키지 관리자로 업데이트하세요. 이런 패키지에서는 자동 업데이트가 꺼져 있으며 설정 화면에도 그렇게 표시됩니다.

## 소스에서 빌드하기

.NET 8 SDK가 필요합니다. Windows: `compila.bat`(프로그램은 `build\win-x64`), 단독 실행판은 `compila.bat /distribuzione`, Windows 7용은 `compila_win7.bat`. Linux와 macOS: `./compila.sh`. 자세한 내용은 [LEGGIMI.txt](LEGGIMI.txt)(이탈리아어)에 있습니다.

## 크레디트와 라이선스

**GPL-3.0**([LICENSE](LICENSE), [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt)). 엔진의 바탕이 된 nec2c, xnec2c와 같습니다. YA-NEC는 아마추어 무선사와 연구자들의 작업 덕분에 존재합니다. 전체 목록은 [CREDITI.txt](CREDITI.txt)와 프로그램의 **크레디트** 탭에 있습니다. 웹사이트에 자료를 무료로 공개해 준 Luis IV3PRK에게 감사드립니다.

## 연락처

ic8tem@gmail.com · [QRZ](https://www.qrz.com/db/ic8tem) · [Facebook 그룹](https://www.facebook.com/groups/1675310160982493) · 후원: [ko-fi.com/ic8tem](https://ko-fi.com/ic8tem)
