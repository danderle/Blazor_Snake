using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Blazor_Snake
{
    public static class JsInterop
    {
        public static Action<int> JsKeyUpAction;

        [JSInvokable]
        public static async void JsKeyUp(KeyboardEventArgs e)
        {
            var code = int.Parse(e.Code);
            if(code >= 37 && code <= 40)
            {
                JsKeyUpAction?.Invoke(code);
                await Task.Delay(1);
            }
        }

    }
}
