// Chat functionality for LlmTornado streaming
window.chatFunctions = {
    eventSource: null,
    
    startEventSource: function(url, postData, dotNetRef) {
        return new Promise((resolve, reject) => {
            // First make the POST request to start streaming
            fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'text/event-stream'
                },
                body: postData
            }).then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                }
                
                // Use ReadableStream to handle the SSE response
                const reader = response.body.getReader();
                const decoder = new TextDecoder();
                let buffer = '';

                function readChunk() {
                    return reader.read().then(({ done, value }) => {
                        if (done) {
                            dotNetRef.invokeMethodAsync('OnStreamComplete');
                            return;
                        }

                        buffer += decoder.decode(value, { stream: true });
                        const lines = buffer.split('\n');
                        buffer = lines.pop() || '';

                        let eventType = '';
                        let data = '';

                        for (const line of lines) {
                            if (line.startsWith('event: ')) {
                                eventType = line.substring(7).trim();
                            } else if (line.startsWith('data: ')) {
                                if (data) data += '\n';
                                data += line.substring(6);
                            } else if (line === '' && eventType && data) {
                                // Complete event received
                                try {
                                    // Try to parse as JSON, if it fails, send as plain text
                                    let parsedData;
                                    try {
                                        parsedData = JSON.parse(data);
                                    } catch {
                                        parsedData = data;
                                    }
                                    
                                    dotNetRef.invokeMethodAsync('OnStreamEvent', eventType, 
                                        typeof parsedData === 'string' ? parsedData : JSON.stringify(parsedData));
                                } catch (error) {
                                    console.error('Error processing SSE event:', error);
                                    dotNetRef.invokeMethodAsync('OnStreamError', error.message);
                                }
                                
                                eventType = '';
                                data = '';
                            }
                        }

                        return readChunk();
                    });
                }

                readChunk().catch(error => {
                    console.error('Stream reading error:', error);
                    dotNetRef.invokeMethodAsync('OnStreamError', error.message);
                });

            }).catch(error => {
                console.error('Fetch error:', error);
                dotNetRef.invokeMethodAsync('OnStreamError', error.message);
                reject(error);
            });
        });
    },

    scrollToBottom: function(elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            element.scrollTop = element.scrollHeight;
        }
    }
};

// Global functions for Blazor to call
window.startEventSource = window.chatFunctions.startEventSource;
window.scrollToBottom = window.chatFunctions.scrollToBottom;