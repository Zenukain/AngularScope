// SPDX-License-Identifier: CC0-1.0
using UnityEngine;

namespace AngularScope.Examples
{
    /// <summary>
    /// Unity-only demonstration. For VRChat avatars replace this MonoBehaviour
    /// with camera-FOV/material animations or your own supported integration.
    /// Owns per-instance render resources; never edits source material assets.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScopeExampleController : MonoBehaviour
    {
        public Camera imageCamera;
        public Renderer lensRenderer;
        public Camera viewer;
        [Min(1)] public float maximumMagnification = 6;
        [Min(1)] public float magnification = 1;
        public bool firstFocalPlane;
        public bool linkedExitPupil = true;
        public bool showControls = true;
        public bool enableLensCharacter = true;
        public bool purpleShadowFringe;

        Material originalMaterial;
        Material runtimeMaterial;
        RenderTexture originalTexture;
        RenderTexture runtimeTexture;
        bool overview;
        Vector3 initialViewerPosition;
        Quaternion initialViewerRotation;
        float initialViewerFov;
        Vector4 lensCharacter;

        void OnEnable()
        {
            if (!imageCamera || !lensRenderer || !lensRenderer.sharedMaterial)
                return;
            originalMaterial = lensRenderer.sharedMaterial;
            originalTexture = imageCamera.targetTexture;
            if (!originalTexture)
                return;
            runtimeMaterial = new Material(originalMaterial);
            runtimeMaterial.name = originalMaterial.name + " (Demo Instance)";
            lensCharacter = ReadLensCharacter(runtimeMaterial);
            purpleShadowFringe=runtimeMaterial.GetFloat("_ShadowFringePalette")>.5f;
            runtimeTexture = new RenderTexture(originalTexture);
            runtimeTexture.name = originalTexture.name + " (Demo Instance)";
            runtimeTexture.Create();
            runtimeMaterial.SetTexture("_MainTex", runtimeTexture);
            lensRenderer.sharedMaterial = runtimeMaterial;
            imageCamera.targetTexture = runtimeTexture;
            ApplySettings();
        }

        void Start()
        {
            if (!viewer) viewer = Camera.main;
            if (!viewer) return;
            initialViewerPosition = viewer.transform.position;
            initialViewerRotation = viewer.transform.rotation;
            initialViewerFov = viewer.fieldOfView;
        }

        public void ApplySettings()
        {
            if (!runtimeMaterial || !imageCamera) return;
            magnification = Mathf.Clamp(magnification, 1, Mathf.Max(1, maximumMagnification));
            float tangent = Mathf.Max(runtimeMaterial.GetFloat("_TanHalfBaseFov"), 0.0001f);
            imageCamera.fieldOfView = 2 * Mathf.Atan(tangent / magnification) * Mathf.Rad2Deg;
            imageCamera.aspect = 1;
            runtimeMaterial.SetFloat("_Magnification", magnification);
            runtimeMaterial.SetFloat("_ReticleFocalPlane", firstFocalPlane ? 1 : 0);
            // The illumination overlay follows the same focal plane in this example.
            runtimeMaterial.SetFloat("_IlluminationFocalPlane", firstFocalPlane ? 1 : 0);
            runtimeMaterial.SetFloat("_ExitPupilMode", linkedExitPupil ? 1 : 0);
            ApplyLensCharacter(enableLensCharacter ? lensCharacter : Vector4.zero);
            runtimeMaterial.SetFloat("_ShadowFringePalette",purpleShadowFringe?1:0);
        }

        static Vector4 ReadLensCharacter(Material material)
        {
            return new Vector4(
                material.GetFloat("_DistortionLow"), material.GetFloat("_DistortionHigh"),
                material.GetFloat("_SceneChromaticAberration"), material.GetFloat("_ShadowChromaticAberration"));
        }

        void ApplyLensCharacter(Vector4 strengths)
        {
            runtimeMaterial.SetFloat("_DistortionLow", strengths.x);
            runtimeMaterial.SetFloat("_DistortionHigh", strengths.y);
            runtimeMaterial.SetFloat("_SceneChromaticAberration", strengths.z);
            runtimeMaterial.SetFloat("_ShadowChromaticAberration", strengths.w);
        }

        void Update()
        {
            ApplySettings();
            if (Input.GetKeyDown(KeyCode.H)) showControls = !showControls;
            if (!viewer) return;
            if (Input.GetKeyDown(KeyCode.R)) ResetEye();
            if (Input.GetKeyDown(KeyCode.V)) ToggleOverview();
            if (overview) return;
            float step = (Input.GetKey(KeyCode.LeftShift) ? 0.03f : 0.01f) * Time.deltaTime;
            Vector3 movement = Vector3.zero;
            if (Input.GetKey(KeyCode.A)) movement.x -= step;
            if (Input.GetKey(KeyCode.D)) movement.x += step;
            if (Input.GetKey(KeyCode.W)) movement.y += step;
            if (Input.GetKey(KeyCode.S)) movement.y -= step;
            if (Input.GetKey(KeyCode.Q)) movement.z -= step;
            if (Input.GetKey(KeyCode.E)) movement.z += step;
            viewer.transform.position += transform.TransformDirection(movement);
        }

        void ResetEye()
        {
            overview = false;
            viewer.transform.SetPositionAndRotation(initialViewerPosition, initialViewerRotation);
            viewer.fieldOfView = initialViewerFov;
        }

        void ToggleOverview()
        {
            if (overview) { ResetEye(); return; }
            overview = true;
            viewer.transform.position = transform.TransformPoint(new Vector3(-0.45f, 0.18f, -0.35f));
            viewer.transform.LookAt(transform.TransformPoint(new Vector3(0, 0, 0.14f)), transform.up);
            viewer.fieldOfView = 40;
        }

        void OnGUI()
        {
            if (!showControls || !runtimeMaterial) return;
            GUILayout.BeginArea(new Rect(15, 15, 230, 365), GUI.skin.box);
            GUILayout.Label("AngularScope - Unity example");
            GUILayout.Label("Zoom: " + magnification.ToString("F2") + "x");
            magnification = GUILayout.HorizontalSlider(magnification, 1, Mathf.Max(1, maximumMagnification));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1x")) magnification = 1;
            if (GUILayout.Button("3x")) magnification = Mathf.Min(3, maximumMagnification);
            if (GUILayout.Button("Max")) magnification = Mathf.Max(1, maximumMagnification);
            GUILayout.EndHorizontal();
            firstFocalPlane = GUILayout.Toggle(firstFocalPlane, "FFP reticle (off = SFP)");
            linkedExitPupil = GUILayout.Toggle(linkedExitPupil, "Magnification-linked exit pupil");
            enableLensCharacter = GUILayout.Toggle(enableLensCharacter, "Lens character (if configured)");
            purpleShadowFringe = GUILayout.Toggle(purpleShadowFringe, "Purple shadow fringe (off = warm)");
            GUILayout.Label("A/D: eye left/right\nW/S: eye up/down\nQ/E: eye away/towards\nShift: faster movement\nH: hide/show this panel");
            if (viewer)
            {
                var localEye = transform.InverseTransformPoint(viewer.transform.position);
                GUILayout.Label("Eye distance: " + (-localEye.z * transform.lossyScale.z * 100).ToString("F1") + " cm");
            }
            if (GUILayout.Button("Reset eye [R]")) ResetEye();
            if (GUILayout.Button(overview ? "Look through scope [V]" : "Inspect model [V]")) ToggleOverview();
            GUILayout.EndArea();
        }

        void OnDisable()
        {
            if (lensRenderer && runtimeMaterial && lensRenderer.sharedMaterial == runtimeMaterial)
                lensRenderer.sharedMaterial = originalMaterial;
            if (imageCamera && runtimeTexture && imageCamera.targetTexture == runtimeTexture)
                imageCamera.targetTexture = originalTexture;
            if (runtimeTexture) { runtimeTexture.Release(); Destroy(runtimeTexture); }
            if (runtimeMaterial) Destroy(runtimeMaterial);
            runtimeTexture = null;
            runtimeMaterial = null;
        }
    }
}
