[Italiano](README.md) · [English](README.en.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Español](README.es.md) · **Português** · [日本語](README.ja.md) · [한국어](README.ko.md)

# YA-NEC

**Simulador de antenas de fio baseado no NEC-2** para Windows, Linux e macOS (C#, .NET 8, Avalonia).
Por Costantino Cerrotta **IC8TEM**.

> *A história deste programa serão vocês a escrevê-la: com os vossos projetos, as vossas ideias, a vossa genialidade, as vossas descobertas e as vossas certezas.*

O motor é uma tradução em C# do **nec2c** (o NEC-2 do Lawrence Livermore National Laboratory), comparada em cada versão com o nec2c e o EZNEC. Abre modelos **NEC (.nec)**, **MMANA (.maa)**, **AutoEZ (.weq)**, **EZNEC (.ez)**, **YO/AO de K6STI (.yag, .ant)** e o formato próprio **.yanec**.

Funções: impedância, ROE, ganho, diagramas 2D e 3D, varrimento em frequência e carta de Smith; correção de Leeson para tubos telescópicos, como no EZNEC; solos (perfeito, coeficiente de reflexão, Sommerfeld, radiais); variáveis, fórmulas e otimizador; mais de 40 calculadoras (Yagi longa DL6WU, Quagi, LPDA, Moxon, hexbeam, dipolos e Yagis multibanda, four-square, verticais, parábolas e iluminadores, suportes dos elementos e mais); antenas de receção e RDF; ruído, G/T e agrupamento (stack); tabela de cabos, linhas de adaptação e baluns; verificação dos dados introduzidos. Cada escolha de cálculo e cada caixa de seleção são explicadas no [manual](doc/Manuale_d_uso.pdf), também disponível no programa (separador Manual). Mais informação no [Wiki](https://github.com/ic8tem/YA-NEC/wiki).

**Idioma:** a interface está disponível em 37 idiomas (menu Idioma, em cima à direita), e o Wiki também. O manual está em italiano.

## Imagens do programa

<table><tr><td width="50%" align="center"><a href="docs/screenshots/struttura-3d.png"><img src="docs/screenshots/struttura-3d.png" alt="Estrutura 3D com as correntes e o lóbulo"></a><br><sub>Estrutura 3D com as correntes e o lóbulo</sub></td><td width="50%" align="center"><a href="docs/screenshots/terreno-reale.png"><img src="docs/screenshots/terreno-reale.png" alt="Terreno real ao estilo HFTA sobre o perfil DEM"></a><br><sub>Terreno real ao estilo HFTA sobre o perfil DEM</sub></td></tr><tr><td width="50%" align="center"><a href="docs/screenshots/ricezione-rdf.png"><img src="docs/screenshots/ricezione-rdf.png" alt="Receção: RDF, DMF e ruído"></a><br><sub>Receção: RDF, DMF e ruído</sub></td><td width="50%" align="center"><a href="docs/screenshots/tolleranze.png"><img src="docs/screenshots/tolleranze.png" alt="Tolerâncias de construção (Monte Carlo)"></a><br><sub>Tolerâncias de construção (Monte Carlo)</sub></td></tr><tr><td width="50%" align="center"><a href="docs/screenshots/cavo-balun.png"><img src="docs/screenshots/cavo-balun.png" alt="Cabo e balun: corrente de modo comum"></a><br><sub>Cabo e balun: corrente de modo comum</sub></td><td width="50%" align="center"><a href="docs/screenshots/yagi-incrociate.png"><img src="docs/screenshots/yagi-incrociate.png" alt="Yagis cruzadas: RHCP, LHCP, razão axial"></a><br><sub>Yagis cruzadas: RHCP, LHCP, razão axial</sub></td></tr><tr><td width="50%" align="center"><a href="docs/screenshots/vento.png"><img src="docs/screenshots/vento.png" alt="Cálculos mecânicos: vento (NTC 2018)"></a><br><sub>Cálculos mecânicos: vento (NTC 2018)</sub></td><td width="50%" align="center"><a href="docs/screenshots/divisori.png"><img src="docs/screenshots/divisori.png" alt="Divisores e cabos de fase"></a><br><sub>Divisores e cabos de fase</sub></td></tr><tr><td width="50%" align="center"><a href="docs/screenshots/carichi-hf.png"><img src="docs/screenshots/carichi-hf.png" alt="Cargas HF: armadilhas e bobinas"></a><br><sub>Cargas HF: armadilhas e bobinas</sub></td><td width="50%" align="center"><a href="docs/screenshots/smith.png"><img src="docs/screenshots/smith.png" alt="Carta de Smith"></a><br><sub>Carta de Smith</sub></td></tr></table>

## Novidades da 1.7

- **Novas calculadoras**: Yagi HF de 2-3 elementos com guia de ajuste, Yagi bibanda entrelaçada, vertical multibanda com traps, L invertido / T para 160 m, half-sloper na torre, dipolo de banda larga, dipolo multibanda com relés, carga linear, fio ressonante em harmônicos, gamma match e casamento por stub com carta de Smith.
- **Quad multibanda** com as linhas das bandas não alimentadas e linhas de desacoplamento; **loop magnético** retangular, com espiras em paralelo, transformador e rede L; **Antenas de fio** com o material do fio e o FT-82-43.
- **Stack** com a área efetiva; **ROE** que mostra de que lado está R; **Terreno real** com os ângulos de saída para um locator; eficiência nas calculadoras com bobinas e traps; nova aba **Dicas**.
- **1.7.1**: cotas da Estrutura 3D que se clicam e alteram; nos Diagramas o espaçamento de um stack muda em direto.
- **1.7.2 (segurança)**: a atualização automática instala só pacotes assinados pelo YA-NEC; ficheiros preparados de propósito já não fecham o programa.

As novidades das versões anteriores (da 1.1 à 1.6) estão nas [notas das versões](https://github.com/ic8tem/YA-NEC/releases), no [Wiki](https://github.com/ic8tem/YA-NEC/wiki) e no manual.

## Transferir

Na página **[Releases](https://github.com/ic8tem/YA-NEC/releases)** transfere-se o zip da sua plataforma, extrai-se para uma **pasta com permissão de escrita** (por exemplo `C:\YA-NEC` ou `~/YA-NEC`) e executa-se `YA-NEC.exe` (Linux e macOS: `YA-NEC`). Não é preciso instalar o .NET. As definições e os dados ficam na pasta `data` ao lado do programa: nada no AppData nem no registo.

| Sistema | Pacote |
|---|---|
| Windows 10/11 64 bits | `YA-NEC-<versão>-windows-x64-portatile.zip` |
| Windows 10/11 32 bits | `YA-NEC-<versão>-windows-x86-portatile.zip` |
| Windows em ARM (Snapdragon) | `YA-NEC-<versão>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 (.NET 6) | `YA-NEC-<versão>-windows7-x64-portatile.zip`, `-windows7-x86-portatile.zip` (requer as atualizações KB2533623 e KB2999226) |
| Linux x86_64 | `YA-NEC-<versão>-linux-x64.tar.gz` |
| Linux ARM64 (Raspberry Pi OS 64 bits, outros aarch64) | `YA-NEC-<versão>-linux-arm64.tar.gz` |
| Linux ARM 32 bits (Raspberry Pi OS 32 bits, armv7l) | `YA-NEC-<versão>-linux-armv7l.tar.gz` |
| Alpine e outros Linux com musl | `YA-NEC-<versão>-alpine-x64.tar.gz`, `-alpine-arm64.tar.gz` |
| macOS Apple Silicon (M1-M4) | `YA-NEC-<versão>-mac-apple-silicon-app.zip` |
| macOS Intel | `YA-NEC-<versão>-mac-intel-app.zip` |

Cada zip vem acompanhado da sua impressão digital `.sha256`.

- **Linux**: depois de extrair, `chmod +x YA-NEC` se necessário; são precisos `libfontconfig1` e `libicu` (já presentes nas distribuições com ambiente gráfico).
- **macOS**: o programa não está assinado pela Apple. Depois de extrair, no Terminal: `xattr -dr com.apple.quarantine ~/YA-NEC` (a pasta extraída) e depois `~/YA-NEC/YA-NEC`.

## Atualização automática

O programa verifica 10 segundos após o arranque e depois a cada 3 minutos se há aqui uma versão nova. Se houver, transfere-a, verifica a impressão SHA-256 e, desde a 1.7.2, a **assinatura do YA-NEC** (ECDSA P-256): sem assinatura ou com assinatura errada nada é instalado. A nova versão arranca no reinício, que o programa faz sozinho reabrindo o modelo; a anterior fica em `data/aggiornamenti/precedente-<versão>`, definições e modelos não são tocados. Desliga-se em **Definições > Atualização**.

**Exceção: pacotes Debian/Ubuntu (.deb) e Fedora/openSUSE (.rpm).** Quando existirem, instalar-se-ão em pastas do sistema com permissões de administrador, pelo que o programa não pode (nem deve) alterar-se sozinho: serão atualizados com o gestor de pacotes. Nesses pacotes a atualização automática está desligada e as Definições indicam-no.

## Compilar a partir do código-fonte

É necessário o SDK do .NET 8. Windows: `compila.bat` (programa em `build\win-x64`), `compila.bat /distribuzione` para a versão autónoma, `compila_win7.bat` para o Windows 7. Linux e macOS: `./compila.sh`. Pormenores em [LEGGIMI.txt](LEGGIMI.txt) (em italiano).

## Créditos e licença

**GPL-3.0-or-later** ([LICENSE](LICENSE), [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt)), como o nec2c e o xnec2c, dos quais deriva o motor. O YA-NEC existe graças ao trabalho de radioamadores e investigadores: a lista completa está em [CREDITI.txt](CREDITI.txt) e no separador **Créditos**. Obrigado a Luis IV3PRK pelas publicações gratuitas no seu site.

## Contactos

ic8tem@gmail.com · [QRZ](https://www.qrz.com/db/ic8tem) · [grupo do Facebook](https://www.facebook.com/groups/1675310160982493) · donativos: [ko-fi.com/ic8tem](https://ko-fi.com/ic8tem)
