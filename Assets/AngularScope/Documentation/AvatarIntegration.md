# PC VRChat avatar integration

AngularScope **can be used on PC VRChat avatars**. It is not world-only.
The shader is the optical display; the demo MonoBehaviour is only a Unity test
driver and must not be used as an avatar runtime script. This guide supplies
animation-driven fixtures, not a complete upload-ready avatar or automatic installer.

## What is included

Import the **WithExample** package. Under `Examples/AvatarIntegration` you get:

- `AngularScopeAnimatedRig.prefab`: procedural scope, camera, dedicated lens
  material/RenderTexture and a temporary test Animator; no runtime scripts.
- `ScopeZoom1x.anim` and `ScopeZoom6x.anim`: endpoint examples for a two-position selector.
- `ScopeZoomSweep.anim`: a one-second calibration curve, sampled by a float parameter.
- `ScopeZoom.controller`: a standalone test controller with `ScopeZoom` (Float, 0 to 1).

These fixtures have been checked in Unity, **not uploaded and headset-tested as
a new VRChat avatar integration**. Test your final avatar locally, in mirrors,
and with a second PC user.

## 1. Understand the hierarchy

```text
Avatar root (existing FX Animator is the animation root)
  ... rigid weapon or hand attachment ...
    AngularScope Animated Rig
      Scene Source Camera -> AnimatedView RenderTexture
      Rear Lens Display -> AnimatedOpticalView material -> same RenderTexture
      Housing and mounts
```

The camera faces the same direction as the scope (+Z in this example). Its
rendered image goes to a **square RenderTexture**, not to the user's main view.
The rear lens must use a rigid MeshRenderer facing the eye, optionally parented
to one weapon bone. SkinnedMeshRenderer optical displays are unsupported, even
when weighted to one bone. The rest of the model may remain skinned.
This is a separate scene-source camera,
not the VRChat player camera.

The example camera sits 29 cm forward of the rear lens. This avoids rendering
the sample housing, but introduces near-field parallax. Position the camera
for your model; alignment at distant targets does not imply exact near-field
or binocular optics.

The sample display uses numeric layer 31 and the camera excludes it. In your
avatar choose layers compatible with VRChat and your project; **do not assume
layer 31 is safe on upload**. Exclude the display from its own camera to avoid
feedback, while ensuring the player can still see the lens. Check the SDK's
layer handling and the final client. Camera culling masks can also exclude
your weapon/housing if needed. Never leave Target Texture empty.

Duplicate the material and RT for each independently active scope. Without the
demo script there is no automatic per-instance resource cloning. Sharing one RT
between active cameras can make one scope show another scope's view.

The supplied rigid-lens material uses `_EyeReliefMode = 1` (LocalSpace),
so distance and axial tolerance follow uniform object scaling together with
the scope. New materials also default to this mode; WorldMetres (0) remains available.
See the main shader guide before switching units on a scaled rig.
Do not mark the optical lens Static; dynamic batching is disabled by the shader.

## 2. Test without modifying an avatar

1. In a disposable copy of the demo scene, remove its original scope instance.
2. Place `AngularScopeAnimatedRig` at (0, 1.5, 0). Keep the main viewer and test range.
3. Enter Play mode. Select the rig's Animator and open its Parameters tab.
4. Set `ScopeZoom` to 0, 0.5 and 1: these represent 1x, 3.5x and 6x.
5. Check camera FOV and the rendered magnification together. Animator material
   bindings use a Renderer property override; the source material asset's Inspector
   may remain at 1 even while the animated renderer uses 3.5 or 6. Do not infer
   animation failure from that asset value alone.

Do not run the scripted demo controller and the animation driver on the same lens.
One would overwrite the other's values each frame.

## 3. Match camera FOV and shader magnification

For a square camera, use:

```text
M = 1 + 5 * ScopeZoom
camera vertical FOV in degrees = 2 * atan(0.17 / M) * 180 / pi
material._Magnification = M
material._TanHalfBaseFov = 0.17 (constant)
```

| ScopeZoom | Magnification | Camera FOV, approximately |
|---|---|---|
| 0 | 1x | 19.30 degrees |
| 0.5 | 3.5x | 5.56 degrees |
| 1 | 6x | 3.25 degrees |

