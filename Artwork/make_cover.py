#!/usr/bin/env python3
"""Renders ModCover.jpg (800x450) for the Stock Day Summary mod.

Drawn at 2x and downscaled for smooth edges. Original artwork only:
space backdrop, a chart "icon" tile with a pause badge, title and a
mock of the in-game summary line.

Needs Pillow and the Inter font (Bold, ExtraBold, SemiBold, Medium):
  py -m pip install pillow        (python3 -m pip install pillow elsewhere)
  Inter: https://rsms.me/inter/ - install it, or unzip it into Artwork/fonts/

Usage: py make_cover.py [out_path]   (default: ModCover.jpg in the mod root)
"""
import os
import random
import sys
from pathlib import Path

try:
    from PIL import Image, ImageDraw, ImageFilter, ImageFont
except ImportError:
    sys.exit("Pillow is missing: run  py -m pip install pillow  (or python3 -m pip install pillow)")

SCALE = 2
W, H = 800 * SCALE, 450 * SCALE
ARTWORK_DIR = Path(__file__).resolve().parent
DEFAULT_OUT = ARTWORK_DIR.parent / "ModCover.jpg"
FONT_EXTENSIONS = (".otf", ".ttf")
_font_paths = {}

BG_TOP = (14, 10, 36)
BG_BOTTOM = (40, 28, 92)
TILE_FILL = (36, 28, 96)
TILE_EDGE = (110, 96, 230)
GRID = (70, 60, 150)
GREEN = (92, 220, 90)
RED = (235, 70, 70)
ORANGE = (245, 168, 30)
WHITE = (240, 238, 255)
MUTED = (175, 168, 220)


def font_dirs():
    """Folders searched for Inter, most specific first: unzipped next to the script,
    INTER_FONT_DIR, then the fonts installed on Windows, Linux or macOS."""
    home = Path.home()
    candidates = [
        ARTWORK_DIR / "fonts",
        os.environ.get("INTER_FONT_DIR"),
        Path(os.environ.get("LOCALAPPDATA", home)) / "Microsoft" / "Windows" / "Fonts",
        Path(os.environ.get("WINDIR", "C:/Windows")) / "Fonts",
        home / ".local" / "share" / "fonts",
        "/usr/share/fonts",
        "/usr/local/share/fonts",
        home / "Library" / "Fonts",
        "/Library/Fonts",
    ]
    return [Path(c) for c in candidates if c and Path(c).is_dir()]


def find_font(stem):
    """Path of e.g. "Inter-Bold" as .otf or .ttf, searched recursively."""
    if stem in _font_paths:
        return _font_paths[stem]
    wanted = {(stem + ext).lower() for ext in FONT_EXTENSIONS}
    for folder in font_dirs():
        for path in folder.rglob("*"):
            if path.name.lower() in wanted:
                _font_paths[stem] = path
                return path
    sys.exit(f"Font {stem} not found. Install Inter (https://rsms.me/inter/) "
             f"or unzip it into {ARTWORK_DIR / 'fonts'}")


def font(stem, size):
    return ImageFont.truetype(str(find_font(stem)), size * SCALE)


def s(v):
    return int(v * SCALE)


def background():
    img = Image.new("RGB", (W, H))
    px = img.load()
    for y in range(H):
        t = y / (H - 1)
        row = tuple(int(a + (b - a) * t) for a, b in zip(BG_TOP, BG_BOTTOM))
        for x in range(W):
            px[x, y] = row
    # soft nebula glow
    glow = Image.new("RGB", (W, H), (0, 0, 0))
    g = ImageDraw.Draw(glow)
    g.ellipse([s(430), s(-120), s(980), s(380)], fill=(90, 40, 140))
    g.ellipse([s(-200), s(260), s(360), s(640)], fill=(40, 30, 120))
    glow = glow.filter(ImageFilter.GaussianBlur(s(90)))
    img = Image.blend(img, Image.composite(glow, img, glow.convert("L")), 0.45)
    # stars
    rng = random.Random(42)
    d = ImageDraw.Draw(img)
    for _ in range(170):
        x, y = rng.randrange(W), rng.randrange(H)
        r = rng.choice([1, 1, 1, 2, 2, 3])
        c = rng.randint(140, 255)
        d.ellipse([x - r, y - r, x + r, y + r], fill=(c, c, min(255, c + 20)))
    return img


def chart_points(x0, y0, x1, y1):
    """Hand-made price path: dip, recovery, strong close."""
    shape = [0.72, 0.62, 0.80, 0.60, 0.50, 0.60, 0.38, 0.46, 0.28, 0.12]
    n = len(shape) - 1
    return [(x0 + (x1 - x0) * i / n, y0 + (y1 - y0) * v) for i, v in enumerate(shape)]


