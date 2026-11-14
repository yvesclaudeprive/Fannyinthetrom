# Fanny in 2 Trom

Jeu iPhone réalisé avec **Unity**. Fanny est capturée au lasso en Bretagne, puis elle frotte ses mains sur une barre du métro parisien (« trom » = métro en verlan)… avant de se les frotter sur le visage. Les épreuves s'enchaînent en boucle : à chaque tour le jeu accélère, avec 3 vies.

| # | Épreuve | Geste |
|---|---------|-------|
| 1 | **Le lasso breton**. Fanny marche dans la rue bretonne, sur fond d'air breton. Attrapée, elle se tortille en criant | Toucher l'écran pour lancer le lasso (il faut viser là où elle va être) |
| 2 | **Le chouchen**. Une rangée de verres à shot au comptoir de la crêperie. Fanny boit très vite : plus on sert vite, plus elle est bourrée (joues rouges, hoquet, vision double, la salle tangue)… et à 100 % d'ivresse, elle vomit | Taper sur les verres (un tapotement = un demi-verre) ou garder le doigt dessus pour verser. Elle boit cul sec, tête renversée, « glou glou glou », puis rote |
| 3 | **La souris**. On choisit une souris bien moche (pelée, pustuleuse, borgne…), on la tient par la queue (elle se balance) et on la guide dans la bouche de Fanny qui bouge la tête. CROC : elle la mâche lentement, la queue dépasse de sa bouche… puis slurp et rot | Tenir et déplacer la souris |
| 4 | **Que va-t-elle frotter ?** Au choix : la barre du métro, la barre de l'ascenseur (avec sa musique), le siège des toilettes publiques ou un pigeon parisien | Frotter très vite pour remplir le Frottomètre |
| 5 | **Sur le visage !** Elle grimace et râle dès qu'on la touche ; sa barre de vie descend… puis elle vomit | Frotter partout sur le visage de Fanny jusqu'à vider sa barre de vie |

Le jeu se joue entièrement **à l'horizontale**.

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
| `fanny_marche_1.png` … `fanny_marche_6.png` | Marche de profil : 6 étapes (appui, genou qui plie, pied qui décolle, passage…) | Fabriquées à partir du profil |
| `fanny_debat_1.png` … `fanny_debat_4.png` | Fanny qui se débat dans le lasso : bras qui battent, jambes qui ruent, grimace | Fabriquées à partir de la vue de face |
| `fanny_visage_yeux_fermes`, `_bouche`, `_grimace_1`, `_beurk_bouche`, `_beurk_yeux_fermes` | Expressions animées : clignement, bouche qui s'ouvre quand elle crie, grimace légère puis franche | Fabriquées en déformant le visage |
| `fanny_visage.png` | Gros plan du visage (épreuve 3, portrait dans l'épreuve 2) | Agrandi depuis la planche, donc un peu flou |
| `bg_metro.jpg` | Rame de métro (épreuves 2 et 3). L'épreuve 2 fait un gros plan sur la grande barre | Ta scène métro |
| `main_barre.png` | Bras + main qui glisse sur la barre | Détourée depuis ton gros plan, manche prolongée hors champ |
| `fanny_visage_beurk.png` | Grimace : dès qu'on lui touche le visage, et à la fin | Fabriquée en déformant le visage (yeux plissés, bouche tordue, teint verdâtre) |

Règles : PNG avec fond transparent pour les personnages, JPG ou PNG pour les décors. Les réglages d'import sont automatiques.
Les barres orange/bleue dessinées dans le décor breton servent de vrai HUD (orange = temps, bleu = lassos restants). Si le décor est remplacé par une image d'une autre taille, ces barres ne sont plus utilisées et le chrono standard s'affiche.

Les sons sont de **vrais enregistrements libres de droits (CC0)** : voix de Fanny (cris, « hmpf », toux de dégoût, rire, respiration paniquée), lasso, pas sur les pavés, gorgées, verres, versement, vomi, ambiance du métro, jingles et voix d'annonceur (« 3, 2, 1, Go ! », « Game over »). La musique bretonne est un air original joué avec de vrais timbres d'instruments (bombarde, cornemuse, caisse claire). Origine et licences : `Docs/CREDITS_SONS.md`.
Pour changer un son, remplacer le fichier dans `Assets/Resources/Audio/` en gardant le même nom (`.ogg`, `.wav` ou `.mp3`). Pour les variantes (`cri_1`, `cri_2`…), le jeu en tire une au hasard.

## 5. Ce qui aiderait pour la suite (visuels à fournir)

1. **La scène bretonne sans Fanny et sans le HUD** (même cadrage). Elle remplacerait le massif d'hortensias ajouté pour masquer la Fanny d'origine.
2. **Un gros plan du visage de Fanny** en haute définition (fond transparent), plus une vraie version **grimace**.
3. **Le décor de l'épreuve 3**, si tu veux une scène dédiée (sinon on reste dans le métro).
4. Une vraie **icône d'app** (1024×1024). Celle d'aujourd'hui est générée depuis le portrait : `Assets/Art/Icone/icone.png`.

## 6. Organisation du code

```
Assets/Scripts/Runtime/
  GameManager.cs   démarrage automatique, menu, enchaînement des épreuves, vies, score, HUD
  MiniGame.cs      base commune des épreuves (chrono, victoire/défaite, effets)
  LassoGame.cs     épreuve 1 : le lasso breton
  ChouchenGame.cs  épreuve 2 : le chouchen
  MouseGame.cs     épreuve 3 : la souris
  MetroBarGame.cs  épreuve 4 : que va-t-elle frotter ? (métro, ascenseur, toilettes, pigeon)
  ArtGross.cs      dessins des souris, du pigeon, de l'ascenseur et des toilettes
  FaceRubGame.cs   épreuve 5 : sur le visage
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
