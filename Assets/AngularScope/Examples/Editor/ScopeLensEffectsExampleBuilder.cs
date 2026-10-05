// SPDX-License-Identifier: CC0-1.0
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AngularScope.Examples.Editor
{
    // Creates a separate example, never changes the original demo/material.
    public static class ScopeLensEffectsExampleBuilder
    {
        const string Root="Assets/AngularScope/Examples/";
        [MenuItem("Tools/AngularScope/Build Lens Character Example")]
        public static void Build()
        {
            string source=Root+"Scenes/AngularScopeDemo.unity";
            string scenePath=Root+"Scenes/AngularScopeLensEffectsDemo.unity";
            string materialPath=Root+"Materials/LensCharacterOpticalView.mat";
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
            if(!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(scenePath)) ||
               !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(materialPath)))
                throw new InvalidOperationException("Lens character example already exists. Existing assets are not overwritten.");
            var original=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/ExampleOpticalView.mat");
            if(!original || !original.HasProperty("_DistortionLow"))throw new InvalidOperationException("Build the base example and import the lens-effects shader first.");
            var material=new Material(original){name="Lens Character Optical View"};
            material.SetFloat("_DistortionLow",.04f);material.SetFloat("_DistortionHigh",-.005f);
            material.SetFloat("_DistortionMinMagnification",1);material.SetFloat("_DistortionMaxMagnification",6);
            material.SetFloat("_DistortReticle",1);material.SetFloat("_SceneChromaticAberration",.002f);
            material.SetFloat("_ShadowChromaticAberration",.012f);material.SetFloat("_ShadowFringePalette",0);
            AssetDatabase.CreateAsset(material,materialPath);AssetDatabase.SaveAssetIfDirty(material);
            if(!AssetDatabase.CopyAsset(source,scenePath))throw new InvalidOperationException("Could not copy the base example scene.");
            var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
            try
            {
                foreach(var root in scene.GetRootGameObjects())
                foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var slots=renderer.sharedMaterials;bool changed=false;
                    for(int i=0;i<slots.Length;i++)if(slots[i]==original){slots[i]=material;changed=true;}
                    if(changed){renderer.sharedMaterials=slots;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save the lens character example.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene,true);
                if(active.IsValid())UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);
            }
            Debug.Log("Created optional lens character demo. Original demo and material preserved.");
        }
    }
}
