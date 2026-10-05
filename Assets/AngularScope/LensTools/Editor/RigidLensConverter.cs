// SPDX-License-Identifier: CC0-1.0
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AngularScope.Editor
{
    /// <summary>Extracts a rigid optical submesh without modifying the imported model.</summary>
    public static class RigidLensConverter
    {
        [MenuItem("Tools/AngularScope/Convert Selected Rigid Skinned Lens")]
        static void ConvertSelected()
        {
            var source = Selection.activeGameObject
                ? Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>() : null;
            if (!source) { Debug.LogError("Select the GameObject containing the skinned lens renderer."); return; }
            var slots = source.sharedMaterials.Select((m, i) => new {m, i})
                .Where(x => x.m && x.m.shader && x.m.shader.name == "AngularScope/Optical View").ToArray();
            if (slots.Length != 1) { Debug.LogError("Exactly one AngularScope optical material slot is required."); return; }
            var folder = EditorUtility.SaveFolderPanel("Save generated lens assets inside Assets", Application.dataPath, "");
            if (string.IsNullOrEmpty(folder)) return;
            folder = folder.Replace('\\', '/');
            var assets = Application.dataPath.Replace('\\', '/');
            if (folder != assets && !folder.StartsWith(assets + "/", StringComparison.Ordinal))
                throw new InvalidOperationException("Choose a folder inside this project's Assets directory.");
            folder = "Assets" + folder.Substring(assets.Length);
            try
            {
                Selection.activeGameObject = Convert(source, slots[0].i, folder).gameObject;
                Debug.Log("Rigid lens created. Rebind optical material animations to this MeshRenderer, " +
                    "update any scope setup target, and verify the lens centre/axes and distances. " +
                    "The source housing and imported mesh are preserved.", source);
            }
            catch (Exception e) { Debug.LogError(e.Message, source); }
        }

        public static MeshRenderer Convert(SkinnedMeshRenderer source, int slot, string folder)
        {
            if (!source || !source.sharedMesh) throw new InvalidOperationException("Missing source mesh.");
            if (PrefabUtility.IsPartOfPrefabAsset(source))
                throw new InvalidOperationException("Open the prefab in Prefab Mode or select a scene instance.");
            var original = source.sharedMesh;
            if (slot < 0 || slot >= original.subMeshCount || slot >= source.sharedMaterials.Length)
                throw new InvalidOperationException("Invalid optical submesh/material slot.");
            var material = source.sharedMaterials[slot];
            if (!material || !material.shader || material.shader.name != "AngularScope/Optical View")
                throw new InvalidOperationException("The chosen slot must use AngularScope/Optical View.");
            var triangles = original.GetTriangles(slot);
            var indices = triangles.Distinct().ToArray();
            if (indices.Length == 0) throw new InvalidOperationException("The optical submesh is empty.");
            var weights = original.boneWeights;
            if (weights.Length != original.vertexCount) throw new InvalidOperationException("Unsupported bone-weight layout.");
            var boneIndex = weights[indices[0]].boneIndex0;
            if (boneIndex < 0 || boneIndex >= source.bones.Length || !source.bones[boneIndex] ||
                boneIndex >= original.bindposes.Length)
                throw new InvalidOperationException("Missing lens attachment bone/bind pose.");
            foreach (var index in indices)
            {
                var w = weights[index];
                if (w.boneIndex0 != boneIndex || w.weight0 < .99999f ||
                    w.weight1 > .00001f || w.weight2 > .00001f || w.weight3 > .00001f)
                    throw new InvalidOperationException("Lens vertices must be rigidly weighted to one bone; deforming lenses are not supported.");
            }
            // Do not silently drop a blendshape that can deform this submesh.
            var deltas = new Vector3[original.vertexCount];
            for (var shape = 0; shape < original.blendShapeCount; shape++)
                for (var frame = 0; frame < original.GetBlendShapeFrameCount(shape); frame++)
                {
                    original.GetBlendShapeFrameVertices(shape, frame, deltas, null, null);
                    if (indices.Any(i => deltas[i].sqrMagnitude > 1e-14f))
                        throw new InvalidOperationException("A blendshape deforms the lens. Export a dedicated rigid lens first.");
                }
            if (!AssetDatabase.IsValidFolder(folder)) throw new InvalidOperationException("Destination folder does not exist.");
            var attach = source.bones[boneIndex];
            var bind = original.bindposes[boneIndex];
            if (bind.determinant <= 0) throw new InvalidOperationException("Mirrored/singular lens bind poses are unsupported.");
            var remap = indices.Select((v, i) => new {v, i}).ToDictionary(x => x.v, x => x.i);
            var mesh = new Mesh {name = source.name + " Optical Lens"};
            if (indices.Length > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            var vertices = original.vertices;
            mesh.vertices = indices.Select(i => bind.MultiplyPoint3x4(vertices[i])).ToArray();
            var normals = original.normals;
            if (normals.Length == original.vertexCount)
                mesh.normals = indices.Select(i => bind.inverse.transpose.MultiplyVector(normals[i]).normalized).ToArray();
            var tangents = original.tangents;
            if (tangents.Length == original.vertexCount)
                mesh.tangents = indices.Select(i => {
                    var t = bind.MultiplyVector((Vector3)tangents[i]).normalized;
                    return new Vector4(t.x, t.y, t.z, tangents[i].w);
                }).ToArray();
            var colors = original.colors;
            if (colors.Length == original.vertexCount) mesh.colors = indices.Select(i => colors[i]).ToArray();
            for (var channel = 0; channel < 8; channel++)
            {
                var uv = new List<Vector4>(); original.GetUVs(channel, uv);
                if (uv.Count == original.vertexCount) mesh.SetUVs(channel, indices.Select(i => uv[i]).ToList());
            }
            mesh.triangles = triangles.Select(i => remap[i]).ToArray();
            if (normals.Length != original.vertexCount) mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var housing = UnityEngine.Object.Instantiate(original);
            housing.name = source.name + " Without Optical Lens";
            housing.SetTriangles(new int[0], slot); // Retain slots and all other bindings.
            var opticalMaterial = new Material(material) {name = material.name + " Rigid Lens"};
            Vector3 right = attach.InverseTransformDirection(source.transform.TransformDirection((Vector3)material.GetVector("_AxisRight"))).normalized;
            Vector3 up = attach.InverseTransformDirection(source.transform.TransformDirection((Vector3)material.GetVector("_AxisUp"))).normalized;
            Vector3 forward = attach.InverseTransformDirection(source.transform.TransformDirection((Vector3)material.GetVector("_AxisForward"))).normalized;
            opticalMaterial.SetVector("_AxisRight", right);
            opticalMaterial.SetVector("_AxisUp", up);
            opticalMaterial.SetVector("_AxisForward", forward);
            // Centre the optical origin on the rear-most lens plane. Axial
            // curvature is not raytraced; this is the rigid display reference.
            var vtx = mesh.vertices;
            var rMin = vtx.Min(v => Vector3.Dot(v, right)); var rMax = vtx.Max(v => Vector3.Dot(v, right));
            var uMin = vtx.Min(v => Vector3.Dot(v, up)); var uMax = vtx.Max(v => Vector3.Dot(v, up));
            var rear = vtx.Min(v => Vector3.Dot(v, forward));
            var centre = right * ((rMin + rMax) * .5f) + up * ((uMin + uMax) * .5f) + forward * rear;
            opticalMaterial.SetVector("_LensCenter", new Vector4(centre.x, centre.y, centre.z, 1));
            AssetDatabase.CreateAsset(mesh, AssetDatabase.GenerateUniqueAssetPath(folder + "/OpticalLens.asset"));
            AssetDatabase.CreateAsset(housing, AssetDatabase.GenerateUniqueAssetPath(folder + "/HousingWithoutLens.asset"));
            AssetDatabase.CreateAsset(opticalMaterial, AssetDatabase.GenerateUniqueAssetPath(folder + "/OpticalLens.mat"));
            Undo.RecordObject(source, "Extract rigid scope lens");
            source.sharedMesh = housing;
            var remainingMaterials = source.sharedMaterials;
            remainingMaterials[slot] = null;
            source.sharedMaterials = remainingMaterials;
            var go = new GameObject("Optical Lens"); Undo.RegisterCreatedObjectUndo(go, "Extract rigid scope lens");
            go.layer = source.gameObject.layer;
            go.transform.SetParent(attach, false);
            go.SetActive(source.gameObject.activeSelf);
            var filter = go.AddComponent<MeshFilter>(); filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = opticalMaterial;
            renderer.enabled = source.enabled;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            EditorUtility.SetDirty(source);
            return renderer;
        }
    }
}
