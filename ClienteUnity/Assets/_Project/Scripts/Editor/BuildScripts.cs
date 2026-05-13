using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DO.Editor
{
    /// <summary>
    /// Utilidades para automatizar la compilación del Servidor Dedicado.
    /// </summary>
    public static class BuildScripts
    {
        [MenuItem("DO/Build/Build Dedicated Server")]
        public static void PerformDedicatedServerBuild()
        {
            string buildPath = "Builds/Server/DO_Server.exe";
            
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new[] { "Assets/_Project/Scenes/Main_Space.unity" };
            buildPlayerOptions.locationPathName = buildPath;
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            
            // Usamos Player normal pero con compresión para que sea ligero
            buildPlayerOptions.subtarget = (int)StandaloneBuildSubtarget.Player;
            
            // Añadimos desarrollo para ver logs en la consola del servidor
            buildPlayerOptions.options = BuildOptions.CompressWithLz4HC | BuildOptions.Development;

            Debug.Log("[DO-BUILD] Iniciando compilación de Servidor Dedicado...");
            
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[DO-BUILD] ✅ Compilación exitosa: {summary.totalSize} bytes en {buildPath}");
                EditorUtility.DisplayDialog("Build Success", "Servidor Dedicado compilado en: " + buildPath, "OK");
            }

            if (summary.result == BuildResult.Failed)
            {
                Debug.LogError("[DO-BUILD] ❌ Error en la compilación.");
            }
        }
    }
}
