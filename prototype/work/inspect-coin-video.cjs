const { chromium } = require('playwright');
const { pathToFileURL } = require('url');
const fs = require('fs');
(async () => {
  const browser = await chromium.launch({
    executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe',
    args: ['--allow-file-access-from-files'],
  });
  try {
    const page = await browser.newPage();
    fs.writeFileSync('work/coin-video.html', '<video muted preload="auto"></video>');
    await page.goto(pathToFileURL(require('path').resolve('work/coin-video.html')).href);
    const frames = await page.evaluate(async (src) => {
      const video = document.querySelector('video');
      await new Promise((resolve, reject) => {
        video.onloadeddata = resolve;
        video.onerror = () => reject(new Error(video.error.message));
        video.src = src;
      });
      const canvas = document.createElement('canvas');
      canvas.width = 1200;
      canvas.height = 3 * 365;
      const context = canvas.getContext('2d');
      const times = [79.5, 81, 82.5, 104, 106, 108.5];
      for (const [index, time] of times.entries()) {
        await new Promise(resolve => { video.onseeked = resolve; video.currentTime = time; });
        const x = index % 2 * 600, y = Math.floor(index / 2) * 365;
        context.drawImage(video, x, y, 600, 337.5);
        context.font = '19px sans-serif'; context.fillStyle = 'white';
        context.fillText(time + ' s', x + 10, y + 359);
      }
      return canvas.toDataURL('image/jpeg', 0.92).split(',')[1];
    }, pathToFileURL(process.argv[2]).href);
    fs.writeFileSync('work/coin-reference-frames.jpg', Buffer.from(frames, 'base64'));
  } finally {
    await browser.close();
  }
})().catch(error => { console.error(error); process.exitCode = 1; });

