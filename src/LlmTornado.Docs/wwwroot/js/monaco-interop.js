// This function is called from C# and stores the service reference globally
function initializeGlobalIntellisense(monacoService) {
    console.log("Blazor: Storing global intellisense service reference.");
    window.intellisenseService = monacoService;
}

// The main listener for all requests from the iframe
window.addEventListener("message", async (event) => {
    const { data } = event;
    if (data?.intellisage && window.intellisenseService) {
        console.log(`Blazor: Received '${data.intellisage.method}' from iframe.`);

        // Invoke the C# method using the globally stored reference
        const resultPayload = await window.intellisenseService.invokeMethodAsync(
            'RunAsync',
            data.intellisage.method,
            data.intellisage.args
        );

        // Post the result back to the iframe
        if (event.source) {

            console.log(`Blazor: Sending response for '${data.intellisage.method}' back to iframe.`);

            const resultString = new TextDecoder().decode(resultPayload);
            const resultObject = JSON.parse(resultString);

            event.source.postMessage({
                intellisage: {
                    id: data.intellisage.id,
                    payload: resultObject?.payload || resultObject
                }
            }, '*');
            
            /*console.log(`Blazor: Sending response for '${data.intellisage.method}' back to iframe.`);
            event.source.postMessage({
                intellisage: {
                    id: data.intellisage.id,
                    payload: JSON.parse(resultPayload)
                }
            }, '*');*/
        }
    }
});

// This object handles the one-time initialization of the iframe
window.monacoEditor = {
    init: (iframe, code) => {
        console.log("Blazor: monacoEditor.init called.");
        const channel = new MessageChannel();

        iframe.addEventListener('load', () => {
            console.log("Blazor: iframe loaded, sending 'init' message with port.");
            iframe.contentWindow.postMessage('init', '*', [channel.port2]);
        });

        channel.port1.onmessage = (e) => {
            if (e.data.type === 'ready') {
                console.log("Blazor: Received 'ready' from iframe, sending code to load.");
                channel.port1.postMessage({ type: 'load', code });
            }
        };
    }
};