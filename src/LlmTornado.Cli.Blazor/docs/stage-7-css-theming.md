# Stage 7: CSS & Theming

## Goal

Define the complete stylesheet for the chat UI using **CSS custom properties** for theming, **BEM naming** for selectors, and **scoped CSS** where appropriate. The stylesheet provides a clean, responsive chat layout that works standalone with no external CSS framework.

---

## 7.1: Global Custom Properties

**Path:** `src/LlmTornado.Cli.Blazor/wwwroot/tornado-chat.css`

This is the single CSS file consumers link. All variables are defined under `:root` (light) and `[data-theme="dark"]` (dark).

```css
/* =====================================================
   tornado-chat.css — LlmTornado.Cli.Blazor theme
   =====================================================*/

/* ── Light theme (default) ── */
:root {
    /* Surfaces */
    --tc-bg:              #ffffff;
    --tc-bg-secondary:    #f5f5f5;
    --tc-bg-sidebar:      #fafafa;
    --tc-border:          #e0e0e0;
    --tc-shadow:          0 1px 3px rgba(0,0,0,0.08);

    /* Text */
    --tc-text:            #1a1a1a;
    --tc-text-secondary:  #666666;
    --tc-text-muted:      #999999;

    /* Accent */
    --tc-accent:          #2563eb;
    --tc-accent-hover:    #1d4ed8;
    --tc-accent-subtle:   #eff6ff;

    /* Bubbles */
    --tc-user-bg:         #2563eb;
    --tc-user-text:       #ffffff;
    --tc-assistant-bg:    #f5f5f5;
    --tc-assistant-text:  #1a1a1a;
    --tc-system-bg:       #fefce8;
    --tc-system-text:     #713f12;

    /* Chips */
    --tc-chip-bg:         #f0f0f0;
    --tc-chip-border:     #d0d0d0;
    --tc-chip-success:    #dcfce7;
    --tc-chip-success-border: #86efac;
    --tc-chip-fail:       #fee2e2;
    --tc-chip-fail-border:#fca5a5;
    --tc-chip-progress:   #dbeafe;
    --tc-chip-progress-border: #93c5fd;

    /* Approval */
    --tc-approve-bg:      #f0fdf4;
    --tc-approve-border:  #86efac;
    --tc-deny-bg:         #fef2f2;
    --tc-deny-border:     #fca5a5;

    /* Inputs */
    --tc-input-bg:        #ffffff;
    --tc-input-border:    #d0d0d0;
    --tc-input-focus:     #2563eb;

    /* Sizing */
    --tc-radius:          8px;
    --tc-radius-sm:       4px;
    --tc-radius-lg:       12px;
    --tc-font-size:       14px;
    --tc-font-family:     -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto、sans-serif;
    --tc-sidebar-width:   280px;
    --tc-max-width:       none;
}

/* ── Dark theme ── */
[data-theme="dark"] {
    --tc-bg:              #1a1a2e;
    --tc-bg-secondary:    #16213e;
    --tc-bg-sidebar:      #0f1525;
    --tc-border:          #2a2a4a;
    --tc-shadow:          0 1px 3px rgba(0,0,0,0.3);

    --tc-text:            #e0e0e0;
    --tc-text-secondary:  #a0a0a0;
    --tc-text-muted:      #666680;

    --tc-accent:          #60a5fa;
    --tc-accent-hover:    #93c5fd;
    --tc-accent-subtle:   #1e293b;

    --tc-user-bg:         #2563eb;
    --tc-user-text:       #ffffff;
    --tc-assistant-bg:    #16213e;
    --tc-assistant-text:  #e0e0e0;
    --tc-system-bg:       #2a2a1e;
    --tc-system-text:     #fbbf24;

    --tc-chip-bg:         #1e293b;
    --tc-chip-border:     #334155;
    --tc-chip-success:    #052e16;
    --tc-chip-success-border: #166534;
    --tc-chip-fail:       #450a0a;
    --tc-chip-fail-border:#991b1b;
    --tc-chip-progress:   #0c1929;
    --tc-chip-progress-border: #1e40af;

    --tc-approve-bg:      #052e16;
    --tc-approve-border:  #166534;
    --tc-deny-bg:         #450a0a;
    --tc-deny-border:     #991b1b;

    --tc-input-bg:        #16213e;
    --tc-input-border:    #2a2a4a;
    --tc-input-focus:     #60a5fa;
}
```

