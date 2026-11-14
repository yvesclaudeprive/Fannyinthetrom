using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FannyIn2Trom.EditorTools
{
    /// <summary>Génère le projet Xcode pour iPhone (à ouvrir ensuite sur un Mac).</summary>
    public static class FannyBuild
    {
        const string OutputDir = "Builds/iOS";

        [MenuItem("Fanny/3. Construire pour iPhone (projet Xcode)", priority = 20)]
        public static void BuildIOS()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                EditorUtility.DisplayDialog("Module iOS manquant",
                    "Le module « iOS Build Support » n'est pas installé.\n\n" +
                    "Unity Hub > Installs > (votre version de Unity) > roue dentée > Add modules > cochez « iOS Build Support ».", "OK");
                return;
            }

            FannyProjectSetup.Setup(false);
            Directory.CreateDirectory(OutputDir);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { FannyProjectSetup.ScenePath },
                locationPathName = OutputDir,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                string full = Path.GetFullPath(OutputDir);
                Debug.Log("[Fanny in 2 Trom] Projet Xcode généré : " + full);
                if (EditorUtility.DisplayDialog("C'est prêt !",
                        "Le projet Xcode est dans :\n" + full + "\n\nSur un Mac : ouvrez Unity-iPhone.xcodeproj, choisissez votre équipe (Signing & Capabilities), branchez l'iPhone puis cliquez ▶.",
                        "Ouvrir le dossier", "Fermer"))
                    EditorUtility.RevealInFinder(full);
            }
            else
            {
                EditorUtility.DisplayDialog("Échec de la construction",
                    "La construction a échoué (" + report.summary.totalErrors + " erreur(s)). Voir la Console de Unity.", "OK");
            }
        }

        // ------------------------------------------------------------------ CI

        /// <summary>
        /// Utilisé par GitHub Actions (game-ci/unity-builder) : configure le projet,
        /// applique le numéro de build et génère le projet Xcode.
        /// Arguments : -customBuildPath &lt;dossier&gt; -buildNumber &lt;n&gt; [-appleTeamId &lt;id&gt;]
        /// </summary>
        public static void BuildIOSCI()
        {
            string path = Arg("-customBuildPath") ?? "build/iOS/FannyIn2Trom";
            FannyProjectSetup.Setup(false);

            string buildNumber = Arg("-buildNumber");
            if (!string.IsNullOrEmpty(buildNumber)) PlayerSettings.iOS.buildNumber = buildNumber;
            string team = Arg("-appleTeamId");
            if (!string.IsNullOrEmpty(team))
            {
                PlayerSettings.iOS.appleDeveloperTeamID = team;
                PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            }
            AssetDatabase.SaveAssets();

            Directory.CreateDirectory(path);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { FannyProjectSetup.ScenePath },
                locationPathName = path,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("[Fanny in 2 Trom] Build iOS : " + report.summary.result + " (" + report.summary.totalErrors + " erreur(s)) -> " + path);
            if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        static string Arg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
