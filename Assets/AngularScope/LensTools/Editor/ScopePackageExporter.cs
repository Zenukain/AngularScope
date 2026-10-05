// SPDX-License-Identifier: CC0-1.0
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AngularScope.Editor
{
    public static class ScopePackageExporter
    {
        [MenuItem("Tools/AngularScope/Export Distribution Packages")]
        public static void Export()
        {
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Exports/AngularScope"));
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-angularScopeOutput") output = Path.GetFullPath(args[i + 1]);
            Directory.CreateDirectory(output);
            const string root = "Assets/AngularScope";
            var shaderOnly = new[] {root + "/Shaders", root + "/LensTools", root + "/Documentation",
                root + "/LICENSE.md", root + "/CC0-1.0.txt"};
            foreach (var path in shaderOnly)
                if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
                    throw new InvalidOperationException("Missing distribution asset: " + path);
            AssetDatabase.ExportPackage(shaderOnly, Path.Combine(output, "AngularScope_ShaderOnly.unitypackage"), ExportPackageOptions.Recurse);
            AssetDatabase.ExportPackage(new[] {root}, Path.Combine(output, "AngularScope_WithExample.unitypackage"), ExportPackageOptions.Recurse);
            Debug.Log("AngularScope distribution packages exported to " + output);
        }
    }
}
