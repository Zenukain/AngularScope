// SPDX-License-Identifier: CC0-1.0
using System;
using UnityEngine;

namespace AngularScope.Editor
{
    public enum ScopeFieldInput { WidthAtDistance, FullAngleDegrees }

    [Serializable]
    public sealed class ScopeSpecificationSettings
    {
        public float minimumMagnification = 1, maximumMagnification = 6, previewMagnification = 1;
        public bool useSpecifications;
        public float objectiveDiameterMm = 24, physicalSizeRatio = 1, eyeReliefCm = 12;
        public ScopeFieldInput fieldInput;
        public float fieldWidthMetres = 32, fieldDistanceMetres = 100, fieldAtMagnification = 1;
        public float fieldFullAngleDegrees = 18.180554f, cameraOverscan = 1.0625f;
        public float fieldTangent = .16f, cameraBaseTangent = .17f;
        public bool followScale = true, linkedPupil = true;
        public float pupilRadiusCm = 1.875f, effectiveObjectiveRadiusCm = 1.2f, farDeadZoneCm = 1;
        public float shadowSoftness = .12f, shadowCoupling = .25f, farTightening = 4;
        // Cardinal texture edge matches the default undistorted field radius.
        public float reticleSize = .16f/.5773503f, illuminationSize = .278f, reticleReference = 6;
        public bool reticleFFP, illuminationFFP;
        public Vector2 reticleOffset, illuminationOffset;
        public Vector3 lensCentre, right = Vector3.right, up = Vector3.up, forward = Vector3.forward;
        public float referenceAxialScale = 1, referenceRadialScale = 1;
        public float distortionLow, distortionHigh, sceneColourFringe, shadowColourFringe;
        public float distortionMinimum = 1, distortionMaximum = 6;
        public bool distortReticle = true;
        public bool purpleShadowFringe;

        public float ApparentFieldTangent => useSpecifications ? fieldAtMagnification *
            (fieldInput == ScopeFieldInput.WidthAtDistance ? fieldWidthMetres/(2*fieldDistanceMetres) :
                Mathf.Tan(fieldFullAngleDegrees*.5f*Mathf.Deg2Rad)) : fieldTangent;
        public float BaseCameraTangent => useSpecifications ? ApparentFieldTangent*cameraOverscan : cameraBaseTangent;
        public float ReferenceReliefMetres => eyeReliefCm*.01f*(useSpecifications ? physicalSizeRatio : 1);
        public float LocalObjectiveRadius => useSpecifications ? objectiveDiameterMm*.0005f*physicalSizeRatio/referenceRadialScale :
            effectiveObjectiveRadiusCm*.01f/referenceRadialScale;
        public float CameraFov(float magnification) => 2*Mathf.Atan(BaseCameraTangent/magnification)*Mathf.Rad2Deg;
        public float RequiredCameraTangent => ScopeLensCoverage.RequiredTangent(
            ApparentFieldTangent,distortionLow,distortionHigh,sceneColourFringe);

        public string Validate()
        {
            foreach(var v in new[]{minimumMagnification,maximumMagnification,previewMagnification,
                objectiveDiameterMm,physicalSizeRatio,eyeReliefCm,fieldWidthMetres,fieldDistanceMetres,
                fieldAtMagnification,fieldFullAngleDegrees,cameraOverscan,fieldTangent,cameraBaseTangent,
                pupilRadiusCm,effectiveObjectiveRadiusCm,farDeadZoneCm,shadowSoftness,shadowCoupling,farTightening,
                reticleSize,illuminationSize,reticleReference,referenceAxialScale,referenceRadialScale,
                lensCentre.x,lensCentre.y,lensCentre.z,right.x,right.y,right.z,up.x,up.y,up.z,forward.x,forward.y,forward.z,
                reticleOffset.x,reticleOffset.y,illuminationOffset.x,illuminationOffset.y,
                distortionLow,distortionHigh,sceneColourFringe,shadowColourFringe,distortionMinimum,distortionMaximum})
                if(float.IsNaN(v)||float.IsInfinity(v)) return "Every setting must be a finite number.";
            if(minimumMagnification<1 || maximumMagnification<minimumMagnification ||
                previewMagnification<minimumMagnification || previewMagnification>maximumMagnification)
                return "Zoom requires 1 <= minimum <= preview <= maximum.";
            if(referenceAxialScale<=0 || referenceRadialScale<=0 || eyeReliefCm<=0 || pupilRadiusCm<=0 ||
                effectiveObjectiveRadiusCm<=0 || farDeadZoneCm<0) return "Lengths/scales must be positive; the far dead zone can be zero.";
            if(useSpecifications && (objectiveDiameterMm<=0 || physicalSizeRatio<=0 || fieldWidthMetres<=0 || fieldDistanceMetres<=0 ||
                fieldAtMagnification<minimumMagnification || fieldAtMagnification>maximumMagnification ||
                fieldFullAngleDegrees<=0 || fieldFullAngleDegrees>=179 || cameraOverscan<=1))
                return "Specification values must be positive, FOV must be measured within the zoom range, and overscan must exceed 1.";
            if(ApparentFieldTangent<=0 || BaseCameraTangent<=ApparentFieldTangent || BaseCameraTangent>10)
                return "The camera must cover a little more than the visible field; check FOV and overscan.";
            if(shadowSoftness<.01f||shadowSoftness>.4f||shadowCoupling<.05f||shadowCoupling>1||farTightening<1||farTightening>8)
                return "Shadow tuning is outside the shader's supported ranges.";
            if(reticleSize<.001f || illuminationSize<.001f || reticleReference<=0) return "Reticle sizes/reference must be positive (size >= .001).";
            if(Mathf.Abs(distortionLow)>.15f || Mathf.Abs(distortionHigh)>.15f ||
                sceneColourFringe<0 || sceneColourFringe>.02f || shadowColourFringe<0 || shadowColourFringe>.05f ||
                distortionMinimum<1 || distortionMaximum<=distortionMinimum)
                return "Lens effects require distortion within +/-15%, scene fringe 0–2%, shadow fringe 0–5%, and an increasing zoom reference range.";
            if(BaseCameraTangent<=RequiredCameraTangent)
                return "Increase camera coverage margin for distortion/colour fringe; then rebuild matching zoom clips.";
            if(right.sqrMagnitude<1e-8f||up.sqrMagnitude<1e-8f||forward.sqrMagnitude<1e-8f ||
                Mathf.Abs(Vector3.Dot(right.normalized,up.normalized))>.001f ||
                Mathf.Abs(Vector3.Dot(right.normalized,forward.normalized))>.001f ||
                Mathf.Abs(Vector3.Dot(up.normalized,forward.normalized))>.001f) return "Optical axes must be nonzero and perpendicular.";
            return null;
        }
    }

    // Editor-only preset: stores numbers, never scene objects or avatar components.
    public sealed class ScopeSpecificationProfile : ScriptableObject
    {
        public ScopeSpecificationSettings settings = new ScopeSpecificationSettings();
    }
}
