# Overview

**LlmTornado.Agents** is a framework designed to facilitate the creation and management of AI agents that can perform complex tasks by leveraging large language models (LLMs). The framework provides a structured approach to building agents that can interact with various tools, manage state, and execute tasks in a modular fashion.

## Key Features

### 🛠️ Tool Integration
Easily integrate external tools and APIs to extend the capabilities of your agents:
- **Delegate Tools** - Convert C# methods into agent tools automatically
- **MCP Tools** - Seamlessly integrate with tools from the Model Context Protocol ecosystem
- **Agents as Tools** - Use other agents as tools for complex workflows

### 📋 Structured Output
Define structured output schemas for agents to ensure consistent and reliable responses:
- **Type-Safe Schemas** - Use C# types to define expected output structure
- **Automatic Validation** - Built-in validation for structured responses
- **JSON Schema Generation** - Automatic conversion from C# types to JSON schemas

### 🔄 Runtime Configurations
Multiple runtime patterns for different use cases:
- **Sequential** - Chain multiple agents in sequence
- **Handoff** - Dynamic agent handoff based on context
- **Orchestration** - Complex state machine-based workflows
- **Concurrent** - Parallel execution patterns

### 🛡️ Guardrails and Safety
Built-in safety mechanisms:
- **Input Guardrails** - Validate and filter input before processing
- **Output Validation** - Ensure responses meet expected criteria
- **Error Handling** - Robust error handling and recovery

## Architecture Overview

The framework is built around several core components:

### TornadoAgent
The main agent class that encapsulates:
- LLM client configuration
- Instructions and behavior
- Tool integration
- Output schema definition

### TornadoRunner
The execution engine that handles:
- Agent conversation flow
- Tool invocation
- Streaming responses
- Error handling

### Chat Runtime
Advanced runtime system for complex workflows:
- State management
- Agent coordination
- Event handling
- Flow control

## Use Cases

LlmTornado.Agents is perfect for:

- **Conversational AI** - Build chatbots and virtual assistants
- **Task Automation** - Automate complex multi-step processes
- **Research Assistants** - Create agents that can search, analyze, and report
- **Code Generation** - Build coding assistants with tool integration
- **Content Creation** - Develop content generation workflows
- **Decision Support** - Create agents that help with decision-making processes

## Next Steps

- Get started with the [Quick Start Guide](quick-start.md)
- Learn about [Basic Agent Usage](basic-agent-usage.md)
- Explore [Tool Integration](tool-integration.md) options
- Dive into [Advanced Features](chat-runtime.md)