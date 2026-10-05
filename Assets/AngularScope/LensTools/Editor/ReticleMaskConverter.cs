// SPDX-License-Identifier: CC0-1.0
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AngularScope.Editor
{
    // Authoring utility only. No runtime component or source-importer mutation.
    public sealed class ReticleMaskConverter : EditorWindow
    {
        Texture2D source;
        Material target;
        bool illumination;
        string status;

        [MenuItem("Tools/AngularScope/Create Lightweight Reticle Mask")]
        public static void Open()
        {
            var window = GetWindow<ReticleMaskConverter>("Reticle Mask");
            window.source = Selection.activeObject as Texture2D;
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Copies PNG alpha into a linear red-channel mask. Keeps full dimensions (including 4K), creates a new asset, and configures PC BC4 + mipmaps. Source is unchanged.", MessageType.Info);
            source = (Texture2D)EditorGUILayout.ObjectField("Source (alpha coverage)", source, typeof(Texture2D), false);
            target = (Material)EditorGUILayout.ObjectField("Assign to material (optional)", target, typeof(Material), false);
            illumination = EditorGUILayout.Toggle("Illumination layer", illumination);
            EditorGUILayout.HelpBox("Opaque PNGs produce a solid mask. Use transparent backgrounds. Cropping is not automatic: keep the same canvas to preserve alignment. For a cropped centre image, adjust layer size/offset as documented.", MessageType.None);
            using (new EditorGUI.DisabledScope(source == null))
                if (GUILayout.Button("Create BC4 Mask (Keep Resolution)"))
                {
                    var path = EditorUtility.SaveFilePanelInProject("Save new mask", source.name + "_Mask", "png", "Choose a new file; source is never overwritten.");
                    if (!string.IsNullOrEmpty(path))
                    {
                        try
                        {
                            var mask = Convert(source, path);
                            if (target != null) Assign(target, mask, illumination);
                            status = mask.width + " x " + mask.height + ", " + mask.format + ". New asset: " + path;
                            EditorGUIUtility.PingObject(mask);
                        }
                        catch (Exception ex) { status = ex.Message; Debug.LogException(ex); }
                    }
                }
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
        }

        public static Texture2D Convert(Texture2D input, string outputPath)
        {
            if (input == null) throw new ArgumentNullException("input");
            outputPath = outputPath.Replace('\\', '/');
            if (!outputPath.StartsWith("Assets/", StringComparison.Ordinal) || outputPath.Contains("../") ||
                !outputPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Output must be a new PNG inside Assets.");
            if (!Directory.Exists(Path.GetDirectoryName(outputPath)))
                throw new ArgumentException("Choose an existing output folder.");
            if (File.Exists(outputPath) || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(outputPath)))
                throw new InvalidOperationException("Output already exists. Choose a new filename.");
            var sourcePath = AssetDatabase.GetAssetPath(input);
            if (!sourcePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Select a PNG source. Other formats are not decoded by this tool.");

            // Decode source bytes, not the possibly downscaled/compressed imported texture.
            // Alpha remains linear; neither Read/Write nor source max size is changed.
            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            Texture2D result = null;
            try
            {
                if (!ImageConversion.LoadImage(decoded, File.ReadAllBytes(sourcePath), false))
                    throw new InvalidOperationException("PNG decode failed.");
                if (decoded.width > 8192 || decoded.height > 8192)
                    throw new InvalidOperationException("Maximum supported dimension is 8192.");
                var pixels = decoded.GetPixels32();
                bool hasTransparent = false;
                for (int i = 0; i < pixels.Length; i++)
                {
                    var a = pixels[i].a;
                    if (a < 255) hasTransparent = true;
                    pixels[i] = new Color32(a, 0, 0, 255);
                }
                if (!hasTransparent) throw new InvalidOperationException("No transparent pixels found: this would create a solid mask.");
                decoded.SetPixels32(pixels);
                decoded.Apply(false, false);
                File.WriteAllBytes(outputPath, decoded.EncodeToPNG());
                AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(outputPath);
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = false;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.alphaIsTransparency = false;
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Trilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.isReadable = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                int maxSize = Mathf.NextPowerOfTwo(Mathf.Max(decoded.width, decoded.height));
                importer.maxTextureSize = Mathf.Max(32, maxSize);
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                    name = "Standalone", overridden = true, maxTextureSize = Mathf.Max(32, maxSize),
                    format = TextureImporterFormat.BC4, textureCompression = TextureImporterCompression.Compressed,
                    crunchedCompression = false, compressionQuality = 100
                });
                importer.SaveAndReimport();
                result = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
                if (result == null) throw new InvalidOperationException("Generated texture did not import.");
                return result;
            }
            finally { DestroyImmediate(decoded); }
        }

        public static void Assign(Material material, Texture2D mask, bool toIllumination)
        {
            string prefix = toIllumination ? "_Illumination" : "_Reticle";
            if (material == null || !material.HasProperty(prefix + "TextureMode"))
                throw new ArgumentException("Choose an updated AngularScope material.");
            Undo.RecordObject(material, "Assign AngularScope mask");
            material.SetTexture(prefix + "Tex", mask);
            material.SetFloat(prefix + "TextureMode", 2);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            // Opacity, colour, FFP, size and offset intentionally remain unchanged.
        }
    }
}

