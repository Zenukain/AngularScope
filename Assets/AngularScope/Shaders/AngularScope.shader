// SPDX-License-Identifier: CC0-1.0
// Angular scope display with independent field-stop and eyebox masks.
// No runtime script or additional avatar parameter is required.
// Projection is calibrated for distant scenery, not a full optical raytrace.
// See the scope README for calibration, installation and limitations.
Shader "AngularScope/Optical View"
{
    Properties
    {
        _MainTex ("Scope View", 2D) = "black" {}
        _ReticleTex ("Reticle", 2D) = "white" {}
        _Color ("View Tint", Color) = (1,1,1,1)
        [HDR] _ReticleColor ("Reticle Color", Color) = (0.7,0,0.04,1)
        _EmissionPower ("Reticle Brightness", Range(0,10)) = 1.11
        _ReticleScale ("Reticle Size", Range(0.1,5)) = 0.278
        _ReticleOffset ("Reticle Offset", Vector) = (0,0,0,0)
        [Enum(SFP,0,FFP,1)] _ReticleFocalPlane ("Reticle Focal Plane", Float) = 0
        _Reticle2Tex ("Centre / Second Reticle (RGBA)", 2D) = "white" {}
        [HDR] _Reticle2Color ("Second Reticle Color", Color) = (1,0,0,1)
        _Reticle2Emission ("Second Reticle Brightness", Range(0,10)) = 1
        _Reticle2Opacity ("Second Reticle Opacity (0 = Disabled)", Range(0,1)) = 0
        _Reticle2Scale ("Second Reticle Size", Range(0.1,5)) = 0.278
        _Reticle2Offset ("Second Reticle Offset", Vector) = (0,0,0,0)
        [Enum(SFP,0,FFP,1)] _Reticle2FocalPlane ("Second Reticle Focal Plane", Float) = 0
        _ReticleRefMagnification ("FFP Size Reference Magnification", Float) = 6
        _TanHalfBaseFov ("Tan Half Minimum-Zoom Camera FOV", Float) = 0.17
        _ReticleTanHalfFov ("Reticle Angular Calibration (tan half angle)", Float) = 0.5773503
        _AxisRight ("Local Camera Right", Vector) = (1,0,0,0)
        _AxisUp ("Local Camera Up", Vector) = (0,1,0,0)
        _AxisForward ("Local Camera Forward", Vector) = (0,0,1,0)
        _LensCenter ("GPU Object-Space Rear Lens Centre", Vector) = (0,0,0,1)
        [Enum(Fixed,0,MagnificationLinked,1)] _ExitPupilMode ("Exit Pupil Mode", Float) = 1
        _Magnification ("Current Optical Magnification", Float) = 1
        _ObjectiveRadius ("GPU Object-Space Effective Objective Radius", Float) = 0.012
        _ExitPupilRadius ("GPU Object-Space Fixed / Maximum Exit Pupil Radius", Float) = 0.01875
        _AxialVignette ("Axial Vignetting Strength", Range(1,8)) = 4
        _FieldTanHalfAngle ("Optical Field Radius (tan half angle)", Range(0.05,0.5)) = 0.16
        _OpticalShadowSoftness ("Moving Shadow Edge Softness", Range(0.01,0.4)) = 0.12
        _PupilFieldCoupling ("Moving Shadow Field Coupling", Range(0.05,1)) = 0.25
        _EyeReliefDist ("Eye Relief (World Metres)", Range(0,0.5)) = 0.12
        _EyeReliefTol ("Axial Vignetting Dead Zone", Range(0,0.2)) = 0.01
        _Darkness ("Outside View Brightness", Range(0,1)) = 0
        // Legacy serialized visibility adapter, retained for old animations.
        // Neutral by default; not part of the optical calibration controls.
        [HideInInspector] _EyeBoxLimit ("Front Cover Animation Input", Float) = 0.873
        [HideInInspector] _ScopeDebug ("Scope Diagnostic Mode", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "ForceNoShadowCasting"="True" }
        Cull Back
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            // View and reticle composition. Keep these property names stable:
            // materials and existing animations depend on the serialized ABI.
            sampler2D _MainTex, _ReticleTex, _Reticle2Tex;
            float4 _MainTex_TexelSize;
            float4 _Color, _ReticleColor, _ReticleOffset;
            float4 _Reticle2Color, _Reticle2Offset;
            float _ReticleFocalPlane, _Reticle2FocalPlane, _ReticleRefMagnification;
            float _Reticle2Emission, _Reticle2Opacity, _Reticle2Scale;
            float4 _AxisRight, _AxisUp, _AxisForward;
            float4 _LensCenter;
            float _ExitPupilRadius, _AxialVignette;
            float _ExitPupilMode, _Magnification, _ObjectiveRadius;
            float _FieldTanHalfAngle, _OpticalShadowSoftness, _PupilFieldCoupling;
            float _EmissionPower, _ReticleScale, _TanHalfBaseFov, _ReticleTanHalfFov;
            float _EyeReliefDist, _EyeReliefTol, _Darkness;
            float _ScopeDebug;
            float _EyeBoxLimit;

            struct ScopeFrame
            {
                float3 right;
                float3 up;
                float3 forward;
                float radialScale;
            };

            ScopeFrame GetScopeFrame()
            {
                ScopeFrame frame;
                float3 rawRight=mul((float3x3)unity_ObjectToWorld,_AxisRight.xyz);
                float3 rawUp=mul((float3x3)unity_ObjectToWorld,_AxisUp.xyz);
                frame.right=normalize(rawRight);
                frame.up=normalize(rawUp);
                frame.forward=normalize(mul((float3x3)unity_ObjectToWorld,_AxisForward.xyz));
                frame.radialScale=0.5*(length(rawRight)+length(rawUp));
                return frame;
            }

            float2 TransverseCoordinates(float3 direction, ScopeFrame frame)
            {
                return float2(dot(direction,frame.right),dot(direction,frame.up));
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
                                     float axial, float lensDepth, ScopeFrame frame)
            {
                float3 lensPoint=ray*(lensDepth/max(axial,0.00001))-lensRay;
                return TransverseCoordinates(lensPoint,frame);
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
                // First-order aperture/magnification relation, capped by an
                // effective internal stop at low zoom. Not a lens raytrace.
                return min(maximumRadius,
                           max(_ObjectiveRadius,0.000001)/max(_Magnification,1));
            }

            float EyeShadowCoverage(float2 lensXY, float2 eyeOffset,
                                    float lensDepth, float radialScale)
            {
                // Empirical eye-shadow model, not a physical exit-pupil raytrace.
                // Closer eye positions do not contract the soft shadow.
                float farError=max(0,lensDepth-_EyeReliefDist-_EyeReliefTol);
                float coupling=_PupilFieldCoupling
                    +farError/max(lensDepth,0.00001)*_AxialVignette;
                float pupilRadius=ExitPupilRadius();
                // Shrink lateral eye tolerance, not the on-axis field of view.
                // Scaling the empirical field shear along with pupil radius
                // preserves its normalized centred-eye coverage at each zoom.
                if(_ExitPupilMode>=0.5)
                    coupling*=pupilRadius/max(_ExitPupilRadius,0.000001);
                float2 pupilXY=eyeOffset+lensXY*coupling;
                float radius=length(pupilXY)/max(pupilRadius*radialScale,0.000001);
                return CircleCoverage(radius,_OpticalShadowSoftness);
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
                                     float scale, float2 offset)
            {
                float2 uv=(projectionUV-0.5)/max(scale,0.001)-offset+0.5;
                fixed4 sample=tex2D(textureSampler,uv);
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

            fixed3 ComposeViewAndReticle(float2 imageUV, float2 slope)
            {
                #if UNITY_UV_STARTS_AT_TOP
                if(_MainTex_TexelSize.y<0) imageUV.y=1-imageUV.y;
                #endif
                fixed3 image=tex2D(_MainTex,imageUV).rgb*_Color.rgb;
                fixed4 reticle=SampleReticleLayer(_ReticleTex,
                    ReticleProjectionUV(slope,_ReticleFocalPlane),_ReticleScale,_ReticleOffset.xy);
                fixed3 composed=BlendReticle(image,reticle,_ReticleColor.rgb,_EmissionPower,1);
                // Material-uniform condition; do not sample an unused overlay.
                // Whether this saves GPU work depends on the target compiler.
                if(_Reticle2Opacity>0)
                {
                    fixed4 centre=SampleReticleLayer(_Reticle2Tex,
                        ReticleProjectionUV(slope,_Reticle2FocalPlane),_Reticle2Scale,_Reticle2Offset.xy);
                    composed=BlendReticle(composed,centre,_Reticle2Color.rgb,
                                          _Reticle2Emission,saturate(_Reticle2Opacity));
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
                o.eyeRay=mul((float3x3)UNITY_MATRIX_I_V,UnityObjectToViewPos(v.vertex));
                // Lens centre must use the same GPU object space as vertices.
                // A MeshRenderer uses native local coordinates. Skinned meshes
                // may bake bone transforms/scale into GPU vertices: calibrate
                // centre and radii in that resulting space, not raw bindpose
                // coordinates. Never apply baked scale a second time.
                o.lensRay=mul((float3x3)UNITY_MATRIX_I_V,UnityObjectToViewPos(float4(_LensCenter.xyz,1)));
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                ScopeFrame frame=GetScopeFrame();
                // 1. Angular projection: do not change this to tune eyebox.
                float3 ray=i.eyeRay;
                float axial=dot(ray,frame.forward);
                float2 slope=TransverseCoordinates(ray,frame)/max(axial,0.00001);
                float2 uv=0.5+slope/(2*max(_TanHalfBaseFov,0.0001));
                float inside=RectangleCoverage(uv)*step(0.00001,axial);
                float lensDepth=dot(i.lensRay,frame.forward);
                if(_ScopeDebug>0.5 && _ScopeDebug<1.5) return float4(frame.radialScale.xxx,1);
                if(_ScopeDebug>1.5 && _ScopeDebug<2.5) return float4((lensDepth/0.2).xxx,1);

                // 2. Independent coverage masks; both scene and reticle use
                // their product. No whole-image distance fade is applied.
                float2 lensXY=ProjectToLensPlane(ray,i.lensRay,axial,lensDepth,frame);
                float2 eyeOffset=-TransverseCoordinates(i.lensRay,frame);
                // The lens mesh and housing supply the physical boundary.
                float pupilMask=EyeShadowCoverage(lensXY,eyeOffset,lensDepth,frame.radialScale);
                float fieldMask=OpticalFieldCoverage(slope);
                if(_ScopeDebug>2.5 && _ScopeDebug<3.5) return float4(fieldMask.xxx,1);
                if(_ScopeDebug>3.5) return float4(pupilMask.xxx,1);
                float visibility=pupilMask*fieldMask*inside*step(0.00001,lensDepth);
                // 0.127 = 1 - 0.873, the original cover's open-to-closed range.
                visibility*=saturate((1-_EyeBoxLimit)/0.127);

                // 3. Composition does not affect angular zero or magnification.
                fixed3 image=ComposeViewAndReticle(uv,slope);
                return fixed4(lerp(_Darkness.xxx,image,visibility),1);
            }
            ENDCG
        }
    }
}
