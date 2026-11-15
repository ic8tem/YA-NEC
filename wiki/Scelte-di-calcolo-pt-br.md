# Escolhas de cálculo

O manual (capítulo 21) explica cada escolha com as suas fontes. Em resumo:

- **Motor**: tradução em C# do nec2c, o NEC-2 do Lawrence Livermore National Laboratory. O autoteste compara-o com o nec2c; os ensaios de aceitação comparam centenas de modelos com o EZNEC. A fatorização da matriz faz-se por blocos (como no LAPACK) e usa todos os processadores menos um, que fica para a janela.
- **Solos**: espaço livre, perfeito, coeficiente de reflexão, Sommerfeld (com a correção de um defeito do nec2c nas tabelas, verificada com o necpp), radiais; solos típicos e personalizados. Avisos automáticos quando os fios estão demasiado perto do solo para o NEC-2.
- **Tubos telescópicos**: o NEC-2 faz ressoar demasiado baixo os elementos com diâmetros escalonados. O YA-NEC usa a correção do livro de D. Leeson W6NL (*Physical Design of Yagi Antennas*, ARRL 1992), a mesma do EZNEC: abaixo da ressonância aplica-se sempre, acima até 45 %, com uma pequena calibração do comprimento feita sobre os valores do EZNEC.
- **Segmentação**: verificação dos segmentos (entre lambda/1000 e lambda/10, relação comprimento/raio) com correção automática; número ímpar de segmentos nos fios alimentados ao centro.
- **G/T**: integração do diagrama 3D como no XLGTa, varredura da elevação como no TANT, temperaturas do céu e do solo de VE7BQH (antigas e novas) e DG7YBN.
- **Verificação dos dados**: 42 verificações que avisam de dados errados sem alterar o modelo.
