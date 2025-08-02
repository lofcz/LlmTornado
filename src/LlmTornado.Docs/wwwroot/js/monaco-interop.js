// This function is called from C# and stores the service reference globally
function initializeGlobalIntellisense(monacoService) {
    console.log("Blazor: Storing global intellisense service reference.");
    window.intellisenseService = monacoService;
}

// Store the current code to ensure it's available during initialization
let currentCode = "";

// This function is called from C# to initialize the Monaco editor with code
function initializeMonacoEditor(code) {
    console.log("Blazor: initializeMonacoEditor called with code length:", code.length);
    currentCode = code;
    
    // Find the Monaco editor iframe
    const iframe = document.querySelector('iframe');
    if (iframe) {
        console.log("Blazor: Found Monaco editor iframe, initializing with code.");
        window.monacoEditor.init(iframe, code);
    } else {
        console.log("Blazor: Monaco editor iframe not found yet, retrying in 100ms...");
        // Retry after a short delay if iframe isn't ready yet
        setTimeout(() => {
            const retryIframe = document.querySelector('iframe');
            if (retryIframe) {
                console.log("Blazor: Found Monaco editor iframe on retry, initializing with code.");
                window.monacoEditor.init(retryIframe, code);
            } else {
                console.error("Blazor: Monaco editor iframe not found after retry.");
            }
        }, 100);
    }
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
        }
    }
    
    // Handle initialization messages from the React app
    if (data?.type === 'init-request' && currentCode) {
        console.log("Blazor: Received init-request from React, sending current code.");
        event.source.postMessage({
            type: 'init-response',
            code: currentCode
        }, '*');
    }
});

// This object handles the initialization of the iframe
window.monacoEditor = {
    init: (iframe, code) => {
        console.log("Blazor: monacoEditor.init called with code length:", code.length);
        
        iframe.addEventListener('load', () => {
            console.log("Blazor: iframe loaded, sending 'init' message.");
            
            // Send the init message without MessageChannel for better compatibility
            iframe.contentWindow.postMessage({
                type: 'init',
                code: code
            }, '*');
        });

        // If the iframe is already loaded, send the init message immediately
        if (iframe.contentWindow && iframe.contentWindow.postMessage) {
            iframe.contentWindow.postMessage({
                type: 'init',
                code: code
            }, '*');
        }
    }
};
