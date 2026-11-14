# Fanny in 2 Trom

Jeu iPhone réalisé avec **Unity**. Fanny est capturée au lasso en Bretagne, puis elle frotte ses mains sur une barre du métro parisien (« trom » = métro en verlan)… avant de se les frotter sur le visage. Les épreuves s'enchaînent en boucle : à chaque tour le jeu accélère, avec 3 vies.

| # | Épreuve | Orientation | Geste |
|---|---------|-------------|-------|
| 1 | **Le lasso breton**. Fanny se balade dans une rue bretonne | Paysage | Toucher l'écran pour lancer le lasso (il faut viser là où elle va être) |
| 2 | **La barre du métro**. Gros plan sur la main posée sur la barre | **Portrait** (le jeu demande de tourner le téléphone) | Glisser le doigt de haut en bas très vite pour remplir le « Frottomètre » |
| 3 | **Sur le visage !** Les mains pleines de microbes | Paysage | Frotter partout sur le visage de Fanny jusqu'à 100 % de « contamination » |

Points : rapidité + lassos restants. Le meilleur score est enregistré sur le téléphone.

---

## 1. Ce qu'il faut installer

1. **Unity Hub** : https://unity.com/download
2. Dans Unity Hub > *Installs* > *Install Editor* : **Unity 6 (LTS)**, en cochant le module **iOS Build Support**.
3. Pour mettre le jeu sur un iPhone : un **Mac** avec **Xcode** (App Store) et un identifiant Apple (gratuit pour tester sur son propre iPhone, compte Apple Developer payant pour TestFlight / App Store).

## 2. Ouvrir et tester le jeu

