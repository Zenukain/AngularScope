# AngularScope - standalone Unity example

All example code, procedural meshes, images and accompanying documentation
are covered by the project's CC0-1.0 dedication. No purchased assets are used.
Unity's built-in primitive meshes/shaders remain external engine resources.

## Try it

1. Import AngularScope_WithExample.unitypackage into a PC Built-in RP project.
2. Open Scenes/AngularScopeDemo.unity and press Play.
3. Use the zoom slider or 1x/3x/Max buttons. Toggle FFP or linked pupil mode.
4. Click the Game view for keyboard focus: A/D shifts your eye sideways,
   W/S shifts up/down, Q moves away and E moves closer. Shift moves faster.
   R resets the eye; V switches between looking through and inspecting the
   model. H hides/shows the controls. A larger Game view is easier to use.

Keyboard movement requires Unity's legacy Input Manager (or Both input modes).
GUI controls remain available independently of keyboard focus.

The default view is 12cm behind the rear lens. 1x/3x/6x camera FOVs are
19.296091/6.486585/3.245893 degrees. The reticle is an uncalibrated procedural
black cross with a separate red centre dot; its ticks are NOT MOA/MRAD marks.
The test board is 20m from the rear lens, with coloured landmarks nearby.

## Prefab structure

Prefabs/AngularScopeExample.prefab is independent of the demo range/viewer.
Root origin = rear lens centre. Local +Z points towards the target; +Y is up.
The model is approximately 28cm long at unit metric scale. Three hollow tube
sections and box mounts are intentionally simple educational placeholders.

- Rear Lens Display: rigid MeshRenderer at origin, radius 0.019m, facing -Z.
- Scene Source Camera: 0.29m forward, aligned +Z, square 512px RenderTexture.
- Eyepiece/Main Tube/Objective Housing/Mounts: physical housing only.
- ScopeExampleController: optional Unity demo controls, not optical shader code.

The lens is on numeric layer 31 and the source camera excludes that layer,
preventing render feedback. No named layer/project setting is required.
If layer 31 is already used in your project, choose an appropriate lens layer
and update the source camera's culling mask together. Main viewer must see it.
Meshes, reticle Texture2D assets, materials and RT are saved under this folder.

## Use on your own model

Start with the prefab and replace only housing geometry. Keep the optical
objects intact first, then calibrate your mesh's centre/axes/pupil sizes as
described in Assets/AngularScope/Documentation/README.md.
Parent the complete scope root under a rigid animated transform. Avoid
nonuniform scale; resizing needs recalibration and eye relief is world metres.

At runtime the controller creates per-instance material and RT copies, so
two prefab instances do not overwrite the same camera image. When removing
the controller, provide separate material/RT assets yourself for each scope.
The viewer reference is optional: the demo controller falls back to Camera.main.
Controllers on multiple visible scopes should not all drive one viewer; disable
showControls and/or remove the controller from noninteractive instances.

## VRChat integration

PC VRChat avatars can use this shader; it is not restricted to worlds.
See [the avatar integration guide](../Documentation/AvatarIntegration.md) for
an animation-driven prefab, endpoint clips, a parameter-controlled zoom sweep,
FX binding paths and expression-menu setup. These fixtures are Unity-tested,
not a complete upload-ready avatar installer.

The example is a normal Unity demo, NOT a ready-to-upload VRChat avatar or
Udon world. Arbitrary MonoBehaviours do not become avatar runtime scripts.
Remove ScopeExampleController from an avatar integration and drive camera FOV
and material._Magnification with your supported animation/controller setup.
Camera availability and remote-client behaviour need platform-specific testing.
The base shader guide still applies. SDK, MA and lilToon are not dependencies
of this example package. Quest/Android avatar deployment is not supported.

## Rebuild and verification

Tools > AngularScope > Rebuild Example Assets and Scene regenerates fixtures
using the included Editor builder. It overwrites generated example assets;
duplicate files first if you edited them. It leaves an unsaved existing scene
open and untouched; a clean previous scene is closed without changing its file.
Run rebuilding only outside Play mode.

Editor checks cover Play mode at 1x/3x/6x, independent cloned material/RT
resources, both reticle layers and rendered close/overview views. These do not
replace headset testing of your final rig. The base demo has no lens effects;
the separate scene below provides optional aesthetic comparisons.
## Optional lens character demo

Open `Scenes/AngularScopeLensEffectsDemo.unity` to try mild radial distortion and
scene/eye-shadow colour fringe. The on-screen **Lens character** toggle compares
the configured values with zero. **Purple shadow fringe** compares the optional purple
palette with the original warm palette at the same 0.012 strength. Source material
assets are not modified in Play mode.
Use the grid, zoom slider, and eye movement controls for desktop tuning before VR.

If the optional scene is absent, **Tools > AngularScope > Build Lens Character Example**
creates a separate scene/material from the base demo; existing outputs are never overwritten.
The base example remains unchanged. These settings are artistic approximations, not
measured specifications from a particular real scope.

