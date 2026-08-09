"""Build a Word (.docx) export of a Markdown design doc.

    python scripts/build_docx.py                       # docs/design-draft.md -> docs/design-draft.docx
    python scripts/build_docx.py docs/other.md         # -> docs/other.docx
    python scripts/build_docx.py docs/other.md out.docx

Requires pandoc on PATH. Everything else is stdlib.

Pandoc's stock reference.docx has no Korean font and no page size, and it
renders code blocks in a proportional font, which shears the ASCII diagrams.
This script patches a throwaway reference doc, converts, then fixes up the
result. The source .md is never modified.
"""

import io
import os
import re
import shutil
import subprocess
import sys
import tempfile
import zipfile

BODY_FONT = "Malgun Gothic"  # 맑은 고딕 — Latin + Hangul
MONO_FONT = "GulimChe"       # 굴림체 — CJK fixed-width, so diagrams line up

# A4 with 20mm margins, in DXA (1440 = 1 inch)
PG_SZ = '<w:pgSz w:w="11906" w:h="16838"/>'
PG_MAR = (
    '<w:pgMar w:top="1134" w:right="1134" w:bottom="1134" w:left="1134"'
    ' w:header="720" w:footer="720" w:gutter="0"/>'
)

# GulimChe draws box-drawing glyphs full-width while ASCII stays half-width,
# which shears any diagram mixing the two. Pure ASCII keeps every cell one
# column wide. Applied to fenced code blocks only, and only in the build copy.
FOLD = {
    "─": "-", "━": "-", "═": "=",
    "│": "|", "┃": "|", "║": "|",
    "┌": "+", "┐": "+", "└": "+", "┘": "+",
    "├": "+", "┤": "+", "┬": "+", "┴": "+", "┼": "+",
    "╭": "+", "╮": "+", "╯": "+", "╰": "+",
    "▶": ">", "◀": "<", "▼": "v", "▲": "^",
    "→": "->", "←": "<-", "↓": "v", "↑": "^",
    "×": "x", "Σ": "sum", "…": "...",
}


def read(path):
    return io.open(path, encoding="utf-8").read()


def write(path, text):
    io.open(path, "w", encoding="utf-8").write(text)


def unpack(docx, dest):
    with zipfile.ZipFile(docx) as z:
        z.extractall(dest)


def pack(src, docx):
    """Rezip a docx. [Content_Types].xml must be the first entry."""
    if os.path.exists(docx):
        os.remove(docx)
    with zipfile.ZipFile(docx, "w", zipfile.ZIP_DEFLATED) as z:
        z.write(os.path.join(src, "[Content_Types].xml"), "[Content_Types].xml")
        for root, _dirs, files in os.walk(src):
            for f in files:
                full = os.path.join(root, f)
                rel = os.path.relpath(full, src).replace(os.sep, "/")
                if rel != "[Content_Types].xml":
                    z.write(full, rel)


def rfonts(font):
    return '<w:rFonts w:ascii="{f}" w:eastAsia="{f}" w:hAnsi="{f}" w:cs="{f}"/>'.format(f=font)


def build_reference(workdir):
    """Pandoc's default reference.docx, repointed at Korean-capable fonts."""
    base = os.path.join(workdir, "reference.docx")
    with io.open(base, "wb") as fh:
        subprocess.run(
            ["pandoc", "--print-default-data-file", "reference.docx"],
            stdout=fh, check=True,
        )

    unpacked = os.path.join(workdir, "reference")
    unpack(base, unpacked)

    p = os.path.join(unpacked, "word", "styles.xml")
    s = read(p)
    # Theme-based font references resolve to a Latin-only font; pin them instead.
    s = re.sub(
        r"<w:rFonts\b[^>]*/>",
        lambda m: rfonts(BODY_FONT) if "Theme" in m.group(0) else m.group(0),
        s,
    )
    s = re.sub(r'<w:rFonts w:ascii="Consolas"[^>]*/>', rfonts(MONO_FONT), s)
    s = s.replace('w:eastAsia="zh-CN"', 'w:eastAsia="ko-KR"')
    write(p, s)

    p = os.path.join(unpacked, "word", "theme", "theme1.xml")
    s = read(p)
    s = re.sub(r'<a:latin typeface="[^"]*"', '<a:latin typeface="%s"' % BODY_FONT, s)
    s = re.sub(r'<a:ea typeface="[^"]*"', '<a:ea typeface="%s"' % BODY_FONT, s)
    write(p, s)

    out = os.path.join(workdir, "reference-ko.docx")
    pack(unpacked, out)
    return out


