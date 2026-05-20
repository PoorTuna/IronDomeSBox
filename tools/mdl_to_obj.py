"""
Source Engine MDL/VVD/VTX -> OBJ converter (v48, single LOD, no fixups).

Reads:
  irondomegmod/models/props/iron_dome.{mdl,vvd,dx90.vtx}

Outputs:
  irondome-sbox/Assets/models/iron_dome.obj
  irondome-sbox/Assets/models/iron_dome.mtl

All offsets in VTX and MDL structs are RELATIVE to the start of the struct
that contains them (Valve convention). This script handles that correctly.
"""

import struct
from pathlib import Path

SCRIPT_DIR = Path(__file__).parent
GMOD_MDL   = SCRIPT_DIR.parent.parent / "irondomegmod" / "models" / "props"
OUT_DIR    = SCRIPT_DIR.parent / "Assets" / "models"
OUT_DIR.mkdir(parents=True, exist_ok=True)

MDL_PATH = GMOD_MDL / "iron_dome.mdl"
VVD_PATH = GMOD_MDL / "iron_dome.vvd"
VTX_PATH = GMOD_MDL / "iron_dome.dx90.vtx"

# Struct sizes (v48 / v7)
MDL_BODYPART_SZ = 16   # sznameindex(4) + nummodels(4) + base(4) + modelindex(4)
MDL_MODEL_SZ    = 148  # name[64] + type(4) + boundrad(4) + nummeshes(4) + meshidx(4) + numverts(4) + vertidx(4) + tangidx(4) + ... + unused[8](32)
MDL_MESH_SZ     = 88   # material(4) + modelindex(4) + numverts(4) + vertoff(4) + ... + unused[8](32)
VTX_BODYPART_SZ = 8    # numModels(4) + modelOffset(4)
VTX_MODEL_SZ    = 8    # numLODs(4) + lodOffset(4)
VTX_LOD_SZ      = 12   # numMeshes(4) + meshOffset(4) + switchPoint(f4)
VTX_MESH_SZ     = 9    # numStripGroups(4) + sgOffset(4) + flags(1)
VTX_SG_SZ       = 25   # numVerts(4)+vertOff(4)+numIdx(4)+idxOff(4)+numStrips(4)+stripOff(4)+flags(1)
VTX_STRIP_SZ    = 27   # numIdx(4)+idxOff(4)+numVerts(4)+vertOff(4)+numBones(2)+flags(1)+numBSC(4)+bscOff(4)
VTX_VERT_SZ     = 9    # boneWeightIdx[3](3) + numBones(1) + origMeshVertID(2) + boneID[3](3)


# ---------------------------------------------------------------------------
# VVD: read vertex positions, normals, UVs
# ---------------------------------------------------------------------------

def read_vvd(path):
    data = open(path, "rb").read()
    _id, _ver, _cksum, num_lods = struct.unpack_from("<4i", data, 0)
    lod_verts = struct.unpack_from("<8i", data, 16)
    num_fixups, _fs, vert_start, _ts = struct.unpack_from("<4i", data, 48)

    n = lod_verts[0]
    print(f"VVD: {n} vertices, {num_fixups} fixups, vert_start={vert_start}")
    assert num_fixups == 0, "VVD fixups not supported"

    # mstudiovertex_t = 48 bytes: boneweight(16) + pos(12) + normal(12) + uv(8)
    verts = []
    for i in range(n):
        off = vert_start + i * 48
        px, py, pz = struct.unpack_from("<3f", data, off + 16)
        nx, ny, nz = struct.unpack_from("<3f", data, off + 28)
        u,  v      = struct.unpack_from("<2f", data, off + 40)
        verts.append(((px, py, pz), (nx, ny, nz), (u, 1.0 - v)))
    return verts


# ---------------------------------------------------------------------------
# MDL: collect (global_vvd_vertex_base) per mesh in body-part/model/mesh order
# ---------------------------------------------------------------------------

def read_mdl_mesh_bases(path):
    """Return list of VVD vertex base indices, one per mesh."""
    data = open(path, "rb").read()
    num_bp  = struct.unpack_from("<i", data, 232)[0]
    bp_base = struct.unpack_from("<i", data, 236)[0]

    bases = []
    for bp in range(num_bp):
        bp_abs = bp_base + bp * MDL_BODYPART_SZ
        _sni, num_models, _base, model_rel = struct.unpack_from("<4i", data, bp_abs)
        for mo in range(num_models):
            mo_abs = bp_abs + model_rel + mo * MDL_MODEL_SZ
            # model fields: name[64] then: type(4) rad(4) nummesh(4) meshidx(4) nv(4) vertidx(4)
            num_meshes, mesh_rel, _nv, vert_idx = struct.unpack_from("<iiii", data, mo_abs + 72)
            for ms in range(num_meshes):
                ms_abs = mo_abs + mesh_rel + ms * MDL_MESH_SZ
                # mesh fields: material(4) modelidx(4) numverts(4) vertoff(4)
                _mat, _mi, _nv2, vert_off = struct.unpack_from("<4i", data, ms_abs)
                # vert_idx is byte offset into VVD vertex buffer; /48 converts to vertex index
                global_base = vert_idx // 48 + vert_off
                bases.append(global_base)

    print(f"MDL: {num_bp} body parts, {len(bases)} meshes, bases={bases}")
    return bases


# ---------------------------------------------------------------------------
# VTX: extract triangles using VVD global vertex indices
# ---------------------------------------------------------------------------

