# Primeiros passos

1. **Exemplos** (barra de comandos) abre um dos modelos prontos: dipolos, ground plane, Yagis VHF (DK7ZB, I4GBZ, DJ9BV BVO, YU7EF), Yagis HF (Force 12 XR-5, ARRL 520-40H), hexbeam, four-square, Yagi de anéis a 1296 MHz e outros. **Abrir modelo** abre um ficheiro NEC, MMANA, AutoEZ, EZNEC ou K6STI; também se pode arrastar o ficheiro para a janela.
2. Escolhe-se a **banda** (de 2200 m a 3 cm, limites da IARU Região 1) ou uma frequência livre e depois **Calcular**.
3. Os **quadros de resultados** mostram impedância, ROE, ganho, F/B, F/R, aberturas a -3 dB, eficiência e AGT (teste de convergência: perto de 0 dB o modelo é fiável).
4. **Varrimento** calcula a banda em torno da frequência: ROE, R/X, ganho, F/B e carta de Smith.

## Os separadores

- **Estrutura 3D**: a antena (arrastar = rodar, roda = zoom, botão direito = deslocar, duplo clique = vista inicial), com o solo desenhado segundo o tipo e a altura. Por baixo, a **tabela dos fios** ao estilo de uma folha de cálculo: as células aceitam números, variáveis e fórmulas. Em baixo à direita, os **objetos do modelo**: fontes, cargas, linhas, transformadores.
- **Diagramas**: horizontal, vertical e 3D, com o ângulo a -3 dB.
- **Frequência, Smith, Correntes**: resultados do varrimento e correntes nos fios.
- **Modelo NEC**: os cartões NEC, editáveis à mão.
- **Variáveis** e **Otimizador**: dá-se um nome às medidas e deixa-se o programa alterá-las para atingir um objetivo (ROE, ganho, F/B, impedância...). O manual tem exemplos passo a passo.
- **Calculadoras**: ver [Calculadoras](Calcolatori-pt).
- **Antenas de receção** e **Ruído/GT/Stack**: RDF das antenas de receção, temperatura de antena e G/T (método de TANT e XLGTa, temperaturas de referência de VE7BQH e DG7YBN), agrupamento.
- **Avisos**: tudo o que o programa encontrou de duvidoso no modelo (segmentos demasiado curtos ou longos, fios perto do solo, junções sobrelotadas). O botão **Verificar...** na barra de cima verifica 42 erros típicos de introdução de dados sem calcular.
- **Definições**, **Manual**, **Créditos**. Em cima à direita: **Idioma**, **Cores** (18 esquemas) e **Acerca de**.

## Guardar

**Guardar** e **Guardar como** escrevem o modelo em NEC, MMANA, AutoEZ, EZNEC ou no formato próprio **.yanec**, que guarda também a descrição e o ficheiro original.
