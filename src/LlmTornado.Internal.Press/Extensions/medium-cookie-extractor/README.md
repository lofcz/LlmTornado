# Medium Cookie Extractor - Chrome Extension

A Chrome extension to extract Medium cookies (sid, uid) and copy them in the format ready for `appCfg.json`.

## Features

- ✅ Extracts HttpOnly cookies that JavaScript cannot access
- ✅ Formats cookies exactly for `appCfg.json`
- ✅ Auto-copies to clipboard
- ✅ Works only on Medium.com domain
- ✅ Beautiful modern UI

## Installation

1. Open Chrome and go to `chrome://extensions/`
2. Enable "Developer mode" (toggle in top-right corner)
3. Click "Load unpacked"
4. Select the `medium-cookie-extractor` folder
5. The extension icon will appear in your toolbar

## Usage

1. **Log into Medium.com** in your browser
2. **Click the extension icon** in your Chrome toolbar
3. **Click "Extract & Copy Cookies"**
4. The cookies will be automatically copied to your clipboard
5. **Paste** into your `appCfg.json` file in the `apiKeys` section

## What it does

The extension reads the `sid` and `uid` cookies from Medium.com (including HttpOnly cookies that normal JavaScript cannot access) and formats them like this:

```json
"medium": {
  "cookieUid": "your-uid-value",
  "cookieSid": "your-sid-value"
}
```

## Permissions

- **cookies**: Required to read Medium cookies
- **clipboardWrite**: Required to copy the formatted output
- **host_permissions**: Only active on `medium.com` domains

## Security

- The extension only requests access to `medium.com` domains
- No data is sent to any external servers
- All processing happens locally in your browser
- Open source - you can review the code

## Troubleshooting

**"Could not find Medium cookies"**
- Make sure you are logged into Medium.com
- Try refreshing the Medium page and clicking the extension again

**Clipboard not working**
- The cookies will still be displayed in the extension popup
- You can manually copy them from there

## Files

- `manifest.json` - Extension configuration
- `popup.html` - Extension popup UI
- `popup.js` - Cookie extraction logic
- `icon*.png` - Extension icons (need to be created)
- `README.md` - This file

