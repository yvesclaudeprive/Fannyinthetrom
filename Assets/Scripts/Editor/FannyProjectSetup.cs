using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build;
#endif

namespace FannyIn2Trom.EditorTools
{
    /// <summary>
    /// Configure automatiquement le projet à la première ouverture dans Unity :
    /// scène principale, réglages iPhone (nom, identifiant, paysage, icône…).
    /// Menu "Fanny" dans la barre de Unity pour relancer la configuration ou construire le projet Xcode.
    /// </summary>
    [InitializeOnLoad]
    public static class FannyProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";
        const string BundleId = "com.yateo.fannyin2trom";
        const string IconPath = "Assets/Art/Icone/icone.png";
        const string DoneKey = "FannyIn2Trom.SetupDone.v1";

        static FannyProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath) || !EditorPrefs.GetBool(DoneKey + Application.dataPath, false))
                    Setup(false);
            };
        }

        [MenuItem("Fanny/1. Configurer le projet pour iPhone", priority = 1)]
        public static void SetupMenu() { Setup(true); }

        [MenuItem("Fanny/2. Jouer dans l'éditeur", priority = 2)]
        public static void PlayInEditor()
        {
            Setup(false);
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                EditorApplication.isPlaying = true;
            }
        }

        public static void Setup(bool verbose)
        {
            EnsureScene();
            ApplyPlayerSettings();
            bool inputChanged = EnsureLegacyInput();
            EditorPrefs.SetBool(DoneKey + Application.dataPath, true);
            AssetDatabase.SaveAssets();
            Debug.Log("[Fanny in 2 Trom] Projet configuré pour iPhone (paysage, " + BundleId + ").");
            if (inputChanged)
            {
                EditorUtility.DisplayDialog("Fanny in 2 Trom",
                    "La gestion des contrôles tactiles a été activée.\nFermez puis rouvrez Unity pour qu'elle soit prise en compte.", "OK");
            }
            else if (verbose)
            {
                EditorUtility.DisplayDialog("Fanny in 2 Trom",
                    "Projet configuré !\n\n• Scène : " + ScenePath + "\n• Identifiant : " + BundleId +
                    "\n• Orientation : paysage\n\nAppuyez sur Play (ou menu Fanny > Jouer) pour tester.", "Super");
            }
        }

        static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var active = EditorSceneManager.GetActiveScene();
                bool untitled = string.IsNullOrEmpty(active.path);
                // Unity refuse d'ajouter une scène à côté d'une scène "sans titre" :
                // dans ce cas (et en mode automatique) on remplace la scène ouverte.
                bool replaceActive = Application.isBatchMode || (untitled && !active.isDirty);
                if (!replaceActive && untitled)
                {
                    Debug.LogWarning("[Fanny in 2 Trom] Enregistrez ou fermez la scène sans titre en cours, puis menu Fanny > 1. Configurer le projet.");
                    return;
                }
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, replaceActive ? NewSceneMode.Single : NewSceneMode.Additive);
                // Une caméra simple ; tout le reste (interface, jeu) est créé par le code au lancement.
                var cam = new GameObject("Main Camera");
                cam.tag = "MainCamera";
                var c = cam.AddComponent<Camera>();
                c.orthographic = true;
                c.clearFlags = CameraClearFlags.SolidColor;
                c.backgroundColor = new Color(0.08f, 0.09f, 0.16f);
                cam.transform.position = new Vector3(0, 0, -10);
                EditorSceneManager.MoveGameObjectToScene(cam, scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                if (!replaceActive) EditorSceneManager.CloseScene(scene, true);
                AssetDatabase.Refresh();
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "YATEO";
            PlayerSettings.productName = "Fanny in 2 Trom";
            PlayerSettings.bundleVersion = string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "0.1" ? "1.0" : PlayerSettings.bundleVersion;
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
#else
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, BundleId);
#endif
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            // jeu entièrement à l'horizontale
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.statusBarHidden = true;
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.buildNumber = string.IsNullOrEmpty(PlayerSettings.iOS.buildNumber) || PlayerSettings.iOS.buildNumber == "0" ? "1" : PlayerSettings.iOS.buildNumber;

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null)
            {
#if UNITY_2021_2_OR_NEWER
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
#else
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
#endif
            }
        }

        /// <summary>Le jeu lit le tactile via l'ancien système d'entrées : on s'assure qu'il est actif.</summary>
        static bool EnsureLegacyInput()
        {
            var settings = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
            if (settings == null) return false;
            var so = new SerializedObject(settings);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null || prop.intValue != 1) return false; // 0 = ancien, 2 = les deux : OK
            prop.intValue = 2;
            so.ApplyModifiedProperties();
            return true;
        }
    }
}
