using System;
using System.Collections.Generic;
using Xunit;

namespace SnakeWebGL.Tests;

internal sealed class FakeEgl : IEglApi
{
    public IntPtr DisplayHandle { get; set; } = (IntPtr)1;
    public bool InitializeResult { get; set; } = true;
    public Queue<bool> ChooseConfigSequence { get; set; } = new(new[] { true });
    public Queue<IntPtr> ConfigHandles { get; set; } = new(new[] { (IntPtr)2 });
    public bool BindApiResult { get; set; } = true;
    public IntPtr ContextHandle { get; set; } = (IntPtr)3;
    public IntPtr SurfaceHandle { get; set; } = (IntPtr)4;
    public bool MakeCurrentResult { get; set; } = true;
    public int ErrorCode { get; set; } = unchecked((int)0xBAD); // Arbitrary default

    public IntPtr GetDisplay(IntPtr displayId) => DisplayHandle;

    public bool Initialize(IntPtr display, out int major, out int minor)
    {
        major = 3;
        minor = 0;
        return InitializeResult;
    }

    public bool ChooseConfig(IntPtr display, int[] attributeList, ref IntPtr config, IntPtr configSize, ref IntPtr numConfig)
    {
        if (ChooseConfigSequence.Count == 0)
        {
            return false;
        }

        var result = ChooseConfigSequence.Dequeue();
        if (result)
        {
            config = ConfigHandles.Count > 0 ? ConfigHandles.Dequeue() : (IntPtr)99;
            numConfig = (IntPtr)1;
        }
        else
        {
            config = IntPtr.Zero;
            numConfig = IntPtr.Zero;
        }

        return result;
    }

    public bool BindApi(int api) => BindApiResult;

    public IntPtr CreateContext(IntPtr display, IntPtr config, IntPtr shareContext, int[] contextAttribs) => ContextHandle;

    public IntPtr CreateWindowSurface(IntPtr display, IntPtr config, IntPtr window, IntPtr attribList) => SurfaceHandle;

    public bool MakeCurrent(IntPtr display, IntPtr draw, IntPtr read, IntPtr context) => MakeCurrentResult;

    public int GetError() => ErrorCode;
}

public class EglStartupTests
{
    [Fact]
    public void FallsBackWhenMsaaConfigUnavailable()
    {
        var fake = new FakeEgl
        {
            ChooseConfigSequence = new Queue<bool>(new[] { false, true }),
            ConfigHandles = new Queue<IntPtr>(new[] { (IntPtr)11, (IntPtr)22 })
        };

        var logs = new List<string>();
        var startup = new EglStartup(fake, logs.Add);

        var handles = startup.Initialize();

        Assert.Equal((IntPtr)22, handles.Config);
        Assert.Contains(logs, l => l.Contains("MSAA config unavailable"));
        Assert.Contains(logs, l => l.Contains("Selected non-MSAA config"));
    }

    [Fact]
    public void SurfaceErrorsBubbleWithErrorCode()
    {
        var fake = new FakeEgl
        {
            DisplayHandle = IntPtr.Zero,
            ErrorCode = 0x3001
        };

        var startup = new EglStartup(fake, _ => { });

        var exception = Assert.Throws<InvalidOperationException>(() => startup.Initialize());
        Assert.Contains("0x3001", exception.Message);
    }
}
