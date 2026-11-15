# Atualização automática

A cada 30 minutos (e 2 minutos depois do inicialização) o YA-NEC verifica em silêncio a última versão publicada neste GitHub. Se for mais recente:

1. baixa o zip da sua plataforma e a sua impressão digital `.sha256`;
2. verifica a impressão digital: se faltar ou for diferente, nada é instalado;
3. substitui os arquivos antigos pelos novos: a nova versão inicia **no próximo inicialização**, com uma mensagem na barra de estado.

A versão anterior fica em `data/aggiornamenti/precedente-<versão>`, para voltar atrás manualmente. As configurações (`data/config.ini`), os registros e os modelos do usuário não são tocados. Se houver várias janelas do YA-NEC abertas, só a primeira verifica.

Desliga-se em **Configurações > Atualização**; o botão **Verificar agora** não espera os 30 minutos. Cada passo fica no registro (categoria AGGIORNAMENTO).

**Pacotes .deb e .rpm**, quando existirem: instalam-se com permissões de administrador e atualizam-se só com o gestor de pacotes (`apt`, `dnf`, `zypper`). Nesses pacotes a atualização automática está desligada e as Configurações indicam-no.
