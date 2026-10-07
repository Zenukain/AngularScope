// SPDX-License-Identifier: CC0-1.0
// Angular scope display with independent field-stop and eyebox masks.
// Display shader; the external scene camera and zoom driver are separate.
// Projection is calibrated for distant scenery, not a full optical raytrace.
// See the scope README for calibration, installation and limitations.
Shader "AngularScope/Optical View"
{
    Properties
    {
        _MainTex ("Scope View", 2D) = "black" {}
        _ReticleTex ("Reticle", 2D) = "white" {}
        [Enum(ColorRGBA,0,AlphaMask,1,RedMask,2)] _ReticleTextureMode ("Reticle Texture Mode", Float) = 0
        _Color ("View Tint", Color) = (1,1,1,1)
        [HDR] _ReticleColor ("Reticle Color", Color) = (0.7,0,0.04,1)
        _EmissionPower ("Reticle Brightness", Range(0,10)) = 1.11
        _ReticleScale ("Reticle Size", Range(0.1,5)) = 0.2771281
        _ReticleOffset ("Reticle Offset", Vector) = (0,0,0,0)
        [Enum(SFP,0,FFP,1)] _ReticleFocalPlane ("Reticle Focal Plane", Float) = 0
        _IlluminationTex ("Illumination Overlay", 2D) = "white" {}
        [Enum(ColorRGBA,0,AlphaMask,1,RedMask,2)] _IlluminationTextureMode ("Illumination Texture Mode", Float) = 0
        [HDR] _IlluminationColor ("Illumination Overlay Color", Color) = (1,0,0,1)
        _IlluminationEmission ("Illumination Overlay Brightness", Range(0,10)) = 1
        _IlluminationOpacity ("Illumination Overlay Opacity (0 = Disabled)", Range(0,1)) = 0
        _IlluminationScale ("Illumination Overlay Size", Float) = 0.278
        _IlluminationOffset ("Illumination Overlay Offset", Vector) = (0,0,0,0)
        [Enum(SFP,0,FFP,1)] _IlluminationFocalPlane ("Illumination Overlay Focal Plane", Float) = 0
        _ReticleRefMagnification ("FFP Size Reference Magnification", Float) = 6
        _TanHalfBaseFov ("Tan Half Minimum-Zoom Camera FOV", Float) = 0.17
        _ReticleTanHalfFov ("Reticle Angular Calibration (tan half angle)", Float) = 0.5773503
        _AxisRight ("Local Camera Right", Vector) = (1,0,0,0)
        _AxisUp ("Local Camera Up", Vector) = (0,1,0,0)
        _AxisForward ("Local Camera Forward", Vector) = (0,0,1,0)
        _LensCenter ("Local Rear Lens Centre (Rigid MeshRenderer)", Vector) = (0,0,0,1)
        [Enum(Fixed,0,MagnificationLinked,1)] _ExitPupilMode ("Exit Pupil Mode", Float) = 1
        _Magnification ("Current Optical Magnification", Float) = 1
        _ObjectiveRadius ("Local Effective Objective Radius", Float) = 0.012
        _ExitPupilRadius ("Local Fixed / Maximum Exit Pupil Radius", Float) = 0.01875
        _AxialVignette ("Axial Vignetting Strength", Range(1,8)) = 4
        _FieldTanHalfAngle ("Optical Field Radius (tan half angle)", Range(0.05,0.5)) = 0.16
        _OpticalShadowSoftness ("Moving Shadow Edge Softness", Range(0.01,0.4)) = 0.12
        _PupilFieldCoupling ("Moving Shadow Field Coupling", Range(0.05,1)) = 0.25
        [Enum(WorldMetres,0,LocalSpace,1)] _EyeReliefMode ("Eye Relief Units", Float) = 1
        _EyeReliefDist ("Eye Relief (Selected Units)", Range(0,0.5)) = 0.12
        _EyeReliefTol ("Axial Vignetting Dead Zone", Range(0,0.2)) = 0.01
        _Darkness ("Outside View Brightness", Range(0,1)) = 0
        _DistortionLow ("Low-Zoom Distortion (+ Barrel / - Pincushion)", Range(-0.15,0.15)) = 0
        _DistortionHigh ("High-Zoom Distortion (+ Barrel / - Pincushion)", Range(-0.15,0.15)) = 0
        _DistortionMinMagnification ("Distortion Low-Zoom Reference", Float) = 1
        _DistortionMaxMagnification ("Distortion High-Zoom Reference", Float) = 6
        [Toggle] _DistortReticle ("Apply Distortion to Both Reticle Layers", Float) = 1
        _SceneChromaticAberration ("Scene Colour Fringe (Field-Edge Fraction)", Range(0,0.02)) = 0
        _ShadowChromaticAberration ("Eye-Shadow Colour Fringe (Radius Fraction)", Range(0,0.05)) = 0
        [Enum(Warm,0,Purple,1)] _ShadowFringePalette ("Eye-Shadow Fringe Palette (Stylized)", Float) = 0
        [HideInInspector] _ScopeDebug ("Scope Diagnostic Mode", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "ForceNoShadowCasting"="True" "DisableBatching"="True" }
        Cull Back
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 eyeRay : TEXCOORD0;
                float3 lensRay : TEXCOORD1;
                float2 objectScale : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            // View and reticle composition. Keep these property names stable:
            // materials and existing animations depend on the serialized ABI.
            sampler2D _MainTex, _ReticleTex, _IlluminationTex;
            float4 _MainTex_TexelSize;
            float4 _Color, _ReticleColor, _ReticleOffset;
            float4 _IlluminationColor, _IlluminationOffset;
            float _ReticleFocalPlane, _IlluminationFocalPlane, _ReticleRefMagnification;
            float _IlluminationEmission, _IlluminationOpacity, _IlluminationScale;
            float _ReticleTextureMode, _IlluminationTextureMode;
            float4 _AxisRight, _AxisUp, _AxisForward;
            float4 _LensCenter;
            float _ExitPupilRadius, _AxialVignette;
            float _ExitPupilMode, _Magnification, _ObjectiveRadius;
            float _FieldTanHalfAngle, _OpticalShadowSoftness, _PupilFieldCoupling;
            float _EmissionPower, _ReticleScale, _TanHalfBaseFov, _ReticleTanHalfFov;
            float _EyeReliefMode, _EyeReliefDist, _EyeReliefTol, _Darkness;
            float _ScopeDebug;
            float _DistortionLow, _DistortionHigh, _DistortionMinMagnification, _DistortionMaxMagnification;
            float _DistortReticle, _SceneChromaticAberration, _ShadowChromaticAberration;
            float _ShadowFringePalette;

            struct ScopeFrame
            {
                float3 right;
                float3 up;
                float3 forward;
                float radialScale;
                float axialScale;
            };

            ScopeFrame GetScopeFrame()
            {
                ScopeFrame frame;
                float3 rawRight=mul((float3x3)unity_ObjectToWorld,_AxisRight.xyz);
                float3 rawUp=mul((float3x3)unity_ObjectToWorld,_AxisUp.xyz);
                frame.right=normalize(rawRight);
                frame.up=normalize(rawUp);
                float3 rawForward=mul((float3x3)unity_ObjectToWorld,_AxisForward.xyz);
                frame.forward=normalize(rawForward);
                frame.radialScale=0.5*(length(rawRight)+length(rawUp));
                frame.axialScale=max(length(rawForward),0.000001);
                return frame;
            }

            float3 ScopeCoordinates(float3 direction, ScopeFrame frame)
            {
                return float3(dot(direction,frame.right),dot(direction,frame.up),
                              dot(direction,frame.forward));
            }

            float RectangleCoverage(float2 uv)
            {
                return step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);
            }

            float CircleCoverage(float normalizedRadius, float softness)
            {
                // Softness is a fraction of this mask's own radius. Derivatives
                // supply antialiasing without blurring the rendered scene.
                float edge=max(fwidth(normalizedRadius),softness);
                return 1-smoothstep(1-edge,1,normalizedRadius);
            }

            float2 ProjectToLensPlane(float3 ray, float3 lensRay,
                                     float axial, float lensDepth)
            {
                return ray.xy*(lensDepth/max(axial,0.00001))-lensRay.xy;
            }

            float OpticalFieldCoverage(float2 slope)
            {
                // Fixed apparent angular circle sharing the view/reticle zero.
                // This is NOT a second image zoom or a physical lens aperture.
                float radius=length(slope)/max(_FieldTanHalfAngle,0.0001);
                return CircleCoverage(radius,0.002);
            }

            float ExitPupilRadius()
            {
                float maximumRadius=max(_ExitPupilRadius,0.000001);
                if(_ExitPupilMode<0.5) return maximumRadius;
                // First-order aperture/magnification relation with an optional
                // cap. The default cap does not engage at M >= 1. Not a raytrace.
                return min(maximumRadius,
                           max(_ObjectiveRadius,0.000001)/max(_Magnification,1));
            }

            // Empirical shear normalization, independent of the adjustable
            // fixed radius / linked radius cap. Preserve the established
            // default look; this is not a measured physical pupil radius.
            static const float PupilShadowReferenceRadius=0.01875;

            float EyeShadowRadius(float2 lensXY, float2 eyeOffset,
                                    float lensDepth, float radialScale, float axialScale)
            {
                // Empirical eye-shadow model, not a physical exit-pupil raytrace.
                // Closer eye positions do not contract the soft shadow.
                // Mode 0 uses fixed world distances, independent of object scale.
                // Mode 1 converts BOTH local distance and tolerance using the
                // forward-axis scale of a rigid MeshRenderer. Skinned optical
                // displays are unsupported: vertex-baked scaling is not visible
                // here and cannot be inferred from this matrix.
                float reliefScale=_EyeReliefMode>=0.5 ? axialScale : 1;
                float farError=max(0,lensDepth-_EyeReliefDist*reliefScale-_EyeReliefTol*reliefScale);
                float coupling=_PupilFieldCoupling
                    +farError/max(lensDepth,0.00001)*_AxialVignette;
                float pupilRadius=ExitPupilRadius();
                // Shrink lateral eye tolerance, not the on-axis field of view.
                // Scaling the empirical field shear along with pupil radius
                // preserves its normalized centred-eye coverage at each zoom.
                if(_ExitPupilMode>=0.5)
                    coupling*=pupilRadius/PupilShadowReferenceRadius;
                float2 pupilXY=eyeOffset+lensXY*coupling;
                float radius=length(pupilXY)/max(pupilRadius*radialScale,0.000001);
                return radius;
            }

            float3 EyeShadowCoverage(float radius)
            {
                float coverage=CircleCoverage(radius,_OpticalShadowSoftness);
                if(_ShadowChromaticAberration<=0) return coverage.xxx;
                // A stylized wavelength-dependent pupil edge, NOT raytraced CA.
                // Reuse pupil geometry; only the three coverage tests differ.
                float fringe=clamp(_ShadowChromaticAberration,0,0.05);
                float outer=CircleCoverage(radius/(1+fringe),_OpticalShadowSoftness);
                float inner=CircleCoverage(radius/(1-fringe),_OpticalShadowSoftness);
                if(_ShadowFringePalette>=0.5)
                {
                    return float3(outer,inner,outer);
                }
                return float3(outer,coverage,inner);
            }

            float2 DistortedSlope(float2 slope, float normalizedRadiusSquared)
            {
                float span=max(_DistortionMaxMagnification-_DistortionMinMagnification,0.0001);
                float t=saturate((_Magnification-_DistortionMinMagnification)/span);
                float k=clamp(lerp(_DistortionLow,_DistortionHigh,t),-0.15,0.15);
                // Inverse image lookup: +k samples farther out, so visible
                // features move inward (barrel). Field/eye masks stay undistorted.
                return slope*(1+k*normalizedRadiusSquared);
            }

            float2 ViewUV(float2 slope)
            {
                float2 uv=0.5+slope/(2*max(_TanHalfBaseFov,0.0001));
                #if UNITY_UV_STARTS_AT_TOP
                if(_MainTex_TexelSize.y<0) uv.y=1-uv.y;
                #endif
                return uv;
            }

            fixed3 SampleScopeView(float2 slope, float normalizedRadiusSquared)
            {
                float2 uv=ViewUV(slope);
                fixed3 image=tex2D(_MainTex,uv).rgb;
                if(_SceneChromaticAberration>0)
                {
                    float fringe=clamp(_SceneChromaticAberration,0,0.02)*normalizedRadiusSquared;
                    float2 redUV=ViewUV(slope*(1+fringe));
                    float2 blueUV=ViewUV(slope*(1-fringe));
                    image.r=lerp(_Darkness,tex2D(_MainTex,redUV).r,RectangleCoverage(redUV));
                    image.b=lerp(_Darkness,tex2D(_MainTex,blueUV).b,RectangleCoverage(blueUV));
                }
                return image*_Color.rgb;
            }

            float2 ReticleProjectionUV(float2 slope, float focalPlane)
            {
                // SFP keeps a fixed apparent size. FFP grows with optical M,
                // matching that SFP size at the chosen reference magnification.
                // Both layers share the angular zero, independently of RT FOV.
                float projectionScale=1;
                if(focalPlane>0.5)
                    projectionScale=max(_ReticleRefMagnification,0.0001)
                        /max(_Magnification,0.0001);
                return 0.5+slope*projectionScale/(2*max(_ReticleTanHalfFov,0.0001));
            }

            fixed4 SampleReticleLayer(sampler2D textureSampler, float2 projectionUV,
                                     float scale, float2 offset, float textureMode)
            {
                float2 uv=(projectionUV-0.5)/max(scale,0.001)-offset+0.5;
                fixed4 sample=tex2D(textureSampler,uv);
                // Masks ignore stored RGB (including dark transparent borders).
                // RedMask is the linear single-channel/BC4 path; alpha is unused.
                if(textureMode>1.5) sample=fixed4(1,1,1,sample.r);
                else if(textureMode>0.5) sample.rgb=1;
                sample.a*=RectangleCoverage(uv);
                return sample;
            }

            fixed3 BlendReticle(fixed3 background, fixed4 reticle,
                               float3 tint, float brightness, float opacity)
            {
                // Zero RGB tint is an opaque black etched-line approximation;
                // brightness cannot turn black into an illuminated line.
                return lerp(background,reticle.rgb*tint*brightness,reticle.a*opacity);
            }

            fixed3 ComposeViewAndReticle(float2 sceneSlope, float2 reticleSlope, float radiusSquared)
            {
                fixed3 image=SampleScopeView(sceneSlope,radiusSquared);
                fixed4 reticle=SampleReticleLayer(_ReticleTex,
                    ReticleProjectionUV(reticleSlope,_ReticleFocalPlane),_ReticleScale,_ReticleOffset.xy,_ReticleTextureMode);
                fixed3 composed=BlendReticle(image,reticle,_ReticleColor.rgb,_EmissionPower,1);
                // Material-uniform condition; do not sample an unused overlay.
                // Whether this saves GPU work depends on the target compiler.
                if(_IlluminationOpacity>0)
                {
                    fixed4 illumination=SampleReticleLayer(_IlluminationTex,
                        ReticleProjectionUV(reticleSlope,_IlluminationFocalPlane),_IlluminationScale,_IlluminationOffset.xy,_IlluminationTextureMode);
                    composed=BlendReticle(composed,illumination,_IlluminationColor.rgb,
                                          _IlluminationEmission,saturate(_IlluminationOpacity));
                }
                return composed;
            }
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v,o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex=UnityObjectToClipPos(v.vertex);
                // Compute camera-relative coordinates before interpolation.
                // Subtracting two large world positions in the fragment shader
                // loses precision in worlds positioned far from the origin.
                // The frame is constant per object/instance. Project rays here
                // to avoid matrix transforms, normalization and dot products
                // per fragment; linear coordinates interpolate consistently.
                ScopeFrame frame=GetScopeFrame();
                o.objectScale=float2(frame.radialScale,frame.axialScale);
                o.eyeRay=ScopeCoordinates(mul((float3x3)UNITY_MATRIX_I_V,
                    UnityObjectToViewPos(v.vertex)),frame);
                // Centre and vertices share the rigid MeshRenderer's local
                // space. Parent that renderer to a bone for rigid attachment;
                // do not use a SkinnedMeshRenderer for the optical display.
                o.lensRay=ScopeCoordinates(mul((float3x3)UNITY_MATRIX_I_V,
                    UnityObjectToViewPos(float4(_LensCenter.xyz,1))),frame);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                // 1. Angular projection: do not change this to tune eyebox.
                float3 ray=i.eyeRay;
                float axial=ray.z;
                float2 slope=ray.xy/max(axial,0.00001);
                // Share the raw angular radius between distortion and scene CA.
                // Coverage masks deliberately retain the undistorted ray.
                float radiusSquared=dot(slope,slope)/max(_FieldTanHalfAngle*_FieldTanHalfAngle,0.00000001);
                float2 sceneSlope=DistortedSlope(slope,radiusSquared);
                float2 uv=ViewUV(sceneSlope);
                float inside=RectangleCoverage(uv)*step(0.00001,axial);
                float lensDepth=i.lensRay.z;
                if(_ScopeDebug>0.5 && _ScopeDebug<1.5) return float4(i.objectScale.xxx,1);
                if(_ScopeDebug>1.5 && _ScopeDebug<2.5) return float4((lensDepth/0.2).xxx,1);

                // 2. Independent coverage masks; both scene and reticle use
                // their product. No whole-image distance fade is applied.
                float2 lensXY=ProjectToLensPlane(ray,i.lensRay,axial,lensDepth);
                float2 eyeOffset=-i.lensRay.xy;
                // The lens mesh and housing supply the physical boundary.
                float pupilRadius=EyeShadowRadius(lensXY,eyeOffset,lensDepth,i.objectScale.x,i.objectScale.y);
                float3 pupilMask=EyeShadowCoverage(pupilRadius);
                float fieldMask=OpticalFieldCoverage(slope);
                if(_ScopeDebug>2.5 && _ScopeDebug<3.5) return float4(fieldMask.xxx,1);
                if(_ScopeDebug>3.5) return float4(pupilMask,1);
                float3 visibility=pupilMask*fieldMask*inside*step(0.00001,lensDepth);

                // 3. Composition does not affect angular zero or magnification.
                float2 reticleSlope=_DistortReticle>=0.5 ? sceneSlope : slope;
                fixed3 image=ComposeViewAndReticle(sceneSlope,reticleSlope,radiusSquared);
                return fixed4(lerp(_Darkness.xxx,image,visibility),1);
            }
            ENDCG
        }
    }
    CustomEditor "AngularScope.Editor.ScopeMaterialInspector"
}
