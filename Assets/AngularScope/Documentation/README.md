# AngularScope - optical view shader

A configurable scope-display shader for Unity's Built-in Render Pipeline.
Shader menu: `AngularScope/Optical View`.
Tested editor: Unity 2022.3.22f1, PC rendering.

## Package and license

The ShaderOnly package includes shader source, editor-only setup, rigid-lens
conversion and mask tools, documentation and CC0 license files, with no model, texture,
camera or animation assets. The WithExample
package additionally includes procedural fixtures and animation-driven zoom.
See [PC avatar integration](AvatarIntegration.md) for the integration workflow.
No proprietary scope package, Modular Avatar or lilToon is needed by the
shader itself. Your scene-source camera and avatar integration are separate.

Independently authored shader/documentation are dedicated under CC0-1.0;
see [NOTICE.md](../NOTICE.md) and [LICENSE.txt](../LICENSE.txt). This does not relicense third-party assets,
Unity's externally provided headers, or models/images you use with it.

## Platform and requirements

Use a PC Built-in Render Pipeline project. VRChat PC avatars are one intended
integration use case; the shader is not avatar-only. Quest/Android avatar shaders,
URP and HDRP are not supported by this package. Stereo macros are present, but one
mono camera texture cannot reproduce exact near-field binocular parallax.

Required display structure: a normal MeshRenderer on a rigid lens mesh,
parented to one bone/transform. SkinnedMeshRenderer optical displays are not
supported: skinning can bake scaling into vertices while the shader sees a
unit-scale matrix, breaking lens-centre and distance calibration on rescaling.
The housing and moving covers may still use skinned meshes. Prefer uniform positive object scale.
Axes must be nonzero, mutually perpendicular, and aligned with the image camera.

## Quick start

Prefer physical specifications and labelled units over shader equations?
Use **Tools > AngularScope > Scope Setup** and follow the
[specification-based authoring guide](ScopeSetup.md). The editor-only window
imports existing tuning without applying it, reviews proposed changes, and can
generate matching zoom clips. No runtime component is added to your avatar.

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
5. Calibrate lens centre, eye relief and pupil radii in the lens's local coordinates.
   Default mode 1 uses local lengths: _EyeReliefDist=0.12 is 12cm only at metric
   unit scale. Defaults are starting points, not measured lens specifications.
6. For 1x, set Camera.fieldOfView=19.296091 degrees and _Magnification=1.
   The camera aspect ratio must be 1. At any magnification M, set camera FOV
   to 2*atan(_TanHalfBaseFov/M) in degrees AND _Magnification=M.
7. Test both eyes, zoom extremes and horizontal/vertical/near/far eye movement.
   Leave _ScopeDebug=0 in normal use. Camera activation/network integration
   must be handled by your own application/avatar setup.

The shader displays an external scene texture; it does NOT replace the
scene camera or produce real zoom by itself. ShaderOnly ships no runtime script;
WithExample includes a Unity demo driver and animation-driven integration fixtures.
The scene camera's extra rendering can dominate total performance cost.

## Projection and coverage

Image projection uses _TanHalfBaseFov (default 0.17), the camera half-angle tangent
at **1x**, even for a scope whose zoom range starts above 1x. Camera FOV at 1x is
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

### Optional near-eye lateral sensitivity

`_NearEyeSensitivity` defaults to **0** (existing behaviour). A positive value
amplifies sideways and vertical eye displacement only when the eye is closer
than `eye relief - axial tolerance`. It does not add near-distance error to the
field shear, so the perfectly centred view stays unchanged; the rearward tunnel
response is also unchanged. Begin with **1** for testing, not as a measured
real-scope prescription. Linked exit pupils may make high zoom much less forgiving.

This is an empirical handling option, not a complete near-side optical model.
The lateral gain is `1 + nearError/lensDepth * strength`, capped at **8x** for
stability near the lens plane. Strength is limited to 0–4. LocalSpace relief
scales both the relief and tolerance as usual; WorldMetres fixes axial distances
only. No extra fade, coverage mask or chromatic effect is added. The field
boundary remains sharp, and centred close-eye viewing still exposes the field
stop inside the housing. Test horizontal and vertical movement in a headset
before choosing a production value. Configure it in the material's Field &
eyebox section or Scope Setup's advanced controls.

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
In ColorRGBA and AlphaMask modes, texture alpha defines coverage; RedMask uses
the red channel instead. Overlay opacity adds a strength control; tint color
alpha is not used. The illumination overlay is composited over the reticle.

