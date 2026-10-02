# AngularScope - optical view shader

A configurable scope-display shader for Unity's Built-in Render Pipeline.
Shader menu: `AngularScope/Optical View`.
Tested editor: Unity 2022.3.22f1, PC rendering.

## Package and license

The ShaderOnly package includes shader source, documentation and CC0 license
files, with no model, texture, camera or animation assets. The WithExample
package additionally includes procedural fixtures and animation-driven zoom.
See [PC avatar integration](AvatarIntegration.md) for the integration workflow.
No proprietary scope package, Modular Avatar or lilToon is needed by the
shader itself. Your scene-source camera and avatar integration are separate.

Independently authored shader/documentation are dedicated under CC0-1.0;
see LICENSE.md and CC0-1.0.txt. This does not relicense third-party assets,
Unity's externally provided headers, or models/images you use with it.

## Platform and requirements

Use a PC Built-in Render Pipeline project. VRChat PC avatars are the intended
integration target. This is NOT a Quest/Android avatar shader. URP/HDRP
compatibility has not been established. Stereo macros are present, but one
mono camera texture cannot reproduce exact near-field binocular parallax.

Recommended starting point: a normal MeshRenderer on a rigid lens mesh,
parented to one bone/transform. A SkinnedMeshRenderer is possible with
GPU-coordinate calibration, but arbitrary deforming lens skinning is not
supported as a general optical model. Prefer uniform positive object scale.
Axes must be nonzero, mutually perpendicular, and aligned with the image camera.

## Quick start

For a working scene instead of manual setup, import the optional
AngularScope_WithExample.unitypackage, open
Assets/AngularScope/Examples/Scenes/AngularScopeDemo.unity and press Play.
See Assets/AngularScope/Examples/README.md for controls and prefab structure.
The shader-only package does not include these example assets or scripts.

1. Create a square mono RenderTexture and a perspective Camera rendering to it.
   Keep the camera aligned with the scope's optical direction. Exclude the
   lens/display mesh from that camera's culling mask to avoid feedback.
2. Create a material using AngularScope/Optical View. Assign the camera texture
   to _MainTex and a white-on-transparent RGBA reticle to _ReticleTex.
   A missing reticle texture uses an opaque white placeholder, not an empty
   layer. To show no main reticle, assign a fully transparent texture.
3. Assign that material to a lens display mesh. The mesh face must point
   toward the observer: back faces are culled. The housing/mesh supplies
   the physical boundary; use your own model or a simple correctly sized disc.
4. Default optical axes are local right +X, up +Y, forward +Z. The observer
   looks from behind the lens along +Z. _LensCenter defaults to (0,0,0):
   put the rear-lens centre at that origin or enter its actual coordinates.
5. Start at world eye distance _EyeReliefDist=0.12 metres. Calibrate lens
   centre and pupil radii for your model rather than treating defaults as
   measured lens specifications.
6. For 1x, set Camera.fieldOfView=19.296091 degrees and _Magnification=1.
   The camera aspect ratio must be 1. At any magnification M, set camera FOV
   to 2*atan(_TanHalfBaseFov/M) in degrees AND _Magnification=M.
7. Test both eyes, zoom extremes and horizontal/vertical/near/far eye movement.
   Leave _ScopeDebug=0 in normal use. Camera activation/network integration
   must be handled by your own application/avatar setup.

The shader displays an external scene texture; it does NOT replace the
scene camera or produce real zoom by itself. No runtime script is shipped.
The scene camera's extra rendering can dominate total performance cost.

## Projection and coverage

Image projection uses _TanHalfBaseFov (default 0.17). Minimum camera FOV is
2*atan(0.17) = 19.296091 degrees; at 6x it is 3.245893 degrees.
Keep _FieldTanHalfAngle (default 0.16) smaller than the camera tangent coverage.
If a fixed black ring intrudes at the best eye position, increase the field
radius, but preserve camera overscan. Changing camera coverage requires
changing image calibration and every zoom FOV sample consistently.

Exactly two optical coverage terms remain: a sharp apparent-angular field
stop and a soft moving eye-position shadow. Both scene and reticle use their
product. There is no extra lens-aperture mask or whole-image distance fade.
_OpticalShadowSoftness controls the moving edge; _PupilFieldCoupling and
_AxialVignette are empirical tuning controls, not physical lens prescriptions.

## Reticle and illumination overlay

Reticle layer: _ReticleTex, _ReticleColor, _EmissionPower, _ReticleScale,
_ReticleOffset and _ReticleFocalPlane.
Optional illumination overlay: _IlluminationTex, _IlluminationColor, _IlluminationEmission,
_IlluminationScale, _IlluminationOffset, _IlluminationFocalPlane and _IlluminationOpacity.
Opacity 0 disables the illumination overlay by default; assign an RGBA centre image
before setting it to 1. Its white placeholder would obscure the scene.

For black etched graduations plus illuminated red centre, supply separate
white-on-transparent images, tint the main black (0,0,0), tint the illumination overlay red,
and enable its opacity. Black multiplied by brightness remains black.
This approximates etched-line contrast through image composition; it does
not simulate scattering or absorption in illuminated glass.
Texture alpha defines both layers' coverage. Overlay opacity adds a strength
control; tint color alpha is not used. The illumination overlay is composited over the reticle.

