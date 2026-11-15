# Choix de calcul

Le manuel (chapitre 21) explique chaque choix avec ses sources. En bref :

- **Moteur** : traduction en C# de nec2c, le NEC-2 du Lawrence Livermore National Laboratory. L'autotest le compare à nec2c ; les essais de recette comparent des centaines de modèles à EZNEC. La factorisation de la matrice se fait par blocs (comme LAPACK) et utilise tous les processeurs sauf un, laissé à la fenêtre.
- **Sols** : espace libre, parfait, coefficient de réflexion, Sommerfeld (avec la correction d'un défaut de nec2c dans les tables, vérifiée avec necpp), réseau de radians ; sols typiques et personnalisés. Avertissements automatiques quand les fils sont trop proches du sol pour NEC-2.
- **Tubes télescopiques** : NEC-2 fait résonner trop bas les éléments à diamètres étagés. YA-NEC utilise la correction du livre de D. Leeson W6NL (*Physical Design of Yagi Antennas*, ARRL 1992), la même qu'EZNEC : sous la résonance elle s'applique toujours, au-dessus jusqu'à 45 %, avec un petit étalonnage de la longueur fait sur les valeurs d'EZNEC.
- **Segmentation** : contrôle des segments (entre lambda/1000 et lambda/10, rapport longueur/rayon) avec correction automatique ; nombre impair de segments sur les fils alimentés au centre.
- **G/T** : intégration du diagramme 3D comme XLGTa, balayage de l'élévation comme TANT, températures du ciel et du sol de VE7BQH (anciennes et nouvelles) et DG7YBN.
- **Contrôle des données** : 42 contrôles qui signalent les données erronées sans modifier le modèle.