---

## 7.2: Layout — `.tornado-chat`

```css
/* ── Main layout ── */
.tornado-chat {
    display: flex;
    height: 100%;
    max-width: var(--tc-max-width);
    font-family: var(--tc-font-family);
    font-size: var(--tc-font-size);
    color: var(--tc-text);
    background: var(--tc-bg);
    border: 1px solid var(--tc-border);
    border-radius: var(--tc-radius-lg);
    overflow: hidden;
    box-shadow: var(--tc-shadow);
}

.tornado-chat__main {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-width: 0;
}
```

---

## 7.3: Header Bar

```css
/* ── Header ── */
.tornado-chat__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 8px 12px;
    border-bottom: 1px solid var(--tc-border);
    background: var(--tc-bg-secondary);
    gap: 8px;
    flex-shrink: 0;
}

.tornado-chat__header-left,
.tornado-chat__header-right {
    display: flex;
    align-items: center;
    gap: 8px;
}

.tornado-chat__select {
    padding: 6px 28px 6px 10px;
    border: 1px solid var(--tc-input-border);
    border-radius: var(--tc-radius);
    background: var(--tc-input-bg);
    color: var(--tc-text);
    font-size: 13px;
    cursor: pointer;
    appearance: none;
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 12 12'%3E%3Cpath fill='%23666' d='M3 4.5L6 8l3-3.5H3z'/%3E%3C/svg%3E");
    background-repeat: no-repeat;
    background-position: right 8px center;
    min-width: 140px;
}

.tornado-chat__select:focus {
    outline: none;
    border-color: var(--tc-input-focus);
    box-shadow: 0 0 0 2px color-mix(in srgb, var(--tc-input-focus) 25%, transparent);
}

.tornado-chat__select option[disabled] {
    color: var(--tc-text-muted);
}

.tornado-chat__select optgroup {
    font-weight: 600;
}
```

---

## 7.4: Messages Area

```css
/* ── Messages ── */
.tornado-chat__messages {
    flex: 1;
    overflow-y: auto;
    padding: 16px;
    display: flex;
    flex-direction: column;
    gap: 8px;
    scroll-behavior: smooth;
}

.tornado-chat__loading {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    padding: 32px;
    color: var(--tc-text-muted);
}

.tornado-chat__empty {
    display: flex;
    align-items: center;
    justify-content: center;
    height: 100%;
    color: var(--tc-text-muted);
}

/* Spinner */
.tornado-chat__spinner {
    width: 20px;
    height: 20px;
    border: 2px solid var(--tc-border);
    border-top-color: var(--tc-accent);
    border-radius: 50%;
    animation: tc-spin 0.8s linear infinite;
}

@keyframes tc-spin {
    to { transform: rotate(360deg); }
}
```

---

## 7.5: Chat Bubbles

