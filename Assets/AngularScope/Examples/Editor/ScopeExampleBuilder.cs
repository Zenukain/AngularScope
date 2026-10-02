// SPDX-License-Identifier: CC0-1.0
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AngularScope.Examples.Editor
{
    /// <summary>Self-contained procedural fixtures; no purchased mesh/image dependency.</summary>
    public static class ScopeExampleBuilder
    {
        const string Root = "Assets/AngularScope/Examples";

        [MenuItem("Tools/AngularScope/Rebuild Example Assets and Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Stop Play mode before rebuilding example assets.");
            var previous = SceneManager.GetActiveScene();
            if (previous.path == Root + "/Scenes/AngularScopeDemo.unity" && previous.isDirty)
                throw new InvalidOperationException("Save or discard your demo scene edits before rebuilding.");
            Folder(Root + "/Meshes"); Folder(Root + "/Materials");
            Folder(Root + "/Textures"); Folder(Root + "/Prefabs"); Folder(Root + "/Scenes");
            var shader = Shader.Find("AngularScope/Optical View");
            if (!shader) throw new InvalidOperationException("Import the AngularScope shader first.");
            var shell = Material("Housing", "Standard", new Color(.12f, .15f, .18f));
            shell.SetFloat("_Metallic", .15f); shell.SetFloat("_Glossiness", .35f);
            var mount = Material("Mount", "Standard", new Color(.22f, .24f, .26f));
            var rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
            rt.name = "Example View"; rt.wrapMode = TextureWrapMode.Clamp;
            rt.filterMode = FilterMode.Bilinear; rt.antiAliasing = 1;
            rt = SaveAsset(rt, Root + "/Textures/ExampleView.renderTexture");
            var lensMaterial = new Material(shader) { name = "Example Optical View" };
            lensMaterial.SetTexture("_MainTex", rt);
            lensMaterial.SetTexture("_ReticleTex", Reticle(false));
            lensMaterial.SetTexture("_IlluminationTex", Reticle(true));
            lensMaterial.SetColor("_ReticleColor", Color.black);
            lensMaterial.SetColor("_IlluminationColor", Color.red);
            lensMaterial.SetFloat("_IlluminationOpacity", 1);
            lensMaterial.SetFloat("_IlluminationEmission", 1);
            lensMaterial.SetFloat("_EyeReliefMode", 1);
            lensMaterial.SetFloat("_ReticleScale", .278f);
            lensMaterial.SetFloat("_IlluminationScale", .278f);
            lensMaterial = SaveAsset(lensMaterial, Root + "/Materials/ExampleOpticalView.mat");

            var scope = new GameObject("AngularScope Example");
            try
            {
                MeshObject(scope, "Eyepiece", Tube(.023f, .019f, 0, .06f), shell);
                MeshObject(scope, "Main Tube", Tube(.017f, .014f, .06f, .22f), shell);
                MeshObject(scope, "Objective Housing", Tube(.022f, .014f, .22f, .28f), shell);
                Cube(scope, "Mount Base", new Vector3(0, -.036f, .14f), new Vector3(.048f, .012f, .12f), mount);
                Cube(scope, "Rear Mount", new Vector3(0, -.026f, .085f), new Vector3(.025f, .02f, .02f), mount);
                Cube(scope, "Front Mount", new Vector3(0, -.026f, .195f), new Vector3(.025f, .02f, .02f), mount);
                var turret = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                turret.name = "Elevation Knob"; turret.transform.SetParent(scope.transform, false);
                turret.transform.localPosition = new Vector3(0, .021f, .14f);
                turret.transform.localScale = new Vector3(.025f, .01f, .025f);
                turret.GetComponent<Renderer>().sharedMaterial = mount;
                UnityEngine.Object.DestroyImmediate(turret.GetComponent<Collider>());
                var lens = MeshObject(scope, "Rear Lens Display", Disc(.019f), lensMaterial);
                // Layer 31 is a demonstration convention, not a named-layer dependency.
                lens.layer = 31;
                var imageObject = new GameObject("Scene Source Camera");
                imageObject.transform.SetParent(scope.transform, false);
                imageObject.transform.localPosition = new Vector3(0, 0, .29f);
                var imageCamera = imageObject.AddComponent<Camera>();
                imageCamera.fieldOfView = 2 * Mathf.Atan(.17f) * Mathf.Rad2Deg;
                imageCamera.aspect = 1; imageCamera.nearClipPlane = .01f; imageCamera.farClipPlane = 200;
                imageCamera.clearFlags = CameraClearFlags.SolidColor;
                imageCamera.backgroundColor = new Color(.45f, .65f, .82f);
                imageCamera.cullingMask = ~(1 << 31); imageCamera.targetTexture = rt;
                imageCamera.allowHDR = false; imageCamera.allowMSAA = false; imageCamera.depth = -10;
                var control = scope.AddComponent<ScopeExampleController>();
                control.imageCamera = imageCamera; control.lensRenderer = lens.GetComponent<Renderer>();
                PrefabUtility.SaveAsPrefabAsset(scope, Root + "/Prefabs/AngularScopeExample.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(scope); }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            if (previous.IsValid() && previous.path == Root + "/Scenes/AngularScopeDemo.unity")
                EditorSceneManager.CloseScene(previous, true);
            var placed = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                Root + "/Prefabs/AngularScopeExample.prefab"), scene);
            placed.transform.position = new Vector3(0, 1.5f, 0);
            var viewerObject = new GameObject("Main Camera"); viewerObject.tag = "MainCamera";
            viewerObject.transform.position = new Vector3(0, 1.5f, -.12f);
            var viewer = viewerObject.AddComponent<Camera>(); viewer.fieldOfView = 28;
            viewer.nearClipPlane = .001f; viewer.farClipPlane = 200;
            viewer.clearFlags = CameraClearFlags.SolidColor;
            viewer.backgroundColor = new Color(.45f, .65f, .82f);
            viewerObject.AddComponent<AudioListener>();
            placed.GetComponent<ScopeExampleController>().viewer = viewer;
            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.1f;
            sun.transform.rotation = Quaternion.Euler(40, -30, 0);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.5f, .5f, .5f);
            var stage = new GameObject("Procedural Test Range");
            var ground = Material("Ground", "Standard", new Color(.38f, .43f, .36f));
            Cube(stage, "Ground", new Vector3(0, -.1f, 25), new Vector3(60, .2f, 70), ground);
            var white = Material("Target White", "Unlit/Color", Color.white);
            var black = Material("Target Black", "Unlit/Color", new Color(.08f, .08f, .08f));
            var blue = Material("Target Blue", "Unlit/Color", new Color(.15f, .45f, .85f));
            var yellow = Material("Target Yellow", "Unlit/Color", new Color(.95f, .65f, .15f));
            Cube(stage, "20m Target Board", new Vector3(0, 1.5f, 20), new Vector3(3.6f, 2.4f, .06f), white);
            for (int row = 0; row < 6; row++)
                for (int col = 0; col < 9; col++)
                    if ((row + col) % 2 == 0)
                        Cube(stage, "Contrast Square", new Vector3(-1.6f + col * .4f, .5f + row * .4f, 19.96f),
                            new Vector3(.4f, .4f, .01f), black);
            Cube(stage, "Aim Centre", new Vector3(0, 1.5f, 19.94f), new Vector3(.14f, .14f, .01f), yellow);
            Cube(stage, "Blue Landmark", new Vector3(-2.4f, 1.3f, 12), new Vector3(.6f, 2.6f, .6f), blue);
            Cube(stage, "Yellow Landmark", new Vector3(3, 1.8f, 30), new Vector3(.8f, 3.6f, .8f), yellow);
            EditorSceneManager.SaveScene(scene, Root + "/Scenes/AngularScopeDemo.unity");
            // Leave the newly built demo active without replacing/saving the user's scene.
            // Prevent the original scene from contaminating the demo's camera render.
            if (previous.IsValid() && !previous.isDirty)
                EditorSceneManager.CloseScene(previous, true);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = placed;
            if (SceneView.lastActiveSceneView)
                SceneView.lastActiveSceneView.LookAt(placed.transform.position + new Vector3(0, 0, .14f),
                    Quaternion.Euler(15, 140, 0), .5f);
        }

        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/')); Folder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }

        static T SaveAsset<T>(T asset, string path) where T : UnityEngine.Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!existing) { AssetDatabase.CreateAsset(asset, path); return asset; }
            EditorUtility.CopySerialized(asset, existing); UnityEngine.Object.DestroyImmediate(asset);
            EditorUtility.SetDirty(existing); return existing;
        }

        static Material Material(string name, string shaderName, Color color)
        {
            var shader = Shader.Find(shaderName);
            if (!shader) throw new InvalidOperationException("Shader not found: " + shaderName);
            return SaveAsset(new Material(shader) { name = name, color = color }, Root + "/Materials/" + name.Replace(" ", "") + ".mat");
        }

        static GameObject Cube(GameObject parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name; obj.transform.SetParent(parent.transform, false);
            obj.transform.localPosition = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>()); return obj;
        }

        static GameObject MeshObject(GameObject parent, string name, Mesh mesh, Material material)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent.transform, false);
            obj.AddComponent<MeshFilter>().sharedMesh = SaveAsset(mesh, Root + "/Meshes/" + name.Replace(" ", "") + ".asset");
            obj.AddComponent<MeshRenderer>().sharedMaterial = material; return obj;
        }

        static Mesh Disc(float radius)
        {
            const int segments = 64;
            var v = new Vector3[segments + 1]; var t = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                v[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
                t[i * 3] = 0; t[i * 3 + 1] = (i + 1) % segments + 1; t[i * 3 + 2] = i + 1;
            }
            var mesh = new Mesh { name = "Rear Lens Display", vertices = v, triangles = t };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        static Mesh Tube(float outer, float inner, float start, float end)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            Action<Vector3, Vector3, Vector3, Vector3> quad = (a, b, c, d) =>
            {
                int n = v.Count; v.AddRange(new[] { a, b, c, d });
                t.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
            };
            for (int i = 0; i < 64; i++)
            {
                float a = i * Mathf.PI * 2 / 64, b = (i + 1) * Mathf.PI * 2 / 64;
                Vector3 ra = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                Vector3 rb = new Vector3(Mathf.Cos(b), Mathf.Sin(b), 0);
                Vector3 z0 = Vector3.forward * start, z1 = Vector3.forward * end;
                quad(ra * outer + z0, rb * outer + z0, rb * outer + z1, ra * outer + z1);
                quad(ra * inner + z0, ra * inner + z1, rb * inner + z1, rb * inner + z0);
                quad(ra * outer + z0, ra * inner + z0, rb * inner + z0, rb * outer + z0);
                quad(ra * outer + z1, rb * outer + z1, rb * inner + z1, ra * inner + z1);
            }
            var mesh = new Mesh { name = "Hollow Scope Tube", vertices = v.ToArray(), triangles = t.ToArray() };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        static Texture2D Reticle(bool centre)
        {
            const int size = 512; var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                int dx = x - size / 2, dy = y - size / 2;
                bool ink = centre ? dx * dx + dy * dy <= 36
                    : ((Mathf.Abs(dx) <= 2 && Mathf.Abs(dy) >= 12 && Mathf.Abs(dy) <= 210)
                        || (Mathf.Abs(dy) <= 2 && Mathf.Abs(dx) >= 12 && Mathf.Abs(dx) <= 210)
                        || (Mathf.Abs(dx) <= 10 && Mathf.Abs(dy) % 35 <= 2 && Mathf.Abs(dy) <= 175 && Mathf.Abs(dy) >= 30)
                        || (Mathf.Abs(dy) <= 10 && Mathf.Abs(dx) % 35 <= 2 && Mathf.Abs(dx) <= 175 && Mathf.Abs(dx) >= 30));
                pixels[y * size + x] = new Color(1, 1, 1, ink ? 1 : 0);
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            texture.name = centre ? "Centre Dot" : "Etched Cross";
            texture.wrapMode = TextureWrapMode.Clamp; texture.filterMode = FilterMode.Trilinear;
            texture.SetPixels(pixels); texture.Apply(true);
            return SaveAsset(texture, Root + "/Textures/" + (centre ? "CentreDot.asset" : "EtchedCross.asset"));
        }
    }
}
