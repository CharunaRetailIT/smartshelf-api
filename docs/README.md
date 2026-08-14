# API document

Client-facing PDF reference for the SmartShelf ESL API, structured to match the
Minew "ESL Cloud Platform Open API" document the client already works from.

| File | Purpose |
|---|---|
| `SmartShelf-ESL-Open-API-V1.0.0.pdf` | The deliverable — 54 pages, A4 |
| `spec.py` | All content, as plain data |
| `build.py` | Renders `spec.py` into print-ready HTML |
| `topdf.js` | Prints the HTML to PDF via headless Chrome |
| `icon.svg` | Cover mark |

## Changing the document

Content lives in `spec.py` and nothing else — endpoints, parameter tables, sample
payloads, return codes, appendices and the brand block at the top. Editing there
keeps spacing, numbering and cross-references consistent, because the layout is
applied by `build.py` rather than repeated by hand.

Adding an endpoint means appending one dictionary to the relevant part. Part and
section numbers (`4.6`, `7.11`) are derived from list position, so inserting an
entry renumbers everything after it automatically.

## Regenerating

Requires Python 3, Node, Google Chrome, and:

```bash
npm install puppeteer-core     # in this directory
brew install poppler           # provides pdfunite
```

Then:

```bash
python3 build.py && node topdf.js && pdfunite cover.pdf body.pdf SmartShelf-ESL-Open-API-V1.0.0.pdf && rm cover.pdf body.pdf cover.html body.html
```

Set `CHROME_PATH` if Chrome is not at the default macOS location.

## Why two PDFs are merged

Chrome applies a single margin box to a whole document, so a full-bleed cover and
page-numbered body cannot coexist in one print pass. The cover is printed with
zero margins and no footer, the body with margins and a running footer, and
`pdfunite` joins them.

Each document therefore declares its own `@page` rule inside `build.py` — the
cover at `margin: 0`, the body at `20mm 24mm 22mm 24mm`. A CSS `@page` margin
overrides the one passed to Puppeteer, so setting the page box in only one place
silently flattens the other.

## Before sending to a client

Endpoints are documented against `https://esl-api.retailit.lk`. If the client is
served from a different host, change it in `spec.py` — it appears in each
endpoint's `url` and in the base-address wording in Part 1 and the Note page.

Sample values throughout (store 3, "Colombo City Centre", the product codes and
the template identifier) come from the test database. Replace them if the client
should see their own data.

Company contact details on the cover and in the running footer are confirmed
correct and live in the `BRAND` block at the top of `spec.py`, alongside the
brand colour and version string.
