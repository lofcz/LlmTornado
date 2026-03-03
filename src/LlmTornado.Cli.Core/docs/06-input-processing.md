# 06 — Input Processing

The input processing pipeline scans user text for inline file references (`@path`), resolves them against the filesystem, and converts them into multimodal `ChatMessage` instances.

## Architecture

```mermaid
classDiagram
    class InputParser {
        +Parse(input, workingDirectory) ParsedInput$
        +MaxFileSizeBytes: 20MB$
    }

    class ParsedInput {
        +string CleanedText
        +List~ParsedFileReference~ Files
        +bool HasFiles
    }

    class ParsedFileReference {
        +string RawToken
        +string FilePath
        +string FileName
        +FileMediaType MediaType
        +string MimeType
    }

    class FileMediaType {
        <<enumeration>>
        Image
        Document
        Audio
    }

    class FileAttachmentResolver {
        +Resolve(parsedInput) FileAttachmentResult$
    }

    class FileAttachmentResult {
        +ChatMessage Message
        +List~ResolvedAttachment~ Attachments
        +List~string~ Errors
    }

    InputParser --> ParsedInput
    ParsedInput --> ParsedFileReference
    ParsedFileReference --> FileMediaType
    FileAttachmentResolver --> FileAttachmentResult
```

## End-to-End Flow

```mermaid
flowchart TD
    Input --> Scan["InputParser.Parse()"]
    Scan --> Regex["Regex scan for @tokens"]
    Regex --> Resolve["Resolve paths relative to CWD"]
    Resolve --> Validate["Validate:<br/>• File exists<br/>• Extension supported<br/>• Size ≤ 20MB"]
    Validate --> Clean["Remove tokens, collapse whitespace"]
    Clean --> PI["ParsedInput{<br/>CleanedText: 'Analyze and compare with',<br/>Files: [photo.png, spec.pdf]<br/>}"]
    PI --> FAR["FileAttachmentResolver.Resolve()"]
    FAR --> Convert["Convert each file to<br/>ChatMessagePart by type"]
    Convert --> Msg["Multipart ChatMessage"]
```

## Input Parser

### Token Syntax

The parser recognizes two `@path` formats:

| Format | Example | Notes |
|--------|---------|-------|
| `@unquoted/path` | `@photo.png`, `@./src/main.py` | No spaces allowed in path |
| `@"quoted path"` | `@"my documents/photo.png"` | Spaces allowed inside quotes |

**Regex pattern**: `@"([^"]+)"|@(\S+)`

### Supported File Types

```mermaid
graph LR
    subgraph "Images"
        PNG[".png"]
        JPG[".jpg / .jpeg"]
        GIF[".gif"]
        WEBP[".webp"]
        BMP[".bmp"]
        SVG[".svg"]
        TIFF[".tiff / .tif"]
        ICO[".ico"]
    end

    subgraph "Documents"
        PDF[".pdf"]
    end

    subgraph "Audio"
        WAV[".wav"]
        MP3[".mp3"]
        OGG[".ogg"]
        FLAC[".flac"]
        M4A[".m4a"]
        AAC[".aac"]
        WMA[".wma"]
        WEBM[".webm"]
    end
```

### Validation Rules

```mermaid
flowchart TD
    Token["@path token found"] --> ResolvePath["Resolve to absolute path<br/>(relative to CWD)"]
    ResolvePath -->|"Invalid path"| Err1["Error: Invalid path"]
    ResolvePath --> Exists{"File exists?"}
    Exists -->|"No"| Err2["Error: File not found"]
    Exists -->|"Yes"| Ext{"Extension<br/>supported?"}
    Ext -->|"No"| Err3["Error: Unsupported file type"]
    Ext -->|"Yes"| Size{"Size ≤ 20MB?"}
    Size -->|"No"| Err4["Error: File too large"]
    Size -->|"Yes"| Success["Add to ParsedFileReference list"]
```

### Text Cleaning

After extracting file tokens, the parser:
1. Removes each `@token` from the original text (processed in reverse order to preserve string indices)
2. Collapses multiple consecutive spaces into single spaces
3. Trims leading/trailing whitespace

**Example**:
- Input: `"Analyze @photo.png and compare with @"./spec.pdf" please"`
- CleanedText: `"Analyze and compare with please"`

## File Attachment Resolution

`FileAttachmentResolver` converts `ParsedInput` into a `ChatMessage` suitable for the LLM.

### Conversion by Media Type

```mermaid
flowchart TD
    File["ParsedFileReference"] --> Type{"MediaType?"}

    Type -->|"Image"| Img["Read file → base64<br/>Create data URI:<br/>data:{mime};base64,{data}<br/>→ ChatMessagePart(dataUri, Auto, mime)"]

    Type -->|"Document"| Doc["Read file → base64<br/>→ ChatDocument(base64)"]

    Type -->|"Audio"| Aud["Read file → byte[]<br/>Detect ChatAudioFormats from ext<br/>→ ChatMessagePart(bytes, format)"]
```

### Message Construction

```mermaid
flowchart TD
    PI["ParsedInput"] --> HasFiles{"Has files?"}

    HasFiles -->|"No"| Plain["Plain text ChatMessage<br/>role: User, content: text"]

    HasFiles -->|"Yes"| Multi["Multipart ChatMessage<br/>role: User"]
    Multi --> TextPart["ChatMessagePart(CleanedText)"]
    Multi --> FileParts["ChatMessagePart for each file"]
    TextPart --> Combine["Combine into message.Parts list"]
    FileParts --> Combine

    Combine --> Fallback{"All files failed?"}
    Fallback -->|"Yes"| Plain2["Fallback to plain text"]
    Fallback -->|"No"| Final["Return multipart message"]
```

### MIME Type Mapping

| Extension | MIME Type |
|-----------|----------|
| `.png` | `image/png` |
| `.jpg`, `.jpeg` | `image/jpeg` |
| `.gif` | `image/gif` |
| `.webp` | `image/webp` |
| `.bmp` | `image/bmp` |
| `.svg` | `image/svg+xml` |
| `.tiff`, `.tif` | `image/tiff` |
| `.ico` | `image/x-icon` |
| `.pdf` | `application/pdf` |
| `.wav` | `audio/wav` |
| `.mp3` | `audio/mpeg` |
| `.ogg` | `audio/ogg` |
| `.flac` | `audio/flac` |
| `.m4a` | `audio/mp4` |
| `.aac` | `audio/aac` |
| `.wma` | `audio/x-ms-wma` |
| `.webm` | `audio/webm` |

## Example Pipeline

```mermaid
sequenceDiagram
    participant User
    participant IP as InputParser
    participant FAR as FileAttachmentResolver
    participant Agent as ChatRuntime

    User->>IP: "What's in @screenshot.png?"
    IP->>IP: Regex match: @screenshot.png
    IP->>IP: Resolve: C:\project\screenshot.png
    IP->>IP: Validate: exists ✓, .png ✓, 2MB ✓
    IP-->>FAR: ParsedInput{<br/>CleanedText: "What's in?",<br/>Files: [{screenshot.png, Image, image/png}]}

    FAR->>FAR: Read screenshot.png → base64
    FAR->>FAR: Create ChatMessagePart<br/>(data:image/png;base64,...)
    FAR-->>Agent: ChatMessage{<br/>Role: User,<br/>Parts: [text, image]}

    Agent-->>User: "The screenshot shows..."
```
