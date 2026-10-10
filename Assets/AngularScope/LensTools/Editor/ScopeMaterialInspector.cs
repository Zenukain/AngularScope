// SPDX-License-Identifier: CC0-1.0
using System;
using UnityEditor;
using UnityEngine;

namespace AngularScope.Editor
{
    // Shared lookup bound. Callers decide whether to validate or clamp inputs.
    internal static class ScopeLensCoverage
    {
        internal static float RequiredTangent(float field, float low, float high, float fringe)
        {
            return field*(1+Mathf.Max(0,low,high))*(1+fringe);
        }
    }

    // Presentation only: edits happen through MaterialEditor, never automatic calibration.
    public sealed class ScopeMaterialInspector : ShaderGUI
    {
        bool connectionOpen=true, reticleOpen=true, illuminationOpen=false;
        bool viewOpen=true, effectsOpen=false, advancedOpen=false, detailOpen=false;

        public override void OnGUI(MaterialEditor editor,MaterialProperty[] properties)
        {
            EditorGUILayout.LabelField("AngularScope · Optical View",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Start with Scope Setup for camera and model calibration. This inspector fine-tunes existing settings; it never updates the camera or zoom animations automatically.",MessageType.Info);
            if(Section(ref connectionOpen,"1 · Scene & zoom","Connect the scope camera RenderTexture. Magnification must match the camera FOV driver."))
                Draw(editor,properties,"_MainTex","_Magnification","_Color");
            if(Section(ref reticleOpen,"2 · Reticle","Main aiming marks. Texture modes: RGBA colour, alpha mask, or red-channel mask (BC4). FFP scales relative to the reference magnification."))
                Draw(editor,properties,"_ReticleTex","_ReticleTextureMode","_ReticleColor","_EmissionPower","_ReticleScale","_ReticleOffset","_ReticleFocalPlane","_ReticleRefMagnification");
            if(Section(ref detailOpen,"Central reticle detail · optional","Replaces a rectangular part of the main reticle, including transparent gaps. Assign a matching high-density crop before enabling. Centre/width/height are in base-texture UV; focal plane, size, offset, tint and distortion are inherited."))
                Draw(editor,properties,"_ReticleDetailEnabled","_ReticleDetailTex","_ReticleDetailTextureMode","_ReticleDetailRegion","_ReticleDetailFeather");
            if(Section(ref illuminationOpen,"Illumination overlay","Optional independent illuminated dot or markings. Opacity 0 disables this layer. The FFP reference above is shared by both layers."))
                Draw(editor,properties,"_IlluminationOpacity","_IlluminationTex","_IlluminationTextureMode","_IlluminationColor","_IlluminationEmission","_IlluminationScale","_IlluminationOffset","_IlluminationFocalPlane");
            if(Section(ref viewOpen,"3 · Field & eyebox","LocalSpace eye relief follows the rigid lens scale; WorldMetres fixes only axial distances. Pupil radii remain local-space values. Use Scope Setup for centimetre/millimetre inputs."))
                Draw(editor,properties,"_FieldTanHalfAngle","_EyeReliefMode","_EyeReliefDist","_EyeReliefTol","_ExitPupilMode","_ObjectiveRadius","_ExitPupilRadius","_OpticalShadowSoftness","_PupilFieldCoupling","_AxialVignette","_NearEyeSensitivity","_Darkness");
            if(Section(ref effectsOpen,"4 · Lens character","Optional stylized effects. Zero strengths disable them. Positive distortion is barrel; negative is pincushion. Reticle distortion affects the base/detail composite and illumination, but not the field boundary."))
                Draw(editor,properties,"_DistortionLow","_DistortionHigh","_DistortionMinMagnification","_DistortionMaxMagnification","_DistortReticle","_SceneChromaticAberration","_ShadowChromaticAberration","_ShadowFringePalette");
            if(Section(ref advancedOpen,"5 · Calibration & diagnostics","Rigid MeshRenderer local coordinates. Camera tangent and reticle calibration are not everyday size controls. Camera tangent changes also require matching camera FOV and zoom animations. Debug: 0 normal, 1 scale, 2 distance, 3 field, 4 pupil."))
                Draw(editor,properties,"_TanHalfBaseFov","_ReticleTanHalfFov","_LensCenter","_AxisRight","_AxisUp","_AxisForward","_ScopeDebug");

            // Future properties remain accessible without breaking the categorized interface.
            foreach(var property in properties)
                if(!KnownProperty(property.name) && (property.flags & MaterialProperty.PropFlags.HideInInspector)==0)
                    editor.ShaderProperty(property,property.displayName);
            foreach(var target in editor.targets)
            {
                var material=target as Material;if(!material)continue;
                string warning=CoverageWarning(material);
                if(warning!=null)EditorGUILayout.HelpBox(material.name+": "+warning,MessageType.Warning);
                warning=DetailWarning(material);
                if(warning!=null)EditorGUILayout.HelpBox(material.name+": "+warning,MessageType.Warning);
            }
        }

        static bool Section(ref bool open,string title,string description)
        {
            EditorGUILayout.Space(8);
            var line=EditorGUILayout.GetControlRect(false,1);
            EditorGUI.DrawRect(line,new Color(.5f,.5f,.5f,.3f));
            EditorGUILayout.Space(4);
            open=EditorGUILayout.Foldout(open,title,true,EditorStyles.foldoutHeader);
            if(open)EditorGUILayout.LabelField(description,EditorStyles.wordWrappedMiniLabel);
            return open;
        }

        static void Draw(MaterialEditor editor,MaterialProperty[] properties,params string[] names)
        {
            foreach(string name in names)
            {
                var property=FindProperty(name,properties,false);
                if(property==null)continue;
                editor.ShaderProperty(property,property.displayName);
            }
        }

        // Includes collapsed sections, so they do not reappear in the fallback list.
        static bool KnownProperty(string name)
        {
            return Array.IndexOf(PropertyOrder,name)>=0;
        }

        public static readonly string[] PropertyOrder={
            "_MainTex","_Magnification","_Color",
            "_ReticleTex","_ReticleTextureMode","_ReticleColor","_EmissionPower","_ReticleScale","_ReticleOffset","_ReticleFocalPlane","_ReticleRefMagnification",
            "_ReticleDetailEnabled","_ReticleDetailTex","_ReticleDetailTextureMode","_ReticleDetailRegion","_ReticleDetailFeather",
            "_IlluminationOpacity","_IlluminationTex","_IlluminationTextureMode","_IlluminationColor","_IlluminationEmission","_IlluminationScale","_IlluminationOffset","_IlluminationFocalPlane",
            "_FieldTanHalfAngle","_EyeReliefMode","_EyeReliefDist","_EyeReliefTol","_ExitPupilMode","_ObjectiveRadius","_ExitPupilRadius","_OpticalShadowSoftness","_PupilFieldCoupling","_AxialVignette","_NearEyeSensitivity","_Darkness",
            "_DistortionLow","_DistortionHigh","_DistortionMinMagnification","_DistortionMaxMagnification","_DistortReticle","_SceneChromaticAberration","_ShadowChromaticAberration","_ShadowFringePalette",
            "_TanHalfBaseFov","_ReticleTanHalfFov","_LensCenter","_AxisRight","_AxisUp","_AxisForward","_ScopeDebug"
        };

        public static string DetailWarning(Material material)
        {
            if(!material || !material.HasProperty("_ReticleDetailEnabled") || material.GetFloat("_ReticleDetailEnabled")<.5f)return null;
            if(!material.GetTexture("_ReticleDetailTex"))return "Central detail is enabled without a texture. Assign a matching crop or disable detail; an empty enabled layer can erase base marks.";
            var r=material.GetVector("_ReticleDetailRegion");
            foreach(float v in new[]{r.x,r.y,r.z,r.w,material.GetFloat("_ReticleDetailFeather")})
                if(float.IsNaN(v)||float.IsInfinity(v))return "Detail region and feather must be finite.";
            if(r.z<=0 || r.w<=0 || r.x-r.z*.5f<0 || r.x+r.z*.5f>1 || r.y-r.w*.5f<0 || r.y+r.w*.5f>1)
                return "Detail width/height must be positive and the complete crop must fit inside base UV 0–1.";
            return null;
        }

        public static string CoverageWarning(Material material)
        {
            if(!material || !material.HasProperty("_DistortionLow"))return null;
            float field=material.GetFloat("_FieldTanHalfAngle"),camera=material.GetFloat("_TanHalfBaseFov");
            float low=material.GetFloat("_DistortionLow"),high=material.GetFloat("_DistortionHigh");
            float fringe=material.GetFloat("_SceneChromaticAberration");
            foreach(float v in new[]{field,camera,low,high,fringe})
                if(float.IsNaN(v)||float.IsInfinity(v))return "Coverage settings must be finite numbers.";
            if(field<=0 || camera<=0)return "Field and camera tangents must be positive.";
            float required=ScopeLensCoverage.RequiredTangent(field,
                Mathf.Clamp(low,-.15f,.15f),Mathf.Clamp(high,-.15f,.15f),Mathf.Clamp(fringe,0,.02f));
            if(camera>required)return null;
            return $"Scope image may clip inside the field at some zoom settings. Camera base tangent must exceed {required:F5} (currently {camera:F5}). "
                +"Use Tools > AngularScope > Scope Setup to increase camera coverage, update the actual image camera FOV and rebuild matching zoom animations. "
                +"Changing this material value alone is NOT sufficient. This inspector does not change any settings automatically.";
        }
    }
}
