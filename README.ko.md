[Italiano](README.md) · [English](README.en.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Português](README.pt.md) · [日本語](README.ja.md) · **한국어**

# YA-NEC

**NEC-2 기반 와이어 안테나 시뮬레이터** (Windows, Linux, macOS 지원. C#, .NET 8, Avalonia).
제작: Costantino Cerrotta **IC8TEM**.

> *이 프로그램의 역사는 여러분이 만들어 갑니다. 여러분의 프로젝트, 아이디어, 재능, 발견, 그리고 확신으로.*

계산 엔진은 **nec2c**(로렌스 리버모어 국립연구소의 NEC-2)를 C#으로 옮긴 것이며, 매 릴리스마다 nec2c 및 EZNEC와 비교합니다. **NEC (.nec)**, **MMANA (.maa)**, **AutoEZ (.weq)**, **EZNEC (.ez)**, **K6STI의 YO/AO (.yag, .ant)** 모델과 자체 **.yanec** 형식을 열 수 있습니다.

주요 기능: 임피던스, SWR, 이득, 2D·3D 방사 패턴, 주파수 스윕과 스미스 차트; 텔레스코픽(단차 직경) 파이프에 대한 Leeson 보정(EZNEC와 동일); 대지(완전 도체, 반사 계수, Sommerfeld, 래디얼); 변수, 수식, 최적화 도구; 40개가 넘는 계산기(DL6WU 롱 야기, 쿼기, LPDA, Moxon, 헥스빔, 멀티밴드 다이폴과 야기, 포스퀘어, 수직 안테나, 파라볼라와 급전부, 엘리먼트 고정 브래킷 등); 수신 안테나와 RDF; 잡음, G/T, 스택; 동축 케이블 표, 정합 선로, 발룬; 입력 데이터 검사. 모든 계산 방식과 체크박스는 [매뉴얼](doc/Manuale_d_uso.pdf)에 설명되어 있으며 프로그램 안(매뉴얼 탭)에서도 볼 수 있습니다. 자세한 내용은 [Wiki](https://github.com/ic8tem/YA-NEC/wiki)를 참고하세요.

**언어:** 화면은 37개 언어를 지원합니다(오른쪽 위 언어 메뉴). 위키도 마찬가지입니다. 매뉴얼은 이탈리아어입니다.

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
| Windows 10/11 64비트 | `YA-NEC-<버전>-win-x64.zip` |
| Windows 10/11 32비트 | `YA-NEC-<버전>-win-x86.zip` |
| ARM용 Windows(Snapdragon) | `YA-NEC-<버전>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1(.NET 6) | `YA-NEC-<버전>-win7-x64.zip`, `-win7-x86.zip`(업데이트 KB2533623, KB2999226 필요) |
| Linux x86_64 | `YA-NEC-<버전>-linux-x64.zip` |
| Linux ARM64(Raspberry Pi OS 64비트, 기타 aarch64) | `YA-NEC-<버전>-linux-arm64.zip` |
| Linux ARM 32비트(Raspberry Pi OS 32비트, armv7l) | `YA-NEC-<버전>-linux-arm.zip` |
| Alpine 등 musl 기반 Linux | `YA-NEC-<버전>-linux-musl-x64.zip`, `-linux-musl-arm64.zip` |
| macOS Apple Silicon(M1~M4) | `YA-NEC-<버전>-osx-arm64.zip` |
| macOS Intel | `YA-NEC-<버전>-osx-x64.zip` |

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
