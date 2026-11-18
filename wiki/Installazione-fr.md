# Installation

YA-NEC ne s'installe pas : on l'extrait et on le lance. Aucun .NET n'est nécessaire, il est déjà dans le paquet. Réglages, modèles et journaux restent dans le dossier du programme (`data`, `Registri`) : pas d'AppData, pas de registre. Pour le supprimer, il suffit d'effacer le dossier.

## Quel paquet

Depuis la page [Releases](https://github.com/ic8tem/YA-NEC/releases) :

| Système | Paquet |
|---|---|
| Windows 10/11 64 bits | `YA-NEC-<version>-windows-x64-portatile.zip` |
| Windows 10/11 32 bits | `YA-NEC-<version>-windows-x86-portatile.zip` |
| Windows sur ARM (Snapdragon) | `YA-NEC-<version>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<version>-windows7-x64-portatile.zip` ou `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<version>-linux-x64.tar.gz` |
| Raspberry Pi OS 64 bits et autres Linux aarch64 | `YA-NEC-<version>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32 bits, armv7l | `YA-NEC-<version>-linux-armv7l.tar.gz` |
| Alpine et Linux musl | `YA-NEC-<version>-alpine-x64.tar.gz` ou `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<version>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<version>-mac-intel-app.zip` |

À côté de chaque zip se trouve l'empreinte `.sha256` pour vérifier le téléchargement.

## Windows

Extraire le zip dans un dossier accessible en écriture, par exemple `C:\YA-NEC` (pas dans `C:\Program Files`, où le programme ne pourrait pas enregistrer ses réglages), puis lancer `YA-NEC.exe`. Au premier démarrage, Windows SmartScreen peut demander une confirmation : « Informations complémentaires » > « Exécuter quand même ».

**Windows 7 SP1, 8 et 8.1** : utiliser les paquets `win7`, compilés avec .NET 6 (.NET 8 ne démarre pas sous Windows 7). Windows 7 nécessite le Service Pack 1 et les mises à jour KB2533623 et KB2999226.

## Linux et Raspberry Pi

```
tar -xzf YA-NEC-<version>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

`libfontconfig1` et `libicu` sont nécessaires ; ils sont déjà présents sur les distributions de bureau. Sous Alpine : `apk add icu-libs fontconfig libstdc++`. Sur le Raspberry Pi le calcul fonctionne, mais les grands modèles (des milliers de segments) demandent du temps et de la mémoire.

## Mac

Extraire le zip, par exemple dans `~/YA-NEC`. Le programme n'est pas signé par Apple : il faut lever la quarantaine une fois, depuis le Terminal :

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Petits écrans

Sur les écrans peu hauts (par exemple 1366 x 768), la fenêtre s'ouvre agrandie et les cadres des résultats passent au format compact.
