window.BlazorWorker = function () {
    
    const workers = {};
    const disposeWorker = function (workerId) {

        const worker = workers[workerId];
        if (worker) {
            if (worker.worker.terminate) {
                worker.worker.terminate();
            }
            URL.revokeObjectURL(worker.url);
            delete workers[workerId];
        }
    };

    const workerDef = function () {

        const initConf = JSON.parse('$initConf$');
        const blobRoot = self.location.href.split("/").slice(0, -1).join("/");
        const proxyLocation = { href: initConf.appRoot };
        const fetchHandler = {
            apply: function (target, thisArg, args) {
                // replaces blob urls with appRoot urls. Mono will attempt to load dlls from self.location.href.
                args[0] = args[0].replace(blobRoot, initConf.appRoot);
                return target.apply(thisArg, args);
            }
        }
        self.nativeFetch = fetch;
        const proxyFetch = new Proxy(fetch, fetchHandler);
        self.fetch = proxyFetch;
        const handler = {
            get: function (target, prop, pxy) {
                if (prop == "location") {
                    return proxyLocation;
                }

                if (prop == "fetch") {
                    return proxyFetch;
                }

                return target[prop];
                
            }
        }
        self.window = new Proxy(self, handler);
        let messageHandler;

        const onReady = () => {
            // Future messages goes directly to the message handler
            self.onmessage = msg => {
                messageHandler(msg.data);
            };

            try {
                self.initMethod();
            } catch (e) {
                console.error(`Init method ${JSON.stringify(initConf.initEndPoint)} failed`, e);
                throw e;
            }
        };

        // reduce dot notation to last member of chain
        const getChildFromDotNotation = (member, root) =>
            member.split(".").reduce((m, prop) => Object.hasOwnProperty.call(m, prop) ? m[prop] : empty, root || self);

        const startDotnet = async () => {
            const { dotnet } = await import(`${initConf.appRoot}/_framework/dotnet.js`);
            const config = {
                "mainAssemblyName": "BlazorWorker.WorkerCore.dll",
                "environmentVariables": {
                    "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT": "1"
                }
            };

            const { setModuleImports, getAssemblyExports } = await dotnet
                .withDiagnosticTracing(initConf.debug)
                .withConfig(config)
                .create();

            setModuleImports('BlazorWorker.js', {
                PostMessage: (messagecontent) => {
                    self.postMessage(messagecontent);
                },
                PostMessageJsDirect: (messagecontent) => {
                    self.postMessage({ isJsDirect: true, jsData: messagecontent });
                },
                ImportLocalScripts: async (urls) => {
                    await self.importLocalScripts(urls);
                },
                IsObjectDefined: (workerScopeObject) => {
                    return self.isObjectDefined(workerScopeObject);
                }
            });

            // Restore the fetch handler to the native fetch for non-invasive future fetch calls
            self.fetch = self.nativeFetch;

            self.BlazorWorker = {
                getChildFromDotNotation,
                getAssemblyExports,
                setModuleImports,
                empty
            };

            const getMethodFromMethodIdentifier = async function (methodIdentifier) {
                const exports = await getAssemblyExports(methodIdentifier.assemblyName);
                const method = getChildFromDotNotation(methodIdentifier.fullMethodName, exports);
                if (!method || method === empty || !(method instanceof Function)) {
                    throw new Error(`Unable to find method '${methodIdentifier.fullMethodName}' in assembly '${methodIdentifier.assemblyName}}'. Are you missing a JSExport attribute?`);
                }

                return method;
            }

            messageHandler = await getMethodFromMethodIdentifier(initConf.messageEndPoint);
            self.initMethod = await getMethodFromMethodIdentifier(initConf.initEndPoint);

            onReady();
        }

        startDotnet();
        
        const empty = {};

        // Import module script from a path relative to approot
        self.importLocalScripts = async (urls) => {
            if (urls === undefined || urls === null) {
                return;
            }
            if (!urls.map) {
                urls = [urls]
            }
            for (const url of urls) {
                const urlToLoad = initConf.appRoot + (url.startsWith('/') ? '' : '/') + url;
                await import(urlToLoad);
            }
        };

        self.isObjectDefined = (workerScopeObject) => {
            return getChildFromDotNotation(workerScopeObject) !== empty;
        };
    };

    const inlineWorker = `self.onmessage = ${workerDef}()`; 

    const initWorker = async function (id, callbackInstance, initOptions) {
        let appRoot = (document.getElementsByTagName('base')[0] || { href: window.location.origin }).href || "";
        if (appRoot.endsWith("/")) {
            appRoot = appRoot.substring(0, appRoot.length - 1);
        }
        
        const initConf = {
            appRoot: appRoot,
            workerId: id,
            runtimePreprocessorSymbols: initOptions.runtimePreprocessorSymbols || {},
            messageEndPoint: initOptions.messageEndPoint,
            initEndPoint: initOptions.initEndPoint,
            endInvokeCallBackEndpoint: initOptions.endInvokeCallBackEndpoint,
            wasmRoot: "_framework", // Hardcoded as it's standard
            envMap: initOptions.envMap,
            debug: initOptions.debug
        };
        
        // Initialize worker
        const renderedConfig = JSON.stringify(initConf);
        const renderedInlineWorker = inlineWorker.replace('$initConf$', renderedConfig);
        window.URL = window.URL || window.webkitURL;
        const blob = new Blob([renderedInlineWorker], { type: 'application/javascript' });
        const workerUrl = URL.createObjectURL(blob);
        const worker = new Worker(workerUrl, { type: 'module'});
        workers[id] = {
            worker: worker,
            url: workerUrl
        };

        worker.onmessage = function (ev) {

            if (ev.data.isJsDirect) {
                if (initOptions.debug) {
                    console.debug(`BlazorWorker.js:worker[${id}]->jsDirect`, initOptions.callbackMethod, ev.data.jsData);
                }

                var event = new CustomEvent("blazorworker:jsdirect",
                    { detail: { workerId: id, data: ev.data.jsData } }
                );
                window.dispatchEvent(event);

                return;
            }

            if (initOptions.debug) {
                console.debug(`BlazorWorker.js:worker[${id}]->blazor`, initOptions.callbackMethod, ev.data);
            }
            callbackInstance.invokeMethod(initOptions.callbackMethod, ev.data);
        };
    };

    const postMessage = function (workerId, message) {
        workers[workerId].worker.postMessage(message);
    };

    return {
        disposeWorker,
        initWorker,
        postMessage
    };
}();
