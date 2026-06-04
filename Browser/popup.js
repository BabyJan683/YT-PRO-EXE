const VIDEO_PATTERNS = [
  /youtube\.com\/watch\?v=/i,
  /youtube\.com\/shorts\//i,
  /youtube\.com\/playlist\?list=/i,
  /youtu\.be\//i,
  /vimeo\.com\/\d+/i,
  /dailymotion\.com\/video\//i,
  /tiktok\.com\/@[\w.]+\/video\//i,
  /instagram\.com\/(p|reel|tv)\//i,
  /twitter\.com\/\w+\/status\//i,
  /x\.com\/\w+\/status\//i,
  /twitch\.tv\/videos\//i,
  /facebook\.com\/(watch|video)\//i,
  /reddit\.com\/r\/\w+\/comments\//i,
];

function isVideoUrl(url) {
  return VIDEO_PATTERNS.some(p => p.test(url));
}

async function checkAppRunning() {
  return new Promise(resolve => {
    chrome.runtime.sendMessage({ type: 'CHECK_APP' }, response => {
      resolve(response?.running ?? false);
    });
  });
}

async function init() {
  const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
  const url = tab?.url || '';
  const title = tab?.title || '';

  document.getElementById('currentUrl').textContent = url || 'Unknown page';

  // Check if app is running
  const appRunning = await checkAppRunning();
  const statusDot = document.getElementById('statusDot');
  const statusText = document.getElementById('statusText');

  if (appRunning) {
    statusDot.classList.remove('offline');
    statusText.textContent = 'App is running ✓';
  } else {
    statusDot.classList.add('offline');
    statusText.textContent = 'App is not running';
  }

  // Enable download button if it's a video URL
  const isVideo = isVideoUrl(url);
  const btnDownload = document.getElementById('btnDownload');
  btnDownload.disabled = !isVideo || !appRunning;

  btnDownload.addEventListener('click', async () => {
    btnDownload.disabled = true;
    btnDownload.textContent = 'Sending...';

    chrome.runtime.sendMessage({ type: 'VIDEO_DETECTED', url, title }, response => {
      const result = document.getElementById('result');
      result.style.display = 'block';

      if (response?.success) {
        result.className = 'success';
        result.textContent = '✓ Sent to YT Downloader Pro!';
        btnDownload.textContent = '✓ Done!';
      } else {
        result.className = 'error';
        result.textContent = '✗ Failed. Is the app running?';
        btnDownload.textContent = 'Download This Video';
        btnDownload.disabled = false;
      }
    });
  });

  document.getElementById('btnOpenApp').addEventListener('click', () => {
    // On Windows, this would ideally open the app.
    // For now, show a message about the app location.
    const result = document.getElementById('result');
    result.style.display = 'block';
    result.className = 'success';
    result.textContent = 'Find YTDownloaderPro in your system tray';
  });
}

init().catch(console.error);
