# Premiers pas

1. **Exemples** (barre de commandes) ouvre l'un des modèles prêts : dipôles, ground plane, Yagi VHF (DK7ZB, I4GBZ, DJ9BV BVO, YU7EF), Yagi HF (Force 12 XR-5, ARRL 520-40H), hexbeam, four-square, Yagi à boucles 1296 MHz, etc. **Ouvrir un modèle** ouvre un fichier NEC, MMANA, AutoEZ, EZNEC ou K6STI ; on peut aussi glisser le fichier sur la fenêtre.
2. Choisir la **bande** (de 2200 m à 3 cm, limites IARU Région 1) ou une fréquence libre, puis **Calculer**.
3. Les **cadres des résultats** affichent impédance, ROS, gain, F/B, F/R, ouvertures à -3 dB, rendement et AGT (test de convergence : proche de 0 dB, le modèle est fiable).
4. **Balayage** calcule la bande autour de la fréquence : ROS, R/X, gain, F/B et abaque de Smith.

## Les onglets

- **Structure 3D** : l'antenne (glisser = tourner, molette = zoom, bouton droit = déplacer, double clic = vue initiale), avec le sol dessiné selon son type et la hauteur. En dessous, le **tableau des fils** façon tableur : les cellules acceptent des nombres, des variables et des formules. En bas à droite, les **objets du modèle** : sources, charges, lignes, transformateurs.
- **Diagrammes** : horizontal, vertical et 3D, avec l'angle à -3 dB.
- **Fréquence, Smith, Courants** : résultats du balayage et courants sur les fils.
- **Modèle NEC** : les cartes NEC, modifiables à la main.
- **Variables** et **Optimiseur** : on nomme les dimensions et on laisse le programme les modifier pour atteindre un objectif (ROS, gain, F/B, impédance...). Le manuel donne des exemples pas à pas.
- **Calculateurs** : voir [Calculateurs](Calcolatori-fr).
- **Antennes de réception** et **Bruit/GT/Stack** : RDF des antennes de réception, température d'antenne et G/T (méthode de TANT et XLGTa, températures de référence de VE7BQH et DG7YBN), couplage.
- **Avertissements** : tout ce que le programme a trouvé de douteux dans le modèle (segments trop courts ou trop longs, fils proches du sol, jonctions encombrées). Le bouton **Contrôler...** de la barre du haut vérifie 42 erreurs de saisie typiques sans calculer.
- **Réglages**, **Manuel**, **Crédits**. En haut à droite : **Langue**, **Couleurs** (18 schémas) et **À propos**.

## Enregistrer

**Enregistrer** et **Enregistrer sous** écrivent le modèle en NEC, MMANA, AutoEZ, EZNEC ou dans le format propre **.yanec**, qui conserve aussi la description et le fichier d'origine.
