// SPDX-License-Identifier: CC0-1.0
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AngularScope.Editor
{
    /// <summary>Explicit, editor-only authoring. No live writes, runtime scripts, SDK or NDMF dependency.</summary>
    public sealed class ScopeSetupWindow : EditorWindow
    {
        [SerializeField] MeshRenderer lens;
        [SerializeField] int slot;
        [SerializeField] Camera sourceCamera;
        [SerializeField] Transform animationRoot;
        [SerializeField] ScopeSpecificationSettings draft = new ScopeSpecificationSettings();
        [SerializeField] bool imported, advanced, differences, lensEffects;
        [SerializeField] ScopeSpecificationProfile profile;
        [SerializeField] bool showGuide = true;
        Vector2 scroll;
        string status;

        [MenuItem("Tools/AngularScope/Scope Setup")]
        public static void Open() { GetWindow<ScopeSetupWindow>("Scope Setup").minSize=new Vector2(430,550); }
        Material Source => lens && slot>=0 && slot<lens.sharedMaterials.Length ? lens.sharedMaterials[slot] : null;
        void OnEnable() { SceneView.duringSceneGui+=DrawGuide; }
        void OnDisable() { SceneView.duringSceneGui-=DrawGuide; SceneView.RepaintAll(); }
        void DrawGuide(SceneView view)
        {
            if(!showGuide || !imported || !lens || draft.Validate()!=null)return;
            var centre=lens.transform.TransformPoint(draft.lensCentre);
            var forward=lens.transform.TransformVector(draft.forward).normalized;
            var eye=centre-forward*CurrentRelief(lens,draft);
            float radial=.5f*(lens.transform.TransformVector(draft.right).magnitude+lens.transform.TransformVector(draft.up).magnitude);
            float pupil=draft.pupilRadiusCm*.01f/draft.referenceRadialScale;
            if(draft.linkedPupil)pupil=Mathf.Min(pupil,draft.LocalObjectiveRadius/Mathf.Max(draft.previewMagnification,1));
            var previous=Handles.color;Handles.color=Color.cyan;
            Handles.DrawLine(centre,eye);Handles.DrawWireDisc(eye,forward,pupil*radial);
            Handles.Label(eye,$"Draft eye position: {CurrentRelief(lens,draft)*100:F1} cm / {draft.previewMagnification:F1}x");
            Handles.color=previous;
        }
        public void Connect(MeshRenderer renderer,int materialSlot,Camera camera)
        {
            var loaded=Import(renderer,materialSlot);
            lens=renderer;slot=materialSlot;sourceCamera=camera;draft=loaded;imported=true;
            status="Current settings loaded into a draft. Derived specification fields are estimates, not recognized manufacturer data. Nothing applied.";
            Repaint();
            SceneView.RepaintAll();
        }
        static GUIContent Label(string title,string help) => new GUIContent(title,help);
        static GUIStyle explanationStyle, headingStyle;
        static void Help(string text)
        {
            if(explanationStyle==null)explanationStyle=new GUIStyle(EditorStyles.helpBox) {
                wordWrap=true,fontSize=12,padding=new RectOffset(10,10,8,8),margin=new RectOffset(2,2,5,7)
            };
            GUILayout.Label(text,explanationStyle);
        }
        static void Divider()
        {
            EditorGUILayout.Space(12);
            var rect=EditorGUILayout.GetControlRect(false,1);
            EditorGUI.DrawRect(rect,EditorGUIUtility.isProSkin?new Color(.38f,.38f,.38f):new Color(.65f,.65f,.65f));
            EditorGUILayout.Space(9);
        }
        static void Heading(string text)
        {
            Divider();
            if(headingStyle==null)headingStyle=new GUIStyle(EditorStyles.boldLabel) {fontSize=13};
            EditorGUILayout.LabelField(text,headingStyle);
            EditorGUILayout.Space(5);
        }
        static float Number(string label,float v,string tip)
        {
            var value=EditorGUILayout.FloatField(Label(label,tip),v);
            EditorGUILayout.Space(2);
            return value;
        }

        void OnGUI()
        {
            EditorGUIUtility.labelWidth=Mathf.Clamp(position.width*.55f,200,330);
            scroll=EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Tune a scope, not shader equations",EditorStyles.boldLabel);
            Help("Nothing is changed while you edit these fields. Import current settings, review the proposed values, then Apply. This tool adds no component to your avatar.");
            Heading("1. Connect your scope");
            if(GUILayout.Button("Use selected lens (read only)"))Try(()=>{
                if(!Selection.activeGameObject)throw new InvalidOperationException("Select the optical lens in the Hierarchy first.");
                Connect(Selection.activeGameObject.GetComponent<MeshRenderer>(),0,null);FindCamera();
            });
            EditorGUI.BeginChangeCheck();
            lens=(MeshRenderer)EditorGUILayout.ObjectField(Label("Optical lens","Rigid MeshRenderer only; the housing may remain skinned."),lens,typeof(MeshRenderer),true);
            slot=EditorGUILayout.IntField("Material slot (first = 0)",slot);
            if(EditorGUI.EndChangeCheck())imported=false;
            sourceCamera=(Camera)EditorGUILayout.ObjectField(Label("Scope image camera","The separate camera rendering into this material's square RenderTexture."),sourceCamera,typeof(Camera),true);
            using(new EditorGUI.DisabledScope(Source==null))
            {
                if(GUILayout.Button("Read current settings — keep their appearance"))Try(()=>{draft=Import(lens,slot);imported=true;status="Imported without changing material/camera. Specification fields are derived estimates, not recognized manufacturer data. Zoom endpoints must be verified.";});
                if(GUILayout.Button("Find camera using this RenderTexture"))Try(()=>FindCamera());
            }
            if(!imported)Help("Start by reading a lens material. Loading a saved preset also requires a connected lens.");
            using(new EditorGUI.DisabledScope(!imported))
            {
                Heading("2. Basic scope settings");
                draft.minimumMagnification=Number("Minimum zoom (x)",draft.minimumMagnification,"Usually 1. This sets the low end of generated zoom clips.");
                draft.maximumMagnification=Number("Maximum zoom (x)",draft.maximumMagnification,"For example 6, 8 or 10. This does not automatically update your existing animations.");
                draft.previewMagnification=EditorGUILayout.Slider(Label("Zoom to apply (x)","Writes BOTH camera FOV and shader magnification when you press Apply."),draft.previewMagnification,draft.minimumMagnification,Mathf.Max(draft.minimumMagnification,draft.maximumMagnification));
                draft.reticleFFP=EditorGUILayout.Popup("Main reticle focal plane",draft.reticleFFP?1:0,new[]{"SFP — constant apparent size","FFP — grows with zoom"})==1;
                draft.reticleReference=Number("FFP size reference (x)",draft.reticleReference,"At this zoom an FFP reticle matches its SFP size. Higher reference makes low-zoom reticles smaller.");
                Help("FFP reference is a size reference, not the maximum zoom. The illumination layer has an independent focal-plane setting under Advanced.");
                draft.useSpecifications=EditorGUILayout.ToggleLeft("Use published specifications to calculate a starting point",draft.useSpecifications);
                if(draft.useSpecifications)
                {
                    draft.objectiveDiameterMm=Number("Objective diameter (mm)",draft.objectiveDiameterMm,"Clear front-lens diameter, NOT tube diameter. Becomes an effective pupil-sizing input, not a physical lens simulation.");
                    draft.physicalSizeRatio=Number("Model size / real size",draft.physicalSizeRatio,"1 = full-size; .7 = a model 70% as large. Scales specification lengths, not field angles or the mesh itself.");
                    draft.fieldInput=(ScopeFieldInput)EditorGUILayout.EnumPopup("Field-of-view input",draft.fieldInput);
                    if(draft.fieldInput==ScopeFieldInput.WidthAtDistance)
                    {
                        draft.fieldWidthMetres=Number("Visible width (m)",draft.fieldWidthMetres,"Example: 32 m wide at 100 m. Use the distance specified by the manufacturer.");
                        draft.fieldDistanceMetres=Number("At target distance (m)",draft.fieldDistanceMetres,"100 m and 100 yd are different. Convert both width and distance to metres.");
                    }
                    else draft.fieldFullAngleDegrees=Number("Full scene FOV (degrees)",draft.fieldFullAngleDegrees,"Full angular width, NOT half-angle or the camera's Unity FOV.");
                    draft.fieldAtMagnification=Number("Measured at zoom (x)",draft.fieldAtMagnification,"Required: a field width without its measurement magnification is ambiguous.");
                    Help("This is the landscape visible through the scope, not the apparent circle at your eye. The tool approximates a constant apparent field using ONE reference FOV. Published FOV at other zooms may differ.");
                }
                draft.eyeReliefCm=Number(draft.useSpecifications?"Published eye relief (cm)":"Eye relief at saved size (cm)",draft.eyeReliefCm,"Lens-to-eye distance. Larger values place the preferred eye position farther back.");
                draft.followScale=EditorGUILayout.ToggleLeft("Eye relief follows uniform scope/avatar scaling",draft.followScale);
                Heading("3. Make the view comfortable");
                draft.linkedPupil=EditorGUILayout.ToggleLeft("Eye box narrows with zoom (otherwise fixed)",draft.linkedPupil);
                draft.pupilRadiusCm=Number(draft.linkedPupil?"Maximum eye-box radius (cm)":"Eye-box radius (cm)",draft.pupilRadiusCm,"Larger = more lateral/vertical eye tolerance. Radius, not diameter, at the saved reference size.");
                draft.farDeadZoneCm=Number("Extra rearward tolerance (cm)",draft.farDeadZoneCm,"Larger = you can move farther back before extra tunnel shading starts. There is no symmetric near cutoff.");
                draft.shadowSoftness=EditorGUILayout.Slider(Label("Softer moving shadow", "Higher = a softer transition, not a larger eye box."),draft.shadowSoftness,.01f,.4f);
                showGuide=EditorGUILayout.ToggleLeft("Show draft eye position / pupil guide in Scene view",showGuide);
                draft.reticleSize=Number("Main reticle size",draft.reticleSize,"Higher = larger reticle. FOV/spec changes do not silently resize it.");
                if(GUILayout.Button("Fit reticle texture bounds to field (optional)"))Try(()=>{
                    var calibration=Source.GetFloat("_ReticleTanHalfFov");if(calibration<=0)throw new InvalidOperationException("Reticle calibration must be positive.");
                    draft.reticleSize=draft.ApparentFieldTangent/calibration;
                    status="Draft size updated only. Fits texture edges at SFP / FFP reference; image margins and ink endpoints still matter.";
                });
                Divider();
                lensEffects=EditorGUILayout.Foldout(lensEffects,"4. Optional lens character — distortion and colour fringe",true);
                if(lensEffects)DrawLensEffects();
                Divider();
                advanced=EditorGUILayout.Foldout(advanced,"5. Advanced tuning and coordinates",true);
                EditorGUILayout.Space(5);
                if(advanced)DrawAdvanced();
                Heading("Review before applying");
                string error=ValidateConnection(lens,slot,sourceCamera,draft);
                if(error!=null)EditorGUILayout.HelpBox(error,MessageType.Warning);
                else
                {
                    EditorGUILayout.LabelField("Camera FOV at zoom endpoints",$"{draft.CameraFov(draft.minimumMagnification):F3}° → {draft.CameraFov(draft.maximumMagnification):F3}°");
                    EditorGUILayout.LabelField("Camera FOV now → after Apply",$"{sourceCamera.fieldOfView:F3}° → {draft.CameraFov(draft.previewMagnification):F3}°");
                    EditorGUILayout.LabelField("Visible width at 100 m (low / high)",$"{200*draft.ApparentFieldTangent/draft.minimumMagnification:F2} / {200*draft.ApparentFieldTangent/draft.maximumMagnification:F2} m");
                    EditorGUILayout.LabelField("Relief at saved / current size",$"{draft.ReferenceReliefMetres*100:F2} / {CurrentRelief(lens,draft)*100:F2} cm");
                    Help("Applying edits the shared material asset and the selected camera (Undo supported). Other scopes sharing that material are affected. Existing Animator curves may overwrite these settings.");
                    differences=EditorGUILayout.Foldout(differences,"Proposed changes vs current material",true);
                    if(differences)
                    {
                        foreach(var pair in Plan(draft))EditorGUILayout.LabelField(pair.Key,$"{Source.GetFloat(pair.Key):G6} → {pair.Value:G6}");
                        foreach(var pair in new Dictionary<string,Vector3>{{"_LensCenter",draft.lensCentre},{"_AxisRight",draft.right},{"_AxisUp",draft.up},{"_AxisForward",draft.forward}})
                            EditorGUILayout.LabelField(pair.Key,$"{(Vector3)Source.GetVector(pair.Key)} → {pair.Value}");
                        EditorGUILayout.LabelField("Reticle offset",$"{(Vector2)Source.GetVector("_ReticleOffset")} → {draft.reticleOffset}");
                        EditorGUILayout.LabelField("Illumination offset",$"{(Vector2)Source.GetVector("_IlluminationOffset")} → {draft.illuminationOffset}");
                    }
                }
                using(new EditorGUI.DisabledScope(error!=null || EditorApplication.isPlaying))
                    if(GUILayout.Button("Apply reviewed values to material + camera"))Try(()=>{
                        Apply(lens,slot,sourceCamera,draft);status="Applied with Undo. Material saved; scene/prefab changes are not saved automatically.";
                    });
                Heading("Save or reuse settings");
                profile=(ScopeSpecificationProfile)EditorGUILayout.ObjectField("Saved numeric preset",profile,typeof(ScopeSpecificationProfile),false);
                if(profile && GUILayout.Button("Load preset into draft — do not apply"))
                {
                    draft=JsonUtility.FromJson<ScopeSpecificationSettings>(JsonUtility.ToJson(profile.settings));
                    status="Preset loaded into draft only. Its reference scale is retained: review it for this model before Apply.";
                }
                if(GUILayout.Button("Save draft as a new preset"))Try(()=>SavePreset());
                Heading("Optional: create matching zoom animations");
                animationRoot=(Transform)EditorGUILayout.ObjectField(Label("Animation root","Choose the actual FX animation root (avatar root) or a scope root for later remapping."),animationRoot,typeof(Transform),true);
                Help("Range changes need new camera-FOV AND magnification curves. This creates three clips only; it does not replace FX, install menus, or modify existing clips.");
                using(new EditorGUI.DisabledScope(error!=null || !animationRoot || EditorApplication.isPlaying))
                    if(GUILayout.Button("Create low / high / continuous zoom clips"))Try(()=>ExportClips());
            }
            if(!string.IsNullOrEmpty(status))EditorGUILayout.HelpBox(status,MessageType.Info);
            EditorGUILayout.EndScrollView();
            if(GUI.changed)SceneView.RepaintAll();
        }

        void DrawAdvanced()
        {
            draft.illuminationFFP=EditorGUILayout.Popup("Illumination focal plane",draft.illuminationFFP?1:0,new[]{"SFP","FFP"})==1;
            draft.illuminationSize=Number("Illumination size",draft.illuminationSize,"Independent size, including small cropped-centre textures.");
            draft.reticleOffset=EditorGUILayout.Vector2Field("Main reticle offset",draft.reticleOffset);
            draft.illuminationOffset=EditorGUILayout.Vector2Field("Illumination offset",draft.illuminationOffset);
            if(draft.useSpecifications)draft.cameraOverscan=Number("Camera coverage margin (ratio)",draft.cameraOverscan,"1.0625 = 6.25% extra tangent coverage. Must be > 1.");
            else
            {
                draft.fieldTangent=Number("Apparent field (tan half angle)",draft.fieldTangent,"Existing shader field radius. Not a published scene FOV.");
                draft.cameraBaseTangent=Number("Camera base coverage (tan)",draft.cameraBaseTangent,"Must exceed apparent field. Updating this requires rebuilding zoom curves.");
                draft.effectiveObjectiveRadiusCm=Number("Effective objective radius (cm)",draft.effectiveObjectiveRadiusCm,"Empirical pupil-sizing input at reference scale; not necessarily the real front lens.");
            }
            draft.shadowCoupling=EditorGUILayout.Slider(Label("Lateral shadow sweep","Controls the stylized sweep response. Not a measured optical prescription."),draft.shadowCoupling,.05f,1);
            draft.farTightening=EditorGUILayout.Slider(Label("Far-distance tunnel strength","Higher = stronger narrowing after the rearward tolerance."),draft.farTightening,1,8);
            draft.nearEyeSensitivity=EditorGUILayout.Slider(Label("Near-eye lateral sensitivity","Optional empirical response. 0 preserves the old view; higher makes sideways/upward eye offsets more sensitive when closer than relief minus tolerance. Centred coverage is unchanged; lateral gain is capped at 8x."),draft.nearEyeSensitivity,0,4);
            draft.lensCentre=EditorGUILayout.Vector3Field("Rear lens centre (local)",draft.lensCentre);
            draft.right=EditorGUILayout.Vector3Field("Optical right axis (local)",draft.right);
            draft.up=EditorGUILayout.Vector3Field("Optical up axis (local)",draft.up);
            draft.forward=EditorGUILayout.Vector3Field("Optical forward axis (local)",draft.forward);
            EditorGUILayout.LabelField("Saved reference axial / radial scale",$"{draft.referenceAxialScale:G6} / {draft.referenceRadialScale:G6}");
            if(GUILayout.Button("Use current transform size as reference — keep cm values"))Try(()=>{
                SetReference(lens,draft);status="Draft reference updated. Centimetre values are unchanged; review before applying.";
            });
            Help("All cm/mm values are interpreted at the saved reference size. Prefer positive uniform scale. Specification-derived objective size still drives an EMPIRICAL eye-box model, not full optical refraction.");
        }
        void DrawLensEffects()
        {
            Help("All effects default to zero. These are stylized lens cues, not a measured optical prescription. Editing here changes only the draft.");
            draft.distortionLow=Number("Low-zoom distortion (%)",draft.distortionLow*100,"Positive = barrel (features move inward); negative = pincushion. Inverse radial lookup, normalized to field radius.")*.01f;
            draft.distortionHigh=Number("High-zoom distortion (%)",draft.distortionHigh*100,"Linearly blended by magnification. This is NOT an exact percentage displacement of visible features.")*.01f;
            draft.distortionMinimum=Number("Low distortion reference (x)",draft.distortionMinimum,"Zoom where the low coefficient is used; outside the range the nearest endpoint is held.");
            draft.distortionMaximum=Number("High distortion reference (x)",draft.distortionMaximum,"Must exceed the low reference. Independent of FFP size reference.");
            draft.distortReticle=EditorGUILayout.ToggleLeft("Distort main reticle + illumination with the scenery",draft.distortReticle);
            draft.sceneColourFringe=Number("Scene colour fringe (%)",draft.sceneColourFringe*100,"R/B radial sample separation at the field edge. 0 disables extra image sampling; keep subtle.")*.01f;
            draft.shadowColourFringe=Number("Moving-shadow colour fringe (%)",draft.shadowColourFringe*100,"Channel-dependent pupil-radius difference. Colours the moving shadow edge, not the fixed field stop.")*.01f;
            draft.purpleShadowFringe=EditorGUILayout.Popup("Moving-shadow colour",draft.purpleShadowFringe?1:0,new[]{"Warm — original R/G/B ordering","Purple — R+B survive outside green"})==1;
            if(GUILayout.Button("Try mild lens character in draft — not applied"))
            {
                draft.distortionLow=.04f;draft.distortionHigh=-.005f;
                draft.distortionMinimum=draft.minimumMagnification;
                draft.distortionMaximum=Mathf.Max(draft.maximumMagnification,draft.minimumMagnification+.001f);
                draft.sceneColourFringe=.004f;draft.shadowColourFringe=.012f;draft.distortReticle=true;
                status="Mild experimental values loaded into draft. Review coverage and Apply explicitly; headset validation is still needed.";
            }
            if(GUILayout.Button("Disable lens effects in draft"))
            {draft.distortionLow=draft.distortionHigh=draft.sceneColourFringe=draft.shadowColourFringe=0;}
            Help($"Required camera base tangent: > {draft.RequiredCameraTangent:F5}. Current draft: {draft.BaseCameraTangent:F5}. If coverage is insufficient, increase Advanced camera margin and regenerate zoom clips. A wider camera uses fewer RT pixels for the visible field.");
        }
        void Try(Action action) { try{action();}catch(Exception e){status=e.Message;Debug.LogWarning(e.Message);}Repaint(); }
        void FindCamera()
        {
            var rt=Source.GetTexture("_MainTex");Camera found=null;
            foreach(var c in Resources.FindObjectsOfTypeAll<Camera>())if(c.gameObject.scene.IsValid() && c.targetTexture && c.targetTexture==rt)
            {if(found)throw new InvalidOperationException("Several cameras use that RT. Select the intended scope camera manually.");found=c;}
            if(!found)throw new InvalidOperationException("No open-scene camera uses this material's RT.");sourceCamera=found;
        }
        static Material MaterialAt(MeshRenderer r,int index)
        {
            if(!r || index<0 || index>=r.sharedMaterials.Length)throw new InvalidOperationException("Select a rigid optical lens and valid material slot.");
            var m=r.sharedMaterials[index];if(!m || !m.shader || m.shader.name!="AngularScope/Optical View")throw new InvalidOperationException("The lens must use AngularScope/Optical View.");return m;
        }
        public static void SetReference(MeshRenderer r,ScopeSpecificationSettings s)
        {
            if(!r)throw new InvalidOperationException("Connect the lens first.");
            s.referenceAxialScale=r.transform.TransformVector(s.forward).magnitude;
            s.referenceRadialScale=.5f*(r.transform.TransformVector(s.right).magnitude+r.transform.TransformVector(s.up).magnitude);
            if(s.referenceAxialScale<=0 || s.referenceRadialScale<=0)throw new InvalidOperationException("Reference scale must be positive.");
        }
        public static ScopeSpecificationSettings Import(MeshRenderer r,int index)
        {
            var m=MaterialAt(r,index);var s=new ScopeSpecificationSettings();
            s.right=m.GetVector("_AxisRight");s.up=m.GetVector("_AxisUp");s.forward=m.GetVector("_AxisForward");SetReference(r,s);
            s.followScale=m.GetFloat("_EyeReliefMode")>.5f;
            s.eyeReliefCm=m.GetFloat("_EyeReliefDist")*(s.followScale?s.referenceAxialScale:1)*100;
            s.farDeadZoneCm=m.GetFloat("_EyeReliefTol")*(s.followScale?s.referenceAxialScale:1)*100;
            s.pupilRadiusCm=m.GetFloat("_ExitPupilRadius")*s.referenceRadialScale*100;
            s.effectiveObjectiveRadiusCm=m.GetFloat("_ObjectiveRadius")*s.referenceRadialScale*100;
            s.objectiveDiameterMm=s.effectiveObjectiveRadiusCm*20;
            s.fieldTangent=m.GetFloat("_FieldTanHalfAngle");s.cameraBaseTangent=m.GetFloat("_TanHalfBaseFov");
            s.cameraOverscan=s.cameraBaseTangent/s.fieldTangent;
            s.fieldInput=ScopeFieldInput.FullAngleDegrees;s.fieldFullAngleDegrees=2*Mathf.Atan(s.fieldTangent)*Mathf.Rad2Deg;
            s.linkedPupil=m.GetFloat("_ExitPupilMode")>.5f;
            s.shadowSoftness=m.GetFloat("_OpticalShadowSoftness");s.shadowCoupling=m.GetFloat("_PupilFieldCoupling");s.farTightening=m.GetFloat("_AxialVignette");
            s.nearEyeSensitivity=m.HasProperty("_NearEyeSensitivity")?m.GetFloat("_NearEyeSensitivity"):0;
            s.reticleSize=m.GetFloat("_ReticleScale");s.illuminationSize=m.GetFloat("_IlluminationScale");
            s.reticleFFP=m.GetFloat("_ReticleFocalPlane")>.5f;s.illuminationFFP=m.GetFloat("_IlluminationFocalPlane")>.5f;
            s.reticleReference=m.GetFloat("_ReticleRefMagnification");s.reticleOffset=m.GetVector("_ReticleOffset");s.illuminationOffset=m.GetVector("_IlluminationOffset");
            s.lensCentre=m.GetVector("_LensCenter");s.previewMagnification=m.GetFloat("_Magnification");
            s.minimumMagnification=Mathf.Min(1,s.previewMagnification);s.maximumMagnification=Mathf.Max(6,s.previewMagnification,s.reticleReference);
            s.distortionLow=m.GetFloat("_DistortionLow");s.distortionHigh=m.GetFloat("_DistortionHigh");
            s.distortionMinimum=m.GetFloat("_DistortionMinMagnification");s.distortionMaximum=m.GetFloat("_DistortionMaxMagnification");
            s.distortReticle=m.GetFloat("_DistortReticle")>.5f;
            s.sceneColourFringe=m.GetFloat("_SceneChromaticAberration");s.shadowColourFringe=m.GetFloat("_ShadowChromaticAberration");
            s.purpleShadowFringe=m.GetFloat("_ShadowFringePalette")>.5f;
            return s;
        }
        public static Dictionary<string,float> Plan(ScopeSpecificationSettings s)
        {
            var e=s.Validate();if(e!=null)throw new InvalidOperationException(e);
            return new Dictionary<string,float>{
                {"_EyeReliefMode",s.followScale?1:0},{"_EyeReliefDist",s.ReferenceReliefMetres/(s.followScale?s.referenceAxialScale:1)},
                {"_EyeReliefTol",s.farDeadZoneCm*.01f/(s.followScale?s.referenceAxialScale:1)},
                {"_NearEyeSensitivity",s.nearEyeSensitivity},
                {"_ExitPupilRadius",s.pupilRadiusCm*.01f/s.referenceRadialScale},{"_ObjectiveRadius",s.LocalObjectiveRadius},
                {"_ExitPupilMode",s.linkedPupil?1:0},{"_OpticalShadowSoftness",s.shadowSoftness},
                {"_PupilFieldCoupling",s.shadowCoupling},{"_AxialVignette",s.farTightening},
                {"_FieldTanHalfAngle",s.ApparentFieldTangent},{"_TanHalfBaseFov",s.BaseCameraTangent},
                {"_Magnification",s.previewMagnification},{"_ReticleScale",s.reticleSize},{"_IlluminationScale",s.illuminationSize},
                {"_ReticleFocalPlane",s.reticleFFP?1:0},{"_IlluminationFocalPlane",s.illuminationFFP?1:0},{"_ReticleRefMagnification",s.reticleReference},
                {"_DistortionLow",s.distortionLow},{"_DistortionHigh",s.distortionHigh},
                {"_DistortionMinMagnification",s.distortionMinimum},{"_DistortionMaxMagnification",s.distortionMaximum},
                {"_DistortReticle",s.distortReticle?1:0},{"_SceneChromaticAberration",s.sceneColourFringe},
                {"_ShadowChromaticAberration",s.shadowColourFringe},{"_ShadowFringePalette",s.purpleShadowFringe?1:0}};
        }
        public static string ValidateConnection(MeshRenderer r,int index,Camera camera,ScopeSpecificationSettings s)
        {
            try
            {
                var m=MaterialAt(r,index);var error=s.Validate();if(error!=null)return error;
                var scale=r.transform.lossyScale;
                if(scale.x<=0||scale.y<=0||scale.z<=0 || Mathf.Abs(scale.x-scale.y)>Mathf.Abs(scale.x)*.001f || Mathf.Abs(scale.y-scale.z)>Mathf.Abs(scale.y)*.001f)
                    return "Use positive uniform lens scale (including parent transforms).";
                if(!camera || camera.orthographic)return "Connect the perspective scope image camera.";
                if(!camera.targetTexture || camera.targetTexture!=m.GetTexture("_MainTex"))return "Camera and material must share the same RenderTexture.";
                if(camera.targetTexture.width!=camera.targetTexture.height)return "Use a square scope RenderTexture.";
                return null;
            }
            catch(Exception e){return e.Message;}
        }
        public static float CurrentRelief(MeshRenderer r,ScopeSpecificationSettings s) => s.ReferenceReliefMetres*(s.followScale?r.transform.TransformVector(s.forward).magnitude/s.referenceAxialScale:1);
        public static void Apply(MeshRenderer r,int index,Camera camera,ScopeSpecificationSettings s)
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before applying authoring settings.");
            var error=ValidateConnection(r,index,camera,s);if(error!=null)throw new InvalidOperationException(error);
            var m=MaterialAt(r,index);Undo.RecordObjects(new UnityEngine.Object[]{m,camera},"Apply scope setup");
            foreach(var p in Plan(s))m.SetFloat(p.Key,p.Value);
            m.SetVector("_LensCenter",new Vector4(s.lensCentre.x,s.lensCentre.y,s.lensCentre.z,1));
            m.SetVector("_AxisRight",s.right);m.SetVector("_AxisUp",s.up);m.SetVector("_AxisForward",s.forward);
            m.SetVector("_ReticleOffset",new Vector4(s.reticleOffset.x,s.reticleOffset.y,0,0));
            m.SetVector("_IlluminationOffset",new Vector4(s.illuminationOffset.x,s.illuminationOffset.y,0,0));
            camera.fieldOfView=s.CameraFov(s.previewMagnification);camera.aspect=1;
            EditorUtility.SetDirty(m);EditorUtility.SetDirty(camera);AssetDatabase.SaveAssetIfDirty(m);
            PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
            if(camera.gameObject.scene.IsValid())EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
            SceneView.RepaintAll();
        }
        void SavePreset()
        {
            var e=draft.Validate();if(e!=null)throw new InvalidOperationException(e);
            var path=EditorUtility.SaveFilePanelInProject("New scope preset","ScopePreset","asset","Creates a numeric editor preset; no scene references.");
            if(string.IsNullOrEmpty(path))return;
            if(!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))throw new InvalidOperationException("Choose a new filename.");
            var asset=CreateInstance<ScopeSpecificationProfile>();asset.settings=JsonUtility.FromJson<ScopeSpecificationSettings>(JsonUtility.ToJson(draft));
            AssetDatabase.CreateAsset(asset,path);AssetDatabase.SaveAssetIfDirty(asset);profile=asset;status="Preset saved. It does not install runtime components.";
        }
        void ExportClips()
        {
            if(!animationRoot || !lens.transform.IsChildOf(animationRoot) || !sourceCamera.transform.IsChildOf(animationRoot))
                throw new InvalidOperationException("Both lens and camera must be below the animation root.");
            if(slot!=0)throw new InvalidOperationException("Clip generation currently supports material slot 0 only.");
            var path=EditorUtility.SaveFilePanelInProject("New continuous zoom clip","ScopeZoomSweep","anim","A unique set of 3 new clips is created; no controller/menu changes.");
            if(string.IsNullOrEmpty(path))return;
            var stem=path.Substring(0,path.Length-5);
            var paths=new[]{path,stem+"_Low.anim",stem+"_High.anim"};
            foreach(var p in paths)if(!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(p)))throw new InvalidOperationException("An output already exists. Choose a new name.");
            var sweep=MakeZoomClip(lens,sourceCamera,animationRoot,draft,false,false);
            AssetDatabase.CreateAsset(sweep,paths[0]);AssetDatabase.CreateAsset(MakeZoomClip(lens,sourceCamera,animationRoot,draft,true,false),paths[1]);
            AssetDatabase.CreateAsset(MakeZoomClip(lens,sourceCamera,animationRoot,draft,true,true),paths[2]);
            status="Created 3 clips. Apply matching base coverage to the material, then integrate clips into your existing FX/menu. Do not blend endpoint FOVs for continuous zoom.";
        }
        public static AnimationClip MakeZoomClip(MeshRenderer lens,Camera camera,Transform root,ScopeSpecificationSettings s,bool endpoint,bool high)
        {
            var error=ValidateConnection(lens,0,camera,s);if(error!=null)throw new InvalidOperationException(error);
            if(!root||!lens.transform.IsChildOf(root)||!camera.transform.IsChildOf(root))throw new InvalidOperationException("Camera/lens must belong to the animation root.");
            var clip=new AnimationClip{frameRate=60,name=endpoint?(high?"Scope Zoom High":"Scope Zoom Low"):"Scope Zoom Sweep"};
            var mag=new AnimationCurve();var fov=new AnimationCurve();
            for(int i=0;i<=(endpoint?1:200);i++)
            {
                float t=endpoint?i:i/200f;
                float m=endpoint?(high?s.maximumMagnification:s.minimumMagnification):Mathf.Lerp(s.minimumMagnification,s.maximumMagnification,t);
                mag.AddKey(t,m);fov.AddKey(t,s.CameraFov(m));
            }
            foreach(var curve in new[]{mag,fov})for(int i=0;i<curve.length;i++)
            {AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(lens.transform,root),typeof(MeshRenderer),"material._Magnification"),mag);
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(camera.transform,root),typeof(Camera),"field of view"),fov);
            return clip;
        }
    }
}
