using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.CustomRP.MyRP
{
    public class MyRenderPipeline : RenderPipeline
    {
        MyCameraRenderer renderer = new MyCameraRenderer();

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (Camera camera in cameras)
            {
                renderer.Render(context, camera);
            }
        }
    }
}