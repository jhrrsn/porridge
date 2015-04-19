using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Light2D
{
    /// <summary>
    /// Main script for lights. Should be attached to camera.
    /// Handles lighting operation like camera setup, shader setup, merging cameras output together, blurring and some others.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof (Camera))]
    public class LightingSystem : MonoBehaviour
    {
        /// <summary>
        /// Size of lighting pixel in Unity meters. Controls resoultion of lighting textures. 
        /// Smaller value - better quality, but lower performance.
        /// </summary>
        public float LightPixelSize = 0.05f;

        /// <summary>
        /// Needed for off screen lights to work correctly. Set that value to radius of largest light. 
        /// Used only when camera is in orthographic mode. Big values could cause a performance drop.
        /// </summary>
        public float LightCameraSizeAdd = 3;

        /// <summary>
        /// Needed for off screen lights to work correctly.
        /// Used only when camera is in perspective mode.
        /// </summary>
        public float LightCameraFovAdd = 30;

        /// <summary>
        /// Enable/disable ambient lights. Disable it to improve performance if you not using ambient light.
        /// </summary>
        public bool EnableAmbientLight = true;

        /// <summary>
        /// LightSourcesBlurMaterial is applied to light sources texture if enabled. Disable to improve performance.
        /// </summary>
        public bool BlurLightSources = true;

        /// <summary>
        /// AmbientLightBlurMaterial is applied to ambient light texture if enabled. Disable to improve performance.
        /// </summary>
        public bool BlurAmbientLight = true;

        /// <summary>
        /// If true RGBHalf RenderTexture type will be used for light processing.
        /// That could improve smoothness of lights. Will be turned off if device is not supports it.
        /// </summary>
        public bool HDR = true;

        /// <summary>
        /// If true light obstacles will be rendered in 2x resolution and then downsampled to 1x.
        /// </summary>
        public bool LightObstaclesAntialiasing = true;

        /// <summary>
        /// Set it to distance from camera to plane with light obstacles. Used only when camera in perspective mode.
        /// </summary>
        public float LightObstaclesDistance = 10;

        public Material AmbientLightComputeMaterial;
        public Material LightOverlayMaterial;
        public Material LightSourcesBlurMaterial;
        public Material AmbientLightBlurMaterial;
        public Camera LightCamera;
        public int LightSourcesLayer;
        public int AmbientLightLayer;
        public int LightObstaclesLayer;

        private RenderTexture _ambientEmissionTexture;
        private RenderTexture _ambientTexture;
        private RenderTexture _prevAmbientTexture;
        private RenderTexture _bluredLightTexture;
        private RenderTexture _obstaclesDownsampledTexture;
        private RenderTexture _lightSourcesTexture;
        private RenderTexture _obstaclesTexture;

        private Camera _camera;
        private ObstacleCameraPostPorcessor _obstaclesPostProcessor;
        private Point2 _lightTextureSize;
        private Vector3 _oldPos;
        private Vector3 _currPos;
        private RenderTextureFormat _texFormat;
        private int _aditionalAmbientLightCycles = 0;
        private RenderTexture _screenBlitTempTex;
        private static LightingSystem _instance;
#if LIGHT2D_2DTK
        private tk2dCamera _tk2dCamera;
#endif

        private float LightPixelsPerUnityMeter
        {
            get { return 1/LightPixelSize; }
        }

        public static LightingSystem Instance
        {
            get { return _instance != null ? _instance : (_instance = FindObjectOfType<LightingSystem>()); }
        }


        private void OnEnable()
        {
            _instance = this;
            _camera = GetComponent<Camera>();
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Shader.SetGlobalTexture("_ObstacleTex", Texture2D.whiteTexture);
                return;
            }
#endif

            if (LightCamera == null)
            {
                Debug.LogError(
                    "Lighting Camera in LightingSystem is null. Please, select Lighting Camera camera for lighting to work.");
                enabled = false;
                return;
            }
            if (LightOverlayMaterial == null)
            {
                Debug.LogError(
                    "LightOverlayMaterial in LightingSystem is null. Please, select LightOverlayMaterial camera for lighting to work.");
                enabled = false;
                return;
            }

            _camera = GetComponent<Camera>();

            if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
                HDR = false;
            _texFormat = HDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;

            var lightPixelsPerUnityMeter = LightPixelsPerUnityMeter;

            InitTK2D();

            if (_camera.orthographic)
            {
                var rawCamHeight = (_camera.orthographicSize + LightCameraSizeAdd)*2f;
                var rawCamWidth = (_camera.orthographicSize*_camera.aspect + LightCameraSizeAdd)*2f;

                _lightTextureSize = new Point2(
                    Mathf.RoundToInt(rawCamWidth*lightPixelsPerUnityMeter),
                    Mathf.RoundToInt(rawCamHeight*lightPixelsPerUnityMeter));
            }
            else
            {
                var lightCamHalfFov = (_camera.fieldOfView + LightCameraFovAdd) * Mathf.Deg2Rad / 2f;
                var lightCamSize = Mathf.Tan(lightCamHalfFov) * LightObstaclesDistance * 2;
                LightCamera.orthographicSize = lightCamSize/2f;

                var gameCamHalfFov = _camera.fieldOfView * Mathf.Deg2Rad / 2f;
                var gameCamSize = Mathf.Tan(gameCamHalfFov) * LightObstaclesDistance * 2;
                _camera.orthographicSize = gameCamSize/2f;

                var texHeight = Mathf.RoundToInt(lightCamSize / LightPixelSize);
                var texWidth = texHeight*_camera.aspect;
                _lightTextureSize = Point2.Round(new Vector2(texWidth, texHeight));
            }

            if (_lightTextureSize.x%2 != 0)
                _lightTextureSize.x++;
            if (_lightTextureSize.y%2 != 0)
                _lightTextureSize.y++;

            var obstacleTextureSize = _lightTextureSize*(LightObstaclesAntialiasing ? 2 : 1);

            _screenBlitTempTex = new RenderTexture(Screen.width, Screen.height, 0, _texFormat);

            LightCamera.orthographicSize = _lightTextureSize.y/(2f*lightPixelsPerUnityMeter);
            LightCamera.fieldOfView = _camera.fieldOfView + LightCameraFovAdd;
            LightCamera.orthographic = _camera.orthographic;

            _lightSourcesTexture = new RenderTexture(_lightTextureSize.x, _lightTextureSize.y,
                0, _texFormat);
            _obstaclesTexture = new RenderTexture(obstacleTextureSize.x, obstacleTextureSize.y,
                0, _texFormat);
            _ambientTexture = new RenderTexture(_lightTextureSize.x, _lightTextureSize.y,
                0, _texFormat);

            if (LightObstaclesAntialiasing)
            {
                _obstaclesDownsampledTexture = new RenderTexture(_lightTextureSize.x, _lightTextureSize.y,
                    0, _texFormat);
            }

            LightCamera.aspect = _lightTextureSize.x/(float) _lightTextureSize.y;

            _obstaclesPostProcessor = new ObstacleCameraPostPorcessor();
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying || Util.IsSceneViewFocused)
            {
                Shader.SetGlobalTexture("_ObstacleTex", Texture2D.whiteTexture);
                if (dest != null)
                    dest.DiscardContents();
                Graphics.Blit(src, dest);
                return;
            }
#endif

            Update2DTK();
            UpdateCamera();
            RenderObstacles();
            SetupShaders();
            RenderLightSources();
            RenderAmbientLight();
            RenderLightOverlay(src, dest);
        }

        void InitTK2D()
        {
#if LIGHT2D_2DTK
            _tk2dCamera = GetComponent<tk2dCamera>();
            if (_tk2dCamera != null && _tk2dCamera.CameraSettings.projection == tk2dCameraSettings.ProjectionType.Orthographic)
            {
                _camera.orthographic = true;
                _camera.orthographicSize = _tk2dCamera.ScreenExtents.yMax;
            }
#endif
        }

        void Update2DTK()
        {
#if LIGHT2D_2DTK
            if (_tk2dCamera != null && _tk2dCamera.CameraSettings.projection == tk2dCameraSettings.ProjectionType.Orthographic)
            {
                _camera.orthographic = true;
                _camera.orthographicSize = _tk2dCamera.ScreenExtents.yMax;
            }
#endif
        }

        private void LateUpdate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && LightCamera != null)
            {
                _camera = GetComponent<Camera>();
                if (_camera != null)
                {
                    InitTK2D();
                    LightCamera.orthographic = _camera.orthographic;
                    if (_camera.orthographic)
                    {
                        LightCamera.orthographicSize = _camera.orthographicSize + LightCameraSizeAdd;
                    }
                    else
                    {
                        LightCamera.fieldOfView = _camera.fieldOfView + LightCameraFovAdd;
                    }
                }
            }
            if (!Application.isPlaying || Util.IsSceneViewFocused)
            {
                Shader.SetGlobalTexture("_ObstacleTex", Texture2D.whiteTexture);
                return;
            }
