using System.IO;
using UnityEditor;
using UnityEngine;

namespace Astra.Editor
{
    /// <summary>
    /// Imports NASA glTF assets into Unity's model pipeline after the project opens.
    /// This lets glTFast and Draco decode the NASA models in-editor so they are
    /// available through Resources in Windows player builds.
    /// </summary>
    [InitializeOnLoad]
    internal static class AstraNASAAssetImportOnOpen
    {
        private const string ImportFlag = "ASTRA_NASA_GLB_IMPORT_V1";
        private const string ImportProgress = "ASTRA_NASA_GLB_IMPORT_PROGRESS_V1";
        private const string ModelDirectory = "Assets/Resources/NASA/MissionHardware/Processed";
        private static string[] pendingModels;
        private static int nextModel;
        private static int importedModels;
        private static int failedModels;

        static AstraNASAAssetImportOnOpen()
        {
            EditorApplication.delayCall += BeginNASAImport;
        }

        private static void BeginNASAImport()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += BeginNASAImport;
                return;
            }
            if (SessionState.GetBool(ImportFlag, false)) return;

            if (!Directory.Exists(ModelDirectory))
            {
                Debug.LogWarning("ASTRA NASA model folder is missing: " + ModelDirectory);
                return;
            }

            pendingModels = Directory.GetFiles(ModelDirectory, "*.glb", SearchOption.TopDirectoryOnly);
            System.Array.Sort(pendingModels, System.StringComparer.OrdinalIgnoreCase);
            nextModel = SessionState.GetInt(ImportProgress, 0);
            importedModels = failedModels = 0;
            EditorApplication.update += ImportNextNASAAsset;
        }

        private static void ImportNextNASAAsset()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (pendingModels == null || nextModel >= pendingModels.Length)
            {
                EditorApplication.update -= ImportNextNASAAsset;
                AssetDatabase.SaveAssets();
                SessionState.SetBool(ImportFlag, true);
                Debug.Log("ASTRA NASA model import finished: " + importedModels + " ready, " + failedModels + " failed.");
                return;
            }

            string assetPath = pendingModels[nextModel++].Replace('\\', '/');
            SessionState.SetInt(ImportProgress, nextModel);
            try
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (model == null)
                {
                    failedModels++;
                    Debug.LogError("ASTRA could not import NASA model as a Unity GameObject: " + assetPath + ". Check the glTFast/Draco import report.");
                    return;
                }

                importedModels++;
                Debug.Log("ASTRA imported NASA model: " + assetPath + " (" + model.name + ")");
            }
            catch (System.Exception exception)
            {
                failedModels++;
                Debug.LogException(exception);
            }
        }
    }
}
