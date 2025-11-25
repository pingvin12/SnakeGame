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
        var canvasReady = Interop.EnsureCanvasReady();
        Console.WriteLine($"[Startup] Canvas ready: {canvasReady}");

        var eglStartup = new EglStartup(new EglApi(), Console.WriteLine);
        bool eglActive = false;

        if (eglStartup.IsSupported())
        {
            try
            {
                var eglHandles = eglStartup.Initialize();
                eglActive = true;
                Console.WriteLine($"[Startup] EGL handles display=0x{eglHandles.Display.ToInt64():X}, config=0x{eglHandles.Config.ToInt64():X}, context=0x{eglHandles.Context.ToInt64():X}, surface=0x{eglHandles.Surface.ToInt64():X} (v{eglHandles.MajorVersion}.{eglHandles.MinorVersion})");
            }
            catch (DllNotFoundException ex)
            {
                Console.Error.WriteLine($"[Startup] EGL unavailable: {ex.Message}. Falling back to emscripten WebGL.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Startup] EGL initialization failed: {ex}");
            }
        }

        if (!eglActive)
        {
            try
            {
                var context = WebGlStartup.EnsureContext(Console.WriteLine);
                Console.WriteLine($"[Startup] WebGL context 0x{context.ToInt64():X} ready via emscripten");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Startup] WebGL fallback failed: {ex}");
                throw;
            }
        }

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
