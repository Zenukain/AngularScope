# Scope Setup — specification-based authoring

Open **Tools > AngularScope > Scope Setup**. This is an **editor-only window**,
not a runtime/avatar component. It requires no SDK, MA, VRCFury or NDMF.
The shader still needs a rigid optical MeshRenderer and a separate perspective
camera rendering into a square RenderTexture.

## Safe starting workflow

1. Select the optical lens in the Hierarchy. Click **Use selected lens**, or
   assign the lens/material slot manually and read its current settings.
2. Connect the **scope image camera**, not the player camera. The finder uses
   the material's RenderTexture and refuses ambiguous matches.
3. Review the zoom endpoints; a material stores only the current zoom, not the
   original supported range. Importing values changes neither material nor camera.
4. Keep specification calculation **off** to tune the existing setup. Enable it
   only when you intend to derive new starting values from known specifications.
5. Review the summary/change list. **Apply** writes the shared material and camera
   with Undo. Only that material is saved; scene/prefab changes require your save.

Fields have tooltips explaining units and what increases/decreases do. Fine
coordinates and empirical tuning are under **Advanced**. The optional cyan Scene
guide displays the draft lens-to-eye position and pupil radius without applying.
It is a calibration aid, not a physical eye-pupil simulation.

## Enter a manufacturer's specification

- **Zoom range:** 1–6, 1–8, 1–10, etc. Changing this does not rewrite existing FX.
- **Objective diameter (mm):** clear front lens, not tube diameter. It provides
  an effective input for the empirical zoom-linked pupil model.
- **Model size / real size:** 1 for full size; .7 for a model 70% as large at the
  saved reference size. This scales published diameter and relief, not FOV angles
  or the mesh itself. Transform scale is handled separately by calibration.
- **Scene FOV:** either visible width and target distance (both in metres), or
  full angular width (degrees). Always enter the zoom at which it was measured.
  A specification at 100 yards is not a specification at 100 metres.
- **Eye relief (cm):** published lens-to-eye distance. Model ratio applies when
  specification calculation is enabled. With it disabled, this is already the
  intended distance at the saved reference size.

Imported specification fields are **derived estimates**, not recognized
manufacturer data. Replace them with actual data when using this mode.

For visible width W at distance D and measurement magnification R:

```text
scene half-angle tangent = W / (2 * D)
apparent field tangent   = R * scene half-angle tangent
base camera tangent     = apparent field tangent * overscan
camera FOV at zoom M    = 2 * atan(base camera tangent / M)
```

An angular input supplies `tan(full_angle / 2)` instead. FOV uses one reference
sample and assumes a constant apparent angular field. Real products may not have
exactly inverse-zoom FOV; this tool does not fit arbitrary zoom-dependent specs.
The visible field is not the camera overscan area or a physical lens aperture.

## Comfort and reticle size

- Larger **eye-box radius** increases lateral/vertical tolerance. In linked mode
  it is a cap; the objective/zoom calculation can produce a smaller actual radius.
- Larger **extra rearward tolerance** postpones added tunnel shading. There is
  no matching near-distance cutoff in the current shader model.
- Higher **shadow softness** softens the edge, rather than enlarging the pupil.
- **Reticle size** remains independent of FOV. The optional fit button updates
  only the draft size to align texture bounds at SFP / FFP reference zoom. Ink
  margins, offsets and cropped illumination still require deliberate alignment.
- Main and illumination focal planes can differ; both use the shared FFP size
  reference. FFP reference is not automatically the maximum magnification.

Axes and lens centre use rigid renderer local coordinates. Prefer positive
uniform scale. Saved axial/radial reference scales convert physical distances to
shader-local values. Recapturing reference size is explicit and keeps cm values.
The WorldMetres option fixes relief distance; pupil geometry still scales with
the scope. Profiles retain their original reference scales: review on a new model.

## Zoom animation creation

Select the actual animation root, e.g. the avatar root for FX bindings. The tool
can create **three new clips**: low zoom, high zoom and a continuous one-second
sweep. Camera FOV and material magnification are bound together. The sweep uses
201 keys with linear tangents to approximate the nonlinear FOV curve.

Existing files, FX controllers, menus and expression parameters are not changed.
Apply matching base calibration to the material, then integrate the clips using
the [avatar guide](AvatarIntegration.md). Do not blend endpoint FOV angles for
continuous zoom. Generation currently supports material slot **0** only; tuning
other material slots is supported, but their animation bindings need manual work.

## Limits

This tool sets a starting point; it does not reproduce lens groups, brightness,
aberrations, real exit-pupil placement or manufacturer eye-box dimensions.
It does not resize meshes, reorient the camera, crop textures, install avatar
components or alter network settings. Check camera alignment, rendering layers,
existing animations and both eyes in your final setup. Numeric/Unity testing is
not a guarantee for every headset or VRChat rig.

If a separate authoring/build-time component also writes this material, update
or reimport its stored values after Apply. This window does not synchronize
third-party or private build components; their bake can otherwise overwrite tuning.