```css
/* ── Bubbles ── */
.chat-bubble {
    max-width: 80%;
    padding: 10px 14px;
    border-radius: var(--tc-radius-lg);
    line-height: 1.5;
    word-wrap: break-word;
    position: relative;
}

.chat-bubble--user {
    align-self: flex-end;
    background: var(--tc-user-bg);
    color: var(--tc-user-text);
    border-bottom-right-radius: var(--tc-radius-sm);
}

.chat-bubble--assistant {
    align-self: flex-start;
    background: var(--tc-assistant-bg);
    color: var(--tc-assistant-text);
    border-bottom-left-radius: var(--tc-radius-sm);
}

.chat-bubble--system {
    align-self: center;
    background: var(--tc-system-bg);
    color: var(--tc-system-text);
    font-size: 12px;
    max-width: 90%;
}

.chat-bubble--error {
    border: 1px solid var(--tc-chip-fail-border);
    background: var(--tc-chip-fail);
}

/* Markdown content within assistant bubbles */
.chat-bubble__markdown {
    line-height: 1.6;
}

.chat-bubble__markdown p {
    margin: 0.4em 0;
}

.chat-bubble__markdown pre {
    background: var(--tc-bg);
    border: 1px solid var(--tc-border);
    border-radius: var(--tc-radius);
    padding: 10px;
    overflow-x: auto;
    margin: 8px 0;
    font-size: 13px;
}

.chat-bubble__markdown code {
    background: var(--tc-bg);
    padding: 1px 4px;
    border-radius: 3px;
    font-size: 13px;
}

.chat-bubble__markdown pre code {
    background: transparent;
    padding: 0;
}

.chat-bubble__markdown ul,
.chat-bubble__markdown ol {
    margin: 0.4em 0;
    padding-left: 1.5em;
}

.chat-bubble__markdown table {
    border-collapse: collapse;
    margin: 8px 0;
    width: 100%;
}

.chat-bubble__markdown th,
.chat-bubble__markdown td {
    border: 1px solid var(--tc-border);
    padding: 6px 10px;
    text-align: left;
}

.chat-bubble__markdown th {
    background: var(--tc-bg-secondary);
    font-weight: 600;
}

/* Streaming cursor */
.chat-bubble__cursor {
    display: inline;
    animation: tc-blink 1s step-end infinite;
    color: var(--tc-accent);
}

@keyframes tc-blink {
    50% { opacity: 0; }
}

/* File chips inside user bubbles */
.chat-bubble__files {
    display: flex;
    flex-wrap: wrap;
    gap: 4px;
    margin-top: 6px;
}

.chat-bubble__file-chip {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    background: rgba(255,255,255,0.2);
    border-radius: var(--tc-radius-sm);
    padding: 2px 8px;
    font-size: 12px;
}

.chat-bubble__file-size {
    opacity: 0.7;
}

/* Meta line */
.chat-bubble__meta {
    font-size: 11px;
    opacity: 0.5;
    margin-top: 4px;
    text-align: right;
}
```

---

## 7.6: Event Chips

```css
/* ── Event chips ── */
.event-chip {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 4px 10px;
    border-radius: 12px;
    font-size: 12px;
    cursor: pointer;
    transition: background 0.15s;
    border: 1px solid var(--tc-chip-border);
    background: var(--tc-chip-bg);
    align-self: flex-start;
    user-select: none;
}

.event-chip:hover {
    filter: brightness(0.95);
}

.event-chip--completed {
    background: var(--tc-chip-success);
    border-color: var(--tc-chip-success-border);
}

.event-chip--failed,
.event-chip--error {
    background: var(--tc-chip-fail);
    border-color: var(--tc-chip-fail-border);
}

.event-chip--in-progress {
    background: var(--tc-chip-progress);
    border-color: var(--tc-chip-progress-border);
}

.event-chip__icon {
    font-size: 14px;
}

.event-chip__title {
    font-weight: 500;
}

.event-chip__spinner {
    width: 12px;
    height: 12px;
    border: 1.5px solid var(--tc-chip-progress-border);
    border-top-color: var(--tc-accent);
    border-radius: 50%;
    animation: tc-spin 0.8s linear infinite;
}

.event-chip__detail {
    background: var(--tc-bg-secondary);
    border: 1px solid var(--tc-border);
    border-radius: var(--tc-radius);
    padding: 8px 12px;
    margin: 4px 0;
    font-size: 12px;
    align-self: flex-start;
    max-width: 80%;
    overflow-x: auto;
}

.event-chip__detail pre {
    margin: 0;
    white-space: pre-wrap;
    word-break: break-all;
}
```

---

## 7.7: Tool Approval Banner

```css
/* ── Tool approval ── */
.tool-approval {
    background: var(--tc-bg-secondary);
    border: 1px solid var(--tc-border);
    border-radius: var(--tc-radius);
    padding: 12px 16px;
    margin: 8px 0;
    align-self: stretch;
}

.tool-approval__header {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 6px;
}

.tool-approval__icon {
    font-size: 16px;
}

.tool-approval__title {
    font-size: 13px;
}

.tool-approval__message {
    font-size: 13px;
    color: var(--tc-text-secondary);
    margin: 4px 0 10px;
}

.tool-approval__args {
    margin: 8px 0;
}

.tool-approval__args summary {
    cursor: pointer;
    font-size: 12px;
    color: var(--tc-text-muted);
}

.tool-approval__args pre {
    background: var(--tc-bg);
    border: 1px solid var(--tc-border);
    border-radius: var(--tc-radius-sm);
    padding: 8px;
    font-size: 12px;
    overflow-x: auto;
    margin-top: 4px;
}

.tool-approval__actions {
    display: flex;
    gap: 8px;
}
```

