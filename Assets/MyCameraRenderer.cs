using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.CustomRP.MyRP
{
    public class MyCameraRenderer
    {
        const string bufferName = "Render Camera";

        static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
#if UNITY_EDITOR
        static ShaderTagId[] legacyShaderTagIds =
        {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM"),
        };

        static Material errorMaterial;
#endif

        CommandBuffer buffer = new CommandBuffer
        {
            name = bufferName
        };

        CullingResults cullingResults;

        ScriptableRenderContext context;

        Camera camera;

        public void Render(ScriptableRenderContext context, Camera camera)
        {
            this.context = context;
            this.camera = camera;

            PrepareBuffer();
            PrepareForSceneWindow();
            if (!Cull())
            {
                return;
            }

            Setup();
            DrawVisibleGeometry();
            //DrawUnsupportedShaders();
            Submit();
        }

        void PrepareBuffer()
        {
#if UNITY_EDITOR
            buffer.name = camera.name;
#endif
        }

        void PrepareForSceneWindow()
        {
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
#endif
        }

        /// <summary>
        /// カリング
        /// </summary>
        /// <returns></returns>
        bool Cull()
        {
            if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
            {
                // カメラの視錘台の中のオブジェクトをcullingResultsとして保存
                cullingResults = context.Cull(ref p);
                return true;
            }
            return false;
        }

        void Setup()
        {
            // カメラの状態を適用
            context.SetupCameraProperties(camera);
            CameraClearFlags flags = camera.clearFlags;
            // RenderTargetを掃除(ColorバッファとDepthバッファもクリアされる）
            //buffer.ClearRenderTarget(true, true, Color.clear);
            buffer.ClearRenderTarget(
                flags <= CameraClearFlags.Depth,
                flags == CameraClearFlags.Color,
                flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear
            );
            // bufferのサンプリング開始
            buffer.BeginSample(bufferName);
            ExecuteBuffer();
        }

        void DrawVisibleGeometry()
        {
            // 不透過オブジェクトだけ先に描画
            var sortingSettings = new SortingSettings(camera){ criteria = SortingCriteria.CommonOpaque };
            var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
            //var filteringSettings = new FilteringSettings(RenderQueueRange.all);
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            context.DrawRenderers(
                cullingResults, ref drawingSettings, ref filteringSettings
            );


            // Skyboxを処理
            context.DrawSkybox(camera);

            // 透過オブジェクトを処理
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;
            context.DrawRenderers(
                cullingResults, ref drawingSettings, ref filteringSettings
            );

            //context.DrawSkybox(camera);
        }

        void DrawUnsupportedShaders()
        {
#if UNITY_EDITOR
            if (errorMaterial == null)
            {
                errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            }

            var drawingSettings = new DrawingSettings(
                legacyShaderTagIds[0], new SortingSettings(camera)
            ){
                overrideMaterial = errorMaterial
            };

            for (int i = 1; i < legacyShaderTagIds.Length; i++)
            {
                drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
            }
            var filteringSettings = FilteringSettings.defaultValue;
            context.DrawRenderers(
                cullingResults, ref drawingSettings, ref filteringSettings
            );
#endif
        }

        void Submit()
        {
            // bufferのサンプリング終了
            buffer.EndSample(bufferName);
            ExecuteBuffer();
            // レンダーターゲットに書き込む
            context.Submit();
        }

        void ExecuteBuffer()
        {
            // CommandBuffer実行
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }
    }
}