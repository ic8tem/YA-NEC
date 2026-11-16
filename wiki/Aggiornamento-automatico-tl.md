# Awtomatikong update

Tuwing 30 minuto (at 2 minuto pagkatapos magsimula) tahimik na sinusuri ng YA-NEC ang pinakabagong bersyong inilathala sa GitHub na ito. Kung mas bago:

1. dina-download nito ang zip para sa platform nito at ang checksum na `.sha256`;
2. sinusuri nito ang checksum: kung wala o iba, walang ini-install;
3. pinapalitan nito ang lumang file ng bago: tatakbo ang bagong bersyon **sa susunod na pagbukas**, na may mensahe sa status bar.

Nananatili ang dating bersyon sa `data/aggiornamenti/precedente-<bersyon>`, para makabalik nang mano-mano. Hindi ginagalaw ang mga setting (`data/config.ini`), log at model ng user. Kung maraming window ng YA-NEC ang bukas, ang una lang ang sumusuri.

Pinapatay ito sa **Mga setting > Update**; hindi naghihintay ng 30 minuto ang button na **Suriin ngayon**. Itinatala sa log ang bawat hakbang (kategoryang AGGIORNAMENTO).

**Mga package na .deb at .rpm**, kapag mayroon na: ini-install gamit ang karapatan ng administrator at ina-update lang sa package manager (`apt`, `dnf`, `zypper`). Sa mga package na iyon, patay ang awtomatikong update at sinasabi ito ng Mga setting.