### Optional central reticle detail

This advanced option replaces a rectangular part of the main reticle with a
higher-density crop. It is **not** a second complete reticle or a zoom-triggered
texture swap. The illumination overlay remains independent and draws last.

1. Prepare a lower-resolution full reticle and a matching, more detailed crop
   from the same master artwork. Merely enlarging the low-resolution image does
   not recover detail. Export the exact crop canvas, including transparent gaps.
2. In the lens material's **Central reticle detail · optional** foldout, assign
   `_ReticleDetailTex` and choose its texture mode. Set Clamp, mipmaps and
   Trilinear filtering; RedMask/BC4 data must be linear, just like the base mask.
3. Set `_ReticleDetailRegion = (centreU, centreV, width, height)` in the **base
   texture's normalized, bottom-left-origin UV coordinates**. For the central
   quarter of each dimension, use `(0.5, 0.5, 0.25, 0.25)`. Non-square crops are
   supported; keep the entire region inside UV 0–1. For a top-origin crop,
   `centreV = 1 - (top + cropHeight/2)/masterHeight`.
4. Enable `_ReticleDetailEnabled` after calibration. It defaults to **0**, so
   existing materials and 4K workflows keep their original view. Leave it off
   when no detail image is assigned; enabling an empty image can erase marks.
5. `_ReticleDetailFeather` blends inward at the crop boundary (0.05 = 5% of crop
   width/height). Zero still uses derivative antialiasing. Check alignment and
   line weight at the seam at both minimum and maximum zoom.

Detail inherits the main reticle's size, offset, tint, brightness, focal plane
and FFP reference. There is no second reference magnification to reconcile.
Base, detail and the replacement boundary share **one inverse-distorted lookup**,
equivalent to warping the composite reticle, not separately distorting the crop.
Inside the replacement region, transparent detail pixels show the scene rather
than the old blurry base line. The boundary crossfades the two completed reticle
views; mismatched artwork or filtering can still produce a visible seam.

Both textures occupy memory even when one is not sampled. The saving comes from
concentrating detail into a small crop, not unloading textures at different zooms.
For example, a 2K BC4 base plus 1K BC4 detail uses about **3.33 MiB** with mipmaps
(illumination excluded), versus 10.67 MiB for a 4K BC4 base. A 1K crop covering
the central quarter has the same nominal texel density there as a full 4K image;
its border transition still includes the base. This does not guarantee subpixel
line visibility. Enabled detail adds one texture lookup and blending; GPU cost
has not been benchmarked. Scope Setup preserves these material settings but does
not author the crop; use the material inspector for this advanced option.

### Texture modes and lightweight 4K masks

Each layer has its own texture mode (`_ReticleTextureMode` /
`_IlluminationTextureMode` / `_ReticleDetailTextureMode`):

- **ColorRGBA (0, default):** texture RGB supplies
  colour and alpha supplies coverage; material tint/brightness still apply.
- **AlphaMask (1):** PNG alpha supplies coverage; stored RGB is ignored.
  Recommended for monochrome PNG art, avoiding dark RGB fringes around illumination.
- **RedMask (2):** linear red-channel data supplies coverage; RGB colour and
  texture alpha are ignored. Use this for BC4 masks.

For a lightweight mask, select a transparent PNG and open **Tools > AngularScope >
Create Lightweight Reticle Mask**. Choose a new output filename. The tool copies
the source PNG's alpha into red and configures linear data, PC Standalone BC4,
full source dimensions, mipmaps, Trilinear filtering and Clamp. The original
asset/import settings are untouched. An optional AngularScope material target
can receive the mask and RedMask mode; colour, opacity, size, offsets and focal
plane remain unchanged. Opaque PNGs and overwriting existing outputs are rejected.
No runtime scripts, expression parameters or special external packages are needed.

