# -*- coding: utf-8 -*-
"""Renders spec.py into a print-ready HTML document for PDF conversion."""

import base64, html, io, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from spec import BRAND, HISTORY, NOTE, PARTS, APPENDICES

GREEN = BRAND["green"]
HERE = os.path.dirname(os.path.abspath(__file__))
ICON = open(os.path.join(HERE, "icon.svg"), "rb").read()
ICON_URI = "data:image/svg+xml;base64," + base64.b64encode(ICON).decode()

E = html.escape


def code_block(text):
    return '<pre class="code">%s</pre>' % E(text)


CSS = """
@page { size: A4; }

* { box-sizing: border-box; }

html { -webkit-print-color-adjust: exact; print-color-adjust: exact; }

body {
  margin: 0;
  font-family: "Helvetica Neue", Helvetica, Arial, sans-serif;
  font-size: 9.6pt;
  line-height: 1.55;
  color: #1c1f1d;
}

/* ---------------------------------------------------------------- cover */

.cover {
  height: 297mm;
  width: 210mm;
  position: relative;
  background: GREEN;
  color: #fff;
  padding: 34mm 26mm;
}
.cover-plate {
  width: 24mm; height: 24mm; border-radius: 5mm; background: #fff;
  display: flex; align-items: center; justify-content: center;
  margin-bottom: 15mm;
}
.cover-icon { width: 16mm; height: 16mm; display: block; }
.cover h1 {
  font-size: 34pt;
  line-height: 1.12;
  font-weight: 700;
  letter-spacing: -0.4pt;
  margin: 0 0 6mm;
  white-space: pre-line;
}
.cover .version {
  font-size: 13pt;
  font-weight: 400;
  letter-spacing: 1.1pt;
  opacity: 0.92;
  margin: 0 0 22mm;
}
.cover .rule { width: 26mm; height: 1.2mm; background: #ffc329; margin-bottom: 9mm; }
.cover .sub { font-size: 11.5pt; line-height: 1.7; opacity: 0.95; max-width: 105mm; }
.cover-foot {
  position: absolute;
  left: 26mm; right: 26mm; bottom: 26mm;
  font-size: 9pt;
  opacity: 0.9;
  border-top: 0.3mm solid rgba(255,255,255,0.35);
  padding-top: 5mm;
  display: flex;
  justify-content: space-between;
}
.cover-foot b { font-weight: 600; }

/* -------------------------------------------------------------- sections */

.page-break { page-break-before: always; }

h2.part {
  font-size: 15.5pt;
  font-weight: 700;
  color: #11150f;
  letter-spacing: -0.15pt;
  margin: 0 0 1.5mm;
  padding-bottom: 3mm;
  border-bottom: 0.7mm solid GREEN;
  page-break-after: avoid;
}
h2.part .num { font-weight: 500; color: #7c8a83; }

p.part-intro {
  margin: 4mm 0 7mm;
  color: #3f4643;
  font-size: 10pt;
  line-height: 1.6;
  page-break-after: avoid;
}

h3.ep {
  font-size: 11.5pt;
  font-weight: 700;
  color: #11150f;
  letter-spacing: -0.1pt;
  margin: 9.5mm 0 4mm;
  padding-left: 3.5mm;
  border-left: 1.1mm solid GREEN;
  page-break-after: avoid;
}
h3.ep .num { color: #11150f; font-weight: 700; margin-right: 2mm; }

h4.sub {
  font-size: 8.2pt;
  font-weight: 700;
  color: #4a544e;
  text-transform: uppercase;
  letter-spacing: 1.1pt;
  margin: 6.5mm 0 2.5mm;
  padding-bottom: 1.2mm;
  border-bottom: 0.2mm solid #dde5e0;
  page-break-after: avoid;
}

h4.prose-h {
  font-size: 10pt;
  font-weight: 700;
  color: #11150f;
  margin: 6.5mm 0 1.8mm;
  page-break-after: avoid;
}

p { margin: 0 0 3mm; }

/* ---------------------------------------------------------------- tables */

table {
  width: 100%;
  border-collapse: collapse;
  font-size: 8.8pt;
  margin: 0 0 3.5mm;
  page-break-inside: auto;
}
thead { display: table-header-group; }
tr { page-break-inside: avoid; }
th {
  background: GREEN;
  color: #fff;
  font-weight: 600;
  text-align: left;
  padding: 2.4mm 3mm;
  border: 0.2mm solid GREEN;
  font-size: 8.4pt;
  letter-spacing: 0.2pt;
}
td {
  padding: 2.5mm 3mm;
  border: 0.2mm solid #d3dcd7;
  vertical-align: top;
  line-height: 1.5;
}
tbody tr:nth-child(even) td { background: #f4f8f5; }

table.meta td:first-child {
  width: 33mm;
  font-weight: 600;
  background: #f2f6f3;
  color: #11150f;
}
table.meta td { border-color: #d3dcd7; }

table.params th:nth-child(1), table.params td:nth-child(1) { width: 28mm; }
table.params th:nth-child(2), table.params td:nth-child(2) { width: 15mm; }
table.params th:nth-child(3), table.params td:nth-child(3) { width: 14mm; }
table.params th:nth-child(4), table.params td:nth-child(4) { width: 17mm; }
table.params th:nth-child(5), table.params td:nth-child(5) { width: 29mm; }

table.codes th:nth-child(1), table.codes td:nth-child(1) { width: 27mm; }

table.hist th:nth-child(1), table.hist td:nth-child(1) { width: 26mm; }
table.hist th:nth-child(2), table.hist td:nth-child(2) { width: 30mm; }
table.hist td { white-space: pre-line; }

/* ------------------------------------------------------------------ code */

pre.code {
  background: #f6f8f7;
  border: 0.2mm solid #d3dcd7;
  border-left: 0.9mm solid GREEN;
  border-radius: 0.6mm;
  padding: 3.4mm 4mm;
  font-family: "SF Mono", Menlo, Consolas, "Courier New", monospace;
  font-size: 8.1pt;
  line-height: 1.5;
  white-space: pre-wrap;
  word-break: break-word;
  margin: 0 0 3mm;
  page-break-inside: avoid;
}

code {
  font-family: "SF Mono", Menlo, Consolas, "Courier New", monospace;
  font-size: 0.92em;
  background: #eef4f0;
  padding: 0.3mm 1mm;
  border-radius: 0.5mm;
  color: #0a5d2b;
}
th code, .verb code { background: none; color: inherit; }

/* ----------------------------------------------------------------- notes */

.note {
  background: #f4f8f5;
  border-left: 1mm solid #ffc329;
  padding: 3mm 4mm;
  margin: 0 0 3mm;
  font-size: 9pt;
  page-break-inside: avoid;
}
.note ul { margin: 0; padding-left: 4.5mm; }
.note li { margin-bottom: 1.6mm; }
.note li:last-child { margin-bottom: 0; }

/* ------------------------------------------------------------------ verb */

.verb {
  display: inline-block;
  font-family: "SF Mono", Menlo, Consolas, monospace;
  font-size: 7.6pt;
  font-weight: 700;
  letter-spacing: 0.5pt;
  color: #fff;
  background: GREEN;
  padding: 0.7mm 2.2mm;
  border-radius: 0.6mm;
  margin-right: 2mm;
  vertical-align: 1.2mm;
}
.verb.get { background: #1f6b45; }
.verb.post { background: #0b7335; }
.verb.put { background: #a1701a; }
.verb.delete { background: #9b2c2c; }

/* -------------------------------------------------------------------- toc */

.toc { margin-top: 5mm; }
.toc-part {
  font-weight: 700;
  color: #11150f;
  margin: 3.4mm 0 1.1mm;
  font-size: 9.6pt;
}
.toc-item {
  display: flex;
  font-size: 8.9pt;
  color: #3f4643;
  margin-bottom: 0.7mm;
  padding-left: 6mm;
}
.toc-item .t { }
.toc-item .d {
  flex: 1;
  border-bottom: 0.2mm dotted #c4cfc9;
  margin: 0 2mm 0.9mm;
}

/* ---------------------------------------------------------------- utility */

.lede {
  font-size: 10.5pt;
  line-height: 1.65;
  color: #3f4643;
  margin-bottom: 6mm;
}
.kv { color: #6b7671; }
""".replace("GREEN", GREEN)


