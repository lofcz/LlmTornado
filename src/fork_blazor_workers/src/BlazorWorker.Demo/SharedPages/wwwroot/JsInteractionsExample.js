// This script will run on the worker.
self.jsInteractionsExample = async (JsInteractionsExampleWorkerService) => {
    console.log(`Interacting with worker Js on worker thread.`);
    await JsInteractionsExampleWorkerService.invokeMethodAsync("CallbackFromJavascript", 'Callback to dotnet instance method');

    // Calling static methods can be done like this
    var demoExports = await self.BlazorWorker.getAssemblyExports("BlazorWorker.Demo.Shared")
    var methodResult = demoExports.BlazorWorker.Demo.Shared.JsInteractionsExampleWorkerService.StaticCallbackFromJs("Static methods are cheaper to call.");

    await JsInteractionsExampleWorkerService.invokeMethodAsync("CallbackFromJavascript", 'StaticCallbackFromJs returned: '+ methodResult);
}