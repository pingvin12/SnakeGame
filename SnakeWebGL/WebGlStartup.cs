using System;

namespace SnakeWebGL;

internal static class WebGlStartup
{
    public static IntPtr EnsureContext(Action<string> log)
    {
        log("[WebGL] Creating context via emscripten");

        Emscripten.WebGlInitContextAttributes(out var attributes);
        attributes.alpha = true;
        attributes.depth = true;
        attributes.stencil = false;
        attributes.antialias = true;
        attributes.majorVersion = 2;
        attributes.minorVersion = 0;
        attributes.enableExtensionsByDefault = true;

        var context = Emscripten.WebGlCreateContext("#canvas", ref attributes);
        log($"[WebGL] Created context handle=0x{context.ToInt64():X} with attributes {attributes}");

        if (context == IntPtr.Zero)
        {
            throw new InvalidOperationException("emscripten_webgl_create_context returned null; check canvas availability and browser WebGL support.");
        }

        var makeCurrentResult = Emscripten.WebGlMakeContextCurrent(context);
        if (makeCurrentResult != 0)
        {
            log("[WebGL] Context made current successfully");
        }
        else
        {
            throw new InvalidOperationException("emscripten_webgl_make_context_current failed to bind the WebGL context");
        }

        return context;
    }
}
