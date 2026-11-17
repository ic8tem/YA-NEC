[Italiano](Novita-1.1) · [English](Novita-1.1-en) · [Deutsch](Novita-1.1-de) · [Nederlands](Novita-1.1-nl) · [Français](Novita-1.1-fr) · [Español](Novita-1.1-es) · [Português](Novita-1.1-pt) · [Português (Brasil)](Novita-1.1-pt-br) · [Dansk](Novita-1.1-da) · [Norsk](Novita-1.1-nb) · [Svenska](Novita-1.1-sv) · [Íslenska](Novita-1.1-is) · [Eesti](Novita-1.1-et) · [Lietuvių](Novita-1.1-lt) · [Latviešu](Novita-1.1-lv) · [Polski](Novita-1.1-pl) · [Čeština](Novita-1.1-cs) · [Slovenčina](Novita-1.1-sk) · [Magyar](Novita-1.1-hu) · [Română](Novita-1.1-ro) · [Ελληνικά](Novita-1.1-el) · [Русский](Novita-1.1-ru) · [Українська](Novita-1.1-uk) · [Türkçe](Novita-1.1-tr) · [Afrikaans](Novita-1.1-af) · [العربية](Novita-1.1-ar) · [עברית](Novita-1.1-he) · [اردو](Novita-1.1-ur) · [हिन्दी](Novita-1.1-hi) · [বাংলা](Novita-1.1-bn) · [ไทย](Novita-1.1-th) · [Bahasa Indonesia](Novita-1.1-id) · [Tagalog](Novita-1.1-tl) · [日本語](Novita-1.1-ja) · **한국어** · [中文](Novita-1.1-zh) · [中文 (繁體)](Novita-1.1-zh-tw)

# 1.1의 새 기능

> **저자의 말.** 이번 버전에서는 잡음 온도에 대해 오래 고민했습니다. 지향성 안테나의 스택에 관한 MATLAB 연구를 소개받았고, 바로 시도해 보고 싶었습니다. YA-NEC 사용자가 DL6WU의 스택 간격(삼각함수 공식)을 넘어서, 원한다면 자신만의 스택 간격을 고를 수 있도록 돕는 것입니다. 이와 함께 사용자 인터페이스를 크게 손보았습니다. — *Costantino IC8TEM*

- **실제 하늘의 잡음.** 상위 탭 **잡음/GT/스택**의 새 페이지: 안테나나 스택의 패턴을 실제 하늘 지도(Haslam의 408 MHz 탐사를 Remazeilles가 재처리하고 G8ONH가 144·432 MHz로 환산) 위에 겹칩니다. 자신의 QTH(로케이터 또는 좌표)에서, 고른 시각에, 원하는 방향이나 달, 가장 차가운 하늘로 향할 수 있습니다. G/T 두 개를 나란히 보여 줍니다: 균일한 하늘(VE7BQH 표와 비교 가능, 앙각 30도, DL6WU 간격 2 x 2 스택)과 실제 하늘(24시간 안테나 온도 곡선과 최소·최대). 계산은 **John C. Sager G8ONH의 skynoise**(MIT 라이선스)를 C#으로 옮긴 것입니다.
- **NEC로 고르는 스택 간격.** **스택 간격** 페이지(계산기에서 옮겨 옴)에서 DL6WU 간격 주변의 여러 간격에 대해 스택을 실제로 계산합니다: 이득, 최대 사이드로브, G/T, 포트 간 결합 행렬(S11, S21...). 기준(최대 이득, 최저 로브, 최적 G/T, 로브가 임계값 아래일 때의 이득)으로 제안 간격을 고르고, 버튼으로 그 간격의 스택을 만듭니다. **HF**에서는 DL6WU 공식이 맞지 않습니다: 지상 스택은 원하는 발사각으로 고르며, 동상(BIP), 단독, 역상(BOP)의 앙각 패턴을 보여 줍니다.
- **계열별 계산기.** 탭 아래 막대가 계산기를 **HF**, **VHF**, **수신 안테나**(이제 별도 탭이 아님), **도구**, **파라볼라와 급전기**, **2,4 GHz**로 나누고, 왼쪽 목록은 고른 계열만 보여 줍니다.
- **패턴 측정.** -3 dB 빔폭에 더해 첫 널 빔폭(FNBW), 최대 사이드로브(SLL), 클릭으로 놓는 커서 C1·C2(레벨, 각도, 둘의 차이).
- **축 방향 F/B** 수정: 빔이 Z축을 향하는 자유 공간 모델(MathWorks Antenna Toolbox의 야기 등).
- **새 계산기**: 바이쿼드와 더블 바이쿼드(파라볼라 급전기), Big Wheel과 다이폴 3개, 수평 멀티밴드 루프(Skywire, 대역별 급전선 손실 포함).
- **번역**: 새 항목 모두 37개 언어로. **비디오 강좌**에 새 주제의 강의를 추가.

NEC, [doc/Manuale_d_uso.pdf](https://github.com/ic8tem/YA-NEC/blob/main/doc/Manuale_d_uso.pdf) (7, 15, 16)