def render_params(params):
    if not params:
        return '<p class="kv">This endpoint takes no parameters.</p>'
    rows = "".join(
        "<tr><td><code>%s</code></td><td>%s</td><td>%s</td><td>%s</td><td><code>%s</code></td><td>%s</td></tr>"
        % (pr["key"], pr["pos"], pr["type"], pr["req"], E(str(pr["example"])), pr["desc"])
        for pr in params
    )
    return (
        '<table class="params"><thead><tr>'
        "<th>Key</th><th>Position</th><th>Type</th><th>Required</th><th>Example</th><th>Description</th>"
        "</tr></thead><tbody>%s</tbody></table>" % rows
    )


def render_codes(codes):
    rows = "".join("<tr><td><code>%s</code></td><td>%s</td></tr>" % (c, d) for c, d in codes)
    return ('<table class="codes"><thead><tr><th>Status code</th><th>Description</th>'
            "</tr></thead><tbody>%s</tbody></table>" % rows)


def render_endpoint(part_no, ep_no, ep):
    verb = ep["method"].lower()
    out = []
    out.append('<h3 class="ep"><span class="num">%d.%d</span> %s</h3>'
               % (part_no, ep_no, E(ep["name"])))

    out.append(
        '<table class="meta"><tbody>'
        "<tr><td>URL</td><td><code>%s</code></td></tr>"
        '<tr><td>Request method</td><td><span class="verb %s">%s</span></td></tr>'
        "<tr><td>Content-type</td><td>%s</td></tr>"
        "<tr><td>Authorisation</td><td>%s</td></tr>"
        "<tr><td>Description</td><td>%s</td></tr>"
        "</tbody></table>"
        % (E(ep["url"]), verb, ep["method"], E(ep["content_type"]), ep["auth"], ep["desc"])
    )

    if ep.get("notes"):
        out.append('<div class="note"><ul>%s</ul></div>'
                   % "".join("<li>%s</li>" % n for n in ep["notes"]))

    out.append('<h4 class="sub">Request parameters</h4>')
    out.append(render_params(ep["params"]))

    out.append('<h4 class="sub">Request demo</h4>')
    out.append(code_block(ep["demo"]))

    out.append('<h4 class="sub">Explanation for return code</h4>')
    out.append(render_codes(ep["codes"]))

    out.append('<h4 class="sub">Sample return result</h4>')
    ret = ep["returns"]
    out.append(code_block(ret) if ret.strip().startswith("{") else '<p>%s</p>' % ret)

    return "\n".join(out)


