class BlazorWorkerBaseSerializer {
    serialize(o) { return JSON.stringify(o); }
    deserialize(s) {return JSON.parse(s); }
}

class DotNetObjectProxy {
    constructor(id) {
        this.__dotNetObject = `${id}`;
        this.serializer = self.jsRuntimeSerializer || new BlazorWorkerBaseSerializer();
    }

    async invokeMethodAsync(methodName, ...methodArgs) {
        if (this.blazorWorkerJSRuntime === undefined) {
            const exports = await self.BlazorWorker.getAssemblyExports("BlazorWorker.Extensions.JSRuntime");
            this.blazorWorkerJSRuntime = exports.BlazorWorker.Extensions.JSRuntime.BlazorWorkerJSRuntime;
        }

        return await new Promise((resolve, reject) => {
            try {
                const argsString = this.serializer.serialize({
                    methodName,
                    methodArgs : methodArgs || []
                });
                var result = this.blazorWorkerJSRuntime.InvokeMethod(this.__dotNetObject, argsString);
                resolve(result);
            } catch (e) {
                reject(e);
            }
        });
    }
}

class BlazorWorkerJSRuntimeSerializer  {
    
    constructor() {
        this.baseSerializer = new BlazorWorkerBaseSerializer();
    }

    serialize = (o) => this.baseSerializer.serialize(o);

    deserialize = (s) => {
        let deserializedObj = this.baseSerializer.deserialize(s);
        deserializedObj = BlazorWorkerJSRuntimeSerializer.recursivelyFindDotnetObjectProxy(deserializedObj);
        return deserializedObj;
    }

    static recursivelyFindDotnetObjectProxy(obj) {
        
        const recursion = BlazorWorkerJSRuntimeSerializer.recursivelyFindDotnetObjectProxy;
        const dotnetObjectKey = "__dotNetObject";
        const keys = Object.keys(obj);
        if (keys.length === 1 && keys[0] === dotnetObjectKey) {
            return new DotNetObjectProxy(obj[dotnetObjectKey]);
        }

        for (let i = 0; i < keys.length; i++) {
            const property = keys[i];
            let value = obj[property];
            if (value !== null && typeof value === "object") {
                obj[property] = recursion(value);
            }
        }

        return obj;
    }
};

const serializer = new BlazorWorkerJSRuntimeSerializer();

const workerInvokeAsync = async function (method, argsString) {
    
    const methodHandle = self.BlazorWorker.getChildFromDotNotation(method);

    if (methodHandle === self.BlazorWorker.empty) {
        throw new Error(`workerInvokeAsync: Method '${method}' not defined`);
    }

    const argsArray = serializer.deserialize(argsString);
    const result = await methodHandle(...argsArray);
    return serializer.serialize(result);
}

const workerInvoke = function (method, argsString) {
    
    const methodHandle = self.BlazorWorker.getChildFromDotNotation(method);
    if (methodHandle === self.BlazorWorker.empty) {
        throw new Error(`workerInvoke: Method '${method}' not defined`);
    }

    const argsArray = serializer.deserialize(argsString);
    const result = methodHandle(...argsArray);
    return serializer.serialize(result);
}

self.BlazorWorker.setModuleImports("BlazorWorkerJSRuntime.js", {
    WorkerInvokeAsync: workerInvokeAsync,
    WorkerInvoke: workerInvoke
});