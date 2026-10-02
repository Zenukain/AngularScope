# AngularScope

A configurable optical-view shader and a standalone Unity example for scope creators. Includes variable magnification, SFP/FFP reticles, two independent reticle layers, a sharp field stop, and a moving soft eye-box shadow.

Built for Unity's **Built-in Render Pipeline on PC**. The example was tested in Unity 2022.3.22f1. This is a geometric approximation, not a physical ray-traced optical system.

## Try it

1. Download [AngularScope with the example](dist/AngularScope_WithExample.unitypackage) and import it into a Built-in RP Unity project.
2. Open `Assets/AngularScope/Examples/Scenes/AngularScopeDemo.unity` and enter Play mode.
3. Use the on-screen magnification and focal-plane controls. Move the eye with A/D, W/S and Q/E; R resets the view and V shows the model.

For the shader and documentation only, download [the smaller shader-only package](dist/AngularScope_ShaderOnly.unitypackage).

The shader **requires a scene-image RenderTexture**. A separate camera creates the magnified image; the shader does not replace that camera. The example controller is a regular Unity MonoBehaviour, **not an upload-ready VRChat avatar script**. For avatars, supply supported camera-FOV/material animations or your own integration.

## Magnification and focal plane

These are actual captures of the included procedural example, not illustrations. SFP keeps the reticle's apparent size fixed while the scene zooms. FFP scales the reticle together with the scene; both meet at the configured reference magnification of 6x.

| | 1x | 6x |
|---|---|---|
| SFP | ![SFP at 1x](docs/images/sfp-1x.png) | ![SFP at 6x](docs/images/sfp-6x.png) |
| FFP | ![FFP at 1x](docs/images/ffp-1x.png) | ![FFP at 6x](docs/images/ffp-6x.png) |

**FFP image note:** This example uses `_ReticleRefMagnification = 6`. FFP reticle size relative to SFP is `current magnification / reference magnification`: at 1x it is one-sixth the SFP size, and at 6x it matches SFP. The two 6x images therefore intentionally look identical. This setting calibrates reticle size; it does not disable FFP zoom scaling.

To match SFP size at 1x instead, set `_ReticleRefMagnification = 1`; the FFP reticle will then be six times that size at 6x, so outer marks may extend beyond the visible field. Choose the reference and reticle size together for your design. See the [reticle and illumination guide](Assets/AngularScope/Documentation/README.md#reticle-and-illumination-overlay).

The black etched cross and red illuminated centre are independent texture layers.

## Eye-box shadow

Moving the eye off axis produces a soft shadow. Exit-pupil size can follow magnification or remain fixed for a more forgiving view.

![Off-axis eye-box shadow](docs/images/eye-box-shadow.png)

## Included example

The scope housing, mount, checkerboard range, meshes and reticle textures are generated specifically for this example. No purchased scope or firearm assets are included.

![Procedural example scope](docs/images/model.png)

## Documentation

- [Shader setup and parameter guide](Assets/AngularScope/Documentation/README.md)
- [Example controls and regeneration](Assets/AngularScope/Examples/README.md)

No lens distortion or chromatic aberration is implemented. Quest avatar custom shaders, URP and HDRP are not supported by this package. A single mono scene texture cannot reproduce exact near-field binocular parallax.

## License

The independently authored shader, scripts, documentation, example meshes and textures are dedicated under **CC0-1.0**. See [LICENSE.md](LICENSE.md) and [the full dedication](CC0-1.0.txt). Unity and third-party components remain subject to their own terms.
