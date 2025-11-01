# LlmTornado.Internal.Press

An AI-powered journalist agent system that generates SEO-optimized, trend-aware articles aligned with promotional objectives.

## Overview

This system uses LlmTornado.Agents framework to create an autonomous article generation pipeline with guided autonomy through review loops. It combines trend analysis, research, writing, and quality review to produce high-quality technical content.

## Features

- **Trend Analysis**: Discovers trending topics using Tavily web search
- **Intelligent Ideation**: Generates article ideas that blend trends with promotional objectives
- **Deep Research**: Conducts thorough research with source citations
- **Professional Writing**: Creates SEO-optimized, engaging markdown articles
- **Review Loop**: Implements guided autonomy with quality thresholds and iterative improvement
- **Image Generation**: Creates hero images using DALL-E (configurable)
- **Export Pipeline**: Outputs articles as markdown + JSON files
- **Queue Management**: Tracks article generation with SQLite database
- **Extensible Publishing**: Module structure for future REST-based publishers

## Architecture

The system is built on LlmTornado.Agents orchestration patterns with the following components:

### Core Agents

1. **TrendAnalysisAgent**: Discovers trending topics using Tavily
2. **IdeationAgent**: Generates compelling article ideas
3. **ResearchAgent**: Conducts deep research with source citations
4. **WritingAgent**: Creates SEO-optimized article content
5. **ReviewAgent**: Evaluates quality and provides feedback
6. **ImageGenerationAgent**: Generates hero images (optional)

### Orchestration Flow

```
Queue → Trends → Ideation → Research → Writing → Review
                                           ↓
                                    [Review Loop]
                                    Approved? → No → Improvement → Research
                                           ↓ Yes
                                    Image → Export → Save → Complete
```

### Guided Autonomy

The review loop implements guided autonomy by:
- Evaluating articles against quality thresholds
- Providing specific, actionable feedback
- Iteratively improving content (up to N iterations)
- Approving articles that meet quality standards
- Failing gracefully if thresholds aren't met

## Configuration

Edit `appCfg.json` to configure the system:

```json
{
  "objective": "Your promotional objective",
  "apiKeys": {
    "openAi": "your-key",
    "anthropic": "your-key",
    "tavily": "your-key"
  },
  "models": {
    "ideation": "gpt-4o-mini",
    "research": "gpt-4o",
    "writing": "gpt-4o",
    "review": "gpt-4o"
  },
  "reviewLoop": {
    "enabled": true,
    "maxIterations": 3,
    "qualityThresholds": {
      "minWordCount": 800,
      "minSeoScore": 70
    }
  }
}
```

## Usage

### Initial Setup

1. Configure `appCfg.json` with your API keys and objective
2. Build the project: `dotnet build`

### Commands

**Seed the article queue:**
```bash
dotnet run -- seed-queue 10
```

**Generate articles:**
```bash
dotnet run -- generate 5
```

**Check status:**
```bash
dotnet run -- status
```

**Export all articles:**
```bash
dotnet run -- export-all
```

**Reset database:**
```bash
dotnet run -- reset-db
```

## Output Structure

Generated articles are exported to:
```
output/articles/
└── 2025-10-26/
    └── article-slug/
        ├── article.md      # Markdown with frontmatter
        └── article.json    # JSON metadata
```

## Database Schema

The system uses SQLite with the following tables:

- **Articles**: Generated articles with metadata
- **ArticleQueue**: Pending article ideas
- **TrendingTopics**: Discovered trends
- **WorkHistory**: Audit log of generation steps

## Extensibility

### Future Publishers

The `Publishers/` module is designed for future REST-based publishers:
- WordPress
- Medium
- Dev.to
- Ghost CMS
- Custom REST APIs

See `Publishers/README.md` for implementation details.

## Dependencies

- **LlmTornado.Agents**: Agent framework and orchestration
- **Entity Framework Core SQLite**: Database ORM
- **Newtonsoft.Json**: JSON serialization
- **Tavily API**: Web search functionality
- **OpenAI/Anthropic/etc**: LLM providers

## Quality Metrics

The review agent evaluates articles on:

- **Word Count**: Minimum length requirements
- **SEO Score**: Title, description, headings, keywords
- **Readability**: Sentence structure and clarity
- **Source Citations**: Presence of authoritative sources
- **Clickbait Effectiveness**: Engaging but accurate titles
- **Temporal Relevance**: Current trends and dates
- **Objective Alignment**: Natural promotion integration

## Development

Built with:
- .NET 8.0
- C# with preview features
- LlmTornado.Agents framework
- Entity Framework Core

## License

Internal use only.

