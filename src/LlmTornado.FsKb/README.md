# FSKB - File System Knowledge Base

File System Knowledge Base (FSKB) is a semantic search engine and indexing tool for code repositories. It leverages local or cloud embeddings to provide intelligent, contextual search capabilities over your codebase, scaling from local scripts to extensive codebases. With built-in integrations, it can run as a headless application, a graphical interface (via PyQt6), or a Model Context Protocol (MCP) server.

## Features

* **Semantic Search:** Understands code context using dense embeddings.
* **Indexing Engine:** Recursively ingests files across directories. Honors `.gitignore` and skips heavy directories like `node_modules` and `bin`.
* **Multiple Embedding Providers:** 
  * **Local:** `sentence-transformers`, `transformers`. GPU support (CUDA, INT8 quantization). Includes defaults for task-specific, code-specialized models like `jinaai/jina-code-embeddings-0.5b`.
  * **API-based:** OpenAI, Anthropic, Google Generative AI, VoyageAI, etc. (via Litellm).
* **Vector Storage:** Utilizes ChromaDB for managing dense vector storage.
* **MCP Server Support:** Native support for running as a standard stdio MCP Server.
* **Versatile UIs:** Supports Graphical Interface (`PyQt6`) or Headless operation with process monitoring and resource caps.
* **File Watching & Syncing:** Keep the index updated as you code via integration with `watchdog`.

## Installation

### Requirements
* Python 3.9+
* Required packages (see `LlmTornado.FsKb/requirements.txt`)

### Basic Setup
```bash
cd LlmTornado.FsKb
pip install -r requirements.txt
pip install -e .
```

### GPU Support Setup
To take advantage of an NVIDIA GPU locally, ensure you install torch compiled with CUDA:
```bash
pip install torch --index-url https://download.pytorch.org/whl/cu118
```
*(Or specify `cu121` if your CUDA toolkit is version 12.1).*

## Usage

Once installed, FSKB exposes a command-line utility. 

```bash
# Display help and arguments
fskb --help

# Index a directory explicitly from the CLI
fskb --add-root /path/to/repository

# Run in headless mode (no GUI)
fskb --no-gui

# Run as an MCP Server (stdio mode)
fskb --mcp
```

### Configuration
By default, FSKB reads its configuration regarding database storage, embedding providers, indexing constraints, and search parameters.

1. **Initialize Config:** 
   Rename or copy `config.json.example` to `config.json`.
2. **Setup Provider:** 
   Configure your embedding provider in the `embedding` section. To use an API like VoyageAI or OpenAI, set `provider` to the API type and supply an `api_key`. For local execution, keep `provider` as `"local"`.
3. **Customize Indexing:**
   Adjust `text_extensions` to include frameworks specific to your repositories. Define `max_file_size_mb` to ignore excessive file sizes.

Your persistent FSKB data (Logs, ChromaDB indexes, Configs) will be created inside the directory assigned to `storage` -> `data_dir` (defaults to `~/.fskb/data`). 
