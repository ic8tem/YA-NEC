# Actualización automática

Cada 30 minutos (y 2 minutos después del arranque) YA-NEC comprueba en silencio la última versión publicada en este GitHub. Si es más nueva:

1. descarga el zip de su plataforma y su huella `.sha256`;
2. verifica la huella: si falta o es distinta, no se instala nada;
3. sustituye los archivos viejos por los nuevos: la nueva versión arranca **en el próximo inicio**, con un mensaje en la barra de estado.

La versión anterior se guarda en `data/aggiornamenti/precedente-<versión>`, para volver atrás a mano. La configuración (`data/config.ini`), los registros y los modelos del usuario no se tocan. Si hay varias ventanas de YA-NEC abiertas, solo comprueba la primera.

Se desactiva en **Configuración > Actualización**; el botón **Comprobar ahora** no espera los 30 minutos. Cada paso queda en el registro (categoría AGGIORNAMENTO).

**Paquetes .deb y .rpm**, cuando existan: se instalan con permisos de administrador y se actualizan solo con el gestor de paquetes (`apt`, `dnf`, `zypper`). En esos paquetes la actualización automática está desactivada y la Configuración lo indica.