def fold_diagrams(md_path, out_path):
    """Rewrite box-drawing glyphs to ASCII inside fenced code blocks."""
    lines, in_fence, changed = read(md_path).split("\n"), False, 0
    result = []
    for ln in lines:
        if ln.lstrip().startswith("```"):
            in_fence = not in_fence
            result.append(ln)
            continue
        if in_fence:
            folded = "".join(FOLD.get(c, c) for c in ln)
            changed += folded != ln
            result.append(folded)
        else:
            result.append(ln)
    write(out_path, "\n".join(result))
    return changed


def postprocess(docx, workdir):
    """A4 pages, full-width tables, monospace code, self-updating TOC."""
    unpacked = os.path.join(workdir, "out")
    unpack(docx, unpacked)

    p = os.path.join(unpacked, "word", "document.xml")
    s = read(p)
    if "<w:pgSz" in s:
        s = re.sub(r"<w:pgSz[^>]*/>", PG_SZ, s)
        s = re.sub(r"<w:pgMar[^>]*/>", PG_MAR, s)
    else:
        s = re.sub(r"(<w:sectPr\b[^>]*>)", r"\1" + PG_SZ + PG_MAR, s)
    # Pandoc emits auto-width tables, which Word renders far narrower than the
    # text column. Pin them to the full column and let Word distribute.
    tables = s.count('<w:tblW w:type="auto" w:w="0" />')
    s = s.replace(
        '<w:tblW w:type="auto" w:w="0" />',
        '<w:tblW w:type="pct" w:w="5000"/><w:tblLayout w:type="autofit"/>',
    )
    write(p, s)

    # Pandoc generates SourceCode at output time, so it can't be set in the
    # reference doc; without this, code blocks inherit the proportional body font.
    p = os.path.join(unpacked, "word", "styles.xml")
    s = read(p)
    m = re.search(r'<w:style[^>]*w:styleId="SourceCode".*?</w:style>', s, re.S)
    if m and "<w:rPr>" not in m.group(0):
        patched = m.group(0).replace(
            "</w:style>",
            "<w:rPr>" + rfonts(MONO_FONT) + '<w:sz w:val="18"/><w:szCs w:val="18"/></w:rPr></w:style>',
        )
        write(p, s.replace(m.group(0), patched))

    # Pandoc writes the TOC as an empty field; this makes Word fill it on open.
    p = os.path.join(unpacked, "word", "settings.xml")
    s = read(p)
    if "updateFields" not in s:
        write(p, re.sub(r"(<w:settings[^>]*>)", r'\1<w:updateFields w:val="true"/>', s, count=1))

    pack(unpacked, docx)
    return tables


def main():
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    md = sys.argv[1] if len(sys.argv) > 1 else os.path.join(root, "docs", "design-draft.md")
    docx = sys.argv[2] if len(sys.argv) > 2 else os.path.splitext(md)[0] + ".docx"

    if not shutil.which("pandoc"):
        sys.exit("pandoc not found on PATH - install it from https://pandoc.org/installing.html")
    if not os.path.exists(md):
        sys.exit("no such file: " + md)

    workdir = tempfile.mkdtemp(prefix="healper-docx-")
    try:
        reference = build_reference(workdir)
        source = os.path.join(workdir, "build.md")
        folded = fold_diagrams(md, source)
        subprocess.run(
            ["pandoc", source, "-f", "gfm", "-o", docx,
             "--reference-doc", reference,
             "--toc", "--toc-depth", "2",
             "--metadata", "toc-title=목차"],
            check=True,
        )
        tables = postprocess(docx, workdir)
    finally:
        shutil.rmtree(workdir, ignore_errors=True)

    print("%s -> %s" % (os.path.relpath(md, root), os.path.relpath(docx, root)))
    print("  %d diagram lines folded, %d tables widened" % (folded, tables))


if __name__ == "__main__":
    main()
