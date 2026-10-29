#!/usr/bin/env python3
"""Static checks for the Stock Day Summary mod that otherwise only fail in game.

Checks:
  * ModInfo.json is strict JSON with the fields the game requires.
  * Every translation CSV has 4 columns, unique keys, the same keys as
    English.csv and the same {n} placeholders per key.
  * Every translation key used in Code/ exists in English.csv
    (including the "<key>.tip" tooltips added by the settings toggles).
  * Each game version folder (Code/V0xx) defines the classes the shared
    code calls (GameBootstrap, GameCompat, SummaryEvent).
  * ModCover.jpg exists and is 800x450, as the Workshop upload requires.
  * Every *.csproj is well-formed XML (e.g. no "--" inside comments).

Usage: python3 .github/scripts/check_mod.py [mod_root]
Exit code: number of failed checks (0 = all good).
"""
import csv
import json
import re
import struct
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

KEY_PREFIX = "stockdaysummary."
REQUIRED_MODINFO_FIELDS = ("Id", "Name", "Description", "ModVersion", "GameVersion")
COVER_SIZE = (800, 450)
PLACEHOLDER = re.compile(r"\{(\d+)\}")
# "key".T(...)
LITERAL_T = re.compile(r'"(stockdaysummary\.[a-z0-9.]+)"\.T\(')
# const string Name = "key";  ...  Name.T(...)
CONST_KEY = re.compile(r'const string (\w+) = "(stockdaysummary\.[a-z0-9.]+)";')
# AddToggle("key", ...) (v0.14) or Toggle(idx++, "key", ...) (v0.15) translate "key" and "key.tip"
TOGGLE_KEY = re.compile(r'\b(?:Add)?Toggle\(\s*(?:\w+\+\+,\s*)?"(stockdaysummary\.[a-z0-9.]+)"')
# Classes each Code/V0xx folder must define: the shared code calls them
VERSION_CLASSES = ("GameBootstrap", "GameCompat", "SummaryEvent")

errors = []


def fail(message):
    """Records a failed check."""
    errors.append(message)
    print(f"FAIL {message}")


def ok(message):
    print(f"OK   {message}")


def check_modinfo(root):
    """ModInfo.json must be strict JSON and contain the required fields."""
    path = root / "ModInfo.json"
    try:
        info = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError) as e:
        fail(f"ModInfo.json is not valid JSON: {e}")
        return
    missing = [f for f in REQUIRED_MODINFO_FIELDS if not str(info.get(f, "")).strip()]
    if missing:
        fail(f"ModInfo.json misses required fields: {', '.join(missing)}")
        return
    ok(f"ModInfo.json: {info['Id']} {info['ModVersion']} for game {info['GameVersion']}")


def read_translations(path):
    """Returns {key: text} of a translation CSV; reports malformed rows."""
    rows = list(csv.reader(path.read_text(encoding="utf-8").splitlines()))
    result = {}
    for line_no, row in enumerate(rows[1:], start=2):  # first line is the header
        if not row:
            continue
        if len(row) != 4:
            fail(f"{path.name}:{line_no}: expected 4 columns, got {len(row)}")
            continue
        key, text = row[0], row[1]
        if key in result:
            fail(f"{path.name}:{line_no}: duplicate key {key}")
        result[key] = text
    return result


def placeholders(text):
    return sorted(set(PLACEHOLDER.findall(text)))


def check_translations(root):
    """All languages must match English.csv in keys and placeholders."""
    folder = root / "Translations"
    english_path = folder / "English.csv"
    if not english_path.exists():
        fail("Translations/English.csv is missing")
        return {}
    english = read_translations(english_path)

    for path in sorted(folder.glob("*.csv")):
        if path == english_path:
            continue
        check_language(path, english)
    ok(f"English.csv: {len(english)} keys")
    return english