---

## 7.8: File Attachment Bar

```css
/* ── File bar ── */
.file-bar {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
    padding: 8px 12px;
    border-top: 1px solid var(--tc-border);
    background: var(--tc-bg-secondary);
}

.file-bar__chip {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    background: var(--tc-bg);
    border: 1px solid var(--tc-border);
    border-radius: var(--tc-radius);
    padding: 4px 8px;
    font-size: 12px;
}

.file-bar__preview {
    width: 28px;
    height: 28px;
    object-fit: cover;
    border-radius: var(--tc-radius-sm);
}

.file-bar__icon {
    font-size: 16px;
}

.file-bar__name {
    max-width: 120px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.file-bar__size {
    color: var(--tc-text-muted);
}

.file-bar__remove {
    background: none;
    border: none;
    cursor: pointer;
    color: var(--tc-text-muted);
    font-size: 14px;
    padding: 0 2px;
    line-height: 1;
}

.file-bar__remove:hover {
    color: var(--tc-chip-fail-border);
}
```

---

## 7.9: Input Area

```css
/* ── Input area ── */
.tornado-chat__input-area {
    display: flex;
    align-items: flex-end;
    gap: 6px;
    padding: 10px 12px;
    border-top: 1px solid var(--tc-border);
    background: var(--tc-bg);
}

.tornado-chat__textarea {
    flex: 1;
    resize: none;
    border: 1px solid var(--tc-input-border);
    border-radius: var(--tc-radius);
    padding: 8px 12px;
    font-family: inherit;
    font-size: var(--tc-font-size);
    color: var(--tc-text);
    background: var(--tc-input-bg);
    line-height: 1.4;
    min-height: 38px;
    max-height: 200px;
    overflow-y: auto;
}

.tornado-chat__textarea:focus {
    outline: none;
    border-color: var(--tc-input-focus);
    box-shadow: 0 0 0 2px color-mix(in srgb, var(--tc-input-focus) 25%, transparent);
}

.tornado-chat__textarea:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

.tornado-chat__textarea::placeholder {
    color: var(--tc-text-muted);
}
```

---

## 7.10: Buttons

```css
/* ── Buttons ── */
.tornado-chat__btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    border: 1px solid var(--tc-border);
    border-radius: var(--tc-radius);
    background: var(--tc-bg);
    color: var(--tc-text);
    cursor: pointer;
    font-size: 14px;
    padding: 6px 12px;
    transition: background 0.15s, border-color 0.15s;
    white-space: nowrap;
}

.tornado-chat__btn:hover {
    background: var(--tc-bg-secondary);
}

.tornado-chat__btn:disabled {
    opacity: 0.4;
    cursor: not-allowed;
}

.tornado-chat__btn--icon {
    padding: 6px 8px;
    font-size: 18px;
    border: none;
    background: transparent;
}

.tornado-chat__btn--icon:hover {
    background: var(--tc-bg-secondary);
    border-radius: var(--tc-radius);
}

.tornado-chat__btn--primary {
    background: var(--tc-accent);
    border-color: var(--tc-accent);
    color: #fff;
}

.tornado-chat__btn--primary:hover {
    background: var(--tc-accent-hover);
}

.tornado-chat__btn--send {
    background: var(--tc-accent);
    border-color: var(--tc-accent);
    color: #fff;
    padding: 6px 10px;
    font-size: 18px;
}

.tornado-chat__btn--send:hover {
    background: var(--tc-accent-hover);
}

.tornado-chat__btn--cancel {
    background: var(--tc-chip-fail);
    border-color: var(--tc-chip-fail-border);
    color: var(--tc-text);
    padding: 6px 10px;
    font-size: 18px;
}

.tornado-chat__btn--approve {
    background: var(--tc-approve-bg);
    border-color: var(--tc-approve-border);
    color: #166534;
    font-weight: 500;
}

.tornado-chat__btn--approve:hover {
    filter: brightness(0.95);
}

.tornado-chat__btn--deny {
    background: var(--tc-deny-bg);
    border-color: var(--tc-deny-border);
    color: #991b1b;
    font-weight: 500;
}

.tornado-chat__btn--deny:hover {
    filter: brightness(0.95);
}
```

