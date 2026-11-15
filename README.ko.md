[Italiano](README.md) · [English](README.en.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Português](README.pt.md) · [日本語](README.ja.md) · **한국어**

# YA-NEC

**NEC-2 기반 와이어 안테나 시뮬레이터** (Windows, Linux, macOS 지원. C#, .NET 8, Avalonia).
제작: Costantino Cerrotta **IC8TEM**.

> *이 프로그램의 역사는 여러분이 만들어 갑니다. 여러분의 프로젝트, 아이디어, 재능, 발견, 그리고 확신으로.*

계산 엔진은 **nec2c**(로렌스 리버모어 국립연구소의 NEC-2)를 C#으로 옮긴 것이며, 매 릴리스마다 nec2c 및 EZNEC와 비교합니다. **NEC (.nec)**, **MMANA (.maa)**, **AutoEZ (.weq)**, **EZNEC (.ez)**, **K6STI의 YO/AO (.yag, .ant)** 모델과 자체 **.yanec** 형식을 열 수 있습니다.

주요 기능: 임피던스, SWR, 이득, 2D·3D 방사 패턴, 주파수 스윕과 스미스 차트; 텔레스코픽(단차 직경) 파이프에 대한 Leeson 보정(EZNEC와 동일); 대지(완전 도체, 반사 계수, Sommerfeld, 래디얼); 변수, 수식, 최적화 도구; 40개가 넘는 계산기(DL6WU 롱 야기, 쿼기, LPDA, Moxon, 헥스빔, 멀티밴드 다이폴과 야기, 포스퀘어, 수직 안테나, 파라볼라와 급전부, 엘리먼트 고정 브래킷 등); 수신 안테나와 RDF; 잡음, G/T, 스택; 동축 케이블 표, 정합 선로, 발룬; 입력 데이터 검사. 모든 계산 방식과 체크박스는 [매뉴얼](doc/Manuale_d_uso.pdf)에 설명되어 있으며 프로그램 안(매뉴얼 탭)에서도 볼 수 있습니다. 자세한 내용은 [Wiki](https://github.com/ic8tem/YA-NEC/wiki)를 참고하세요.

**언어:** 화면은 이탈리아어이며 일부는 영어도 지원합니다(오른쪽 위 언어 메뉴). 매뉴얼은 이탈리아어입니다. 다른 언어는 차례로 추가됩니다.

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

**GPL-3.0**([COPYING](COPYING)). 엔진의 바탕이 된 nec2c, xnec2c와 같습니다. YA-NEC는 아마추어 무선사와 연구자들의 작업 덕분에 존재합니다. 전체 목록은 [CREDITI.txt](CREDITI.txt)와 프로그램의 **크레디트** 탭에 있습니다. 웹사이트에 자료를 무료로 공개해 준 Luis IV3PRK에게 감사드립니다.

## 연락처

ic8tem@gmail.com · [QRZ](https://www.qrz.com/db/ic8tem) · [Facebook 그룹](https://www.facebook.com/groups/1675310160982493) · 후원: [ko-fi.com/ic8tem](https://ko-fi.com/ic8tem)
