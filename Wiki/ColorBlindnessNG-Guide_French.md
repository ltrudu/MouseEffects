# ColorBlindnessNG - Guide Complet de l'Utilisateur

Bienvenue dans le guide d'utilisation de ColorBlindnessNG ! Ce document vous aidera a comprendre et utiliser ce plugin depuis le debut, meme si vous n'avez aucune experience technique.

---

## Table des Matieres

1. [Qu'est-ce que le Daltonisme ?](#quest-ce-que-le-daltonisme-)
2. [Que Fait ce Plugin ?](#que-fait-ce-plugin-)
3. [Installer MouseEffects](#installer-mouseeffects)
4. [Premier Lancement](#premier-lancement)
5. [Trouver et Activer ColorBlindnessNG](#trouver-et-activer-colorblindnessng)
6. [Comprendre l'Interface](#comprendre-linterface)
7. [Tutoriel : Corriger les Couleurs Etape par Etape](#tutoriel--corriger-les-couleurs-etape-par-etape)
8. [Meilleures Couleurs pour Chaque Type de Daltonisme](#meilleures-couleurs-pour-chaque-type-de-daltonisme)
9. [Fonctionnalites Avancees](#fonctionnalites-avancees)
   - [Mode Re-simulation](#mode-re-simulation-nouveau-dans-v1031)
   - [Algorithmes de Correction](#algorithmes-de-correction)
     - [Correction par LUT](#1-correction-par-lut-par-defaut)
     - [Daltonisation](#2-daltonisation)
     - [Rotation de Teinte](#3-rotation-de-teinte-nouveau-dans-v1030)
     - [Remappage CIELAB](#4-remappage-cielab-nouveau-dans-v1030)
   - [Correction Guidee par Simulation](#correction-guidee-par-simulation)
   - [Modes d'Application](#modes-dapplication)
   - [Types de Degrade](#types-de-degrade)
   - [Modes de Fusion](#modes-de-fusion)
   - [Modes Cercle et Rectangle](#modes-cercle-et-rectangle)
   - [Protection du Blanc](#protection-du-blanc)
   - [Raccourcis Clavier](#raccourcis-clavier)
10. [Questions Frequentes](#questions-frequentes)

---

## Qu'est-ce que le Daltonisme ?

Le daltonisme (aussi appele Deficience de la Vision des Couleurs ou DVC) est une condition ou les personnes voient les couleurs differemment de la plupart des gens. Ce n'est pas vraiment une "cecite" - les personnes daltoniens peuvent voir, mais certaines couleurs leur paraissent similaires alors qu'elles sont en realite differentes.

### Types de Daltonisme

| Type | Ce qu'il Affecte | Frequence |
|------|------------------|-----------|
| **Deuteranopie** | Ne peut pas distinguer le vert du rouge | Le plus courant (affecte ~6% des hommes) |
| **Deuteranomalie** | Perception faible du vert | Tres courant |
| **Protanopie** | Ne peut pas distinguer le rouge du vert | Courant (affecte ~2% des hommes) |
| **Protanomalie** | Perception faible du rouge | Courant |
| **Tritanopie** | Ne peut pas distinguer le bleu du jaune | Rare |
| **Tritanomalie** | Perception faible du bleu | Rare |
| **Achromatopsie** | Voit uniquement en noir et blanc | Tres rare |

**Exemple :** Pour quelqu'un avec une deuteranopie, une pomme rouge et des feuilles vertes peuvent paraitre presque de la meme couleur !

---

## Que Fait ce Plugin ?

ColorBlindnessNG a deux objectifs principaux :

### 1. Mode Simulation (Pour Tester)
Montre a quoi ressemble l'ecran a travers les yeux d'une personne daltonienne. C'est utile pour :
- Les designers qui verifient si leur travail est accessible
- Les enseignants qui expliquent le daltonisme aux etudiants
- Toute personne curieuse de savoir comment les autres voient le monde

### 2. Mode Correction (Pour Aider)
Change les couleurs sur votre ecran pour aider les personnes daltoniennes a distinguer les couleurs. Par exemple :
- Les objets rouges peuvent avoir une teinte bleue
- Les objets verts peuvent virer au cyan
- Cela rend le rouge et le vert differents l'un de l'autre

---

## Installer MouseEffects

### Etape 1 : Telecharger MouseEffects

1. Allez sur la page de telechargement de MouseEffects :
   **https://github.com/LeCaiss662/MouseEffects/releases**

2. Trouvez la derniere version (en haut de la page)

3. Cliquez sur **MouseEffects-win-Setup.exe** pour le telecharger

4. Attendez que le telechargement se termine (generalement quelques secondes)

### Etape 2 : Lancer l'Installateur

1. Ouvrez votre dossier **Telechargements**
   - Appuyez sur `Windows + E` pour ouvrir l'Explorateur de fichiers
   - Cliquez sur "Telechargements" sur le cote gauche

2. Double-cliquez sur **MouseEffects-win-Setup.exe**

3. Si Windows affiche un avertissement de securite :
   - Cliquez sur "Informations complementaires"
   - Cliquez sur "Executer quand meme"
   - C'est normal pour les nouveaux logiciels

4. L'installation demarre automatiquement
   - Pas besoin de mot de passe administrateur
   - S'installe dans votre dossier utilisateur
   - Prend environ 10-30 secondes

5. Une fois termine, MouseEffects demarrera automatiquement

### Etape 3 : Verifier l'Installation

Apres l'installation, vous devriez voir :
- Une petite icone dans votre zone de notification (coin inferieur droit de votre ecran, pres de l'horloge)
- L'icone ressemble a un curseur de souris avec des effets

**Vous ne trouvez pas l'icone ?** Cliquez sur la petite fleche (^) dans la zone de notification pour afficher les icones cachees.

---

## Premier Lancement

### Ce que Vous Verrez

Quand MouseEffects demarre pour la premiere fois :

1. **Icone dans la Zone de Notification** apparait dans le coin inferieur droit
2. L'application s'execute en arriere-plan (pas de fenetre principale)
3. Aucun effet n'est active par defaut

### Ouvrir la Fenetre des Parametres

**Methode 1 : Clic droit sur l'icone**
1. Trouvez l'icone MouseEffects dans la zone de notification
2. Faites un clic droit dessus
3. Cliquez sur "Settings" (Parametres)

**Methode 2 : Double-clic sur l'icone**
1. Trouvez l'icone MouseEffects dans la zone de notification
2. Double-cliquez dessus

### La Fenetre des Parametres

Quand la fenetre des parametres s'ouvre, vous verrez :

```
+-----------------------------------------------------+
|  MouseEffects Settings                    [-][口][X] |
+-----------------------------------------------------+
|                                                     |
|  [ ] Particle Trail                                 |
|  [ ] Laser Work                                     |
|  [ ] Screen Distortion                              |
|  [ ] Color Blindness                                |
|  [ ] Color Blindness NG    <- C'est ce qu'il nous faut ! |
|  [ ] Radial Dithering                               |
|  [ ] Tile Vibration                                 |
|  ... plus d'effets ...                              |
|                                                     |
+-----------------------------------------------------+
```

---

## Trouver et Activer ColorBlindnessNG

### Etape 1 : Ouvrir les Parametres
1. Faites un clic droit sur l'icone MouseEffects
2. Cliquez sur "Settings"

### Etape 2 : Trouver ColorBlindnessNG
1. Faites defiler la liste des effets
2. Cherchez **"Color Blindness NG"**
   - Note : Il y a aussi "Color Blindness" (l'ancienne version)
   - Assurez-vous de selectionner "Color Blindness **NG**" (la version plus recente et amelioree)

### Etape 3 : Activer le Plugin
1. Cliquez sur la case a cocher a cote de "Color Blindness NG"
   - [ ] -> [x]
2. L'effet est maintenant actif !

### Etape 4 : Developper les Parametres
1. Cliquez sur la fleche ou le nom "Color Blindness NG" pour developper
2. Vous verrez toutes les options de configuration

---

## Comprendre l'Interface

### Le Panneau Principal des Parametres

Quand vous developpez ColorBlindnessNG, vous verrez :

```
+-----------------------------------------------------+
| [x] Color Blindness NG                         [v]  |
+-----------------------------------------------------+
|                                                     |
|  Split Mode: [Plein ecran           v]              |
|                                                     |
|  [ ] Mode Comparaison                               |
|                                                     |
|  -------------------------------------------------- |
|                                                     |
|  ZONE 0                                             |
|  Mode: [Correction v]                               |
|                                                     |
|  Preset: [Deuteranopie v]                           |
|                                                     |
|  [Appliquer] [Enregistrer...] [Exporter] [Importer] |
|                                                     |
|  Intensite: xxxxxxxxxxxx______ 80%                  |
|                                                     |
|  [Parametres detailles des couleurs...]             |
|                                                     |
+-----------------------------------------------------+
```

### Signification de Chaque Parametre

| Parametre | Ce qu'il Fait |
|-----------|---------------|
| **Split Mode** | Comment diviser votre ecran en zones |
| **Mode Comparaison** | Affiche le meme contenu dans toutes les zones pour faciliter la comparaison |
| **Mode Zone** | Ce que fait chaque zone : Original, Simulation ou Correction |
| **Preset** | Parametres preconfigures pour chaque type de daltonisme |
| **Intensite** | Force de l'effet (0% = desactive, 100% = pleine puissance) |

### Modes de Division Expliques

| Mode | A quoi ca Ressemble | Ideal Pour |
|------|---------------------|------------|
| **Plein ecran** | Tout l'ecran est affecte | Utilisation quotidienne |
| **Division Verticale** | Gauche / Droite | Comparer deux parametres |
| **Division Horizontale** | Haut / Bas | Comparer deux parametres |
| **Quadrants** | 4 coins | Comparer plusieurs parametres |
| **Cercle** | Le cercle suit votre souris | Verification rapide |
| **Rectangle** | Le rectangle suit votre souris | Verification rapide |

### Modes de Zone Expliques

| Mode | Ce qu'il Fait | Quand l'Utiliser |
|------|---------------|------------------|
| **Original** | Pas de changement, couleurs normales | Reference/comparaison |
| **Simulation** | Montre comment les daltoniens voient | Test/education |
| **Correction** | Aide a distinguer les couleurs | Assistance quotidienne |
| **Re-simulation** | Applique une simulation sur la correction d'une autre zone | Verifier que les corrections fonctionnent pour les daltoniens |

---

## Tutoriel : Corriger les Couleurs Etape par Etape

### Tutoriel 1 : Demarrage Rapide - Correction de Base

**Objectif :** Faire fonctionner la correction des couleurs en 2 minutes.

**Pour qui :** Quelqu'un avec un daltonisme rouge-vert qui veut une solution rapide.

---

**Etape 1 : Ouvrir les Parametres**

1. Regardez dans le coin inferieur droit de votre ecran (pres de l'horloge)
2. Trouvez l'icone MouseEffects (ressemble a un curseur avec des effets)
3. Faites un clic droit dessus
4. Cliquez sur "Settings"

---

**Etape 2 : Activer ColorBlindnessNG**

1. Dans la fenetre des parametres, faites defiler pour trouver "Color Blindness NG"
2. Cliquez sur la case a cocher pour l'activer : [ ] -> [x]
3. Cliquez sur "Color Blindness NG" pour developper les parametres

---

**Etape 3 : Choisir Votre Type de Daltonisme**

1. Assurez-vous que "Mode" est regle sur **Correction**
2. Cliquez sur le menu deroulant "Preset"
3. Selectionnez le type qui vous correspond :
   - **Deuteranopie** - Ne peut pas distinguer le vert du rouge (le plus courant)
   - **Protanopie** - Ne peut pas distinguer le rouge du vert
   - **Tritanopie** - Ne peut pas distinguer le bleu du jaune

   *Pas sur lequel choisir ? Essayez d'abord Deuteranopie - c'est le plus courant.*

---

**Etape 4 : Appliquer et Tester**

1. Cliquez sur le bouton **Appliquer**
2. Regardez autour de votre ecran
3. Ouvrez une image coloree ou un site web
4. Est-ce que les rouges et les verts paraissent differents maintenant ?

**Si oui :** C'est termine ! La correction fonctionne.

**Si non :** Essayez ces ajustements :
- Augmentez l'**Intensite** a 100%
- Essayez un preset different
- Voir le Tutoriel 3 pour des parametres personnalises

---

### Tutoriel 2 : Comparer Avant et Apres

**Objectif :** Voir la difference entre les couleurs corrigees et originales cote a cote.

---

**Etape 1 : Configurer la Vue Divisee**

1. Ouvrez les parametres de ColorBlindnessNG (si ce n'est pas deja fait)
2. Trouvez "Split Mode" en haut
3. Cliquez sur le menu deroulant et selectionnez **Division Verticale**
4. Votre ecran est maintenant divise en moities gauche et droite

---

**Etape 2 : Configurer le Cote Gauche (Zone 0)**

1. Trouvez "ZONE 0" dans les parametres
2. Reglez **Mode** sur "Original"
3. Ce cote montrera les couleurs normales, inchangees

---

**Etape 3 : Configurer le Cote Droit (Zone 1)**

1. Trouvez "ZONE 1" dans les parametres
2. Reglez **Mode** sur "Correction"
3. Choisissez votre **Preset** selon votre type de daltonisme
4. Cliquez sur **Appliquer**

---

**Etape 4 : Activer le Mode Comparaison (Optionnel mais Recommande)**

1. Cochez la case "Mode Comparaison"
2. Maintenant les deux cotes montrent le MEME contenu
3. Un petit point curseur montre ou se trouve votre souris de chaque cote

---

**Etape 5 : Comparez !**

1. Ouvrez une image coloree (essayez de chercher "test daltonisme" dans Google Images)
2. Regardez le cote gauche (original) et le cote droit (corrige)
3. Remarquez comment les couleurs qui semblaient identiques a gauche paraissent differentes a droite

---

### Tutoriel 3 : Creer des Corrections Personnalisees

**Objectif :** Affiner les couleurs selon vos besoins specifiques.

---

**Etape 1 : Commencer avec un Preset**

1. Reglez le Mode sur "Correction"
2. Choisissez le preset le plus proche de vos besoins
3. Cliquez sur **Appliquer**

Cela vous donne un point de depart.

---

**Etape 2 : Comprendre les Controles de Couleur**

Sous le preset, vous verrez des controles pour trois canaux de couleur :
- **Canal Rouge** - Controle comment les couleurs rouges sont corrigees
- **Canal Vert** - Controle comment les couleurs vertes sont corrigees
- **Canal Bleu** - Controle comment les couleurs bleues sont corrigees

Chaque canal a ces controles :

| Controle | Ce qu'il Fait | Exemple |
|----------|---------------|---------|
| **Active** | Activer/desactiver ce canal | [x] Rouge active |
| **Couleur de Depart** | Couleur de sortie quand la valeur du canal est 0 | Noir (#000000) |
| **Couleur d'Arrivee** | Couleur de sortie quand la valeur du canal est 255 | Cyan (#00FFFF) |
| **Force** | Intensite du changement (0-100%) | 80% |
| **Protection du Blanc** | Empeche le blanc/gris d'etre teinte | 50% |

---

**Comprendre Comment Fonctionne la LUT (Correction des Couleurs)**

Il est important de comprendre comment la correction transforme reellement les couleurs.

Une LUT (Table de Correspondance) est un degrade de 256 couleurs stocke en memoire. Pensez-y comme une regle avec des couleurs au lieu de chiffres :

```
Index:    0 ──────────────────────────────────► 255
          │                                      │
Couleur: DEPART ─────── degrade ───────────► ARRIVEE
         (noir)                              (cyan)
```

**Comment ca transforme un pixel :**

Quand on traite un pixel, on utilise la **valeur originale du canal comme index** dans le degrade.

Exemple : LUT du canal Rouge avec Depart=Noir, Arrivee=Cyan

```
Le pixel original a Rouge = 200

Etape 1 : Prendre la valeur rouge (200)
Etape 2 : L'utiliser comme index dans le degrade de la LUT Rouge
Etape 3 : Position 200/255 = 78% a travers le degrade
Etape 4 : Obtenir la couleur a 78% entre Noir et Cyan
Etape 5 : Resultat = (0, 200, 200) - une couleur cyan
```

**Ce qui arrive aux differentes valeurs de rouge :**

| Valeur Rouge Originale | Position dans le Degrade | Couleur de Sortie |
|------------------------|-------------------------|-------------------|
| 0 (pas de rouge) | 0% (depart) | Noir (0,0,0) |
| 64 (rouge faible) | 25% | Cyan Fonce (0,64,64) |
| 128 (rouge moyen) | 50% | Cyan Moyen (0,128,128) |
| 200 (rouge fort) | 78% | Cyan Vif (0,200,200) |
| 255 (rouge maximum) | 100% (arrivee) | Cyan Complet (0,255,255) |

**Pourquoi ca fonctionne pour le daltonisme :**

Pour quelqu'un avec une Deuteranopie (ne peut pas distinguer le rouge du vert) :
- On cree une LUT Rouge : Noir → Cyan
- Les rouges forts deviennent des cyans forts (qui contiennent du bleu - ils PEUVENT voir le bleu)
- Les rouges faibles deviennent des cyans faibles
- Pas de rouge reste noir (inchange)

Resultat : Les objets rouges recoivent une teinte bleue/cyan **proportionnelle a leur niveau de rouge**.

**Analogie simple - pensez a un thermometre :**

```
Temperature (entree) :  Froid ◄─────────────────► Chaud
                         0°                       100°
                         │                         │
                         ▼                         ▼
Couleur affichee :     Bleu ◄─────────────────► Rouge
                     (Depart)                  (Arrivee)
```

La temperature n'est pas "remplacee" - elle determine simplement quelle couleur afficher. C'est pareil avec la LUT - la valeur rouge determine ou echantillonner dans le degrade.

**Pourquoi la Couleur de Depart est generalement Noir :**

- Si Depart = Noir : les pixels sans rouge restent inchanges
- Si Depart = Une autre couleur : meme les pixels SANS rouge seraient teintes (generalement non desire)

C'est pourquoi les presets utilisent le Noir comme Couleur de Depart - on veut seulement affecter les pixels qui contiennent reellement du rouge.

---

**Etape 3 : Ajuster les Couleurs**

Pour changer une couleur :

1. Trouvez le canal que vous voulez ajuster (Rouge, Vert ou Bleu)
2. Cliquez sur la case coloree a cote de "Couleur de Depart" ou "Couleur d'Arrivee"
3. Une fenetre de selection de couleur apparait
4. Selectionnez la couleur desiree
5. Cliquez sur OK

**Exemple :** Pour faire virer le rouge vers le bleu :
- Couleur de Depart : Rouge (#FF0000)
- Couleur d'Arrivee : Cyan (#00FFFF)

---

**Etape 4 : Tester Vos Changements**

1. Ouvrez une image coloree
2. Regardez comment les couleurs apparaissent
3. Ajustez les parametres si necessaire :
   - Les couleurs semblent toujours identiques ? -> Augmentez la Force
   - Les couleurs semblent trop bizarres ? -> Diminuez la Force ou augmentez la Protection du Blanc
   - Mauvaises couleurs affectees ? -> Verifiez quels canaux sont actives

---

**Etape 5 : Enregistrer Votre Preset Personnalise**

Une fois satisfait de vos parametres :

1. Cliquez sur **Enregistrer sous...**
2. Entrez un nom pour votre preset (ex: "Mes Parametres Personnalises")
3. Cliquez sur OK
4. Votre preset apparait maintenant dans le menu deroulant avec un symbole *

---

### Tutoriel 4 : Verifier que Votre Correction Fonctionne

**Objectif :** Confirmer que la correction aide vraiment a distinguer les couleurs.

---

**Etape 1 : Configurer Votre Correction**

1. Activez ColorBlindnessNG
2. Reglez le Mode sur "Correction"
3. Choisissez votre preset
4. Cliquez sur Appliquer

---

**Etape 2 : Activer la Verification**

1. Faites defiler dans les parametres de la Zone
2. Trouvez "Re-simuler pour Verification"
3. Cochez la case pour l'activer
4. Choisissez le type de DVC correspondant a votre preset (ex: si vous utilisez le preset Deuteranopie, selectionnez Deuteranopie)

---

**Etape 3 : Comprendre ce que Vous Voyez**

L'ecran montre maintenant :
1. Votre ecran original ->
2. Correction des couleurs appliquee ->
3. Simulation daltonienne par-dessus

Cela simule ce qu'une personne daltonienne verrait APRES l'application de la correction.

---

**Etape 4 : Interpreter les Resultats**

Regardez les couleurs qui sont normalement confondues (comme le rouge et le vert) :

**Bon Resultat :** Les couleurs paraissent DIFFERENTES l'une de l'autre
- Cela signifie que la correction fonctionne !
- Une personne daltonienne pourrait les distinguer

**Mauvais Resultat :** Les couleurs paraissent toujours IDENTIQUES
- La correction n'est pas assez forte
- Essayez : Augmentez l'Intensite, augmentez la Force du canal, ou essayez un preset different

---

## Meilleures Couleurs pour Chaque Type de Daltonisme

### Comprendre Pourquoi Ces Couleurs Fonctionnent

L'objectif de la correction est de decaler les couleurs confondues vers une gamme que les daltoniens PEUVENT voir. Voici ce qui fonctionne le mieux pour chaque type :

---

### Deuteranopie & Deuteranomalie (Aveugle au Vert)

**Le Probleme :** Le rouge et le vert semblent similaires (les deux paraissent brunatres/jaunes)

**La Solution :** Ajouter du bleu au rouge, pour qu'il paraisse clairement different du vert

| Canal | Activer ? | Couleur Depart | Couleur Arrivee | Force | Pourquoi ca Marche |
|-------|-----------|----------------|-----------------|-------|-------------------|
| **Rouge** | Oui | Noir (#000000) | Cyan (#00FFFF) | 80-100% | Les rouges forts deviennent cyan (contient du bleu, qu'ils PEUVENT voir) |
| **Vert** | Generalement Non | - | - | - | Souvent pas necessaire |
| **Bleu** | Non | - | - | - | La vision du bleu est normale |

**Preset a Utiliser :** Deuteranopie ou Deuteranomalie

**Resultat Visuel :**
- Les pommes rouges auront une teinte bleue/cyan
- Les feuilles vertes restent principalement vertes
- Maintenant elles paraissent differentes !

---

### Protanopie & Protanomalie (Aveugle au Rouge)

**Le Probleme :** Le rouge apparait tres sombre (presque noir), difficile a voir contre le vert

**La Solution :** Eclaircir le rouge et le decaler vers le bleu/cyan

| Canal | Activer ? | Couleur Depart | Couleur Arrivee | Force | Pourquoi ca Marche |
|-------|-----------|----------------|-----------------|-------|-------------------|
| **Rouge** | Oui | Noir (#000000) | Cyan (#00FFFF) | 80-100% | Les rouges forts deviennent cyan lumineux (bleu visible ajoute) |
| **Vert** | Optionnel | Noir (#000000) | Jaune (#FFFF00) | 50% | Peut aider a eclaircir les verts |
| **Bleu** | Non | - | - | - | La vision du bleu est normale |

**Preset a Utiliser :** Protanopie ou Protanomalie

**Resultat Visuel :**
- Les objets rouges deviennent plus lumineux avec des tons cyan/bleu
- Le vert reste vert ou vire au jaune
- Le rouge ne parait plus sombre et cache

---

### Tritanopie & Tritanomalie (Aveugle au Bleu)

**Le Probleme :** Le bleu et le jaune semblent similaires, le violet ressemble au rouge

**La Solution :** Decaler le bleu vers une gamme visible

| Canal | Activer ? | Couleur Depart | Couleur Arrivee | Force | Pourquoi ca Marche |
|-------|-----------|----------------|-----------------|-------|-------------------|
| **Rouge** | Non | - | - | - | La vision du rouge est normale |
| **Vert** | Optionnel | Noir (#000000) | Cyan (#00FFFF) | 50% | Ajoute une distinction bleue |
| **Bleu** | Oui | Noir (#000000) | Jaune (#FFFF00) | 80-100% | Les bleus forts deviennent jaune (visible) |

**Preset a Utiliser :** Tritanopie ou Tritanomalie

**Resultat Visuel :**
- Les objets bleus se decalent vers des couleurs visibles
- Le jaune reste visible
- Le bleu et le jaune paraissent maintenant differents

---

### Rouge-Vert Combine (Les Deux Canaux)

**Le Probleme :** Le rouge et le vert sont tous deux difficiles a distinguer

**La Solution :** Corriger les deux canaux

| Canal | Activer ? | Couleur Depart | Couleur Arrivee | Force | Pourquoi ca Marche |
|-------|-----------|----------------|-----------------|-------|-------------------|
| **Rouge** | Oui | Noir (#000000) | Cyan (#00FFFF) | 80% | Les rouges forts deviennent cyan |
| **Vert** | Oui | Noir (#000000) | Magenta (#FF00FF) | 60% | Les verts forts deviennent magenta |
| **Bleu** | Non | - | - | - | Generalement pas necessaire |

**Preset a Utiliser :** Rouge-Vert (Les Deux)

---

### Tableau de Reference Rapide

| Votre Type | Utiliser le Preset | Canal Principal | Couleur d'Arrivee |
|------------|-------------------|-----------------|-------------------|
| Deuteranopie | Deuteranopie | Rouge | Cyan |
| Deuteranomalie | Deuteranomalie | Rouge | Cyan (plus clair) |
| Protanopie | Protanopie | Rouge | Cyan |
| Protanomalie | Protanomalie | Rouge | Cyan (plus clair) |
| Tritanopie | Tritanopie | Bleu | Jaune |
| Tritanomalie | Tritanomalie | Bleu | Jaune (plus clair) |
| Pas sur | Deuteranopie | Rouge | Cyan |

---

## Fonctionnalites Avancees

### Mode Re-simulation (Nouveau dans v1.0.31)

**Ce que ca fait :** Applique une simulation de daltonisme sur la sortie corrigee d'une autre zone. Cela vous permet de previsualiser exactement comment vos corrections de couleurs apparaitront a une personne daltonienne.

**Pourquoi l'utiliser :**
- Verifier que vos corrections aident vraiment a distinguer les couleurs
- Concevoir du contenu accessible en voyant le resultat final a travers les yeux des daltoniens
- Affiner les parametres de correction avec un retour visuel immediat

**Comment ca fonctionne :**
1. La Zone A applique une **Correction** a l'ecran original
2. La Zone B (en mode **Re-simulation**) prend la sortie corrigee de la Zone A
3. La Zone B applique une **Simulation** par-dessus, montrant comment une personne daltonienne verrait les couleurs corrigees

**Comment l'activer :**
1. Configurez au moins 2 zones (utilisez le mode Division Verticale, Horizontale ou Quadrants)
2. Mettez une zone en mode **Correction** et configurez votre preset de correction
3. Mettez une autre zone en mode **Re-simulation**
4. Dans les parametres de Re-simulation :
   - **Zone Source** : Selectionnez la zone avec la Correction (ex: "Gauche", "Haut-Droit")
   - **Type de DVC** : Choisissez le type de daltonisme a simuler
   - **Intensite** : Force de l'effet de simulation

**Exemple de Configuration (Quadrants) :**
| Zone | Mode | Objectif |
|------|------|----------|
| Haut-Gauche | Original | Voir les couleurs normales |
| Haut-Droit | Correction | Appliquer votre correction |
| Bas-Gauche | Simulation | Voir comment les daltoniens voient l'original |
| Bas-Droit | Re-simulation (source: Haut-Droit) | Voir comment les daltoniens voient votre correction |

**Interpreter les Resultats :**
- **Bon :** Bas-Gauche montre des couleurs confondues, Bas-Droit montre des couleurs distinguables
- **Mauvais :** Les deux zones du bas se ressemblent → La correction n'aide pas, essayez des parametres plus forts

**Notes Importantes :**
- La Re-simulation ne peut sourcer que des zones en mode **Correction**
- Si vous changez une zone source hors du mode Correction, vous verrez un avertissement
- Les labels des zones sources utilisent des noms descriptifs (Gauche/Droite, Haut/Bas, etc.) au lieu de numeros

---

### Algorithmes de Correction

ColorBlindnessNG offre quatre algorithmes de correction differents, chacun avec sa propre approche pour aider les utilisateurs daltoniens a distinguer les couleurs. Vous pouvez choisir l'algorithme dans le menu deroulant **Algorithme de Correction** en mode Correction.

---

#### 1. Correction par LUT (Par Defaut)

**Ce que ca fait :** Utilise des Tables de Correspondance (LUT) pour remapper les couleurs canal par canal. C'est l'approche la plus personnalisable.

**Comment ca fonctionne :**
- Chaque canal de couleur (Rouge, Vert, Bleu) a son propre degrade d'une Couleur de Depart a une Couleur d'Arrivee
- Quand un pixel contient du rouge, cette valeur de rouge determine ou echantillonner dans le degrade du canal Rouge
- Cela decale les couleurs problematiques vers des couleurs plus distinguables

**Ideal pour :** Les utilisateurs qui veulent un controle precis sur exactement comment chaque couleur est transformee.

**Parametres :** Voir la section [Controles de Couleur LUT](#etape-2--comprendre-les-controles-de-couleur) pour une explication detaillee.

---

#### 2. Daltonisation

**Ce que ca fait :** Un algorithme base sur la science qui simule quelles couleurs une personne daltonienne perdrait, puis redistribue cette information de couleur perdue vers des canaux qu'elle PEUT voir.

**Comment ca fonctionne :**
1. Simule la vue daltonienne du pixel
2. Calcule l'"erreur" (quelle information de couleur a ete perdue)
3. Rajoute cette information perdue en utilisant des couleurs que la personne peut percevoir

**Ideal pour :** Une correction equilibree et automatique qui fonctionne bien pour la plupart des contenus sans ajustement.

**Parametres :**
| Parametre | Ce qu'il fait |
|-----------|---------------|
| **Type de DVC** | Quel type de daltonisme corriger |
| **Force** | Quelle quantite de correction appliquer (0-100%) |

---

#### 3. Rotation de Teinte (Nouveau dans v1.0.30)

**Ce que ca fait :** Fait pivoter les couleurs sur le "cercle chromatique" pour que les couleurs confondues se deplacent vers des positions plus faciles a distinguer.

**Pensez-y comme ceci :** Imaginez toutes les couleurs disposees en cercle (le cercle chromatique). Les daltoniens rouge-vert ont du mal avec les couleurs proches sur certaines parties du cercle. La Rotation de Teinte "fait simplement tourner" ces couleurs problematiques vers une position differente ou elles paraissent differentes.

```
Avant :  Le Rouge et le Vert sont tous deux dans la "zone de confusion"
         ↓
Apres :  Le Rouge est pivote pour paraitre plus bleu/violet
         Le Vert reste ou il est
         Maintenant ils paraissent differents !
```

**Comment ca fonctionne :**
1. Identifie les couleurs dans la "plage problematique" (ex: rouges et verts pour la deuteranopie)
2. Fait pivoter uniquement ces couleurs d'un certain nombre de degres
3. Les couleurs hors de la plage problematique restent inchangees

**Ideal pour :** Des corrections naturelles qui preservent l'aspect general des images tout en rendant les couleurs problematiques distinguables.

**Parametres Mode Simple :**
| Parametre | Ce qu'il fait |
|-----------|---------------|
| **Type de DVC** | Configure automatiquement quelles teintes pivoter selon votre type de daltonisme |
| **Force** | Quelle quantite de l'effet de rotation appliquer (0-100%) |

**Mode Avance** (cochez "Mode Avance" pour voir ces options) :

| Parametre | Plage | Ce que ca signifie |
|-----------|-------|-------------------|
| **Debut Source** | 0-360° | Ou la plage de couleurs affectee commence sur le cercle chromatique |
| **Fin Source** | 0-360° | Ou la plage de couleurs affectee finit |
| **Decalage** | -180° a +180° | De combien pivoter les couleurs (positif = sens horaire, negatif = sens anti-horaire) |
| **Attenuation** | 0.0-1.0 | Douceur des limites (0 = coupure nette, 1 = fondu tres progressif) |

**Comprendre le Cercle Chromatique (pour le Mode Avance) :**
```
        Jaune (60°)
            |
Vert (120°)-------- Rouge (0°/360°)
            |
        Bleu (240°)
```

**Exemple pour la Deuteranopie (aveugle au vert) :**
- Debut Source : 0° (rouge)
- Fin Source : 120° (vert)
- Decalage : +60° (pivoter vers jaune/bleu)
- Resultat : Les rouges virent vers le magenta, les verts vers le cyan

---

#### 4. Remappage CIELAB (Nouveau dans v1.0.30)

**Ce que ca fait :** Utilise un espace colorimetrique special appele CIELAB qui est concu pour correspondre a la facon dont les humains percoivent reellement les couleurs. Il peut transferer l'information de couleur entre differents "axes" de perception.

**Comprendre CIELAB (simplifie) :**

Imaginez que les couleurs ont trois proprietes :
- **L (Luminosite) :** A quel point c'est clair ou sombre (noir a blanc)
- **a* (Axe Rouge-Vert) :** A quel point quelque chose est rouge ou vert
- **b* (Axe Bleu-Jaune) :** A quel point quelque chose est bleu ou jaune

```
                    +a* (Rouge)
                       |
        +b* (Jaune)----+----−b* (Bleu)
                       |
                    −a* (Vert)
```

**Le Probleme :** Les daltoniens ont du mal a voir les differences sur certains axes :
- Daltonisme rouge-vert : Ne peuvent pas bien voir les differences sur l'axe a*
- Daltonisme bleu-jaune : Ne peuvent pas bien voir les differences sur l'axe b*

**La Solution :** Le Remappage CIELAB peut :
1. **Transferer** l'information de l'axe qu'ils ne voient pas vers un qu'ils peuvent voir
2. **Amplifier** le contraste sur certains axes
3. **Encoder** les differences de couleur comme differences de luminosite (tout le monde voit la luminosite !)

**Ideal pour :** Des corrections sophistiquees qui fonctionnent au niveau perceptuel, particulierement bonnes pour les images complexes avec des variations de couleur subtiles.

**Parametres Mode Simple :**
| Parametre | Ce qu'il fait |
|-----------|---------------|
| **Type de DVC** | Configure automatiquement le remappage selon votre type de daltonisme |
| **Force** | Quelle quantite de l'effet appliquer (0-100%) |

**Mode Avance** (cochez "Mode Avance" pour voir ces options) :

| Parametre | Plage | Ce que ca signifie |
|-----------|-------|-------------------|
| **Transfert A→B** | -1.0 a +1.0 | Transfere l'information rouge-vert vers l'axe bleu-jaune. Les valeurs positives signifient "prendre ce qui est sur l'axe rouge-vert et l'ajouter a bleu-jaune" |
| **Transfert B→A** | -1.0 a +1.0 | Transfere l'information bleu-jaune vers l'axe rouge-vert |
| **Amplification A** | 0.0 a 2.0 | Amplifie les differences rouge-vert. Valeurs >1 augmentent le contraste, <1 le reduisent |
| **Amplification B** | 0.0 a 2.0 | Amplifie les differences bleu-jaune |
| **Amplification L** | 0.0 a 1.0 | Convertit les differences de couleur en differences de luminosite. A 1.0, les couleurs differentes auront aussi une luminosite differente |

**Exemple de Parametres pour la Deuteranopie :**
- Transfert A→B : 0.5 (envoyer la moitie de l'info rouge-vert vers bleu-jaune, qu'ils PEUVENT voir)
- Transfert B→A : 0.0 (ne pas transferer en retour)
- Amplification A : 1.0 (garder rouge-vert tel quel)
- Amplification B : 1.2 (booster legerement bleu-jaune pour plus de distinction)
- Amplification L : 0.2 (ajouter un peu de difference de luminosite pour plus d'aide)

**Quand utiliser quel algorithme :**

| Situation | Algorithme Recommande |
|-----------|----------------------|
| Veut un controle maximum | Correction par LUT |
| Veut "configurer et oublier" | Daltonisation |
| Photos naturelles | Rotation de Teinte |
| Graphiques/diagrammes complexes | Remappage CIELAB |
| Pas sur | Essayez d'abord Daltonisation |

---

### Correction Guidee par Simulation

**Ce que ca fait :** Au lieu de corriger TOUTES les couleurs, cette fonctionnalite verifie d'abord quels pixels seraient reellement affectes par le daltonisme, puis corrige uniquement ces pixels specifiques.

**Pourquoi l'utiliser :**
- Resultats plus naturels
- Les couleurs non problematiques restent inchangees
- Ecran moins "bizarre"

**Comment l'activer :**
1. Reglez la zone sur le mode "Correction"
2. Faites defiler pour trouver "Correction Guidee par Simulation"
3. Cochez la case pour activer
4. Choisissez le type de DVC a detecter
5. Ajustez la **Sensibilite** :

| Sensibilite | Effet | Quand l'Utiliser |
|-------------|-------|------------------|
| 0.5 - 1.0 | Conservative | Corrige uniquement les couleurs evidemment affectees |
| 2.0 | Equilibree | Bon defaut pour la plupart des utilisateurs |
| 3.0 - 5.0 | Agressive | Corrige meme les couleurs legerement affectees |

---

### Modes d'Application

Controlez COMMENT la correction des couleurs est appliquee :

| Mode | Comment ca Fonctionne | Ideal Pour |
|------|----------------------|------------|
| **Canal Complet** | Corrige tout pixel contenant cette couleur | Correction forte et constante |
| **Dominant Uniquement** | Corrige uniquement si cette couleur est la plus forte | Look plus subtil, naturel |
| **Seuil** | Corrige uniquement les couleurs au-dessus d'une certaine luminosite | Ignorer les couleurs sombres/faibles |

**Comment changer :**
1. Dans les parametres de la zone, trouvez "Mode d'Application"
2. Selectionnez votre mode prefere dans le menu deroulant

---

### Types de Degrade

Comment les couleurs se melangent de la couleur de depart a la couleur d'arrivee :

| Type | Description | Resultat Visuel |
|------|-------------|-----------------|
| **RGB Lineaire** | Melange direct simple | Rapide mais peut paraitre terne |
| **LAB Perceptuel** | Base sur la perception humaine | Transitions douces et naturelles |
| **HSL** | Passe par le cercle chromatique | Couleurs plus vibrantes |

**Recommandation :** Utilisez "LAB Perceptuel" pour les meilleurs resultats visuels.

---

### Modes de Fusion

**Nouveau dans v1.0.29 !** Controlez COMMENT la couleur de correction LUT se melange avec le pixel original.

| Mode | Comment ca Fonctionne | Ideal Pour |
|------|----------------------|------------|
| **Pondere par Canal** (defaut) | L'intensite du melange depend de l'intensite de la couleur | Couleurs pures/vives (bouton rouge, icone verte) |
| **Direct** | Remplacement complet, controle uniquement par la force | Couleurs sombres, photos naturelles, forets vertes |
| **Proportionnel** | Melange base sur la dominance relative du canal | Couleurs mixtes ou le canal n'est pas dominant |
| **Additif** | Ajoute le decalage de couleur en preservant la luminosite | Corrections subtiles qui gardent la luminosite |
| **Ecran** | Eclaircit les couleurs (comme le mode ecran de Photoshop) | Creer des effets plus clairs, delaves |

**Le Probleme que les Modes de Fusion Resolvent :**

La formule originale (Pondere par Canal) fonctionne bien pour les **couleurs pures et vives** comme :
- Un bouton rouge (RVB : 255, 0, 0) → 100% de correction appliquee
- Une icone verte (RVB : 0, 255, 0) → 100% de correction appliquee

Mais elle est **faible pour les couleurs sombres ou mixtes** comme :
- Un vert foret sombre (RVB : 60, 100, 50) → Seulement 40% de correction appliquee !
- Un marron (RVB : 139, 90, 43) → Tres peu de correction

**Solution :** Passez au mode de fusion **Direct** pour les photos et images naturelles.

**Comment changer :**
1. Dans les parametres de la zone, trouvez le menu deroulant "Mode de Fusion"
2. Essayez "Direct" si les couleurs ne sont pas assez corrigees
3. Experimentez avec d'autres modes pour trouver ce qui convient le mieux a votre contenu

**Exemple Visuel :**

*Image de foret originale avec des verts sombres :*
- Pondere par Canal : Le vert change a peine (correction faible)
- Direct : Le vert se decale clairement vers le cyan (correction complete)

---

### Modes Cercle et Rectangle

Au lieu d'affecter tout votre ecran, l'effet peut suivre votre souris :

**Mode Cercle :**
- Une zone circulaire autour de votre curseur recoit l'effet
- Ajustez le **Rayon** pour changer la taille (50-500 pixels)
- Ajustez la **Douceur des Bords** pour des bords lisses (1.0) ou nets (0.0)

**Mode Rectangle :**
- Une zone rectangulaire autour de votre curseur
- Ajustez la **Largeur** et la **Hauteur** separement
- Cochez **Carre** pour que largeur = hauteur

**Quand l'utiliser :**
- Verification rapide des couleurs sans affecter tout l'ecran
- Comparer les couleurs en deplacant la souris dessus
- Moins intrusif visuellement que le plein ecran

---

### Protection du Blanc

Empeche les couleurs neutres (blanc, gris, noir) d'etre teintees.

**Le Probleme :** Sans protection du blanc, le papier blanc pourrait paraitre legerement cyan ou magenta.

**La Solution :** Curseur de Protection du Blanc (0.01 a 1.0)
- Faible (0.01-0.2) : Protection minimale, plus de correction
- Moyen (0.3-0.5) : Equilibre (recommande)
- Eleve (0.6-1.0) : Protection forte, les blancs restent blancs

---

### Raccourcis Clavier

| Raccourci | Ce qu'il Fait |
|-----------|---------------|
| **Alt+Maj+M** | Activer/desactiver rapidement ColorBlindnessNG |
| **Alt+Maj+L** | Ouvrir/fermer la fenetre des parametres |

**Pour activer/desactiver les raccourcis :**
1. Ouvrez les parametres de ColorBlindnessNG
2. Trouvez la section "Raccourcis"
3. Cochez ou decochez chaque raccourci

---

## Questions Frequentes

### Q : Quel preset devrais-je choisir ?

**R :** Si vous connaissez votre type de daltonisme, choisissez ce preset. Si vous n'etes pas sur :

1. Essayez d'abord **Deuteranopie** (c'est le type le plus courant)
2. Si ca n'aide pas, essayez **Protanopie**
3. Si vous avez des problemes avec le bleu/jaune, essayez **Tritanopie**

Vous pouvez aussi passer un test de daltonisme en ligne pour decouvrir votre type.

---

### Q : Les couleurs semblent trop bizarres/fortes

**R :** Essayez ces ajustements :

1. **Reduisez l'Intensite** - Mettez le curseur a 60-70%
2. **Augmentez la Protection du Blanc** - Mettez a 0.3-0.5
3. **Activez la Correction Guidee** - Corrige uniquement les couleurs affectees
4. **Utilisez le mode Dominant Uniquement** - Correction plus subtile

---

### Q : Puis-je l'utiliser toute la journee ?

**R :** Absolument ! Beaucoup d'utilisateurs le gardent active en permanence. Conseils pour l'utilisation quotidienne :

- Utilisez le mode **Plein ecran**
- Reglez l'**Intensite** a un niveau confortable (vous vous y habituerez)
- Utilisez **Alt+Maj+M** pour basculer rapidement si necessaire
- Enregistrez vos parametres preferes comme preset personnalise

---

### Q : L'effet a disparu apres le redemarrage

**R :** MouseEffects devrait se souvenir de vos parametres. Si ce n'est pas le cas :

1. Assurez-vous de fermer MouseEffects correctement (clic droit sur l'icone -> Quitter)
2. Attendez un moment apres avoir change les parametres avant de fermer
3. Verifiez que votre preset est toujours selectionne

---

### Q : Puis-je partager mes parametres avec quelqu'un d'autre ?

**R :** Oui ! Utilisez Exporter/Importer :

**Pour partager vos parametres :**
1. Enregistrez vos parametres comme preset personnalise
2. Cliquez sur **Exporter**
3. Choisissez ou enregistrer le fichier .json
4. Envoyez ce fichier a votre ami

**Pour utiliser les parametres de quelqu'un d'autre :**
1. Cliquez sur **Importer**
2. Trouvez le fichier .json qu'ils vous ont envoye
3. Le preset sera ajoute a votre liste

---

### Q : Quelle est la difference entre "Color Blindness" et "Color Blindness NG" ?

**R :** ColorBlindnessNG est la version plus recente et amelioree :

| Fonctionnalite | Color Blindness | Color Blindness NG |
|----------------|-----------------|-------------------|
| Parametres par zone | Limite | Controle complet |
| Presets personnalises | Non | Oui, avec export/import |
| Algorithmes de correction | 1 (Daltonisation) | 4 (LUT, Daltonisation, Rotation Teinte, CIELAB) |
| Correction Guidee | Non | Oui |
| Mode Re-simulation | Non | Oui (previsualiser les corrections a travers les yeux des daltoniens) |
| Modes de forme | Basique | Cercle & Rectangle |

**Recommandation :** Utilisez Color Blindness NG pour la meilleure experience.

---

### Q : Ca ne fonctionne pas du tout

**R :** Etapes de depannage :

1. **Le plugin est-il active ?** Verifiez la case a cocher a cote de "Color Blindness NG"
2. **La zone est-elle en mode Correction ?** Verifiez le menu deroulant Mode
3. **L'Intensite est-elle au-dessus de 0 ?** Montez le curseur
4. **Un preset est-il selectionne ?** Choisissez-en un dans le menu deroulant et cliquez sur Appliquer
5. **Essayez de redemarrer** - Fermez et rouvrez MouseEffects

---

## Liste de Verification Demarrage Rapide

Utilisez cette liste pour une configuration rapide :

- [ ] Telecharger MouseEffects depuis les releases GitHub
- [ ] Lancer l'installateur (MouseEffects-win-Setup.exe)
- [ ] Trouver l'icone dans la zone de notification (en bas a droite, pres de l'horloge)
- [ ] Clic droit -> Settings
- [ ] Activer "Color Blindness NG" (case a cocher)
- [ ] Developper les parametres (cliquer sur le nom)
- [ ] Mettre le Mode sur "Correction"
- [ ] Choisir votre Preset (Deuteranopie si incertain)
- [ ] Cliquer sur "Appliquer"
- [ ] Tester avec une image coloree
- [ ] Ajuster l'Intensite si necessaire

**Termine !** Vous avez maintenant la correction des couleurs activee.

---

## Obtenir de l'Aide

Si vous avez besoin d'aide supplementaire :

- **Documentation Technique :** [Reference des Plugins](Plugins.md#color-blindness-ng)
- **Apercu des Fonctionnalites :** [Features](Features.md#color-vision-accessibility-colorblindnessng)
- **Signaler des Problemes :** [GitHub Issues](https://github.com/LeCaiss662/MouseEffects/issues)

---

*Ce guide a ete cree pour aider tout le monde a utiliser ColorBlindnessNG, quelle que soit leur experience technique. Si quelque chose n'est pas clair, n'hesitez pas a nous le faire savoir !*
