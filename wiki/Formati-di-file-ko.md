# 파일 형식

| 형식 | 열기 | 저장 | 비고 |
|---|---|---|---|
| NEC-2 (.nec) | 예 | 예 | GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY 카드(변수와 수식은 4nec2, xnec2c와 같음) |
| MMANA (.maa) | 예 | 예 | 세그먼트 분할을 NEC-2용으로 다시 계산(MMANA는 MININEC 엔진) |
| AutoEZ (.weq) | 예 | 예 | 변수와 Excel 수식 |
| EZNEC (.ez) | 예 | 예 | 와이어, 급전원, 부하, 선로, 대지. MININEC 대지는 경고와 함께 GN 3으로 읽음 |
| K6STI의 YO / AO (.yag, .ant) | 예 | 아니요 | |
| YA-NEC (.yanec) | 예 | 예 | 원래 형식의 모델에 이름, 설명, 분류를 붙인 것 |

.yanec 파일은 모델을 **변환 없이** 담습니다(NEC 카드는 그대로, .maa, .weq, .yag, .ant는 텍스트, .ez는 base64). YA-NEC는 원본 파일과 같은 가져오기 기능으로 읽기 때문에 결과가 원본 파일과 똑같습니다.

자세한 내용은 매뉴얼 10장에 있습니다.
