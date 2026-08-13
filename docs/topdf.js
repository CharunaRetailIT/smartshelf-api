const puppeteer = require('puppeteer-core');
const path = require('path');

const GREEN = '#0b7335';
const DIR = __dirname;

// The cover is full-bleed with no running footer; the body carries margins and a
// page-numbered footer. They are printed separately and merged, because Chrome
// applies one margin box to the whole document.
const footer = `
  <style>
    #f { font-family: Helvetica, Arial, sans-serif; font-size: 7.5pt; color: #6b7671;
         width: 100%; padding: 0 24mm; display: flex; justify-content: space-between;
         border-top: 0.3pt solid #d3dcd7; padding-top: 3mm; }
    #f b { color: ${GREEN}; font-weight: 600; }
  </style>
  <div id="f">
    <span><b>Retail IT (Pvt) Ltd</b> &nbsp;&#183;&nbsp; SmartShelf ESL Open API V1.0.0</span>
    <span>Page <span class="pageNumber"></span></span>
  </div>`;

(async () => {
  const browser = await puppeteer.launch({
    executablePath: process.env.CHROME_PATH
      || '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
    headless: 'new',
    args: ['--no-sandbox', '--disable-gpu', '--font-render-hinting=none'],
  });

  const cover = await browser.newPage();
  await cover.goto('file://' + path.join(DIR, 'cover.html'), { waitUntil: 'networkidle0' });
  await cover.pdf({
    path: path.join(DIR, 'cover.pdf'),
    format: 'A4',
    printBackground: true,
    displayHeaderFooter: false,
    margin: { top: '0mm', right: '0mm', bottom: '0mm', left: '0mm' },
  });

  const body = await browser.newPage();
  await body.goto('file://' + path.join(DIR, 'body.html'), { waitUntil: 'networkidle0' });
  await body.pdf({
    path: path.join(DIR, 'body.pdf'),
    format: 'A4',
    printBackground: true,
    displayHeaderFooter: true,
    headerTemplate: '<div></div>',
    footerTemplate: footer,
    preferCSSPageSize: true,
  });

  await browser.close();
  console.log('cover.pdf and body.pdf written');
})();
