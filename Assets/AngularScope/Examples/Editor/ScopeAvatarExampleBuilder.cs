// SPDX-License-Identifier: CC0-1.0
using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AngularScope.Examples.Editor
{
    /// <summary>Editor-only generation of animation-driven integration fixtures.
    /// No SDK dependency or runtime MonoBehaviour is included in the generated rig.</summary>
    public static class ScopeAvatarExampleBuilder
    {
        const string Root = "Assets/AngularScope/Examples/AvatarIntegration";
        const string CameraPath = "Scene Source Camera";
        const string LensPath = "Rear Lens Display";
        const float BaseTangent = .17f;

        [MenuItem("Tools/AngularScope/Build Animation-Driven Integration Example")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Stop Play mode before generating assets.");
            Folder(Root);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/AngularScope/Examples/Prefabs/AngularScopeExample.prefab");
            if (!source) throw new InvalidOperationException("Build the procedural example first.");
            var rig = UnityEngine.Object.Instantiate(source);
            try
            {
                rig.name = "AngularScope Animated Rig";
                foreach (var script in rig.GetComponentsInChildren<MonoBehaviour>(true))
                    UnityEngine.Object.DestroyImmediate(script);
                var camera = rig.transform.Find(CameraPath).GetComponent<Camera>();
                var lens = rig.transform.Find(LensPath).GetComponent<MeshRenderer>();
                var texture = Save(new RenderTexture(camera.targetTexture) { name = "Animated Scope View" },
                    Root + "/AnimatedView.renderTexture");
                var material = new Material(lens.sharedMaterial) { name = "Animated Optical View" };
                material.SetTexture("_MainTex", texture);
                material.SetFloat("_Magnification", 1);
                material.SetFloat("_TanHalfBaseFov", BaseTangent);
                material.SetFloat("_EyeReliefMode", 1);
                lens.sharedMaterial = Save(material, Root + "/AnimatedOpticalView.mat");
                camera.targetTexture = texture;
                camera.fieldOfView = Fov(1);
                camera.aspect = 1;

                Save(Endpoint(1), Root + "/ScopeZoom1x.anim");
                Save(Endpoint(6), Root + "/ScopeZoom6x.anim");
                var sweep = new AnimationClip { name = "Scope Zoom Sweep", frameRate = 60 };
                var magnification = new AnimationCurve();
                var fov = new AnimationCurve();
                // 101 linear segments approximate the analytic FOV function.
                // A two-key linear FOV curve would give incorrect intermediate zoom.
                for (int i = 0; i <= 100; i++)
                {
                    float t = i / 100f, m = 1 + 5 * t;
                    magnification.AddKey(t, m);
                    fov.AddKey(t, Fov(m));
                }
                Linear(magnification); Linear(fov);
                Bind(sweep, magnification, fov);
                sweep = Save(sweep, Root + "/ScopeZoomSweep.anim");

                var controllerPath = Root + "/ScopeZoom.controller";
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                if (!controller)
                    controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                // This dedicated generated controller is intentionally replaced on rebuild.
                foreach (var layer in controller.layers)
                    foreach (var state in layer.stateMachine.states)
                        layer.stateMachine.RemoveState(state.state);
                controller.parameters = new[] { new AnimatorControllerParameter {
                    name = "ScopeZoom", type = AnimatorControllerParameterType.Float, defaultFloat = 0 } };
                var machine = controller.layers[0].stateMachine;
                var zoom = machine.AddState("Zoom by parameter");
                zoom.motion = sweep;
                zoom.timeParameter = "ScopeZoom";
                zoom.timeParameterActive = true;
                zoom.speed = 0;
                zoom.writeDefaultValues = false;
                machine.defaultState = zoom;
                var animator = rig.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                PrefabUtility.SaveAsPrefabAsset(rig, Root + "/AngularScopeAnimatedRig.prefab");
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(rig); }
        }

        static float Fov(float m) { return 2 * Mathf.Atan(BaseTangent / m) * Mathf.Rad2Deg; }
        static AnimationClip Endpoint(float m)
        {
            var clip = new AnimationClip { name = "Scope Zoom " + m + "x", frameRate = 60 };
            Bind(clip, AnimationCurve.Constant(0, 1, m), AnimationCurve.Constant(0, 1, Fov(m)));
            return clip;
        }
        static void Bind(AnimationClip clip, AnimationCurve magnification, AnimationCurve fov)
        {
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(LensPath, typeof(MeshRenderer), "material._Magnification"), magnification);
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(CameraPath, typeof(Camera), "field of view"), fov);
        }
        static void Linear(AnimationCurve curve)
        {
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
        }
        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            Folder(parent); AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }
        static T Save<T>(T asset, string path) where T : UnityEngine.Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!existing) { AssetDatabase.CreateAsset(asset, path); return asset; }
            EditorUtility.CopySerialized(asset, existing);
            UnityEngine.Object.DestroyImmediate(asset);
            EditorUtility.SetDirty(existing); return existing;
        }
    }
}