def read_vtx_triangles(path, mesh_bases):
    data = open(path, "rb").read()

    # FileHeader_t (36 bytes)
    _v, _vcs, _mbps, _mbt, _mbpv, _cksum, _nlods, _mrl, num_bp, bp_off = \
        struct.unpack_from("<i i H H i i i i i i", data, 0)
    print(f"VTX: {num_bp} body parts, bodyPartOffset={bp_off}")

    triangles = []
    mesh_idx = 0

    for bp in range(num_bp):
        bp_abs = bp_off + bp * VTX_BODYPART_SZ
        num_models, model_rel = struct.unpack_from("<ii", data, bp_abs)

        for mo in range(num_models):
            mo_abs = bp_abs + model_rel + mo * VTX_MODEL_SZ
            num_lods, lod_rel = struct.unpack_from("<ii", data, mo_abs)

            # LOD 0 only
            lod_abs = mo_abs + lod_rel
            num_meshes, mesh_rel, _sw = struct.unpack_from("<iif", data, lod_abs)

            for ms in range(num_meshes):
                ms_abs = lod_abs + mesh_rel + ms * VTX_MESH_SZ
                num_sg, sg_rel, _flags = struct.unpack_from("<iib", data, ms_abs)

                vvd_base = mesh_bases[mesh_idx] if mesh_idx < len(mesh_bases) else 0
                mesh_idx += 1

                for sg in range(num_sg):
                    sg_abs = ms_abs + sg_rel + sg * VTX_SG_SZ
                    (num_verts, vert_rel, num_idx, idx_rel,
                     num_strips, strip_rel, _sg_flags) = struct.unpack_from("<iiiiiib", data, sg_abs)

                    # Read VTX vertices: origMeshVertID at bytes [4:6] of each 9-byte entry
                    vtx_to_vvd = []
                    for vi in range(num_verts):
                        v_abs = sg_abs + vert_rel + vi * VTX_VERT_SZ
                        orig = struct.unpack_from("<H", data, v_abs + 4)[0]
                        vtx_to_vvd.append(vvd_base + orig)

                    # Read index buffer for this strip group
                    idx_buf = list(struct.unpack_from(f"<{num_idx}H", data, sg_abs + idx_rel))

                    for st in range(num_strips):
                        st_abs = sg_abs + strip_rel + st * VTX_STRIP_SZ
                        # StripHeader_t: numIdx(4) idxOff(4) numVerts(4) vertOff(4) numBones(2) flags(1) numBSC(4) bscOff(4)
                        si_count, si_off, sv_count, sv_off, _nb, st_flags, _nbsc, _bsc = \
                            struct.unpack_from("<iiiiHBii", data, st_abs)

                        strip_idx = idx_buf[si_off: si_off + si_count]

                        if st_flags & 0x01:  # IS_TRILIST
                            for t in range(0, len(strip_idx) - 2, 3):
                                a = vtx_to_vvd[strip_idx[t]]
                                b = vtx_to_vvd[strip_idx[t+1]]
                                c = vtx_to_vvd[strip_idx[t+2]]
                                if a != b and b != c and a != c:
                                    triangles.append((a, b, c))
                        else:  # triangle strip
                            for t in range(len(strip_idx) - 2):
                                a = vtx_to_vvd[strip_idx[t]]
                                b = vtx_to_vvd[strip_idx[t+1]]
                                c = vtx_to_vvd[strip_idx[t+2]]
                                if a != b and b != c and a != c:
                                    if t % 2 == 0:
                                        triangles.append((a, b, c))
                                    else:
                                        triangles.append((a, c, b))

    print(f"VTX: {len(triangles)} triangles (mesh_idx={mesh_idx})")
    return triangles


# ---------------------------------------------------------------------------
# OBJ writer — swaps Y/Z (Source is Z-up, OBJ is Y-up)
# ---------------------------------------------------------------------------

def write_obj(verts, triangles, obj_path, mtl_path):
    with open(obj_path, "w") as f:
        f.write("# Iron Dome - exported by mdl_to_obj.py\n")
        f.write(f"mtllib {mtl_path.name}\n\n")
        f.write("g iron_dome\nusemtl iron_dome\n\n")

        for (px, py, pz), _, _ in verts:
            f.write(f"v {px:.6f} {pz:.6f} {py:.6f}\n")   # Source Z-up -> OBJ Y-up
        f.write("\n")
        for _, (nx, ny, nz), _ in verts:
            f.write(f"vn {nx:.6f} {nz:.6f} {ny:.6f}\n")
        f.write("\n")
        for _, _, (u, v) in verts:
            f.write(f"vt {u:.6f} {v:.6f}\n")
        f.write("\n")

        for a, b, c in triangles:
            a1, b1, c1 = a+1, b+1, c+1
            f.write(f"f {a1}/{a1}/{a1} {b1}/{b1}/{b1} {c1}/{c1}/{c1}\n")

    print(f"OBJ: {len(verts)} verts, {len(triangles)} tris -> {obj_path}")

    with open(mtl_path, "w") as f:
        f.write("newmtl iron_dome\n")
        f.write("map_Kd iron_dome_dif.png\n")
        f.write("map_bump iron_dome_nrm.png\n")
        f.write("Ns 50\nKa 0.1 0.1 0.1\nKd 0.8 0.8 0.8\nKs 0.3 0.3 0.3\n")


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

print("=== VVD ===")
verts = read_vvd(VVD_PATH)

print("\n=== MDL ===")
mesh_bases = read_mdl_mesh_bases(MDL_PATH)

print("\n=== VTX ===")
triangles = read_vtx_triangles(VTX_PATH, mesh_bases)

print("\n=== OBJ ===")
write_obj(verts, triangles, OUT_DIR / "iron_dome.obj", OUT_DIR / "iron_dome.mtl")
print("\nDone. Import iron_dome.obj into s&box ModelDoc -> iron_dome.vmdl")
