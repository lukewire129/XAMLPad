using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using OpenSilver.WebAssembly;

namespace XAMLPad.Browser.Pages
{
    [Route("/")]
    [Route ("/index.html")]    // 파일명 직접 호출 경로 (https://.../XAMLPad/index.html)
    public class Index : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
        }

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await Runner.RunApplicationAsync<XAMLPad.App>();
        }
    }
}