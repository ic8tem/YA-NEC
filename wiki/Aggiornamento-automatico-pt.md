# Atualização automática

A cada 30 minutos (e 2 minutos depois do arranque) o YA-NEC verifica em silêncio a última versão publicada neste GitHub. Se for mais recente:

1. transfere o zip da sua plataforma e a sua impressão digital `.sha256`;
2. verifica a impressão digital: se faltar ou for diferente, nada é instalado;
3. substitui os ficheiros antigos pelos novos: a nova versão arranca **no próximo arranque**, com uma mensagem na barra de estado.

A versão anterior fica em `data/aggiornamenti/precedente-<versão>`, para voltar atrás à mão. As definições (`data/config.ini`), os registos e os modelos do utilizador não são tocados. Se houver várias janelas do YA-NEC abertas, só a primeira verifica.

Desliga-se em **Definições > Atualização**; o botão **Verificar agora** não espera os 30 minutos. Cada passo fica no registo (categoria AGGIORNAMENTO).

**Pacotes .deb e .rpm**, quando existirem: instalam-se com permissões de administrador e atualizam-se só com o gestor de pacotes (`apt`, `dnf`, `zypper`). Nesses pacotes a atualização automática está desligada e as Definições indicam-no.