---

## 7.11: Conversation Sidebar

```css
/* ── Sidebar ── */
.tornado-chat.tornado-chat--with-sidebar {
    /* sidebar + main */
}

.conversation-sidebar {
    width: var(--tc-sidebar-width);
    background: var(--tc-bg-sidebar);
    border-right: 1px solid var(--tc-border);
    display: flex;
    flex-direction: column;
    flex-shrink: 0;
    overflow: hidden;
    transition: width 0.2s, opacity 0.2s;
}

.conversation-sidebar--hidden {
    width: 0;
    opacity: 0;
    pointer-events: none;
}

.conversation-sidebar__new-btn {
    margin: 10px;
    text-align: center;
}

.conversation-sidebar__list {
    flex: 1;
    overflow-y: auto;
    padding: 0 6px 6px;
}

.conversation-sidebar__item {
    display: flex;
    flex-direction: column;
    padding: 8px 10px;
    border-radius: var(--tc-radius);
    cursor: pointer;
    position: relative;
    margin-bottom: 2px;
    transition: background 0.1s;
}

.conversation-sidebar__item:hover {
    background: var(--tc-bg-secondary);
}

.conversation-sidebar__item--active {
    background: var(--tc-accent-subtle);
    border: 1px solid color-mix(in srgb, var(--tc-accent) 20%, transparent);
}

.conversation-sidebar__title {
    font-weight: 500;
    font-size: 13px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    padding-right: 24px;
}

.conversation-sidebar__meta {
    font-size: 11px;
    color: var(--tc-text-muted);
    margin-top: 2px;
}

.conversation-sidebar__delete {
    position: absolute;
    right: 6px;
    top: 8px;
    background: none;
    border: none;
    cursor: pointer;
    font-size: 14px;
    opacity: 0;
    transition: opacity 0.1s;
    padding: 2px;
}

.conversation-sidebar__item:hover .conversation-sidebar__delete {
    opacity: 0.5;
}

.conversation-sidebar__delete:hover {
    opacity: 1 !important;
}

.conversation-sidebar__empty {
    text-align: center;
    padding: 24px 12px;
    color: var(--tc-text-muted);
    font-size: 13px;
}
```

---

## 7.12: Responsive Breakpoint

```css
/* ── Responsive ── */
@media (max-width: 768px) {
    .conversation-sidebar {
        position: absolute;
        left: 0;
        top: 0;
        bottom: 0;
        z-index: 10;
        box-shadow: 2px 0 8px rgba(0,0,0,0.15);
    }

    .chat-bubble {
        max-width: 90%;
    }

    .tornado-chat__select {
        min-width: 100px;
        font-size: 12px;
    }
}
```

---

## 7.13: How Consumers Use It

### Linking the stylesheet

In `_Host.cshtml` (Server) or `index.html` (WASM):

```html
<link rel="stylesheet" href="_content/LlmTornado.Cli.Blazor/tornado-chat.css" />
```

### Switching themes

```html
<!-- Light (default) -->
<html>

<!-- Dark -->
<html data-theme="dark">
```

Or toggle at runtime with JS:

```js
document.documentElement.setAttribute('data-theme', 'dark');
```

### Overriding variables

Consumers can override any variable without touching the library:

```css
:root {
    --tc-accent: #10b981;          /* Green accent */
    --tc-user-bg: #10b981;
    --tc-radius: 4px;              /* Sharp corners */
    --tc-font-family: 'JetBrains Mono', monospace;
    --tc-sidebar-width: 320px;     /* Wider sidebar */
}
```

### Constraining size

```css
.my-chat-container {
    height: 600px;
    max-width: 900px;
}

/* Or full viewport */
.my-chat-container {
    height: 100vh;
}
```

The `TornadoChatPanel` fills its container with `height: 100%`.