def arrow_head(a, b, length, half_width):
    """Arrow head pointing along segment a->b, with its tip slightly past b.

    Returns (tip, base_center, wing_a, wing_b).
    """
    dx, dy = b[0] - a[0], b[1] - a[1]
    norm = (dx * dx + dy * dy) ** 0.5
    ux, uy = dx / norm, dy / norm          # direction of the segment
    nx, ny = -uy, ux                       # perpendicular
    tip = (b[0] + ux * length * 0.35, b[1] + uy * length * 0.35)
    base = (tip[0] - ux * length, tip[1] - uy * length)
    wing_a = (base[0] + nx * half_width, base[1] + ny * half_width)
    wing_b = (base[0] - nx * half_width, base[1] - ny * half_width)
    return tip, base, wing_a, wing_b


def draw_tile(img):
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = s(48), s(75), s(348), s(375)
    # shadow
    sh = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(sh).rounded_rectangle([x0 + s(6), y0 + s(10), x1 + s(6), y1 + s(10)], s(34), fill=(0, 0, 0, 150))
    sh = sh.filter(ImageFilter.GaussianBlur(s(14)))
    img.paste(sh, (0, 0), sh)
    d.rounded_rectangle([x0, y0, x1, y1], s(34), fill=TILE_FILL, outline=TILE_EDGE, width=s(4))

    # grid
    cx0, cy0, cx1, cy1 = x0 + s(36), y0 + s(70), x1 - s(36), y1 - s(40)
    for i in range(5):
        y = cy0 + (cy1 - cy0) * i / 4
        d.line([cx0, y, cx1, y], fill=GRID, width=s(1))
    for i in range(5):
        x = cx0 + (cx1 - cx0) * i / 4
        d.line([x, cy0, x, cy1], fill=GRID, width=s(1))

    # baseline (average cost), like the in-game yellow line
    base_y = cy0 + (cy1 - cy0) * 0.56
    d.line([cx0, base_y, cx1, base_y], fill=(230, 210, 60), width=s(3))

    # price path, red when falling and green when rising
    pts = chart_points(cx0, cy0, cx1, cy1)
    tip, base, wing_a, wing_b = arrow_head(pts[-2], pts[-1], length=s(34), half_width=s(19))
    path = pts[:-1] + [base]  # last segment stops where the head begins
    for a, b in zip(path, path[1:]):
        d.line([a, b], fill=GREEN if b[1] < a[1] else RED, width=s(9), joint="curve")
    for p in pts[:-1]:
        d.ellipse([p[0] - s(4), p[1] - s(4), p[0] + s(4), p[1] + s(4)], fill=WHITE)
    d.polygon([tip, wing_a, wing_b], fill=GREEN)

    # pause badge
    bx, by, r = x1 - s(10), y0 + s(10), s(40)
    d.ellipse([bx - r, by - r, bx + r, by + r], fill=ORANGE, outline=(255, 230, 170), width=s(4))
    bw, bh, gap = s(10), s(36), s(9)
    d.rounded_rectangle([bx - gap - bw, by - bh / 2, bx - gap, by + bh / 2], s(3), fill=(40, 24, 10))
    d.rounded_rectangle([bx + gap, by - bh / 2, bx + gap + bw, by + bh / 2], s(3), fill=(40, 24, 10))

    # day label
    d.text((x0 + s(36), y0 + s(26)), "DAY CLOSE", font=font("Inter-Bold", 22), fill=MUTED)


def draw_text(img):
    d = ImageDraw.Draw(img)
    x = s(392)
    d.text((x, s(92)), "STOCK DAY", font=font("Inter-ExtraBold", 54), fill=WHITE)
    d.text((x, s(150)), "SUMMARY", font=font("Inter-ExtraBold", 54), fill=WHITE)

    # mock of the in-game summary line
    card = [x, s(238), s(760), s(318)]
    d.rounded_rectangle(card, s(14), fill=(24, 18, 62), outline=(150, 70, 220), width=s(3))
    f = font("Inter-SemiBold", 21)
    d.text((x + s(20), s(252)), "Stock market day:", font=f, fill=WHITE)
    d.text((x + s(20), s(282)), "+$3.12K", font=font("Inter-Bold", 21), fill=GREEN)
    d.text((x + s(112), s(282)), "(+4.64%)", font=f, fill=WHITE)

    d.text((x, s(342)), "Daily P/L  ·  auto-pause  ·  per asset", font=font("Inter-Medium", 20), fill=MUTED)
    d.text((x, s(392)), "for Stardeus v0.15 & v0.14", font=font("Inter-Medium", 16), fill=(130, 122, 190))


def main(out_path):
    img = background()
    draw_tile(img)
    draw_text(img)
    img = img.resize((800, 450), Image.LANCZOS)
    img.save(out_path, "JPEG", quality=92, optimize=True)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else DEFAULT_OUT)
