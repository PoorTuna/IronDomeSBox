"""
VTF v7.x → PNG extractor (DXT1/DXT3/DXT5 and RGBA8888).
Wraps the highest-mip image data as a DDS then opens with Pillow.

Usage:
    python extract_textures.py
Outputs PNGs to ../Assets/materials/
"""

import struct
import io
import os
from pathlib import Path
from PIL import Image

SCRIPT_DIR  = Path(__file__).parent
GMOD_MAT    = SCRIPT_DIR.parent.parent / "irondomegmod" / "materials" / "props"
OUT_DIR     = SCRIPT_DIR.parent / "Assets" / "materials"
OUT_DIR.mkdir(parents=True, exist_ok=True)

# Valve VTF image format enum (from VTFLib / Source SDK)
# RGBA8888=0 ABGR8888=1 RGB888=2 BGR888=3 RGB565=4 I8=5 IA88=6 P8=7 A8=8
# RGB888_BLUESCREEN=9 BGR888_BLUESCREEN=10 ARGB8888=11 BGRA8888=12
# DXT1=13 DXT3=14 DXT5=15
VTF_FMT = {
    0:  ("RGBA", 4),
    2:  ("RGB",  3),
    3:  ("BGR",  3),
    12: ("BGRA", 4),
    13: ("DXT1", 8),   # DXT1 — 8 bytes per 4x4 block
    14: ("DXT3", 16),  # DXT3 — 16 bytes per 4x4 block
    15: ("DXT5", 16),  # DXT5 — 16 bytes per 4x4 block
}


def vtf_block_size(w, h, bpb):
    """Bytes for a DXT image at given dimensions."""
    bx = max(1, (w + 3) // 4)
    by = max(1, (h + 3) // 4)
    return bx * by * bpb


def build_dds(width, height, fourcc, img_data):
    """Wrap raw DXT data in a minimal DDS container for Pillow."""
    bpb = 8 if fourcc == "DXT1" else 16
    linear = vtf_block_size(width, height, bpb)

    buf = io.BytesIO()
    buf.write(b"DDS ")

    # DDS_HEADER (124 bytes)
    flags = 0x1 | 0x2 | 0x4 | 0x1000 | 0x80000  # CAPS|HEIGHT|WIDTH|PIXELFORMAT|LINEARSIZE
    buf.write(struct.pack("<7I", 124, flags, height, width, linear, 0, 1))
    buf.write(b"\x00" * 44)  # dwReserved1[11]

    # DDS_PIXELFORMAT (32 bytes)
    buf.write(struct.pack("<2I", 32, 0x4))       # size, DDPF_FOURCC
    buf.write(fourcc.encode("ascii"))            # FourCC
    buf.write(b"\x00" * 20)                      # rest of pixelformat

    # dwCaps etc.
    buf.write(struct.pack("<5I", 0x1000, 0, 0, 0, 0))  # DDSCAPS_TEXTURE

    buf.write(img_data[:linear])
    buf.seek(0)
    return buf


def parse_vtf(path):
    with open(path, "rb") as f:
        data = f.read()

    magic   = data[:4]
    assert magic == b"VTF\x00", f"Not a VTF file: {path}"
    ver     = struct.unpack_from("<2I", data, 4)
    hdr_sz  = struct.unpack_from("<I",  data, 12)[0]
    width   = struct.unpack_from("<H",  data, 16)[0]
    height  = struct.unpack_from("<H",  data, 18)[0]
    hi_fmt  = struct.unpack_from("<I",  data, 52)[0]
    mips    = data[56]
    lo_fmt  = struct.unpack_from("<I",  data, 57)[0]
    lo_w    = data[61]
    lo_h    = data[62]

    print(f"  VTF v{ver[0]}.{ver[1]}  {width}×{height}  fmt={hi_fmt}  mips={mips}")

    # Remap format 13 → DXT5 (Valve enum quirk)
    if hi_fmt in VTF_FMT:
        fourcc, bpb = VTF_FMT[hi_fmt]
        if isinstance(bpb, int) and fourcc not in ("RGBA","RGB","BGR","BGRA"):
            pass  # DXT, bpb is bytes per block
    else:
        raise ValueError(f"Unsupported VTF format ID {hi_fmt}")

    # v7.3+ uses a resource table to locate image data
    if ver >= (7, 3):
        num_resources = struct.unpack_from("<I", data, 68)[0]
        res_start = 80
        img_offset = None
        for i in range(num_resources):
            rtype = struct.unpack_from("<I", data, res_start + i * 8)[0] & 0xFFFFFF
            rdata = struct.unpack_from("<I", data, res_start + i * 8 + 4)[0]
            if rtype == 0x30:   # high-res image resource
                img_offset = rdata
        if img_offset is None:
            raise ValueError("Could not find high-res image resource in VTF")
    else:
        # v7.0–7.2: thumbnail immediately after header, then mip chain
        lo_bytes = vtf_block_size(lo_w, lo_h, bpb) if lo_fmt == hi_fmt else (lo_w * lo_h * 4)
        img_offset = hdr_sz + lo_bytes

    # Image data in VTF is stored smallest mip first; we want mip 0 (largest)
    # Skip smaller mips: sum up bytes for mips 1..(mips-1)
    skip = 0
    for m in range(mips - 1, 0, -1):
        mw = max(1, width  >> m)
        mh = max(1, height >> m)
        skip += vtf_block_size(mw, mh, bpb)

    mip0_offset = img_offset + skip
    mip0_size   = vtf_block_size(width, height, bpb)
    img_data    = data[mip0_offset: mip0_offset + mip0_size]

    if fourcc in ("DXT1", "DXT3", "DXT5"):
        dds = build_dds(width, height, fourcc, img_data)
        img = Image.open(dds)
    elif fourcc == "RGBA":
        img = Image.frombytes("RGBA", (width, height), img_data)
    elif fourcc == "RGB":
        img = Image.frombytes("RGB", (width, height), img_data)
    elif fourcc == "BGR":
        r, g, b = Image.frombytes("RGB", (width, height), img_data).split()
        img = Image.merge("RGB", (b, g, r))
    else:
        raise ValueError(f"Cannot decode format {fourcc}")

    return img


FILES = {
    "iron_dome_dif.vtf": "iron_dome_dif.png",
    "iron_dome_nrm.vtf": "iron_dome_nrm.png",
}

for src_name, dst_name in FILES.items():
    src = GMOD_MAT / src_name
    dst = OUT_DIR / dst_name
    print(f"\n{src_name} -> {dst_name}")
    img = parse_vtf(src)
    img.save(dst)
    print(f"  saved {img.size} {img.mode} -> {dst}")

print("\nDone.")
