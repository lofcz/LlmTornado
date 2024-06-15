# Changelog

For newer versions see [releases](https://github.com/lofcz/LlmTornado/releases).

### 3.0
- rebranded from `OpenAING` to `LLMTornado`

### 2.3.2 - 12/3/23
- completed the transition to `tools` from `function`

### 2.3.1 - 12/2/23
- added a few convenience methods in `Conversation` to make API on par with older models
- added a missing ctor in `ChatMessage`
- fixed typos in XML doc

### 2.3.0 - 12/1/23
- implemented [message parts](https://platform.openai.com/docs/api-reference/chat/create) for outbound image messages
- added `gpt4-vision-preview` model

### 2.2.9 - 11/9/23
- implemented [TTS](https://platform.openai.com/docs/api-reference/audio/createSpeech) endpoint
- added additional API ctors with just API key, API key & ORG key for ease of use
- fixed several `ToString()` overrides

### 2.2.8 - 9/9/23
- implemented `tool_call` support, removed deprecated `function_call`

### Old versions
- [commit history](https://github.com/lofcz/OpenAI-API-dotnet/commits/master)
