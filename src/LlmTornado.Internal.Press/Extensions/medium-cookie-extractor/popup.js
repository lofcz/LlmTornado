document.getElementById('extractBtn').addEventListener('click', async () => {
  const button = document.getElementById('extractBtn');
  const statusDiv = document.getElementById('status');
  const cookieDisplay = document.getElementById('cookieDisplay');
  
  button.disabled = true;
  button.textContent = 'Extracting...';
  statusDiv.className = '';
  statusDiv.style.display = 'none';
  cookieDisplay.classList.remove('show');
  
  try {
    // Get cookies from medium.com
    const uidCookie = await chrome.cookies.get({
      url: 'https://medium.com',
      name: 'uid'
    });
    
    const sidCookie = await chrome.cookies.get({
      url: 'https://medium.com',
      name: 'sid'
    });
    
    if (!uidCookie || !sidCookie) {
      throw new Error('Could not find Medium cookies. Make sure you are logged into Medium.');
    }
    
    const uid = uidCookie.value;
    const sid = sidCookie.value;
    
    // Format for appCfg.json with proper indentation
    const config = `"medium": {\n  "cookieUid": "${uid}",\n  "cookieSid": "${sid}"\n}`;
    
    // Copy to clipboard
    try {
      await navigator.clipboard.writeText(config);
      
      // Show success message
      statusDiv.className = 'success';
      statusDiv.innerHTML = '✅ <strong>Success!</strong> Cookies copied to clipboard.<br>Paste into your appCfg.json file.';
      
      // Display the cookies
      cookieDisplay.textContent = config;
      cookieDisplay.classList.add('show');
      
      // Reset button after delay
      setTimeout(() => {
        button.disabled = false;
        button.textContent = 'Extract & Copy Cookies';
      }, 2000);
      
    } catch (clipboardError) {
      // Fallback: show cookies for manual copy
      statusDiv.className = 'warning';
      statusDiv.innerHTML = '⚠️ Could not auto-copy. Please copy manually from below:';
      
      cookieDisplay.textContent = config;
      cookieDisplay.classList.add('show');
      
      button.disabled = false;
      button.textContent = 'Extract & Copy Cookies';
    }
    
  } catch (error) {
    statusDiv.className = 'error';
    statusDiv.innerHTML = `❌ <strong>Error:</strong> ${error.message}`;
    
    button.disabled = false;
    button.textContent = 'Extract & Copy Cookies';
  }
});

// Check if on Medium when popup opens
chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
  const currentTab = tabs[0];
  if (currentTab && !currentTab.url.includes('medium.com')) {
    const statusDiv = document.getElementById('status');
    statusDiv.className = 'warning';
    statusDiv.innerHTML = '⚠️ <strong>Note:</strong> You are not on Medium.com. The extension will still work if you are logged in.';
  }
});