The decoder reads original PNG bytes even if Unity currently imports it at 2K:
a 4096-square source becomes a 4096-square mask. Check the final target platform's
format and size after import; the supplied preset is **PC Standalone**, not Android.
BC4 is lossy: inspect fine graduations at low and high zoom. It does not guarantee
subpixel-line visibility or replace MSDF/vector rendering.

| Texture with full mip chain | Approximate GPU storage |
|---|---:|
| 4K BC4 | 10.67 MiB |
| 512-square BC4 | 0.17 MiB |
| 4K BC3/DXT5 or BC7 | 21.33 MiB |

These are compressed texture-storage estimates, not PNG size, download size or
total Editor process memory. A 4K main mask plus 512 centre mask is about 10.84 MiB
(11.36 MB). Two full-size 4K BC4 masks instead total about 21.33 MiB.

### Optional cropped illumination image

Cropping is deliberately **not automatic**: the tool preserves the canvas and
alignment. You may draw/export just the centre illumination on a smaller canvas.
To preserve alignment when cropping a square source of width W to a square region
of width w, with crop centre c in normalized bottom-left-origin UV coordinates:

```text
f = w / W
new layer size = old layer size * f
new layer offset = (old layer offset + c - 0.5) / f
```

For a centred 512 crop from 4096: size becomes old size / 8 and an initially zero
offset remains zero. Keep focal-plane selection/reference unchanged. Resizing
the cropped image changes texel density, not this geometric ratio. Merely
resizing the whole canvas to 512 is NOT a crop. The illumination size field
accepts small positive values needed for this workflow. Transparent backgrounds
are still required; enable the illumination opacity separately.

Each focal-plane selector: 0 = SFP, 1 = FFP. SFP apparent size stays fixed.
FFP apparent size scales with optical M, matching SFP size at the shared
_ReticleRefMagnification. This is a positive Float, not limited to 6x:
for a 10x size reference, set it to 10. A mark at 1x then has one-tenth its
reference apparent size. Both layers may use different focal-plane selections.

The illumination overlay is not restricted to a dot: rings, horseshoes and
other illuminated patterns work too.
For consistent geometry, size-reference calibration must be deliberate.
At low zoom, thin FFP marks can become subpixel and lose visibility; choose
appropriate line widths, texture filtering and mipmaps for your target image.

_ReticleTanHalfFov (default 0.5773503) and each layer's size/offset are
independent of camera overscan. Do not change reticle calibration merely to
crop the scene camera. Supplied images are not automatically split, and
the shader does not calibrate arbitrary graduations in MOA/MRAD.

### Calculate size relative to the field stop

With zero offset, for an SFP layer (or an FFP layer at its reference
magnification), the centre-to-edge angular tangent along the horizontal or
vertical texture axis is `_ReticleTanHalfFov * _ReticleScale`. To align those
texture edges with the circular field boundary:

```text
_ReticleScale = _FieldTanHalfAngle / _ReticleTanHalfFov
              = 0.16 / 0.5773503 ~= 0.277128
```

The preset uses 0.2771281, the calculated ratio rounded to float precision.
This is the undistorted reference size: the formula aligns cardinal texture
edges, not square corners or ink endpoints. Optional distortion can move their
apparent positions. The independent illumination size is not recalibrated here.

To fill a fraction q of the field radius, use `q * field / calibration`.
For FFP at a different magnification M, multiply the resulting size by
`_ReticleRefMagnification / M` to obtain that fraction specifically at M.
The illumination layer follows the same equations with `_IlluminationScale`.

Margins, offsets and housing affect visible fit. If ink extends a fraction a
of centre-to-texture-edge distance, divide the calculated size by a. The example
cross has margins: fitting the texture does not align every ink endpoint.

Field radius and reticle size remain independent: recalculate size explicitly
when changing field radius. This is not zoom; existing values do not auto-resize.

## Exit pupil and zoom integration

