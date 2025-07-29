import React, { useState, useEffect } from 'react';
import useBaseUrl from '@docusaurus/useBaseUrl';

const initialCode = `
var result = await Api.Chat.CreateChatCompletion(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt4.Turbo,
    ResponseFormat = ChatRequestResponseFormats.Json,
    Messages = [
        new ChatMessage(ChatMessageRoles.System, "Solve the math problem given by user, respond in JSON format."),
        new ChatMessage(ChatMessageRoles.User, "2+2=?")
    ]
});

Console.WriteLine(result?.Choices?.Count > 0 ? result.Choices?[0].Message?.Content : "no response");
`;

declare global {
    interface Window {
        DotNet: any;
    }
}

export default function CSharpRunner() {
  const [code, setCode] = useState(initialCode);
  const [output, setOutput] = useState('');
  const [isBlazorReady, setBlazorReady] = useState(false);
  const blazorScriptUrl = useBaseUrl('blazor.webassembly.js');

  useEffect(() => {
    const script = document.createElement('script');
    script.src = blazorScriptUrl;
    script.onload = () => {
        window.DotNet.invokeMethodAsync('LlmTornado.Docs', 'IsReady').then(() => {
            setBlazorReady(true);
        });
    };
    document.body.appendChild(script);
  }, []);

  const handleRun = async () => {
    if (!isBlazorReady) {
        setOutput('Blazor runtime is not ready yet. Please wait...');
        return;
    }

    setOutput('Running...');
    try {
        const result = await window.DotNet.invokeMethodAsync('LlmTornado.Docs', 'RunCode', code);
        setOutput(result);
    } catch (e) {
        setOutput(e.toString());
    }
  };

  return (
    <div>
      <textarea
        value={code}
        onChange={(e) => setCode(e.target.value)}
        style={{ width: '100%', height: '200px', fontFamily: 'monospace' }}
      />
      <button onClick={handleRun} style={{ marginTop: '10px' }} disabled={!isBlazorReady}>
        {isBlazorReady ? 'Run' : 'Loading Blazor...'}
      </button>
      <pre style={{ background: '#f5f5f5', padding: '10px', marginTop: '10px' }}>
        <code>{output}</code>
      </pre>
    </div>
  );
}