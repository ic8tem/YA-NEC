# Unang hakbang

1. Binubuksan ng **Mga halimbawa** (command bar) ang isa sa mga handang model: dipole, ground plane, VHF Yagi (DK7ZB, I4GBZ, DJ9BV BVO, YU7EF), HF Yagi (Force 12 XR-5, ARRL 520-40H), hexbeam, four-square, loop Yagi para sa 1296 MHz at iba pa. Binubuksan ng **Buksan ang model** ang file na NEC, MMANA, AutoEZ, EZNEC o K6STI; puwede ring i-drag ang file sa window.
2. Piliin ang **band** (mula 2200 m hanggang 3 cm, mga hangganan ng IARU Region 1) o anumang frequency, saka **Kalkulahin**.
3. Ipinapakita ng **mga kahon ng resulta** ang impedance, SWR, gain, F/B, F/R, lapad ng beam sa -3 dB, efficiency at AGT (convergence test: malapit sa 0 dB, maaasahan ang model).
4. Kinakalkula ng **Sweep** ang band sa paligid ng frequency: SWR, R/X, gain, F/B at Smith chart.

## Ang mga tab

- **3D na istruktura**: ang antenna (i-drag = paikutin, gulong = zoom, kanang button = ilipat, double click = unang tanaw), at iginuguhit ang lupa ayon sa uri at taas. Sa ilalim nito ang **talahanayan ng wire** na parang spreadsheet: tumatanggap ang mga cell ng numero, variable at formula. Sa kanang ibaba ang **mga object ng model**: source, load, linya, transformer.
- **Mga diagram**: pahalang, patayo at 3D, kasama ang anggulo sa -3 dB.
- **Frequency, Smith, Mga kuryente**: resulta ng sweep at mga kuryente sa wire.
- **NEC model**: ang mga NEC card, na puwedeng i-edit nang mano-mano.
- **Mga variable** at **Optimizer**: pangalanan ang mga sukat at hayaang baguhin ito ng programa hanggang maabot ang layunin (SWR, gain, F/B, impedance...). May mga hakbang-hakbang na halimbawa sa manwal.
- **Mga calculator**: tingnan ang [Mga calculator](Calcolatori-tl).
- **Mga antenna sa pagtanggap** at **Ingay/GT/Stack**: RDF ng mga antenna sa pagtanggap, temperatura ng antenna at G/T (paraan ng TANT at XLGTa, reference na temperatura ng VE7BQH at DG7YBN), pag-stack ng antenna.
- **Mga babala**: lahat ng kahina-hinala na nakita ng programa sa model (masyadong maikli o mahabang segment, wire na malapit sa lupa, siksik na dugtungan). Sinusuri ng button na **Suriin...** sa itaas na bar ang 42 karaniwang mali sa input nang hindi nagkakalkula.
- **Mga setting**, **Manwal**, **Pasasalamat**. Sa kanang itaas: **Wika**, **Mga kulay** (18 scheme) at **Tungkol**.

## Pag-save

Isinusulat ng **I-save** at **I-save bilang** ang model bilang NEC, MMANA, AutoEZ, EZNEC o sa sariling format na **.yanec**, na nag-iingat din ng paglalarawan at ng orihinal na file.
