# Decisiones de cálculo

El manual (capítulo 21) explica cada decisión con sus fuentes. En resumen:

- **Motor**: traducción a C# de nec2c, el NEC-2 del Lawrence Livermore National Laboratory. La autoprueba lo compara con nec2c; las pruebas de aceptación comparan cientos de modelos con EZNEC. La factorización de la matriz se hace por bloques (como LAPACK) y usa todos los procesadores menos uno, que queda para la ventana.
- **Suelos**: espacio libre, perfecto, coeficiente de reflexión, Sommerfeld (con la corrección de un defecto de nec2c en las tablas, verificada con necpp), radiales; suelos típicos y personalizados. Avisos automáticos cuando los hilos están demasiado cerca del suelo para NEC-2.
- **Tubos telescópicos**: NEC-2 hace resonar demasiado bajo los elementos con diámetros escalonados. YA-NEC usa la corrección del libro de D. Leeson W6NL (*Physical Design of Yagi Antennas*, ARRL 1992), la misma de EZNEC: por debajo de la resonancia se aplica siempre, por encima hasta el 45 %, con una pequeña calibración de la longitud hecha sobre los valores de EZNEC.
- **Segmentación**: comprobación de los segmentos (entre lambda/1000 y lambda/10, relación longitud/radio) con corrección automática; número impar de segmentos en los hilos alimentados en el centro.
- **G/T**: integración del diagrama 3D como XLGTa, barrido de la elevación como TANT, temperaturas del cielo y del suelo de VE7BQH (antiguas y nuevas) y DG7YBN.
- **Control de datos**: 42 controles que avisan de datos erróneos sin cambiar el modelo.