1. Unity Hub > *Projects* > *Add* > *Add project from disk* > choisir ce dossier (`Fannyinthetrom`).
2. Si Unity Hub demande une version, choisir Unity 6.
3. À la première ouverture, le projet **se configure tout seul** : scène, nom de l'app, identifiant `com.yateo.fannyin2trom`, orientation, icône. Le menu **Fanny** apparaît dans la barre du haut.
4. Dans l'onglet **Game**, choisir une résolution paysage (ex. *2556x1179 Landscape*, ou ajouter « iPhone 15 » avec le `+`).
5. Cliquer **Play** ▶ (ou menu *Fanny > 2. Jouer dans l'éditeur*). La souris remplace le doigt.

> Dans l'éditeur, l'écran ne tourne pas tout seul : pour voir l'épreuve 2 comme sur iPhone, passez l'onglet Game en résolution portrait pendant qu'elle se joue.

Si un message indique que « la gestion des contrôles tactiles a été activée », fermer puis rouvrir Unity.

## 3. Mettre le jeu sur un iPhone

1. Dans Unity : menu **Fanny > 3. Construire pour iPhone (projet Xcode)**. Le projet Xcode est généré dans `Builds/iOS`.
2. Sur le Mac : ouvrir `Builds/iOS/Unity-iPhone.xcodeproj` dans Xcode.
3. *Signing & Capabilities* : cocher *Automatically manage signing* et choisir votre équipe (Team).
4. Brancher l'iPhone, le sélectionner en haut de Xcode, cliquer ▶.
   (Sur l'iPhone, la première fois : Réglages > Général > VPN et gestion de l'appareil > faire confiance au développeur.)
5. Pour diffuser à des testeurs : *Product > Archive* puis envoi vers **TestFlight**.

## 3 bis. TestFlight automatique avec GitHub (sans Mac)

Le workflow `.github/workflows/ios-testflight.yml` compile le jeu avec Unity, le signe et l'envoie sur TestFlight. Il se lance depuis l'onglet **Actions** > **iPhone → TestFlight** > **Run workflow**, ou en poussant un tag `v1.0.1`, etc. Chaque lancement produit un nouveau numéro de build.

### À préparer une seule fois

**Côté Apple** (compte Apple Developer) :
1. **Team ID** : developer.apple.com > Account > *Membership details*. Il s'agit de 10 caractères, ex. `AB12CD34EF`.
2. **Identifiant de l'app** : *Certificates, Identifiers & Profiles* > *Identifiers* > **+** > *App IDs* > *App*. Choisir *Explicit* et saisir `com.yateo.fannyin2trom`.
3. **Fiche de l'app** : appstoreconnect.apple.com > *Apps* > **+** > *Nouvelle app*. Renseigner : iOS, nom « Fanny in 2 Trom » (ou une variante si le nom est pris), langue française, identifiant de lot `com.yateo.fannyin2trom`, SKU `fannyin2trom`.
4. **Clé API** : App Store Connect > *Utilisateurs et accès* > *Intégrations* > *API App Store Connect* > *Clés d'équipe* > **+**. Accès **Admin**, nécessaire pour que la signature soit automatique.
   - Noter l'**ID de la clé** et l'**ID de l'émetteur** (Issuer ID).
   - Télécharger le fichier `.p8`. Attention : il n'est téléchargeable qu'une fois.

**Côté Unity** :
1. Créer un compte sur id.unity.com, avec email et mot de passe et sans double authentification par application.
2. Sur ton PC : ouvrir Unity Hub, se connecter, puis *Preferences* > *Licenses* > *Add* > *Get a free personal license*.
3. Ouvrir dans le Bloc-notes le fichier `C:\ProgramData\Unity\Unity_lic.ulf` : son contenu est la licence.
   > La licence **Personal** est réservée aux structures sous 200 000 $ de chiffre d'affaires annuel. Au-delà, il faut Unity Pro : mettre alors le numéro de série dans `UNITY_SERIAL` au lieu de `UNITY_LICENSE`.

**Côté GitHub** : dans le dépôt, aller dans *Settings* > *Secrets and variables* > *Actions* > *New repository secret* et créer :

| Secret | Valeur |
|--------|--------|
| `UNITY_EMAIL` | email du compte Unity |
| `UNITY_PASSWORD` | mot de passe du compte Unity |
| `UNITY_LICENSE` | tout le contenu du fichier `Unity_lic.ulf` (ou `UNITY_SERIAL` pour Unity Pro) |
| `APPLE_TEAM_ID` | Team ID Apple |
| `APPSTORE_KEY_ID` | ID de la clé API |
| `APPSTORE_ISSUER_ID` | Issuer ID |
| `APPSTORE_P8` | tout le contenu du fichier `.p8`, lignes `-----BEGIN/END PRIVATE KEY-----` comprises |

### Ensuite
- Lancer le workflow. Le premier passage prend environ 40 min, les suivants sont plus rapides grâce au cache.
- Le build apparaît dans App Store Connect > **TestFlight** après le traitement par Apple (5 à 30 min).
- S'ajouter comme testeur interne, installer l'app **TestFlight** sur l'iPhone, et c'est parti.
- Coût : sur un dépôt privé, les minutes macOS de GitHub Actions comptent 10 fois. Une compilation consomme environ 150 à 250 minutes du quota mensuel.

## 4. Les images (remplaçables)

Toutes les images sont dans `Assets/Resources/Art/`. **Pour en changer une, il suffit de remplacer le fichier en gardant le même nom.** Si une image est absente, le jeu dessine une version provisoire.

| Fichier | Rôle | Source actuelle |
|---------|------|-----------------|
| `bg_bretagne.jpg` | Décor de l'épreuve 1 et de l'écran titre | Ta scène bretonne. La Fanny déjà dessinée a été **effacée et masquée par un massif d'hortensias**, en provisoire |
| `fanny_face.png`, `fanny_profil.png`, `fanny_dos.png` | Fanny qui se balade (face / profil / dos selon sa direction) | Détourées depuis ta planche personnage (fond transparent) |
| `fanny_visage.png` | Gros plan du visage (épreuve 3, portrait dans l'épreuve 2) | Agrandi depuis la planche, donc un peu flou |
| `bg_metro.jpg` | Rame de métro (épreuves 2 et 3). Le jeu se cale sur la grande barre | Ta scène métro |
| `main_barre.png` | Bras + main qui glisse sur la barre | Détourée depuis ton gros plan, manche prolongée hors champ |
| `fanny_visage_beurk.png` *(optionnel)* | Visage dégoûté, affiché à partir de 50 % de microbes | Absent, le visage est teinté en vert à la place |

Règles : PNG avec fond transparent pour les personnages, JPG ou PNG pour les décors. Les réglages d'import sont automatiques.
Les barres orange/bleue dessinées dans le décor breton servent de vrai HUD (orange = temps, bleu = lassos restants). Si le décor est remplacé par une image d'une autre taille, ces barres ne sont plus utilisées et le chrono standard s'affiche.

Les sons sont générés par le code (lasso, « wah wah wah » de trombone quand on perd, carillon du métro…). Pour mettre de vrais sons, déposer des `.wav`/`.mp3` dans `Assets/Resources/Audio/` avec ces noms : `lasso`, `attrape`, `rate`, `frotte`, `beurk`, `victoire`, `perdu`, `tic`, `carillon`, `click`.

## 5. Ce qui aiderait pour la suite (visuels à fournir)

1. **La scène bretonne sans Fanny et sans le HUD** (même cadrage). Elle remplacerait le massif d'hortensias ajouté pour masquer la Fanny d'origine.
2. **Un gros plan du visage de Fanny** en haute définition (fond transparent), plus une version **dégoûtée**.
3. **Le décor de l'épreuve 3**, si tu veux une scène dédiée (sinon on reste dans le métro).
4. Une vraie **icône d'app** (1024×1024). Celle d'aujourd'hui est générée depuis le portrait : `Assets/Art/Icone/icone.png`.

## 6. Organisation du code

```
Assets/Scripts/Runtime/
  GameManager.cs   démarrage automatique, menu, enchaînement des épreuves, vies, score, HUD, orientation
  MiniGame.cs      base commune des épreuves (chrono, victoire/défaite, effets)
  LassoGame.cs     épreuve 1 : le lasso breton
  MetroBarGame.cs  épreuve 2 : la barre du métro (portrait)
  FaceRubGame.cs   épreuve 3 : sur le visage
  Art.cs           chargement des images + repères (position de la barre, de la main…)
  Painter.cs       dessin des visuels provisoires
  Sfx.cs           bruitages synthétisés
  Pointer.cs       doigt / souris
  UIKit.cs         construction de l'interface
Assets/Scripts/Editor/
  FannyProjectSetup.cs  configuration automatique iPhone + menu Fanny
  FannyBuild.cs         génération du projet Xcode
  ArtImporter.cs        réglages d'import des images
Docs/references/        tes visuels d'origine
```

Les réglages de difficulté (durées, vitesse de Fanny, sensibilité du Frottomètre, part du visage à couvrir) sont en haut de chaque fichier d'épreuve.