def build():
    parts_html = []
    toc = []

    for i, part in enumerate(PARTS, start=1):
        toc.append('<div class="toc-part">Part %d. %s</div>' % (i, E(part["title"])))
        body = ['<h2 class="part"><span class="num">Part %d.</span> %s</h2>' % (i, E(part["title"]))]
        body.append('<p class="part-intro">%s</p>' % part["intro"])

        for h, txt in part.get("prose", []):
            body.append('<h4 class="prose-h">%s</h4><p>%s</p>' % (E(h), txt))

        for j, ep in enumerate(part["endpoints"], start=1):
            toc.append('<div class="toc-item"><span class="t">%d.%d&nbsp;&nbsp;%s</span>'
                       '<span class="d"></span></div>' % (i, j, E(ep["name"])))
            body.append(render_endpoint(i, j, ep))

        parts_html.append('<section class="page-break">%s</section>' % "\n".join(body))

    for ap in APPENDICES:
        toc.append('<div class="toc-part">Appendix %s. %s</div>' % (ap["letter"], E(ap["title"])))
        body = ['<h2 class="part"><span class="num">Appendix %s.</span> %s</h2>'
                % (ap["letter"], E(ap["title"]))]
        body.append('<p class="part-intro">%s</p>' % ap["intro"])
        if ap.get("code"):
            body.append(code_block(ap["code"]))
        t = ap["table"]
        head = "".join("<th>%s</th>" % h for h in t["head"])
        rows = "".join("<tr>%s</tr>" % "".join("<td>%s</td>" % c for c in r) for r in t["rows"])
        body.append("<table><thead><tr>%s</tr></thead><tbody>%s</tbody></table>" % (head, rows))
        parts_html.append('<section class="page-break">%s</section>' % "\n".join(body))

    hist_rows = "".join(
        "<tr><td><code>%s</code></td><td>%s</td><td>%s</td></tr>" % (v, d, E(c))
        for v, d, c in HISTORY
    )
    note_items = "".join("<li>%s</li>" % n for n in NOTE)

    cover = """<!doctype html>
<html lang="en"><head><meta charset="utf-8">
<title>%(product)s Open API %(version)s</title>
<style>%(css)s
@page { size: A4; margin: 0; }</style></head>
<body>
<div class="cover">
  <div class="cover-plate"><img class="cover-icon" src="%(icon)s" alt=""></div>
  <h1>%(title)s</h1>
  <div class="version">%(version)s</div>
  <div class="rule"></div>
  <div class="sub">Integration reference for electronic shelf label
  operations — products, pricing, bulk loading and scheduled display.</div>
  <div class="cover-foot">
    <span><b>%(company)s</b></span>
    <span>%(website)s &nbsp;·&nbsp; %(email)s</span>
  </div>
</div>
</body></html>"""

    body = """<!doctype html>
<html lang="en"><head><meta charset="utf-8">
<title>%(product)s Open API %(version)s</title>
<style>%(css)s
@page { size: A4; margin: 20mm 24mm 22mm 24mm; }</style></head>
<body>
<section>
  <h2 class="part">Document history</h2>
  <table class="hist"><thead><tr><th>Revision</th><th>Release date</th><th>Changes / notes</th></tr></thead>
  <tbody>%(hist)s</tbody></table>

</section>

<section class="page-break">
  <h2 class="part">Contents</h2>
  <div class="toc">%(toc)s</div>
</section>

<section class="page-break">
  <h2 class="part">Note</h2>
  <p class="part-intro">Read before working through the parts that follow.</p>
  <div class="note"><ul>%(note)s</ul></div>
</section>

%(parts)s

</body></html>""" % {
        "product": E(BRAND["product"]),
        "version": E(BRAND["version"]),
        "title": E(BRAND["title"]),
        "company": E(BRAND["company"]),
        "website": E(BRAND["website"]),
        "email": E(BRAND["email"]),
        "css": CSS,
        "icon": ICON_URI,
        "hist": hist_rows,
        "toc": "\n".join(toc),
        "note": note_items,
        "parts": "\n".join(parts_html),
    }

    here = os.path.dirname(os.path.abspath(__file__))
    fields = {
        "product": E(BRAND["product"]), "version": E(BRAND["version"]),
        "title": E(BRAND["title"]), "company": E(BRAND["company"]),
        "website": E(BRAND["website"]), "email": E(BRAND["email"]),
        "css": CSS, "icon": ICON_URI,
    }
    io.open(os.path.join(here, "cover.html"), "w", encoding="utf-8").write(cover % fields)
    io.open(os.path.join(here, "body.html"), "w", encoding="utf-8").write(body)
    out = os.path.join(here, "body.html")
    eps = sum(len(p["endpoints"]) for p in PARTS)
    print("written:", out)
    print("parts: %d | endpoints: %d | appendices: %d" % (len(PARTS), eps, len(APPENDICES)))


if __name__ == "__main__":
    build()