_ExitPupilMode=1 (MagnificationLinked) is the new-material default:
radius = min(_ExitPupilRadius, _ObjectiveRadius / max(M,1)).
_ExitPupilMode=0 (Fixed) preserves _ExitPupilRadius at every zoom.
_ObjectiveRadius is an EFFECTIVE radius, not diameter; default 0.012 is an
illustrative 24mm diameter at metric unit scale. The default radius cap
0.01875 is an adjustable empirical value, not a measured human pupil.
With the default objective radius and M >= 1, the linked pupil radius is
0.012/M, always below that cap: the default cap does not engage. Other
objective/cap settings can engage it. In Fixed mode this property is the
actual radius, not merely a cap.

Linked shadow shear uses independent constant `PupilShadowReferenceRadius=0.01875`.
Changing the cap affects pupil visibility, not that normalization. Coupling and
axial vignetting remain adjustable empirical controls.

The linked mode reduces lateral eye tolerance while preserving the centred
apparent field in this empirical model. It is not a full exit-pupil raytrace.
_Magnification is actual optical magnification, not a 0-to-1 menu position.
Keep camera FOV and material._Magnification driven by the SAME zoom control.
For any camera FOV: M = _TanHalfBaseFov / tan(FOV/2).
Use radians in tan/atan, then convert to degrees for Camera.fieldOfView.
Fixed pupil mode ignores M only for pupil sizing; FFP reticles still need M.

ShaderOnly supplies no zoom driver. WithExample includes an animation-driven
rig and clips; see [avatar integration](AvatarIntegration.md). Renderer animations
may use a MaterialPropertyBlock, so sharedMaterial need not show animated values.
One zoom control can drive both FOV and magnification; remote cameras depend on platform safety.

## Local-coordinate calibration and diagnostics

For a normal MeshRenderer, _LensCenter is in native local mesh coordinates.
Pupil/objective radii use those units; the object's transverse world scale
converts them to world lengths. `_EyeReliefMode` selects the units for BOTH
`_EyeReliefDist` and `_EyeReliefTol`:

- **WorldMetres (0):** uses fixed distances independent of object scale. A value
  of 0.12 stays 12 cm regardless of object scale.
  Only relief and axial tolerance are fixed; pupil/objective radii still scale
  with the lens. This mode does not freeze the entire eye-box geometry.
- **LocalSpace (1, shader and supplied material default):** lengths use the same local
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

Do not calibrate around skinned-renderer GPU behaviour. Use a dedicated rigid
MeshRenderer and test the avatar at multiple sizes after uploading. The optional
editor tool under LensTools converts an optical submesh rigidly weighted to one
bone: select its SkinnedMeshRenderer object, then use Tools > AngularScope >
Convert Selected Rigid Skinned Lens. Choose a generated-assets folder inside Assets.
The imported model is not changed; the housing gets a mesh copy without optical
triangles, and a new MeshRenderer is attached to the lens bone. Blendshapes that
deform the lens and mixed-bone weighting are rejected rather than silently lost.
Rebind optical animations to the new renderer/path and update any setup component.
If animations toggle the old renderer or its object separately from the attachment
bone, also wire those visibility controls to the new lens. The converter preserves
the current enabled/active flags, not arbitrary future animation logic.
Check the generated rear-plane centre, axes and physical distances; values from
an unsupported skinned setup are not guaranteed to preserve its apparent tuning.

Hidden _ScopeDebug may be set by script/material debugging:
- 1: transverse object scale as greyscale.
- 2: axial lens distance / 0.2 (grey 0.6 means 12cm).
- 3: field-stop coverage.
- 4: moving-eye-shadow coverage.
Restore 0 afterwards. View distance 2 helps detect wrong centre/unit settings.

## Optional distortion and colour fringe

