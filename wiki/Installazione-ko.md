# 설치

YA-NEC는 설치하지 않습니다. 압축을 풀고 실행하면 됩니다. .NET은 패키지에 들어 있으므로 필요 없습니다. 설정, 모델, 로그는 프로그램 폴더(`data`, `Registri`)에 저장되며 AppData나 레지스트리는 사용하지 않습니다. 지우려면 폴더를 삭제하면 됩니다.

## 어떤 패키지

[Releases](https://github.com/ic8tem/YA-NEC/releases) 페이지에서:

| 시스템 | 패키지 |
|---|---|
| Windows 10/11 64비트 | `YA-NEC-<버전>-win-x64.zip` |
| Windows 10/11 32비트 | `YA-NEC-<버전>-win-x86.zip` |
| ARM용 Windows(Snapdragon) | `YA-NEC-<버전>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<버전>-win7-x64.zip` 또는 `-win7-x86.zip` |
| Linux x86_64 | `YA-NEC-<버전>-linux-x64.zip` |
| Raspberry Pi OS 64비트 및 기타 aarch64 Linux | `YA-NEC-<버전>-linux-arm64.zip` |
| Raspberry Pi OS 32비트, armv7l | `YA-NEC-<버전>-linux-arm.zip` |
| Alpine 및 musl 기반 Linux | `YA-NEC-<버전>-linux-musl-x64.zip` 또는 `-linux-musl-arm64.zip` |
| Mac Apple Silicon(M1~M4) | `YA-NEC-<버전>-osx-arm64.zip` |
| Mac Intel | `YA-NEC-<버전>-osx-x64.zip` |

각 zip 옆에는 다운로드를 확인하는 `.sha256` 체크섬이 있습니다.

## Windows

zip을 쓰기 가능한 폴더(예: `C:\YA-NEC`)에 풀고 `YA-NEC.exe`를 실행합니다(`C:\Program Files`에서는 설정을 저장할 수 없으니 피하세요). 처음 실행할 때 Windows SmartScreen이 확인을 요청할 수 있습니다: "추가 정보" > "실행".

**Windows 7 SP1, 8, 8.1**: .NET 6으로 빌드한 `win7` 패키지를 사용하세요(.NET 8은 Windows 7에서 실행되지 않습니다). Windows 7에는 서비스 팩 1과 업데이트 KB2533623, KB2999226이 필요합니다.

## Linux와 Raspberry Pi

```
unzip YA-NEC-<버전>-linux-x64.zip -d ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

`libfontconfig1`과 `libicu`가 필요합니다(데스크톱 배포판에는 보통 설치되어 있음). Alpine에서는 `apk add icu-libs fontconfig libstdc++`. Raspberry Pi에서도 계산은 되지만 큰 모델(수천 개 세그먼트)은 시간과 메모리가 필요합니다.

## Mac

zip을 예를 들어 `~/YA-NEC`에 풉니다. Apple 서명이 없으므로 터미널에서 한 번만 격리 속성을 제거합니다:

```
xattr -dr com.apple.quarantine ~/YA-NEC
~/YA-NEC/YA-NEC
```

## 작은 화면

세로가 짧은 화면(예: 1366 x 768)에서는 창이 최대화되어 열리고 결과 칸이 압축 형식으로 바뀝니다.
