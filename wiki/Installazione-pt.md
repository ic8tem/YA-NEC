# Instalação

O YA-NEC não se instala: extrai-se e executa-se. Não é preciso .NET, já vem dentro do pacote. Definições, modelos e registos ficam na pasta do programa (`data`, `Registri`): nada no AppData nem no registo. Para o remover, basta apagar a pasta.

## Que pacote

Na página [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Sistema | Pacote |
|---|---|
| Windows 10/11 64 bits | `YA-NEC-<versão>-windows-x64-portatile.zip` |
| Windows 10/11 32 bits | `YA-NEC-<versão>-windows-x86-portatile.zip` |
| Windows em ARM (Snapdragon) | `YA-NEC-<versão>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<versão>-windows7-x64-portatile.zip` ou `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<versão>-linux-x64.tar.gz` |
| Raspberry Pi OS 64 bits e outros Linux aarch64 | `YA-NEC-<versão>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32 bits, armv7l | `YA-NEC-<versão>-linux-armv7l.tar.gz` |
| Alpine e Linux com musl | `YA-NEC-<versão>-alpine-x64.tar.gz` ou `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<versão>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<versão>-mac-intel-app.zip` |

Ao lado de cada zip está a impressão digital `.sha256` para verificar a transferência.

## Windows

Extrair o zip para uma pasta com permissão de escrita, por exemplo `C:\YA-NEC` (não em `C:\Programas`, onde o programa não conseguiria guardar as definições) e executar `YA-NEC.exe`. No primeiro arranque o Windows SmartScreen pode pedir confirmação: «Mais informações» > «Executar mesmo assim».

**Windows 7 SP1, 8 e 8.1**: usar os pacotes `win7`, compilados com .NET 6 (o .NET 8 não arranca no Windows 7). O Windows 7 precisa do Service Pack 1 e das atualizações KB2533623 e KB2999226.

## Linux e Raspberry Pi

```
tar -xzf YA-NEC-<versão>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

São precisos `libfontconfig1` e `libicu`, já presentes nas distribuições com ambiente gráfico. No Alpine: `apk add icu-libs fontconfig libstdc++`. No Raspberry Pi o cálculo funciona, mas os modelos grandes (milhares de segmentos) exigem tempo e memória.

## Mac

Extrair o zip, por exemplo para `~/YA-NEC`. O programa não está assinado pela Apple, por isso é preciso retirar a quarentena uma vez, no Terminal:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Ecrãs pequenos

Em ecrãs baixos (por exemplo 1366 x 768) a janela abre maximizada e os quadros de resultados passam ao formato compacto.