New materials have zero effect strengths. The Example starts with effects off;
its Lens character toggle enables a mild runtime comparison preset. Use the editor
[Scope Setup window](ScopeSetup.md#optional-lens-character) or these material controls:

| Property | Meaning |
|---|---|
| `_DistortionLow`, `_DistortionHigh` | Field-edge inverse lookup coefficients; positive barrel, negative pincushion |
| `_DistortionMinMagnification`, `_DistortionMaxMagnification` | Zoom references for linear interpolation; clamp outside their range |
| `_DistortReticle` | Apply the geometric distortion to both reticle layers, before SFP/FFP scaling |
| `_SceneChromaticAberration` | R/B radial image lookup offset fraction at the field edge; 0–0.02 |
| `_ShadowChromaticAberration` | Channel-dependent pupil-radius offset fraction; 0–0.05 |
| `_ShadowFringePalette` | 0 Warm (R outside G outside B), 1 Purple (R+B outside G); artistic palette choice |

Image distortion uses `sampleSlope = slope * (1 + k * normalizedRadiusSquared)`.
The field-stop and empirical pupil geometry use the original slope, so lens
character does not alter those masks or introduce an aim offset at the centre.
Colour fringe is a stylized RGB approximation, not wavelength raytracing. The
reticle gets geometric distortion when enabled, but not scene RGB separation.
Shadow colour fringe affects the composed scene **and** reticle near the pupil edge.
The scene-fringe value is the R/B lookup scale offset at the field edge,
relative to the geometrically distorted slope. It scales that slope by a radius-squared factor:
displacement grows roughly cubically with field radius, not linearly. Shadow
fringe strength is fixed; eye position changes the existing shadow geometry,
not the palette or an additional distance-dependent colour multiplier.

Scene colour fringe adds two RT texture lookups when enabled; shadow colour fringe
evaluates three edge coverages instead of one, reusing pupil geometry. Material-uniform
zero branches skip those extra evaluations in source; compiler/hardware behaviour
and actual GPU cost must be checked on your target. No additional camera or RT is added.

Increase camera overscan if distorted R/B lookups approach texture boundaries.
Keep camera FOV animations synchronized with the updated base tangent. Start on
the demo grid, then verify the headset and both eyes. These controls are not a
claim of matching any particular real scope.
The included editor-only material Inspector warns when the worst configured edge
lookup exceeds camera coverage, but does not modify or block direct edits. Update
the actual camera and zoom animations as well as the shader tangent. The default
Warm is the default palette; Purple is optional. The mild preset uses distortion
+0.04 / -0.005 at 1x / 6x, scene fringe 0.004 and shadow fringe 0.012.

## Limits and validation

This is a distant-scene visual approximation, not physical lens-group
refraction. It does not model measured lens aberrations, brightness loss, human-pupil
integration, zoom-dependent eye relief or calibrated ballistic graduations.
One square mono texture per scope is assumed. Use separate cameras/textures
for simultaneous scopes to avoid image overwrites.

The lens writes camera Z, but has no ShadowCaster pass. Built-in Forward
separate `_CameraDepthTexture` generation can omit it: depth-based DOF may
treat the lens as background. This was reproduced locally, not validated for
every VRChat photo/render path. See [Unity's depth documentation](https://docs.unity3d.com/2022.3/Documentation/Manual/SL-CameraDepthTexture.html).

The example RenderTexture uses a lightweight LDR, non-MSAA configuration.
MSAA and HDR can improve particular scenes but increase memory/render cost;
choose them for your target hardware rather than assuming they are free.

### Optional RenderTexture quality settings

Keep the default for a lightweight starting point. To compare higher quality,
duplicate the RT first and assign the same copy to the image camera and `_MainTex`.

- **Geometry antialiasing:** set RT Anti-Aliasing to 2x or 4x and enable the
  image camera's Allow MSAA. This smooths covered geometry edges, not arbitrary
  texture detail, reticle lines or temporal shimmer. Rendering-path support matters.
- **HDR:** choose `R16G16B16A16_SFloat` / ARGBHalf and enable the image camera's
  Allow HDR. A local Direct3D11 test preserved above-1 RGB through the scope shader;
  LDR clipped those highlights. HDR does not automatically reproduce the viewer's
  exposure, tone mapping or post effects: compare in your target scene/client.

A 512-square ARGBHalf colour surface alone is 2 MiB. Depth, multisample storage,
resolve surfaces and driver allocations are additional; this is not total RT cost.
Compare fixed viewing positions before changing the shipped defaults. Neither
setting changes optical magnification or requires new zoom-animation keys.

The shader has editor compilation and render regression coverage. This is
not a claim of validation on every headset, rig, Unity version or graphics API.
Calibrate and test your own model before release. Development logs and
avatar-specific tools are deliberately not shipped in this package.
