// This 'classic' script will run on the main thread and cirectly receieve message from the worker
function setupJsDirectForWorker(myWorkerId) {
    const currtime = () => [new Date()].map(d => `${d.toTimeString().split(" ")[0]}:${d.getMilliseconds()}`)[0];
    const output = document.getElementById('jsDirectOutputElement');
    output.innerText += `\n${currtime()} Setting up event listener.`;
    window.addEventListener('blazorworker:jsdirect', function (e) {
        if (e.detail.workerId === myWorkerId) {
            output.innerText += `\n${currtime()} blazorworker:jsdirect listener. workerId: ${e.detail.workerId}. data: '${e.detail.data}'`;
        }
        else {
            console.log('blazorworker:jsdirect handler for some other worker not handled by this listener', { workerId: e.detail.workerId, data: e.detail.data });
        }
    });
}