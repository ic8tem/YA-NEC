# Instalação

O YA-NEC não se instala: extrai-se e executa-se. Não é preciso .NET, já vem dentro do pacote. Configurações, modelos e registros ficam na pasta do programa (`data`, `Registri`): nada no AppData nem no registro. Para o remover, basta apagar a pasta.

## Que pacote

Na página [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Sistema | Pacote |
|---|---|
| Windows 10/11 64 bits | `YA-NEC-<versão>-win-x64.zip` |
| Windows 10/11 32 bits | `YA-NEC-<versão>-win-x86.zip` |
| Windows em ARM (Snapdragon) | `YA-NEC-<versão>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<versão>-win7-x64.zip` ou `-win7-x86.zip` |
| Linux x86_64 | `YA-NEC-<versão>-linux-x64.zip` |
| Raspberry Pi OS 64 bits e outros Linux aarch64 | `YA-NEC-<versão>-linux-arm64.zip` |
| Raspberry Pi OS 32 bits, armv7l | `YA-NEC-<versão>-linux-arm.zip` |
| Alpine e Linux com musl | `YA-NEC-<versão>-linux-musl-x64.zip` ou `-linux-musl-arm64.zip` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<versão>-osx-arm64.zip` |
| Mac Intel | `YA-NEC-<versão>-osx-x64.zip` |

Ao lado de cada zip está a impressão digital `.sha256` para verificar a download.

## Windows

Extrair o zip para uma pasta com permissão de escrita, por exemplo `C:\YA-NEC` (não em `C:\Programas`, onde o programa não conseguiria salvar as configurações) e executar `YA-NEC.exe`. No primeiro inicialização o Windows SmartScreen pode pedir confirmação: «Mais informações» > «Executar mesmo assim».

**Windows 7 SP1, 8 e 8.1**: usar os pacotes `win7`, compilados com .NET 6 (o .NET 8 não inicia no Windows 7). O Windows 7 precisa do Service Pack 1 e das atualizações KB2533623 e KB2999226.

## Linux e Raspberry Pi

```
unzip YA-NEC-<versão>-linux-x64.zip -d ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

São precisos `libfontconfig1` e `libicu`, já presentes nas distribuições com ambiente gráfico. No Alpine: `apk add icu-libs fontconfig libstdc++`. No Raspberry Pi o cálculo funciona, mas os modelos grandes (milhares de segmentos) exigem tempo e memória.

## Mac

Extrair o zip, por exemplo para `~/YA-NEC`. O programa não está assinado pela Apple, por isso é preciso retirar a quarentena uma vez, no Terminal:

```
xattr -dr com.apple.quarantine ~/YA-NEC
~/YA-NEC/YA-NEC
```

## Telas pequenos

Em telas baixos (por exemplo 1366 x 768) a janela abre maximizada e os quadros de resultados passam ao formato compacto.
