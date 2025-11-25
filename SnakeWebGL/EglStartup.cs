using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SnakeWebGL;

internal interface IEglApi
{
    IntPtr GetDisplay(IntPtr displayId);
    bool Initialize(IntPtr display, out int major, out int minor);
    bool ChooseConfig(IntPtr display, int[] attributeList, ref IntPtr config, IntPtr configSize, ref IntPtr numConfig);
    bool BindApi(int api);
    IntPtr CreateContext(IntPtr display, IntPtr config, IntPtr shareContext, int[] contextAttribs);
    IntPtr CreateWindowSurface(IntPtr display, IntPtr config, IntPtr window, IntPtr attribList);
    bool MakeCurrent(IntPtr display, IntPtr draw, IntPtr read, IntPtr context);
    int GetError();
}

internal sealed class EglApi : IEglApi
{
    public IntPtr GetDisplay(IntPtr displayId) => EGL.GetDisplay(displayId);

    public bool Initialize(IntPtr display, out int major, out int minor) => EGL.Initialize(display, out major, out minor);

    public bool ChooseConfig(IntPtr display, int[] attributeList, ref IntPtr config, IntPtr configSize, ref IntPtr numConfig)
        => EGL.ChooseConfig(display, attributeList, ref config, configSize, ref numConfig);

    public bool BindApi(int api) => EGL.BindApi(api);

    public IntPtr CreateContext(IntPtr display, IntPtr config, IntPtr shareContext, int[] contextAttribs)
        => EGL.CreateContext(display, config, shareContext, contextAttribs);

    public IntPtr CreateWindowSurface(IntPtr display, IntPtr config, IntPtr window, IntPtr attribList)
        => EGL.CreateWindowSurface(display, config, window, attribList);

    public bool MakeCurrent(IntPtr display, IntPtr draw, IntPtr read, IntPtr context)
        => EGL.MakeCurrent(display, draw, read, context);

    public int GetError() => EGL.GetError();
}

internal readonly record struct EglHandles(IntPtr Display, IntPtr Config, IntPtr Context, IntPtr Surface, int MajorVersion, int MinorVersion);

internal sealed class EglStartup
{
    private readonly IEglApi _egl;
    private readonly Action<string> _log;

    private static readonly int[] AttributeListMsaa =
    {
        EGL.EGL_RED_SIZE, 8,
        EGL.EGL_GREEN_SIZE, 8,
        EGL.EGL_BLUE_SIZE, 8,
        EGL.EGL_SAMPLES, 16,
        EGL.EGL_NONE
    };

    private static readonly int[] AttributeListNoMsaa =
    {
        EGL.EGL_RED_SIZE, 8,
        EGL.EGL_GREEN_SIZE, 8,
        EGL.EGL_BLUE_SIZE, 8,
        EGL.EGL_NONE
    };

    private static readonly int[] ContextAttributes =
    {
        EGL.EGL_CONTEXT_CLIENT_VERSION, 3,
        EGL.EGL_NONE
    };

    public EglStartup(IEglApi eglApi, Action<string>? log)
    {
        _egl = eglApi ?? throw new ArgumentNullException(nameof(eglApi));
        _log = log ?? (_ => { });
    }

    public EglHandles Initialize()
    {
        _log("[EGL] Starting initialization");

        var display = _egl.GetDisplay(IntPtr.Zero);
        _log($"[EGL] GetDisplay returned 0x{display.ToInt64():X}");
        if (display == IntPtr.Zero)
            throw new InvalidOperationException($"EGL.GetDisplay returned null (error: 0x{_egl.GetError():X}). Ensure a canvas is available and WebGL is enabled.");

        if (!_egl.Initialize(display, out int major, out int minor))
            throw new InvalidOperationException($"EGL.Initialize failed (error: 0x{_egl.GetError():X}).");

        _log($"[EGL] Initialized version {major}.{minor}");

        var config = PickConfig(display);

        if (!_egl.BindApi(EGL.EGL_OPENGL_ES_API))
            throw new InvalidOperationException($"EGL.BindApi failed (error: 0x{_egl.GetError():X}).");

        var context = _egl.CreateContext(display, config, (IntPtr)EGL.EGL_NO_CONTEXT, ContextAttributes);
        _log($"[EGL] CreateContext returned 0x{context.ToInt64():X}");
        if (context == IntPtr.Zero)
            throw new InvalidOperationException($"EGL.CreateContext failed (error: 0x{_egl.GetError():X}).");

        var surface = _egl.CreateWindowSurface(display, config, IntPtr.Zero, IntPtr.Zero);
        _log($"[EGL] CreateWindowSurface returned 0x{surface.ToInt64():X}");
        if (surface == IntPtr.Zero)
            throw new InvalidOperationException($"EGL.CreateWindowSurface failed (error: 0x{_egl.GetError():X}).");

        if (!_egl.MakeCurrent(display, surface, surface, context))
            throw new InvalidOperationException($"EGL.MakeCurrent failed (error: 0x{_egl.GetError():X}).");

        _log("[EGL] Context made current successfully");

        return new EglHandles(display, config, context, surface, major, minor);
    }

    private IntPtr PickConfig(IntPtr display)
    {
        _log($"[EGL] Selecting config with MSAA attributes: {DescribeAttributes(AttributeListMsaa)}");
        if (TryChooseConfig(display, AttributeListMsaa, out var config))
        {
            _log($"[EGL] Selected MSAA config 0x{config.ToInt64():X}");
            return config;
        }

        _log("[EGL] MSAA config unavailable, retrying without MSAA");

        if (TryChooseConfig(display, AttributeListNoMsaa, out config))
        {
            _log($"[EGL] Selected non-MSAA config 0x{config.ToInt64():X}");
            return config;
        }

        throw new InvalidOperationException($"EGL.ChooseConfig failed for both attribute sets (last error: 0x{_egl.GetError():X}).");
    }

    private bool TryChooseConfig(IntPtr display, int[] attributes, out IntPtr config)
    {
        config = IntPtr.Zero;
        var numConfig = IntPtr.Zero;
        var result = _egl.ChooseConfig(display, attributes, ref config, (IntPtr)1, ref numConfig) && numConfig != IntPtr.Zero;
        if (!result)
        {
            _log($"[EGL] ChooseConfig failed for attributes {DescribeAttributes(attributes)} (error: 0x{_egl.GetError():X})");
        }

        return result;
    }

    private static string DescribeAttributes(IReadOnlyList<int> attributes)
    {
        var writer = new DefaultInterpolatedStringHandler(0, 0);

        writer.AppendLiteral("[");
        for (int i = 0; i < attributes.Count; i += 2)
        {
            var key = attributes[i];
            if (key == EGL.EGL_NONE)
                break;

            var value = (i + 1) < attributes.Count ? attributes[i + 1] : 0;
            writer.AppendFormatted(key);
            writer.AppendLiteral("=");
            writer.AppendFormatted(value);
            writer.AppendLiteral(", ");
        }

        writer.AppendLiteral("]");

        var result = writer.ToStringAndClear();
        if (result == "[")
            return "[]";

        if (result.EndsWith(", ]"))
            result = result[..^3] + "]";

        return result;
    }
}