`_TanHalfBaseFov` calibrates the shader projection; it is **not** an animated zoom
parameter. If you change it, regenerate/recalculate all FOV keys. Reticle size,
reference magnification, SFP/FFP selection and exit-pupil mode are separate settings.

The sweep has 101 keys with linear interpolation. This approximates the analytic
FOV curve, while magnification itself increases linearly. Do not simply blend
the two endpoint clips for continuous zoom: linear interpolation of FOV degrees
does not produce the intended intermediate magnification.

## 4. Transfer the animation into the avatar FX controller

The supplied clips use paths **relative to the scope rig**, exactly:

```text
Scene Source Camera : Camera / field of view
Rear Lens Display   : MeshRenderer / material._Magnification
```

An avatar FX Animator evaluates paths relative to the avatar root. Duplicate
the clips and prefix both bindings with your actual attachment path. For
example `Armature/.../Hand/AngularScope Animated Rig/Scene Source Camera`.
Record the two properties on your actual hierarchy in Unity's Animation window
if uncertain; compare the recorded paths with these examples. Renaming or
reparenting objects requires rebinding. All optical display bindings must target
MeshRenderer, including lenses converted from a skinned model.

**Do not replace your entire FX controller with ScopeZoom.controller.** Copy its
single state/layer into your existing FX, or use an animation-merging tool that
supports root-relative path remapping. Remove the rig's temporary child Animator
after integrating into FX so two Animators do not compete. No merge tool is a
dependency of this package; follow that tool's current documentation separately.

The state uses `ScopeZoomSweep` as Motion, **Motion Time controlled by ScopeZoom**,
Speed 0, no transitions, and Write Defaults Off. Layer weight must be 1. Add a
Float named `ScopeZoom` to the FX controller. Do not treat the standalone
controller's Write Defaults choice as a reason to switch your avatar's existing
controllers globally; check compatibility with the rest of your rig.

## 5. Connect an expression menu

In the VRChat SDK, add an expression Float parameter named `ScopeZoom`, default
0. A Radial Puppet control uses that same parameter and provides the normalized
0-to-1 value. If remote users should see the same zoom, enable network sync;
a synced Float consumes 8 expression-parameter bits. Unsynced/local zoom is a
deliberate alternative, not a guarantee of consistent remote viewing.

This guide does not include SDK parameter/menu assets and will not alter your
existing menu or parameter budget. Use your own SDK configuration or optional
MA/VRCFury setup, keeping the parameter name and animation-root paths consistent.

For two-position zoom instead, use the endpoint clips in two states and your
own Bool/menu toggle. Both clips write both properties, so zoom can return to 1x.

## 6. Final checks and troubleshooting

- Black image: check camera enabled, assigned RT, same RT on material, camera
  culling mask, and whether VRChat safety settings permit the avatar camera.
- Feedback: the camera is rendering the display that shows its own RT.
- Scene zoom changes but FFP/pupil behavior does not: `_Magnification` is not animated.
- Reticle changes but scenery does not zoom: the camera FOV binding is wrong.
- Works on the rig but not the avatar: check root-relative paths, binding component
  type, FX layer weight and competing Animators/material animations.
- Separate users/avatars show the wrong image: check shared RT/material assets
  and camera behavior. Cameras and shaders may be blocked by safety settings.
- Large performance cost: every active source camera renders the scene again.
  Start at 512 square and disable the source camera when the scope is unused.
  Rendering cost, RT memory and avatar download size are different measurements.
- Quest/Android avatar custom shaders, URP and HDRP are outside this package's scope.

## References

- [VRChat allowed avatar components and camera safety behavior](https://creators.vrchat.com/avatars/whitelisted-avatar-components/whitelisted-avatar-components/)
- [VRChat FX/playable layers](https://creators.vrchat.com/avatars/playable-layers/)
- [VRChat animator and expression parameters](https://creators.vrchat.com/avatars/animator-parameters/)
- [Unity Animator state normalized-time parameter](https://docs.unity3d.com/ScriptReference/Animations.AnimatorState-timeParameter.html)

Editor regeneration: **Tools > AngularScope > Build Animation-Driven Integration
Example**. This overwrites only the generated integration assets, not the original
demo scene. Duplicate generated assets before personal edits.

The Unity fixtures have interpolation/binding checks, not VRChat upload or
safety validation. Test the final avatar in the client.

