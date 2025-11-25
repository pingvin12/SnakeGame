// @ts-ignore
import { dotnet } from './_framework/dotnet.js';

const { setModuleImports, getAssemblyExports, getConfig, runMain } = await dotnet
    //.withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    //.withApplicationArguments("start")
    .create();
const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
const interop = exports.SnakeWebGL.Interop;

var canvas = globalThis.document.getElementById("canvas") as HTMLCanvasElement;
dotnet.instance.Module["canvas"] = canvas;

const keyBoard: { [key: string]: any } = {
    prevKeys: {},
    currKeys: {}
}

const mouse = {
    x: 0,
    y: 0
}

const resizeObserver = new ResizeObserver(onResize);
try {
    // only call us of the number of device pixels changed
    resizeObserver.observe(canvas, { box: 'device-pixel-content-box' });
} catch (ex) {
    // device-pixel-content-box is not supported so fallback to this
    resizeObserver.observe(canvas, { box: 'content-box' });
}

setModuleImports("main.js", {
    initialize: () => {
        function step() {
            requestAnimationFrame(step); // The callback only called after this method returns.
        }

        var keyDown = (e: KeyboardEvent) => {
            keyBoard.currKeys[e.code] = false;
        };

        var keyUp = (e: KeyboardEvent) => {
            keyBoard.currKeys[e.code] = true;
        };

        var mouseMove = (e: MouseEvent) => {
            const rect = canvas.getBoundingClientRect();
            const dpr = window.devicePixelRatio || 1;
            mouse.x = (e.clientX - rect.left) * dpr;
            mouse.y = (e.clientY - rect.top) * dpr;
            interop?.OnMouseMove(mouse.x, mouse.y);
        };

        var mouseDown = (e: MouseEvent) => {
            keyBoard.currKeys[`Mouse${e.button}`] = false;
            const rect = canvas.getBoundingClientRect();
            const dpr = window.devicePixelRatio || 1;
            const x = (e.clientX - rect.left) * dpr;
            const y = (e.clientY - rect.top) * dpr;
            interop?.OnMouseDown(e.shiftKey, e.ctrlKey, e.altKey, e.button, x, y);
        };

        var mouseUp = (e: MouseEvent) => {
            keyBoard.currKeys[`Mouse${e.button}`] = true;
            const rect = canvas.getBoundingClientRect();
            const dpr = window.devicePixelRatio || 1;
            const x = (e.clientX - rect.left) * dpr;
            const y = (e.clientY - rect.top) * dpr;
            interop?.OnMouseUp(e.shiftKey, e.ctrlKey, e.altKey, e.button, x, y);
        };

        canvas.addEventListener("keydown", keyDown, false);
        canvas.addEventListener("keyup", keyUp, false);
        canvas.addEventListener("mousemove", mouseMove, false);
        canvas.addEventListener("mousedown", mouseDown, false);
        canvas.addEventListener("mouseup", mouseUp, false);
        step();
    },

    updateInput: () => {
        keyBoard.prevKeys = { ...keyBoard.currKeys };
    },

    isKeyPressed: (key: string) => {
        return !keyBoard.currKeys[key] && keyBoard.prevKeys[key];
    }
});

//await runMain();
await dotnet.run();

function onResize(entries: ResizeObserverEntry[]) {
    for (const entry of entries) {
        let width;
        let height;
        let dpr = window.devicePixelRatio;
        if (entry.devicePixelContentBoxSize) {
            // NOTE: Only this path gives the correct answer
            // The other paths are imperfect fallbacks
            // for browsers that don't provide anyway to do this
            width = entry.devicePixelContentBoxSize[0].inlineSize;
            height = entry.devicePixelContentBoxSize[0].blockSize;
            dpr = 1; // it's already in width and height
        } else if (entry.contentBoxSize) {
            if (entry.contentBoxSize[0]) {
                width = entry.contentBoxSize[0].inlineSize;
                height = entry.contentBoxSize[0].blockSize;
            } else {
                // but old versions of Firefox treat it as a single item
                // @ts-ignore
                width = entry.contentBoxSize?.inlineSize;
                // @ts-ignore
                height = entry.contentBoxSize?.blockSize;
            }
        } else {
            width = entry.contentRect.width;
            height = entry.contentRect.height;
        }
        const displayWidth = Math.round(width * dpr);
        const displayHeight = Math.round(height * dpr);
        interop?.OnCanvasResize(canvas.width, canvas.height);
    }
}
