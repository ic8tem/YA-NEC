[Italiano](README.md) · [English](README.en.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Español](README.es.md) · **Português** · [日本語](README.ja.md) · [한국어](README.ko.md)

# YA-NEC

**Simulador de antenas de fio baseado no NEC-2** para Windows, Linux e macOS (C#, .NET 8, Avalonia).
Por Costantino Cerrotta **IC8TEM**.

> *A história deste programa serão vocês a escrevê-la: com os vossos projetos, as vossas ideias, a vossa genialidade, as vossas descobertas e as vossas certezas.*

O motor é uma tradução em C# do **nec2c** (o NEC-2 do Lawrence Livermore National Laboratory), comparada em cada versão com o nec2c e o EZNEC. Abre modelos **NEC (.nec)**, **MMANA (.maa)**, **AutoEZ (.weq)**, **EZNEC (.ez)**, **YO/AO de K6STI (.yag, .ant)** e o formato próprio **.yanec**.

Funções: impedância, ROE, ganho, diagramas 2D e 3D, varrimento em frequência e carta de Smith; correção de Leeson para tubos telescópicos, como no EZNEC; solos (perfeito, coeficiente de reflexão, Sommerfeld, radiais); variáveis, fórmulas e otimizador; mais de 40 calculadoras (Yagi longa DL6WU, Quagi, LPDA, Moxon, hexbeam, dipolos e Yagis multibanda, four-square, verticais, parábolas e iluminadores, suportes dos elementos e mais); antenas de receção e RDF; ruído, G/T e agrupamento (stack); tabela de cabos, linhas de adaptação e baluns; verificação dos dados introduzidos. Cada escolha de cálculo e cada caixa de seleção são explicadas no [manual](doc/Manuale_d_uso.pdf), também disponível no programa (separador Manual). Mais informação no [Wiki](https://github.com/ic8tem/YA-NEC/wiki).

**Idioma:** a interface está em italiano, com parte em inglês (menu Idioma, em cima à direita). O manual está em italiano. Outros idiomas virão.

## Transferir

Na página **[Releases](https://github.com/ic8tem/YA-NEC/releases)** transfere-se o zip da sua plataforma, extrai-se para uma **pasta com permissão de escrita** (por exemplo `C:\YA-NEC` ou `~/YA-NEC`) e executa-se `YA-NEC.exe` (Linux e macOS: `YA-NEC`). Não é preciso instalar o .NET. As definições e os dados ficam na pasta `data` ao lado do programa: nada no AppData nem no registo.

| Sistema | Pacote |
|---|---|
| Windows 10/11 64 bits | `YA-NEC-<versão>-win-x64.zip` |
| Windows 10/11 32 bits | `YA-NEC-<versão>-win-x86.zip` |
| Windows em ARM (Snapdragon) | `YA-NEC-<versão>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1 (.NET 6) | `YA-NEC-<versão>-win7-x64.zip`, `-win7-x86.zip` (requer as atualizações KB2533623 e KB2999226) |
| Linux x86_64 | `YA-NEC-<versão>-linux-x64.zip` |
| Linux ARM64 (Raspberry Pi OS 64 bits, outros aarch64) | `YA-NEC-<versão>-linux-arm64.zip` |
| Linux ARM 32 bits (Raspberry Pi OS 32 bits, armv7l) | `YA-NEC-<versão>-linux-arm.zip` |
| Alpine e outros Linux com musl | `YA-NEC-<versão>-linux-musl-x64.zip`, `-linux-musl-arm64.zip` |
| macOS Apple Silicon (M1-M4) | `YA-NEC-<versão>-osx-arm64.zip` |
| macOS Intel | `YA-NEC-<versão>-osx-x64.zip` |

Cada zip vem acompanhado da sua impressão digital `.sha256`.

- **Linux**: depois de extrair, `chmod +x YA-NEC` se necessário; são precisos `libfontconfig1` e `libicu` (já presentes nas distribuições com ambiente gráfico).
- **macOS**: o programa não está assinado pela Apple. Depois de extrair, no Terminal: `xattr -dr com.apple.quarantine ~/YA-NEC` (a pasta extraída) e depois `~/YA-NEC/YA-NEC`.

## Atualização automática

A cada 30 minutos o programa verifica em silêncio se há aqui uma versão nova. Se houver, transfere o zip da sua plataforma, verifica a impressão digital SHA-256 e instala-o: a nova versão arranca **no próximo arranque**, com uma mensagem na barra de estado. A versão anterior fica em `data/aggiornamenti/precedente-<versão>`; as definições (`data/config.ini`) e os modelos do utilizador não são tocados. Sem impressão digital, ou com uma diferente, nada é instalado. A atualização desliga-se em **Definições > Atualização**.

**Exceção: pacotes Debian/Ubuntu (.deb) e Fedora/openSUSE (.rpm).** Quando existirem, instalar-se-ão em pastas do sistema com permissões de administrador, pelo que o programa não pode (nem deve) alterar-se sozinho: serão atualizados com o gestor de pacotes. Nesses pacotes a atualização automática está desligada e as Definições indicam-no.

## Compilar a partir do código-fonte

É necessário o SDK do .NET 8. Windows: `compila.bat` (programa em `build\win-x64`), `compila.bat /distribuzione` para a versão autónoma, `compila_win7.bat` para o Windows 7. Linux e macOS: `./compila.sh`. Pormenores em [LEGGIMI.txt](LEGGIMI.txt) (em italiano).

## Créditos e licença

**GPL-3.0** ([COPYING](COPYING)), como o nec2c e o xnec2c, dos quais deriva o motor. O YA-NEC existe graças ao trabalho de radioamadores e investigadores: a lista completa está em [CREDITI.txt](CREDITI.txt) e no separador **Créditos**. Obrigado a Luis IV3PRK pelas publicações gratuitas no seu site.

## Contactos

ic8tem@gmail.com · [QRZ](https://www.qrz.com/db/ic8tem) · [grupo do Facebook](https://www.facebook.com/groups/1675310160982493) · donativos: [ko-fi.com/ic8tem](https://ko-fi.com/ic8tem)
