using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.CustomRP.MyRP
{
	[CreateAssetMenu(menuName = "Rendering/MyRP")]
	public class MyRenderPipelineAsset : RenderPipelineAsset
	{
		protected override RenderPipeline CreatePipeline()
		{
			return new MyRenderPipeline();
		}
	}
}
