// YT Downloader Pro - Browser Extension Background Service Worker

const APP_HOST = 'http://localhost:9614';

// Video URL patterns
const VIDEO_PATTERNS = [
  /(?:https?:\/\/)?(?:www\.)?(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/shorts\/|youtube\.com\/live\/)[\w-]+/i,
  /(?:https?:\/\/)?(?:www\.)?youtube\.com\/playlist\?list=[\w-]+/i,
  /(?:https?:\/\/)?(?:www\.)?vimeo\.com\/\d+/i,
  /(?:https?:\/\/)?(?:www\.)?dailymotion\.com\/video\/[\w]+/i,
  /(?:https?:\/\/)?(?:www\.)?twitch\.tv\/videos\/\d+/i,
  /(?:https?:\/\/)?(?:www\.)?tiktok\.com\/@[\w.]+\/video\/\d+/i,
  /(?:https?:\/\/)?(?:www\.)?instagram\.com\/(?:p|reel|tv)\/[\w-]+/i,
  /(?:https?:\/\/)?(?:www\.)?(?:twitter|x)\.com\/\w+\/status\/\d+/i,
  /(?:https?:\/\/)?(?:www\.)?reddit\.com\/r\/\w+\/comments\//i,
  /(?:https?:\/\/)?(?:www\.)?facebook\.com\/(?:watch|video)\//i,
];

function isVideoUrl(url) {
  return VIDEO_PATTERNS.some(p => p.test(url));
}

async function sendToApp(url, title = '') {
  try {
    const response = await fetch(`${APP_HOST}/download`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ url, title })
    });
    if (response.ok) {
      const data = await response.json();
      return data.success;
    }
    return false;
  } catch (e) {
    console.warn('YT Downloader Pro app not running:', e.message);
    return false;
  }
}

async function checkAppRunning() {
  try {
    const response = await fetch(`${APP_HOST}/status`, { method: 'GET' });
    return response.ok;
  } catch {
    return false;
  }
}

// Context menu
chrome.runtime.onInstalled.addListener(() => {
  chrome.contextMenus.create({
    id: 'ytdlp-download-link',
    title: 'Download with YT Downloader Pro',
    contexts: ['link']
  });
  chrome.contextMenus.create({
    id: 'ytdlp-download-page',
    title: 'Download this page with YT Downloader Pro',
    contexts: ['page']
  });
  chrome.contextMenus.create({
    id: 'ytdlp-download-video',
    title: 'Download video with YT Downloader Pro',
    contexts: ['video']
  });
});

chrome.contextMenus.onClicked.addListener(async (info, tab) => {
  let url = '';
  let title = tab?.title || '';

  if (info.menuItemId === 'ytdlp-download-link') {
    url = info.linkUrl || '';
  } else if (info.menuItemId === 'ytdlp-download-page') {
    url = info.pageUrl || tab?.url || '';
  } else if (info.menuItemId === 'ytdlp-download-video') {
    url = info.srcUrl || tab?.url || '';
  }

  if (!url) return;

  const success = await sendToApp(url, title);
  if (!success) {
    chrome.notifications.create({
      type: 'basic',
      iconUrl: 'icons/icon48.png',
      title: 'YT Downloader Pro',
      message: 'App is not running. Please open YT Downloader Pro first.'
    });
  }
});

// Message from content script
chrome.runtime.onMessage.addListener(async (message, sender, sendResponse) => {
  if (message.type === 'VIDEO_DETECTED') {
    const { url, title } = message;
    await sendToApp(url, title);
    sendResponse({ success: true });
  } else if (message.type === 'CHECK_APP') {
    const running = await checkAppRunning();
    sendResponse({ running });
  }
  return true;
});

// Tab update - detect when navigating to video pages
chrome.tabs.onUpdated.addListener(async (tabId, changeInfo, tab) => {
  if (changeInfo.status === 'complete' && tab.url && isVideoUrl(tab.url)) {
    // Update the badge
    chrome.action.setBadgeText({ text: '▼', tabId });
    chrome.action.setBadgeBackgroundColor({ color: '#E53935', tabId });
  } else if (changeInfo.status === 'complete') {
    chrome.action.setBadgeText({ text: '', tabId });
  }
});
