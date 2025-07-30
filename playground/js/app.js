export function captureConsoleOutput(dotNetObject) {
    const originalConsole = {
        log: console.log,
        error: console.error,
        warn: console.warn
    };

    console.log = function (message) {
        originalConsole.log.apply(console, arguments);
        dotNetObject.invokeMethodAsync('OnConsoleLog', message);
    };

    console.error = function (message) {
        originalConsole.error.apply(console, arguments);
        dotNetObject.invokeMethodAsync('OnConsoleError', message);
    };

    console.warn = function (message) {
        originalConsole.warn.apply(console, arguments);
        dotNetObject.invokeMethodAsync('OnConsoleWarn', message);
    };
}