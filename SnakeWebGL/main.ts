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

function domReady(): Promise<void> {
    if (globalThis.document.readyState === "complete" || globalThis.document.readyState === "interactive") {
        return Promise.resolve();
    }

    return new Promise((resolve) => {
        globalThis.document.addEventListener("DOMContentLoaded", () => resolve(), { once: true });
    });
}

async function ensureCanvas(): Promise<HTMLCanvasElement> {
    await domReady();

    let canvas = globalThis.document.getElementById("canvas") as HTMLCanvasElement | null;
    if (!canvas) {
        canvas = globalThis.document.createElement("canvas") as HTMLCanvasElement;
        canvas.id = "canvas";
        globalThis.document.body.appendChild(canvas);
    }

    return canvas;
}

const canvas = await ensureCanvas();
dotnet.instance.Module["canvas"] = canvas;

function resizeCanvasToDisplaySize(entry?: ResizeObserverEntry) {
    let width: number;
    let height: number;
    let dpr = window.devicePixelRatio;

    if (entry?.devicePixelContentBoxSize) {
        width = entry.devicePixelContentBoxSize[0].inlineSize;
        height = entry.devicePixelContentBoxSize[0].blockSize;
        dpr = 1; // already in physical pixels
    } else if (entry?.contentBoxSize) {
        if (Array.isArray(entry.contentBoxSize) && entry.contentBoxSize[0]) {
            width = entry.contentBoxSize[0].inlineSize;
            height = entry.contentBoxSize[0].blockSize;
        } else {
            // Firefox legacy path
            // @ts-ignore
            width = entry?.contentBoxSize?.inlineSize;
            // @ts-ignore
            height = entry?.contentBoxSize?.blockSize;
        }
    } else if (entry?.contentRect) {
        width = entry.contentRect.width;
        height = entry.contentRect.height;
    } else {
        // Fallback to current CSS size if no observer entry is provided
        const rect = canvas.getBoundingClientRect();
        width = rect.width;
        height = rect.height;
    }

    if (!width || !height) {
        width = globalThis.innerWidth;
        height = globalThis.innerHeight;
    }

    const displayWidth = Math.round(width * dpr);
    const displayHeight = Math.round(height * dpr);

    if (canvas.width !== displayWidth || canvas.height !== displayHeight) {
        canvas.width = displayWidth;
        canvas.height = displayHeight;
    }

    interop?.OnCanvasResize(canvas.width, canvas.height);
}

resizeCanvasToDisplaySize();

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
    ensureCanvasReady: () => {
        resizeCanvasToDisplaySize();
        const ready = Boolean(canvas && canvas.width > 0 && canvas.height > 0);
        console.log(`[SnakeWebGL] ensureCanvasReady ready=${ready} size=${canvas.width}x${canvas.height} dpr=${window.devicePixelRatio}`);
        return ready;
    },

    initialize: () => {
        function step() {
            requestAnimationFrame(step); // The callback only called after this method returns.
        }

        var keyDown = (e: KeyboardEvent) => {
            keyBoard.currKeys[e.code] = true;
        };

        var keyUp = (e: KeyboardEvent) => {
            keyBoard.currKeys[e.code] = false;
        };

        var mouseMove = (e: MouseEvent) => {
            const rect = canvas.getBoundingClientRect();
            const dpr = window.devicePixelRatio || 1;
            mouse.x = (e.clientX - rect.left) * dpr;
            mouse.y = (e.clientY - rect.top) * dpr;
            interop?.OnMouseMove(mouse.x, mouse.y);
        };

        var mouseDown = (e: MouseEvent) => {
            keyBoard.currKeys[`Mouse${e.button}`] = true;
            const rect = canvas.getBoundingClientRect();
            const dpr = window.devicePixelRatio || 1;
            const x = (e.clientX - rect.left) * dpr;
            const y = (e.clientY - rect.top) * dpr;
            interop?.OnMouseDown(e.shiftKey, e.ctrlKey, e.altKey, e.button, x, y);
        };

        var mouseUp = (e: MouseEvent) => {
            keyBoard.currKeys[`Mouse${e.button}`] = false;
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
        console.log("[SnakeWebGL] Input listeners attached");
        step();
    },

    updateInput: () => {
        keyBoard.prevKeys = { ...keyBoard.currKeys };
    },

    isKeyPressed: (key: string) => {
        const current = Boolean(keyBoard.currKeys[key]);
        const previous = Boolean(keyBoard.prevKeys[key]);
        return current && !previous;
    }
});

//await runMain();
await dotnet.run();

function onResize(entries: ResizeObserverEntry[]) {
    for (const entry of entries) {
        resizeCanvasToDisplaySize(entry);
    }
}
