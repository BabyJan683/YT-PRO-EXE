// YT Downloader Pro - Content Script
// Injects download buttons on supported video sites

(function() {
  'use strict';

  const currentUrl = window.location.href;
  const VIDEO_PATTERNS = [
    /youtube\.com\/watch\?v=/i,
    /youtube\.com\/shorts\//i,
    /youtube\.com\/live\//i,
    /youtu\.be\//i,
    /vimeo\.com\/\d+/i,
    /dailymotion\.com\/video\//i,
    /tiktok\.com\/@[\w.]+\/video\//i,
    /instagram\.com\/(p|reel|tv)\//i,
    /twitter\.com\/\w+\/status\//i,
    /x\.com\/\w+\/status\//i,
  ];

  function isVideoPage() {
    return VIDEO_PATTERNS.some(p => p.test(currentUrl));
  }

  function notifyBackground() {
    chrome.runtime.sendMessage({
      type: 'VIDEO_DETECTED',
      url: currentUrl,
      title: document.title
    });
  }

  function injectYouTubeButton() {
    const existingBtn = document.getElementById('ytdlp-download-btn');
    if (existingBtn) return;

    const actionsContainer = document.querySelector('#top-level-buttons-computed, ytd-menu-renderer');
    if (!actionsContainer) {
      setTimeout(injectYouTubeButton, 2000);
      return;
    }

    const btn = document.createElement('button');
    btn.id = 'ytdlp-download-btn';
    btn.title = 'Download with YT Downloader Pro';
    btn.style.cssText = `
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 0 16px;
      height: 36px;
      border-radius: 18px;
      background: #E53935;
      color: white;
      border: none;
      cursor: pointer;
      font-family: 'YouTube Sans', Roboto, sans-serif;
      font-size: 14px;
      font-weight: 500;
      margin-left: 8px;
      transition: background 0.2s;
    `;
    btn.innerHTML = `
      <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor">
        <path d="M5 20h14v-2H5v2zm7-18l-7 7h4v6h6v-6h4l-7-7z"/>
      </svg>
      Download
    `;

    btn.addEventListener('mouseenter', () => btn.style.background = '#C62828');
    btn.addEventListener('mouseleave', () => btn.style.background = '#E53935');
    btn.addEventListener('click', () => {
      notifyBackground();
      btn.innerHTML = `<span>✓ Sent to YT Downloader Pro</span>`;
      btn.style.background = '#2E7D32';
      setTimeout(() => {
        btn.innerHTML = `
          <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor">
            <path d="M5 20h14v-2H5v2zm7-18l-7 7h4v6h6v-6h4l-7-7z"/>
          </svg>
          Download
        `;
        btn.style.background = '#E53935';
      }, 3000);
    });

    actionsContainer.appendChild(btn);
  }

  // Initialize
  if (isVideoPage()) {
    if (window.location.hostname.includes('youtube.com')) {
      // YouTube: wait for dynamic content
      const observer = new MutationObserver(() => {
        injectYouTubeButton();
      });
      observer.observe(document.body, { childList: true, subtree: true });
      injectYouTubeButton();
    }
  }

  // Listen for navigation changes (YouTube SPA)
  let lastUrl = currentUrl;
  new MutationObserver(() => {
    const url = window.location.href;
    if (url !== lastUrl) {
      lastUrl = url;
      if (isVideoPage()) {
        setTimeout(injectYouTubeButton, 1500);
      }
    }
  }).observe(document, { subtree: true, childList: true });
})();
