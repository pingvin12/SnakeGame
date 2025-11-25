using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Silk.NET.OpenGLES;
using SnakeWebGL;
using WebGL.Sample;

[assembly: SupportedOSPlatform("browser")]
[assembly: DisableRuntimeMarshalling]

namespace SnakeWebGL;

public static class Program
{
    private static Game? Game { get; set; }

    private static double? currentTime = null; // Milliseconds, 100 is 0.1 Seconds
    private static double accumulator = 0; // Seconds, 1.5 is 1 second and 500 millisecond

    [UnmanagedCallersOnly]
    public static int Frame(double newTime, int userData)
    {
        ArgumentNullException.ThrowIfNull(Game);

        const float dt = 1 / 60f; // 60 FPS (1 / 60 = 0.01666666)
        currentTime ??= newTime;

        var frameTime = (newTime - (int)currentTime) / 1000;
        accumulator += frameTime;

        var direction = SnakeCore.Direction.None;
        if (Interop.IsKeyPressed(Keys.W))
            direction = SnakeCore.Direction.Up;
        else if (Interop.IsKeyPressed(Keys.D))
            direction = SnakeCore.Direction.Right;
        else if (Interop.IsKeyPressed(Keys.S))
            direction = SnakeCore.Direction.Down;
        else if (Interop.IsKeyPressed(Keys.A))
            direction = SnakeCore.Direction.Left;

        var startRequested = Interop.IsKeyPressed("Space") ||
                             Interop.IsKeyPressed("Enter") ||
                             Interop.IsKeyPressed("Mouse0");

        if(accumulator >= dt)
        {
            Interop.UpdateInput();
        }

        while (accumulator >= dt)
        {
            Game.Update(dt, direction, startRequested);
            accumulator -= dt;
        }

        Game.Draw();

        currentTime = newTime;
        return 1; // The return value should be a bolean, false if the Frame is canceled and the animation should stop
    }

    public static void CanvasResized(int width, int height)
    {
        Game?.CanvasResized(width, height);
    }

    public static void OnMouseMove(float x, float y)
    {
    }

    public static void OnMouseDown(int button, float x, float y)
    {
        if (Game == null)
            return;

        Game.HandlePointerDown(new Vector2(x, y), button);
    }

    public static void OnMouseUp(int button, float x, float y)
    {
    }

    public static void Main(string[] args)
    {
        Console.WriteLine($"Hello from dotnet 9!");

        // Ensure the JS side has already hooked up the canvas and input handlers before creating the GL context.
        Interop.Initialize();
        Interop.EnsureCanvasReady();

        var display = EGL.GetDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero)
            throw new Exception("Display was null; ensure the canvas element is available and WebGL is enabled.");

        if (!EGL.Initialize(display, out int major, out int minor))
            throw new Exception($"Initialize() returned false (error: 0x{EGL.GetError():X}).");

        int[] attributeListMsaa = new int[]
        {
            EGL.EGL_RED_SIZE  , 8,
            EGL.EGL_GREEN_SIZE, 8,
            EGL.EGL_BLUE_SIZE , 8,
            EGL.EGL_SAMPLES, 16, //MSAA, 16 samples
            EGL.EGL_NONE
        };

        int[] attributeListNoMsaa = new int[]
        {
            EGL.EGL_RED_SIZE  , 8,
            EGL.EGL_GREEN_SIZE, 8,
            EGL.EGL_BLUE_SIZE , 8,
            EGL.EGL_NONE
        };

        var config = IntPtr.Zero;
        var numConfig = IntPtr.Zero;

        static bool TryChooseConfig(IntPtr displayHandle, int[] attributes, ref IntPtr chosenConfig, ref IntPtr availableConfig)
        {
            return EGL.ChooseConfig(displayHandle, attributes, ref chosenConfig, (IntPtr)1, ref availableConfig) && availableConfig != IntPtr.Zero;
        }

        if (!TryChooseConfig(display, attributeListMsaa, ref config, ref numConfig))
        {
            Console.WriteLine("Falling back to a non-MSAA EGL config.");
            config = IntPtr.Zero;
            numConfig = IntPtr.Zero;

            if (!TryChooseConfig(display, attributeListNoMsaa, ref config, ref numConfig))
            {
                throw new Exception($"ChooseConfig() failed (error: 0x{EGL.GetError():X}).");
            }
        }

        if (!EGL.BindApi(EGL.EGL_OPENGL_ES_API))
            throw new Exception($"BindApi() failed (error: 0x{EGL.GetError():X}).");

        // No other attribute is supported...
        int[] ctxAttribs = new int[]
        {
            EGL.EGL_CONTEXT_CLIENT_VERSION, 3,
            EGL.EGL_NONE 
        };

        var context = EGL.CreateContext(display, config, (IntPtr)EGL.EGL_NO_CONTEXT, ctxAttribs);
        if (context == IntPtr.Zero)
            throw new Exception($"CreateContext() failed (error: 0x{EGL.GetError():X}).");

        // now create the surface
        var surface = EGL.CreateWindowSurface(display, config, IntPtr.Zero, IntPtr.Zero);
        if (surface == IntPtr.Zero)
            throw new Exception($"CreateWindowSurface() failed (error: 0x{EGL.GetError():X}).");

        if (!EGL.MakeCurrent(display, surface, surface, context))
            throw new Exception($"MakeCurrent() failed (error: 0x{EGL.GetError():X}).");

        //_ = EGL.DestroyContext(display, context);
        //_ = EGL.DestroySurface(display, surface);
        //_ = EGL.Terminate(display);

        TrampolineFuncs.ApplyWorkaroundFixingInvocations();
        
        var gl = GL.GetApi(EGL.GetProcAddress);

        Game = Game.Create(gl);

        unsafe
        {
            // https://emscripten.org/docs/api_reference/html5.h.html?highlight=emscripten_request_animation_frame#c.emscripten_request_animation_frame_loop
            Emscripten.RequestAnimationFrameLoop((delegate* unmanaged<double, int, int>)&Frame, nint.Zero);
        }
    }
}
