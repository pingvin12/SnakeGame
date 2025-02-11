// @ts-ignore
import { dotnet } from './_framework/dotnet.js';
import * as Sentry from "@sentry/browser";

const { setModuleImports, getAssemblyExports, getConfig } = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();
const config = getConfig();
const isDebug = config?.applicationEnvironment == 'Development';

const exports = await getAssemblyExports(config.mainAssemblyName);

console.log(`Is debug: ${isDebug}`);
console.log(`C# exports: ${exports}`);
console.log(`dotnet: ${dotnet}`);
console.log(`config: ${config}`);

const interop = exports.WebGL.Sample.Interop;

var canvas = globalThis.document.getElementById("canvas") as HTMLCanvasElement;
dotnet.instance.Module["canvas"] = canvas;

const keyBoard: { [key: string]: any } = {
    prevKeys: {},
    currKeys: {}
}

setModuleImports("main.js", {
    initialize: () => {
        var checkCanvasResize = (dispatch: boolean) => {
            var devicePixelRatio = window.devicePixelRatio || 1.0;
            var displayWidth = window.innerWidth * devicePixelRatio;
            var displayHeight = window.innerHeight * devicePixelRatio;

            if (canvas.width != displayWidth || canvas.height != displayHeight) {
                canvas.width = displayWidth;
                canvas.height = displayHeight;
                dispatch = true;
            }

            if (dispatch) interop.OnCanvasResize(displayWidth, displayHeight, devicePixelRatio);
        }

        function checkCanvasResizeFrame() {
            checkCanvasResize(false);
            requestAnimationFrame(checkCanvasResizeFrame); // The callback only called after this method returns.
        }

        var keyDown = (e: KeyboardEvent) => {
            keyBoard.currKeys[e.code] = false;
        };

        var keyUp = (e: KeyboardEvent) => {
            keyBoard.currKeys[e.code] = true;
        };

        canvas.addEventListener("keydown", keyDown, false);
        canvas.addEventListener("keyup", keyUp, false);
        checkCanvasResize(true);
        checkCanvasResizeFrame();

        canvas.tabIndex = 1000;
    },

    updateInput: () => {
        keyBoard.prevKeys = { ...keyBoard.currKeys };
    },

    isKeyPressed: (key: string) => {
        var res = !keyBoard.currKeys[key] && keyBoard.prevKeys[key];

        return res;
    }
});

await dotnet.run();