Each focal-plane selector: 0 = SFP, 1 = FFP. SFP apparent size stays fixed.
FFP apparent size scales with optical M, matching SFP size at the shared
_ReticleRefMagnification. This is a positive Float, not limited to 6x:
for a 10x size reference, set it to 10. A mark at 1x then has one-tenth its
reference apparent size. Both layers may use different focal-plane selections.

The illumination overlay is not restricted to a dot: rings, horseshoes and
other illuminated patterns work too. For pre-release materials or animations
using the old `_Reticle2` prefix, rename those property keys/bindings to
`_Illumination` when upgrading; existing values do not migrate automatically.
For consistent geometry, size-reference calibration must be deliberate.
At low zoom, thin FFP marks can become subpixel and lose visibility; choose
appropriate line widths, texture filtering and mipmaps for your target image.

_ReticleTanHalfFov (default 0.5773503) and each layer's size/offset are
independent of camera overscan. Do not change reticle calibration merely to
crop the scene camera. Supplied images are not automatically split, and
the shader does not calibrate arbitrary graduations in MOA/MRAD.

## Exit pupil and zoom integration

_ExitPupilMode=1 (MagnificationLinked) is the new-material default:
radius = min(_ExitPupilRadius, _ObjectiveRadius / max(M,1)).
_ExitPupilMode=0 (Fixed) preserves _ExitPupilRadius at every zoom.
_ObjectiveRadius is an EFFECTIVE radius, not diameter; default 0.012 is an
illustrative 24mm diameter at metric unit scale. The default radius cap
0.01875 is an adjustable empirical value, not a measured human pupil.
The first-order pupil relation is described in
[Nikon's optics guide](https://imaging.nikon.com/sport-optics/guide/binoculars/basic/basic_05/).

The linked mode reduces lateral eye tolerance while preserving the centred
apparent field in this empirical model. It is not a full exit-pupil raytrace.
_Magnification is actual optical magnification, not a 0-to-1 menu position.
Keep camera FOV and material._Magnification driven by the SAME zoom control.
For any camera FOV: M = _TanHalfBaseFov / tan(FOV/2).
Use radians in tan/atan, then convert to degrees for Camera.fieldOfView.
Fixed pupil mode ignores M only for pupil sizing; FFP reticles still need M.

You must provide your own script/animation/controller. No zoom clip is
included. Unity material animations may use a renderer MaterialPropertyBlock;
reading sharedMaterial alone does not show the animated property value.
An existing application zoom control can drive both values without adding
another synced parameter. Remote camera availability is platform-dependent.

## GPU-coordinate calibration and diagnostics

For a normal MeshRenderer, _LensCenter is in native local mesh coordinates.
Pupil/objective radii use those units; the object's transverse world scale
converts them to world lengths. `_EyeReliefMode` selects the units for BOTH
`_EyeReliefDist` and `_EyeReliefTol`:

- **WorldMetres (0, shader default):** preserves existing materials. A value
  of 0.12 stays 12 cm regardless of object scale.
- **GPUObjectSpace (1, procedural example preset):** lengths use the same GPU
  object units as the lens centre. Forward-axis object-to-world scale converts
  them to world lengths. On a unit-scale MeshRenderer, 0.12 is 12 cm; at uniform
  scale 2 it becomes 24 cm, and the axial tolerance scales with it.

For an existing uniformly scaled MeshRenderer, preserve the present world
distance when switching to mode 1 by dividing BOTH distance and tolerance
by its current forward-axis world scale. Do not simply switch units on a
non-unit-scale model without recalibrating. Axes should be unit-length and
orthogonal; nonuniform scale/shear is not a generally supported optical rig.

Dynamic batching is disabled because the shader depends on each lens's object
coordinate frame. Do not mark the optical lens Static for static batching.
This tag is not a blanket prohibition on every Unity batching/instancing path.

For skinned renderers, GPU vertex coordinates may already contain bone
transforms and scale. Lens centre and radii must use THAT same resulting
coordinate space. Do not blindly use untransformed source vertices or apply
baked scale twice. In mode 1, calibrate axial lengths in that resulting GPU
space too. Only scale present in the renderer's object-to-world matrix is
automatically applied; bone-baked/runtime skinning scale needs its own check.
The scale-invariance tests cover a rigid MeshRenderer at uniform scales
0.5, 1 and 2, not every VRChat skinning arrangement. Prefer a rigid lens and
test your avatar at multiple sizes in the client.

Hidden _ScopeDebug may be set by script/material debugging:
- 1: transverse object scale as greyscale.
- 2: axial lens distance / 0.2 (grey 0.6 means 12cm).
- 3: field-stop coverage.
- 4: moving-eye-shadow coverage.
Restore 0 afterwards. View distance 2 helps detect wrong centre/unit settings.

## Limits and validation

This is a distant-scene visual approximation, not physical lens-group
refraction. It does not model aberrations, brightness loss, human-pupil
integration, zoom-dependent eye relief or calibrated ballistic graduations.
One square mono texture per scope is assumed. Use separate cameras/textures
for simultaneous scopes to avoid image overwrites.

The shader has editor compilation and render regression coverage. This is
not a claim of validation on every headset, rig, Unity version or graphics API.
Calibrate and test your own model before release. Development logs and
avatar-specific tools are deliberately not shipped in this package.
