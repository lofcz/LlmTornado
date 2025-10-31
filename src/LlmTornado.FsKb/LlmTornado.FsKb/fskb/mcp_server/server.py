"""
MCP (Model Context Protocol) server implementation.
Exposes tools and resources for semantic search.
"""

import asyncio
from pathlib import Path
from typing import Any, Optional
from loguru import logger

try:
    from mcp.server import Server
    from mcp.types import Tool, TextContent, ImageContent, EmbeddedResource
    import mcp.server.stdio
    MCP_AVAILABLE = True
except ImportError:
    MCP_AVAILABLE = False
    logger.error("mcp package not installed, MCP server unavailable")

from ..config import Settings
from ..indexing import IndexingEngine
from ..search import QueryEngine


class MCPServer:
    """MCP server for FSKB semantic search."""
    
    def __init__(
        self,
        settings: Settings,
        indexing_engine: IndexingEngine,
        query_engine: QueryEngine,
    ):
        if not MCP_AVAILABLE:
            raise RuntimeError("mcp package not installed")
        
        self.settings = settings
        self.indexing_engine = indexing_engine
        self.query_engine = query_engine
        
        # Create MCP server
        self.server = Server("fskb-server")
        
        # Register handlers
        self._register_handlers()
        
        logger.info("MCP server initialized")
    
    def _register_handlers(self):
        """Register MCP tool and resource handlers."""
        
        # List available tools
        @self.server.list_tools()
        async def list_tools() -> list[Tool]:
            return [
                Tool(
                    name="add_root",
                    description="Add a root directory to index for semantic search",
                    inputSchema={
                        "type": "object",
                        "properties": {
                            "path": {
                                "type": "string",
                                "description": "Absolute path to the root directory"
                            }
                        },
                        "required": ["path"]
                    }
                ),
                Tool(
                    name="remove_root",
                    description="Remove a root directory from indexing",
                    inputSchema={
                        "type": "object",
                        "properties": {
                            "path": {
                                "type": "string",
                                "description": "Absolute path to the root directory"
                            }
                        },
                        "required": ["path"]
                    }
                ),
                Tool(
                    name="search",
                    description="Search for code or text semantically across indexed files",
                    inputSchema={
                        "type": "object",
                        "properties": {
                            "query": {
                                "type": "string",
                                "description": "Search query describing what to find"
                            },
                            "root_path": {
                                "type": "string",
                                "description": "Root directory to search in"
                            },
                            "branch": {
                                "type": "string",
                                "description": "Git branch to search (optional, defaults to current)"
                            },
                            "top_k": {
                                "type": "integer",
                                "description": "Number of results to return (default 10)"
                            }
                        },
                        "required": ["query", "root_path"]
                    }
                ),
                Tool(
                    name="get_status",
                    description="Get indexing status and statistics",
                    inputSchema={
                        "type": "object",
                        "properties": {
                            "root_path": {
                                "type": "string",
                                "description": "Optional root path to get specific stats"
                            }
                        }
                    }
                ),
                Tool(
                    name="list_roots",
                    description="List all indexed root directories",
                    inputSchema={
                        "type": "object",
                        "properties": {}
                    }
                ),
            ]
        
        # Handle tool calls
        @self.server.call_tool()
        async def call_tool(name: str, arguments: Any) -> list[TextContent]:
            try:
                if name == "add_root":
                    return await self._handle_add_root(arguments)
                elif name == "remove_root":
                    return await self._handle_remove_root(arguments)
                elif name == "search":
                    return await self._handle_search(arguments)
                elif name == "get_status":
                    return await self._handle_get_status(arguments)
                elif name == "list_roots":
                    return await self._handle_list_roots(arguments)
                else:
                    return [TextContent(type="text", text=f"Unknown tool: {name}")]
            
            except Exception as e:
                logger.error(f"Error handling tool {name}: {e}")
                return [TextContent(type="text", text=f"Error: {str(e)}")]
        
        # List resources
        @self.server.list_resources()
        async def list_resources() -> list[Any]:
            resources = []
            
            # Add resources for each indexed root
            for root_path in self.indexing_engine.roots.keys():
                root_state = self.indexing_engine.roots[root_path]
                
                resources.append({
                    "uri": f"fskb://{root_path}/stats",
                    "name": f"Stats for {root_path}",
                    "mimeType": "application/json",
                    "description": f"Indexing statistics for {root_path}"
                })
                
                resources.append({
                    "uri": f"fskb://{root_path}/branch/{root_state.current_branch}",
                    "name": f"Branch {root_state.current_branch}",
                    "mimeType": "text/plain",
                    "description": f"Current branch for {root_path}"
                })
            
            return resources
        
        # Read resource
        @self.server.read_resource()
        async def read_resource(uri: str) -> str:
            # Parse URI
            if not uri.startswith("fskb://"):
                raise ValueError(f"Invalid URI: {uri}")
            
            parts = uri[7:].split("/")
            if len(parts) < 2:
                raise ValueError(f"Invalid URI format: {uri}")
            
            root_path = Path(parts[0])
            resource_type = parts[1]
            
            if resource_type == "stats":
                stats = self.indexing_engine.get_stats(root_path)
                import json
                return json.dumps(stats, indent=2)
            
            elif resource_type == "branch" and len(parts) > 2:
                branch_name = parts[2]
                root_state = self.indexing_engine.roots.get(root_path)
                if root_state:
                    return f"Current branch: {root_state.current_branch}"
                return "Root not found"
            
            else:
                raise ValueError(f"Unknown resource type: {resource_type}")
    
    async def _handle_add_root(self, arguments: dict) -> list[TextContent]:
        """Handle add_root tool call."""
        path = arguments.get("path")
        if not path:
            return [TextContent(type="text", text="Error: path argument required")]
        
        root_path = Path(path)
        success = await self.indexing_engine.add_root(root_path)
        
        if success:
            return [TextContent(
                type="text",
                text=f"Successfully added root: {root_path}\nIndexing will begin shortly."
            )]
        else:
            return [TextContent(
                type="text",
                text=f"Failed to add root: {root_path}"
            )]
    
    async def _handle_remove_root(self, arguments: dict) -> list[TextContent]:
        """Handle remove_root tool call."""
        path = arguments.get("path")
        if not path:
            return [TextContent(type="text", text="Error: path argument required")]
        
        root_path = Path(path)
        success = await self.indexing_engine.remove_root(root_path)
        
        if success:
            return [TextContent(
                type="text",
                text=f"Successfully removed root: {root_path}"
            )]
        else:
            return [TextContent(
                type="text",
                text=f"Failed to remove root: {root_path}"
            )]
    
    async def _handle_search(self, arguments: dict) -> list[TextContent]:
        """Handle search tool call."""
        query = arguments.get("query")
        root_path_str = arguments.get("root_path")
        branch = arguments.get("branch")
        top_k = arguments.get("top_k", 10)
        
        if not query:
            return [TextContent(type="text", text="Error: query argument required")]
        if not root_path_str:
            return [TextContent(type="text", text="Error: root_path argument required")]
        
        root_path = Path(root_path_str)
        
        # Get branch name
        if not branch:
            root_state = self.indexing_engine.roots.get(root_path)
            if not root_state:
                return [TextContent(type="text", text=f"Error: root not found: {root_path}")]
            branch = root_state.current_branch
        
        # Search
        results = await self.query_engine.search(
            query=query,
            root_path=root_path,
            branch_name=branch,
            top_k=top_k,
        )
        
        # Format results
        if not results:
            return [TextContent(
                type="text",
                text=f"No results found for query: {query}"
            )]
        
        output_lines = [f"Found {len(results)} results for: {query}\n"]
        
        for i, result in enumerate(results, 1):
            output_lines.append(f"\n{i}. {result.file_path} (lines {result.line_start}-{result.line_end}) [score: {result.score:.3f}]")
            output_lines.append(f"   {result.content[:200]}..." if len(result.content) > 200 else f"   {result.content}")
        
        return [TextContent(type="text", text="\n".join(output_lines))]
    
    async def _handle_get_status(self, arguments: dict) -> list[TextContent]:
        """Handle get_status tool call."""
        root_path_str = arguments.get("root_path")
        
        if root_path_str:
            root_path = Path(root_path_str)
            stats = self.indexing_engine.get_stats(root_path)
        else:
            stats = self.indexing_engine.get_stats()
        
        # Format stats
        import json
        stats_json = json.dumps(stats, indent=2)
        
        return [TextContent(
            type="text",
            text=f"Indexing Status:\n{stats_json}"
        )]
    
    async def _handle_list_roots(self, arguments: dict) -> list[TextContent]:
        """Handle list_roots tool call."""
        roots = list(self.indexing_engine.roots.keys())
        
        if not roots:
            return [TextContent(type="text", text="No roots currently indexed.")]
        
        output_lines = ["Indexed roots:"]
        for root in roots:
            root_state = self.indexing_engine.roots[root]
            output_lines.append(f"  - {root} (branch: {root_state.current_branch})")
        
        return [TextContent(type="text", text="\n".join(output_lines))]
    
    async def run(self):
        """Run the MCP server (stdio mode)."""
        logger.info("Starting MCP server (stdio mode)")
        
        async with mcp.server.stdio.stdio_server() as (read_stream, write_stream):
            await self.server.run(
                read_stream,
                write_stream,
                self.server.create_initialization_options()
            )

