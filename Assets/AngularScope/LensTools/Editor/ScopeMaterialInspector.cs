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

    // Warning-only inspector: never adjusts material, camera or animation data.
    public sealed class ScopeMaterialInspector : ShaderGUI
    {
        public override void OnGUI(MaterialEditor editor,MaterialProperty[] properties)
        {
            editor.PropertiesDefaultGUI(properties);
            foreach(var target in editor.targets)
            {
                var material=target as Material;if(!material)continue;
                string warning=CoverageWarning(material);
                if(warning!=null)EditorGUILayout.HelpBox(material.name+": "+warning,MessageType.Warning);
            }
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
