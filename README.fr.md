[Italiano](README.md) · [English](README.en.md) · **Français** · [Deutsch](README.de.md) · [Español](README.es.md) · [Português](README.pt.md) · [日本語](README.ja.md) · [한국어](README.ko.md)

# YA-NEC

**Simulateur d'antennes filaires basé sur NEC-2**, pour Windows, Linux et macOS (C#, .NET 8, Avalonia).
Par Costantino Cerrotta **IC8TEM**.

> *L'histoire de ce programme, c'est vous qui l'écrirez : avec vos projets, vos idées, votre génie, vos découvertes et vos certitudes.*

Le moteur est une traduction en C# de **nec2c** (le NEC-2 du Lawrence Livermore National Laboratory), comparée à chaque version avec nec2c et EZNEC. Il ouvre les modèles **NEC (.nec)**, **MMANA (.maa)**, **AutoEZ (.weq)**, **EZNEC (.ez)**, **YO/AO de K6STI (.yag, .ant)** et son propre format **.yanec**.

Fonctions : impédance, ROS, gain, diagrammes 2D et 3D, balayage en fréquence et abaque de Smith ; correction de Leeson des tubes télescopiques, comme EZNEC ; sols (parfait, coefficient de réflexion, Sommerfeld, radians) ; variables, formules et optimiseur ; plus de 40 calculateurs (Yagi longue DL6WU, Quagi, LPDA, Moxon, hexbeam, dipôles et Yagi multibandes, four-square, verticales, paraboles et sources, fixations des éléments, etc.) ; antennes de réception et RDF ; bruit, G/T et couplage (stack) ; abaque des câbles, lignes d'adaptation et baluns ; contrôle des données saisies. Chaque choix de calcul et chaque case à cocher sont expliqués dans le [manuel](doc/Manuale_d_uso.pdf), également disponible dans le programme (onglet Manuel). Plus d'informations dans le [Wiki](https://github.com/ic8tem/YA-NEC/wiki).

**Langue :** l'interface est en italien, avec une partie en anglais (menu Langue, en haut à droite). Le manuel est en italien. D'autres langues suivront.

## Téléchargement

Depuis la page **[Releases](https://github.com/ic8tem/YA-NEC/releases)**, téléchargez le zip de votre plateforme, extrayez-le dans un **dossier accessible en écriture** (par exemple `C:\YA-NEC` ou `~/YA-NEC`) et lancez `YA-NEC.exe` (Linux et macOS : `YA-NEC`). Aucune installation de .NET n'est nécessaire. Les réglages et les données restent dans le dossier `data` à côté du programme : pas d'AppData, pas de registre.

| Système | Paquet |
|---|---|
| Windows 10/11 64 bits | `YA-NEC-<version>-win-x64.zip` |
| Windows 10/11 32 bits | `YA-NEC-<version>-win-x86.zip` |
| Windows sur ARM (Snapdragon) | `YA-NEC-<version>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1 (.NET 6) | `YA-NEC-<version>-win7-x64.zip`, `-win7-x86.zip` (mises à jour KB2533623 et KB2999226 requises) |
| Linux x86_64 | `YA-NEC-<version>-linux-x64.zip` |
| Linux ARM64 (Raspberry Pi OS 64 bits, autres aarch64) | `YA-NEC-<version>-linux-arm64.zip` |
| Linux ARM 32 bits (Raspberry Pi OS 32 bits, armv7l) | `YA-NEC-<version>-linux-arm.zip` |
| Alpine et autres Linux musl | `YA-NEC-<version>-linux-musl-x64.zip`, `-linux-musl-arm64.zip` |
| macOS Apple Silicon (M1-M4) | `YA-NEC-<version>-osx-arm64.zip` |
| macOS Intel | `YA-NEC-<version>-osx-x64.zip` |

Chaque zip est accompagné de son empreinte `.sha256`.

- **Linux** : après extraction, `chmod +x YA-NEC` si nécessaire ; `libfontconfig1` et `libicu` sont requis (déjà présents sur les distributions de bureau).
- **macOS** : le programme n'est pas signé par Apple. Après extraction, dans le Terminal : `xattr -dr com.apple.quarantine ~/YA-NEC` (le dossier extrait), puis `~/YA-NEC/YA-NEC`.

## Mise à jour automatique

Toutes les 30 minutes, le programme vérifie discrètement s'il existe ici une nouvelle version. Si c'est le cas, il télécharge le zip de sa plateforme, vérifie l'empreinte SHA-256 et l'installe : la nouvelle version démarre **au prochain lancement**, avec un message dans la barre d'état. La version précédente est conservée dans `data/aggiornamenti/precedente-<version>` ; les réglages (`data/config.ini`) et les modèles de l'utilisateur ne sont pas touchés. Sans empreinte, ou avec une empreinte différente, rien n'est installé. La mise à jour se désactive dans **Réglages > Mise à jour**.

**Exception : paquets Debian/Ubuntu (.deb) et Fedora/openSUSE (.rpm).** Quand ils existeront, ils s'installeront dans les dossiers système avec les droits d'administrateur ; le programme ne peut donc pas (et ne doit pas) se modifier lui-même : ils se mettront à jour avec le gestionnaire de paquets. Dans ces paquets, la mise à jour automatique est désactivée et les Réglages l'indiquent.

## Compiler depuis les sources

SDK .NET 8 requis. Windows : `compila.bat` (programme dans `build\win-x64`), `compila.bat /distribuzione` pour la version autonome, `compila_win7.bat` pour Windows 7. Linux et macOS : `./compila.sh`. Détails dans [LEGGIMI.txt](LEGGIMI.txt) (en italien).

## Crédits et licence

**GPL-3.0** ([LICENSE](LICENSE), [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt)), comme nec2c et xnec2c dont dérive le moteur. YA-NEC existe grâce au travail de radioamateurs et de chercheurs : la liste complète est dans [CREDITI.txt](CREDITI.txt) et dans l'onglet **Crédits**. Merci à Luis IV3PRK pour les publications gratuites sur son site.

## Contacts

ic8tem@gmail.com · [QRZ](https://www.qrz.com/db/ic8tem) · [groupe Facebook](https://www.facebook.com/groups/1675310160982493) · dons : [ko-fi.com/ic8tem](https://ko-fi.com/ic8tem)