#endif
        }

        private void RenderObstacles()
        {
            LightCamera.enabled = false;

            LightCamera.targetTexture = _obstaclesTexture;
            LightCamera.cullingMask = 1 << LightObstaclesLayer;
            LightCamera.backgroundColor = new Color(1, 1, 1, 0);

            _obstaclesPostProcessor.DrawMesh(LightCamera, LightObstaclesAntialiasing ? 2 : 1);

            LightCamera.Render();
            LightCamera.targetTexture = null;
            LightCamera.cullingMask = 0;
            LightCamera.backgroundColor = new Color(0, 0, 0, 0);

            if (LightObstaclesAntialiasing && _obstaclesDownsampledTexture != null)
            {
                _obstaclesDownsampledTexture.DiscardContents();
                Graphics.Blit(_obstaclesTexture, _obstaclesDownsampledTexture);
            }
        }

        private void SetupShaders()
        {
            var lightPixelsPerUnityMeter = LightPixelsPerUnityMeter;

            if (HDR) Shader.EnableKeyword("HDR");
            else Shader.DisableKeyword("HDR");

            if (_camera.orthographic) Shader.DisableKeyword("PERSPECTIVE_CAMERA");
            else Shader.EnableKeyword("PERSPECTIVE_CAMERA");

            Shader.SetGlobalTexture("_ObstacleTex",
                _obstaclesDownsampledTexture != null ? _obstaclesDownsampledTexture : _obstaclesTexture);
            Shader.SetGlobalFloat("_PixelsPerBlock", lightPixelsPerUnityMeter);
        }

        private void RenderLightSources()
        {
            LightCamera.targetTexture = _lightSourcesTexture;
            LightCamera.cullingMask = 1 << LightSourcesLayer;
            LightCamera.backgroundColor = new Color(0, 0, 0, 0);
            LightCamera.Render();
            LightCamera.targetTexture = null;
            LightCamera.cullingMask = 0;

            if (BlurLightSources && LightSourcesBlurMaterial != null)
            {
                Profiler.BeginSample("LightingSystem.OnRenderImage Bluring Light Sources");

                if (_bluredLightTexture == null)
                {
                    _bluredLightTexture = new RenderTexture(_lightTextureSize.x, _lightTextureSize.y, 0,
                        _texFormat);
                }

                _bluredLightTexture.DiscardContents();
                LightSourcesBlurMaterial.mainTexture = _lightSourcesTexture;
                Graphics.Blit(null, _bluredLightTexture, LightSourcesBlurMaterial);

                Profiler.EndSample();
            }
        }

        private void RenderAmbientLight()
        {
            if (EnableAmbientLight && AmbientLightComputeMaterial != null)
            {
                Profiler.BeginSample("LightingSystem.OnRenderImage Ambient Light");

                if (_ambientTexture == null)
                {
                    _ambientTexture =
                        new RenderTexture(_lightTextureSize.x, _lightTextureSize.y, 0, _texFormat);
                }
                if (_prevAmbientTexture == null)
                {
                    _prevAmbientTexture =
                        new RenderTexture(_lightTextureSize.x, _lightTextureSize.y, 0, _texFormat);
                }
                if (_ambientEmissionTexture == null)
                {
                    _ambientEmissionTexture =
                        new RenderTexture(_lightTextureSize.x, _lightTextureSize.y, 0, _texFormat);
                }

                if (EnableAmbientLight)
                {
                    LightCamera.targetTexture = _ambientEmissionTexture;
                    LightCamera.cullingMask = 1 << AmbientLightLayer;
                    LightCamera.backgroundColor = new Color(0, 0, 0, 0);
                    LightCamera.Render();
                    LightCamera.targetTexture = null;
                    LightCamera.cullingMask = 0;
                }

                for (int i = 0; i < _aditionalAmbientLightCycles + 1; i++)
                {
                    var tmp = _prevAmbientTexture;
                    _prevAmbientTexture = _ambientTexture;
                    _ambientTexture = tmp;

                    var texSize = new Vector2(_ambientTexture.width, _ambientTexture.height);
                    var posShift = ((Vector2) (_currPos - _oldPos)/LightPixelSize).Div(texSize);
                    _oldPos = _currPos;

                    AmbientLightComputeMaterial.SetTexture("_LightSourcesTex", _ambientEmissionTexture);
                    AmbientLightComputeMaterial.SetTexture("_MainTex", _prevAmbientTexture);
                    AmbientLightComputeMaterial.SetVector("_Shift", posShift);

                    _ambientTexture.DiscardContents();
                    Graphics.Blit(null, _ambientTexture, AmbientLightComputeMaterial);

                    if (BlurAmbientLight && AmbientLightBlurMaterial != null)
                    {
                        Profiler.BeginSample("LightingSystem.OnRenderImage Bluring Ambient Light");

                        _prevAmbientTexture.DiscardContents();
                        AmbientLightBlurMaterial.mainTexture = _ambientTexture;
                        Graphics.Blit(null, _prevAmbientTexture, AmbientLightBlurMaterial);

                        var tmpblur = _prevAmbientTexture;
                        _prevAmbientTexture = _ambientTexture;
                        _ambientTexture = tmpblur;

                        Profiler.EndSample();
                    }
                }

                _aditionalAmbientLightCycles = 0;
                Profiler.EndSample();
            }
        }

        private void RenderLightOverlay(RenderTexture src, RenderTexture dest)
        {
            Profiler.BeginSample("LightingSystem.OnRenderImage Light Overlay");

            Vector2 lightTexelSize = new Vector2(1f/_lightTextureSize.x, 1f/_lightTextureSize.y);
            float lightPixelsPerUnityMeter = LightPixelsPerUnityMeter;
            Vector2 worldOffest = LightCamera.transform.position - _camera.transform.position;
            Vector2 offest = Vector2.Scale(lightTexelSize, -worldOffest*lightPixelsPerUnityMeter);

            var lightSourcesTex = BlurLightSources && LightSourcesBlurMaterial != null
                ? _bluredLightTexture
                : _lightSourcesTexture;
            float xDiff = _camera.aspect/LightCamera.aspect;

            if (!_camera.orthographic)
            {
                var gameCamHalfFov = _camera.fieldOfView * Mathf.Deg2Rad / 2f;
                var gameCamSize = Mathf.Tan(gameCamHalfFov) * LightObstaclesDistance * 2;
                _camera.orthographicSize = gameCamSize / 2f;
            }

            float scaleY = _camera.orthographicSize/LightCamera.orthographicSize;
            var scale = new Vector2(scaleY*xDiff, scaleY);

            LightOverlayMaterial.SetTexture("_AmbientLightTex", EnableAmbientLight ? _ambientTexture : null);
            LightOverlayMaterial.SetTexture("_LightSourcesTex", lightSourcesTex);
            LightOverlayMaterial.SetTexture("_GameTex", src);
            LightOverlayMaterial.SetVector("_Offest", offest);
            LightOverlayMaterial.SetVector("_Scale", scale);

            if (_screenBlitTempTex == null || _screenBlitTempTex.width != src.width ||
                _screenBlitTempTex.height != src.height)
            {
                if (_screenBlitTempTex != null)
                    _screenBlitTempTex.Release();
                _screenBlitTempTex = new RenderTexture(src.width, src.height, 0, _texFormat);
            }

            _screenBlitTempTex.DiscardContents();
            Graphics.Blit(null, _screenBlitTempTex, LightOverlayMaterial);
            Graphics.Blit(_screenBlitTempTex, dest);

            Profiler.EndSample();

        }

        private void UpdateCamera()
        {
            var lightPixelsPerUnityMeter = LightPixelsPerUnityMeter;
            var mainPos = _camera.transform.position;
            var pos = new Vector3(
                Mathf.Round(mainPos.x*lightPixelsPerUnityMeter)/lightPixelsPerUnityMeter,
                Mathf.Round(mainPos.y*lightPixelsPerUnityMeter)/lightPixelsPerUnityMeter,
                mainPos.z);
            LightCamera.transform.position = pos;
            _currPos = pos;
        }

        public void LoopAmbientLight(int cycles)
        {
            _aditionalAmbientLightCycles += cycles;
        }
    }
}