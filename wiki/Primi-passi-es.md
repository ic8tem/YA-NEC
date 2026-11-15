# Primeros pasos

1. **Ejemplos** (barra de órdenes) abre uno de los modelos listos: dipolos, ground plane, Yagis VHF (DK7ZB, I4GBZ, DJ9BV BVO, YU7EF), Yagis HF (Force 12 XR-5, ARRL 520-40H), hexbeam, four-square, Yagi de bucles a 1296 MHz y otros. **Abrir modelo** abre un archivo NEC, MMANA, AutoEZ, EZNEC o K6STI; también se puede arrastrar el archivo a la ventana.
2. Se elige la **banda** (de 2200 m a 3 cm, límites de la IARU Región 1) o una frecuencia libre y luego **Calcular**.
3. Los **recuadros de resultados** muestran impedancia, ROE, ganancia, F/B, F/R, aperturas a -3 dB, eficiencia y AGT (prueba de convergencia: cerca de 0 dB el modelo es fiable).
4. **Barrido** calcula la banda alrededor de la frecuencia: ROE, R/X, ganancia, F/B y carta de Smith.

## Las pestañas

- **Estructura 3D**: la antena (arrastrar = girar, rueda = zoom, botón derecho = desplazar, doble clic = vista inicial), con el suelo dibujado según su tipo y la altura. Debajo, la **tabla de hilos** al estilo de una hoja de cálculo: las celdas aceptan números, variables y fórmulas. Abajo a la derecha, los **objetos del modelo**: fuentes, cargas, líneas, transformadores.
- **Diagramas**: horizontal, vertical y 3D, con el ángulo a -3 dB.
- **Frecuencia, Smith, Corrientes**: resultados del barrido y corrientes en los hilos.
- **Modelo NEC**: las tarjetas NEC, editables a mano.
- **Variables** y **Optimizador**: se da nombre a las medidas y se deja que el programa las cambie para alcanzar un objetivo (ROE, ganancia, F/B, impedancia...). El manual tiene ejemplos paso a paso.
- **Calculadoras**: ver [Calculadoras](Calcolatori-es).
- **Antenas de recepción** y **Ruido/GT/Stack**: RDF de las antenas de recepción, temperatura de antena y G/T (método de TANT y XLGTa, temperaturas de referencia de VE7BQH y DG7YBN), agrupación.
- **Avisos**: todo lo dudoso que el programa ha encontrado en el modelo (segmentos demasiado cortos o largos, hilos cerca del suelo, uniones abarrotadas). El botón **Comprobar...** de la barra superior revisa 42 errores típicos de introducción de datos sin calcular.
- **Configuración**, **Manual**, **Créditos**. Arriba a la derecha: **Idioma**, **Colores** (18 esquemas) y **Acerca de**.

## Guardar

**Guardar** y **Guardar como** escriben el modelo en NEC, MMANA, AutoEZ, EZNEC o en el formato propio **.yanec**, que conserva también la descripción y el archivo original.
