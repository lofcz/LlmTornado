# Publishers Module

This directory is reserved for future publisher implementations that will post articles to various platforms via REST APIs.

## Planned Implementations

### WordPress Publisher
- Post articles to WordPress sites via REST API
- Handle featured images, categories, tags
- Support custom post types and taxonomies

### Medium Publisher
- Publish articles to Medium via API
- Handle formatting conversion
- Manage publication settings

### Dev.to Publisher
- Post articles to Dev.to platform
- Convert markdown format
- Handle tags and series

### Ghost Publisher
- Publish to Ghost CMS via Admin API
- Handle post status (draft/published)
- Manage authors and tags

### Custom REST Publisher
- Generic REST API publisher
- Configurable endpoints and authentication
- Flexible request/response mapping

## Implementation Pattern

Each publisher should:

1. Implement `IArticlePublisher` interface
2. Handle authentication and API communication
3. Convert article format as needed
4. Provide error handling and retry logic
5. Log publication history to database

## Configuration

Publishers will be configured in `appCfg.json`:

```json
{
  "publishers": {
    "wordpress": {
      "enabled": true,
      "apiUrl": "https://yoursite.com/wp-json/wp/v2",
      "username": "user",
      "password": "app-password"
    },
    "medium": {
      "enabled": false,
      "apiToken": "your-token"
    }
  }
}
```

## Usage

```csharp
var publisher = new WordPressPublisher(config);
var result = await publisher.PublishAsync(article);
```

