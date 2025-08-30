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
    },

    // Theme management functions
    initializeTheme: function() {
        // Check for saved theme preference or default to dark mode
        const savedTheme = localStorage.getItem('llmtornado-theme');
        const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
        const theme = savedTheme || (prefersDark ? 'dark' : 'light');
        
        // Apply theme to document
        document.documentElement.setAttribute('data-theme', theme);
        
        return theme;
    },

    setTheme: function(theme) {
        localStorage.setItem('llmtornado-theme', theme);
        document.documentElement.setAttribute('data-theme', theme);
    },

    getTheme: function() {
        return localStorage.getItem('llmtornado-theme') || 'dark';
    },

    // Auto-resize textarea
    autoResizeTextarea: function(elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            element.style.height = 'auto';
            element.style.height = element.scrollHeight + 'px';
        }
    },

    // Focus management
    focusElement: function(elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            element.focus();
        }
    },

    // Smooth scroll animation
    smoothScrollTo: function(elementId, position = 'bottom') {
        const element = document.getElementById(elementId);
        if (element) {
            const scrollOptions = {
                behavior: 'smooth'
            };
            
            if (position === 'bottom') {
                scrollOptions.top = element.scrollHeight;
            } else if (position === 'top') {
                scrollOptions.top = 0;
            }
            
            element.scrollTo(scrollOptions);
        }
    }
};

// Global functions for Blazor to call
window.startEventSource = window.chatFunctions.startEventSource;
window.scrollToBottom = window.chatFunctions.scrollToBottom;
window.initializeTheme = window.chatFunctions.initializeTheme;
window.setTheme = window.chatFunctions.setTheme;
window.getTheme = window.chatFunctions.getTheme;
window.autoResizeTextarea = window.chatFunctions.autoResizeTextarea;
window.focusElement = window.chatFunctions.focusElement;
window.smoothScrollTo = window.chatFunctions.smoothScrollTo;

// Initialize theme on page load
document.addEventListener('DOMContentLoaded', function() {
    window.chatFunctions.initializeTheme();
    
    // Listen for system theme changes
    if (window.matchMedia) {
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function(e) {
            // Only auto-switch if no explicit theme preference is saved
            if (!localStorage.getItem('llmtornado-theme')) {
                const theme = e.matches ? 'dark' : 'light';
                document.documentElement.setAttribute('data-theme', theme);
            }
        });
    }
});