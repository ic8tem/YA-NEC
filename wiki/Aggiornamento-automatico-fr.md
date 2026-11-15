# Mise à jour automatique

Toutes les 30 minutes (et 2 minutes après le démarrage), YA-NEC vérifie discrètement la dernière version publiée sur ce GitHub. Si elle est plus récente :

1. il télécharge le zip de sa plateforme et son empreinte `.sha256` ;
2. il vérifie l'empreinte : si elle manque ou diffère, rien n'est installé ;
3. il remplace les anciens fichiers par les nouveaux : la nouvelle version démarre **au prochain lancement**, avec un message dans la barre d'état.

La version précédente est conservée dans `data/aggiornamenti/precedente-<version>`, pour revenir en arrière à la main. Les réglages (`data/config.ini`), les journaux et les modèles de l'utilisateur ne sont pas touchés. Si plusieurs fenêtres de YA-NEC sont ouvertes, seule la première vérifie.

Elle se désactive dans **Réglages > Mise à jour** ; le bouton **Vérifier maintenant** n'attend pas les 30 minutes. Chaque étape est inscrite au journal (catégorie AGGIORNAMENTO).

**Paquets .deb et .rpm**, quand ils existeront : ils s'installent avec les droits d'administrateur et se mettent à jour uniquement avec le gestionnaire de paquets (`apt`, `dnf`, `zypper`). Dans ces paquets la mise à jour automatique est désactivée et les Réglages l'indiquent.