def check_language(path, english):
    """Compares one language file with English."""
    other = read_translations(path)
    missing = sorted(set(english) - set(other))
    extra = sorted(set(other) - set(english))
    if missing:
        fail(f"{path.name}: missing keys {missing}")
    if extra:
        fail(f"{path.name}: keys not in English.csv {extra}")
    for key in sorted(set(english) & set(other)):
        if placeholders(english[key]) != placeholders(other[key]):
            fail(f"{path.name}: {key} placeholders {placeholders(other[key])}, "
                 f"English has {placeholders(english[key])}")
    if not (missing or extra):
        ok(f"{path.name}: same keys as English")


def check_code_keys(root, english):
    """Every key the code asks for must have an English text."""
    used = set()
    for source in sorted((root / "Code").rglob("*.cs")):
        used.update(translation_keys(source.read_text(encoding="utf-8")))
    used = {k for k in used if k.startswith(KEY_PREFIX)}
    missing = sorted(used - set(english))
    if missing:
        fail(f"keys used in Code/ but missing in English.csv: {missing}")
        return
    ok(f"all {len(used)} keys used in Code/ are translated")


def translation_keys(text):
    """Keys a C# source passes to the translator (.T), not settings keys."""
    keys = set(LITERAL_T.findall(text))
    for name, key in CONST_KEY.findall(text):
        if re.search(rf"\b{name}\.T\(", text):
            keys.add(key)
    for key in TOGGLE_KEY.findall(text):
        keys.update((key, key + ".tip"))
    return keys


def jpeg_size(path):
    """Reads (width, height) from the first SOF marker of a JPEG file."""
    data = path.read_bytes()
    if data[:2] != b"\xff\xd8":
        raise ValueError("not a JPEG file")
    i = 2
    while i + 9 < len(data):
        if data[i] != 0xFF:
            raise ValueError("broken JPEG marker")
        marker = data[i + 1]
        length = struct.unpack(">H", data[i + 2:i + 4])[0]
        if 0xC0 <= marker <= 0xCF and marker not in (0xC4, 0xC8, 0xCC):
            height, width = struct.unpack(">HH", data[i + 5:i + 9])
            return width, height
        i += 2 + length
    raise ValueError("no size marker found")


def check_cover(root):
    """The Workshop upload asks for ModCover.jpg of 800x450."""
    path = root / "ModCover.jpg"
    if not path.exists():
        fail("ModCover.jpg is missing")
        return
    try:
        size = jpeg_size(path)
    except ValueError as e:
        fail(f"ModCover.jpg: {e}")
        return
    if size != COVER_SIZE:
        fail(f"ModCover.jpg is {size[0]}x{size[1]}, expected {COVER_SIZE[0]}x{COVER_SIZE[1]}")
        return
    ok("ModCover.jpg is 800x450")


def check_version_folders(root):
    """Every Code/V0xx folder must provide the classes the shared code calls."""
    folders = sorted(p for p in (root / "Code").glob("V0*") if p.is_dir())
    if not folders:
        fail("no Code/V0xx game version folders found")
        return
    for folder in folders:
        text = "\n".join(p.read_text(encoding="utf-8") for p in folder.rglob("*.cs"))
        missing = [c for c in VERSION_CLASSES if not re.search(rf"\bclass {c}\b", text)]
        if missing:
            fail(f"{folder.relative_to(root)} misses {', '.join(missing)}")
            continue
        ok(f"{folder.relative_to(root)} defines {', '.join(VERSION_CLASSES)}")


def check_projects(root):
    """MSBuild refuses malformed project files before compiling anything."""
    projects = sorted(p for p in root.rglob("*.csproj") if "obj" not in p.parts)
    for path in projects:
        try:
            ET.parse(path)
        except ET.ParseError as e:
            fail(f"{path.relative_to(root)} is not valid XML: {e}")
            return
    ok(f"{len(projects)} project file(s) are valid XML")


def main():
    root = Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()
    check_modinfo(root)
    english = check_translations(root)
    check_code_keys(root, english)
    check_version_folders(root)
    check_cover(root)
    check_projects(root)
    print(f"\n{len(errors)} problem(s)" if errors else "\nALL CHECKS PASSED")
    return len(errors)


if __name__ == "__main__":
    sys.exit(main())
