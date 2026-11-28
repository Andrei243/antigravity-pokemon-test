# Meduzel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/meduzel.py -- <the export's Meduzel.glb> <out.glb>
#
# Carpathia's moon jellyfish (No. 10158, Water/Poison), from its picture
# (docs/art/reference/gemini/carpathia/10158-Meduzel.jpg) and its brief (CP-dex-3b-river-and-caves-more.md, 158): a
# round milky-white bell, pale lavender and blue in its shade, marked on its shoulders with four pale rings; a smaller
# soft dome inside its front, where two small dark eyes sit; the bell's margin hanging in soft scallops with a short
# fringe of lavender-tipped tentacles all round; four or five flat ribbon arms hanging below and drifting, one curling
# out to the side. It hovers.
# Written to be grown from: meduzaur.py imports it (sibling("meduzel")) for the bell, the inner dome, the rings, the
# fringe, the ribbons and the eyes, with numbers of its own.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, outward, srgb, inside, smoothstep, unit, eye_look, args


def board(k):
    """The game's board check on the built body: the span it shows at the board's turn against its square."""
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    sq = 1.3 * max(h, 0.85 * max(w, l))
    print("KIT: board: shows %.3f in a square of %.3f (%.2f); four corners %.3f" % (np.ptp(proj), sq, np.ptp(proj) / sq,
                                                                                  0.853 * w + 0.522 * l))


def on_ell(c, r, d):
    d = np.array(d, float)
    return np.array(c, float) + d / np.linalg.norm(d / np.array(r, float))


def around(a, r=1.0, z=0.0):
    t = math.radians(a)
    return np.array([r * math.sin(t), -r * math.cos(t), z])


class Jelly:
    """Meduzel's numbers; meduzaur.py copies and changes them."""
    BELL, SHADE, BLUE, INNER, RING, FRINGE, UNDER, RIBBON, RIBBONEDGE = (
        "#EEF0FA", "#D8D2F0", "#D6E6F4", "#DCD6F4", "#BDB6DA", "#C2A8E2", "#CCC2EA", "#E8EAF8", "#CDBFEA")
    INK = "#1C1A2A"
    C = np.array([0.0, 0.0, 0.452])            # the bell's centre
    R = np.array([0.2, 0.192, 0.176])
    FLAT = -0.6                                 # where the bell's underside is cut flat, in its own radii
    LOBES = 12                                  # scallops round its margin
    FRINGE_N, FRINGE_LEN, FRINGE_R = 26, 0.05, 0.0095
    IC = np.array([0.0, -0.148, 0.432])         # the inner dome, standing out of the bell's front
    IR = np.array([0.088, 0.062, 0.074])
    RINGS = [(40, 52), (-40, 52), (140, 42), (-140, 42)]       # (azimuth, elevation) of each ring
    RING_HALF, RING_IN, RING_OUT = 0.04, 0.58, 0.96
    EYE_X, EYE_Z, EYE = 0.03, 0.428, (0.019, 0.021)
    VOXEL = 0.0045
    RIB0, RIB1 = 0.03, 0.018
    LOW = 0.088                                  # the ribbons' ends, over the export's lowest point


def rim_z(J):
    return J.C[2] + J.R[2] * J.FLAT


def bell(k, J, extra=None):
    """The bell: a dome cut flat beneath, its margin hanging in soft scallops, its underside lavender."""
    def flat(V):
        V = V.copy()
        V[:, 2] = np.maximum(V[:, 2], J.FLAT)
        if extra is not None:
            V = extra(V)
        return V
    k.add("bell", ell(J.C, J.R, deform=flat), J.BELL, "body", surface="shell")
    rz = rim_z(J)
    for j in range(J.LOBES):
        a = 360 * (j + 0.5) / J.LOBES
        p = around(a, J.R[0] * 0.84) + np.array([0, 0, rz + 0.008])
        k.add(f"lobe{j}", ell(p, (J.R[0] * 0.26, J.R[0] * 0.2, 0.024 * J.R[0] / 0.2), rot=(0, 0, -a)), J.BELL, "body",
              surface="shell")
    # the inner dome standing out of the front, where the face is
    k.add("inner", ell(J.IC, J.IR), J.INNER, "body", surface="shell")


def tints(k, J):
    """Lavender and pale blue in the bell's shade, the underside lavender, the margin's lobes lavender beneath."""
    c, r = J.C, J.R
    k.paint(lambda P: smoothstep(c[2] + r[2] * 0.2, c[2] - r[2] * 0.4, P[:, 2]) * 0.85
            * smoothstep(-0.2, 0.6, np.sin(np.arctan2(P[:, 0], -P[:, 1]) * 2 + 0.6)), J.SHADE, on=["bell", "lobe"])
    k.paint(lambda P: smoothstep(c[2] + r[2] * 0.25, c[2] - r[2] * 0.35, P[:, 2]) * 0.8
            * smoothstep(-0.2, 0.6, -np.sin(np.arctan2(P[:, 0], -P[:, 1]) * 2 + 0.6)), J.BLUE, on=["bell", "lobe"])
    rz = rim_z(J)
    k.paint(lambda P: inside(P[:, 2] - (rz + 0.004), 0.004), J.UNDER, on=["bell", "lobe"])


def fringe(k, J):
    """A short fringe of thick lobes hanging all round the margin, their tips lavender."""
    rz = rim_z(J)
    for j in range(J.FRINGE_N):
        a = 360 * j / J.FRINGE_N + 4
        p = around(a, J.R[0] * 0.95) + np.array([0, 0, rz + 0.012])
        d = unit(*(around(a, 0.25) + np.array([0, 0, -1.0])))
        k.add(f"fringe{j}", tube([p, p + d * J.FRINGE_LEN * 0.6, p + d * J.FRINGE_LEN],
                                 [J.FRINGE_R, J.FRINGE_R * 0.95, J.FRINGE_R * 0.8]), J.BELL, "body", surface="shell")
    k.paint(lambda P: inside(P[:, 2] - (rz - J.FRINGE_LEN * 0.45), 0.006), J.FRINGE, on=["fringe"])


def ring_look(J, ticks=0):
    """A ring of the bell's marks: an annulus of the ring's colour on the bell's, ticks round it for the grown one."""
    B, Rg = srgb(J.BELL), srgb(J.RING)
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(B, (len(u), 1))
        on = (r > J.RING_IN) & (r < J.RING_OUT - (0.16 if ticks else 0))
        if ticks:
            th = np.arctan2(v, u)
            on |= (r > J.RING_OUT - 0.2) & (np.cos(th * ticks) > 0.55)
        c[on] = Rg
        return c
    return look


def rings(k, J, ticks=0):
    for az, el in J.RINGS:
        e = math.radians(el)
        d = around(az) * math.cos(e) + np.array([0, 0, math.sin(e)])
        p = on_ell(J.C, J.R, d)
        nrm = unit(*((p - J.C) / (J.R ** 2)))
        k.decal(p, nrm, (J.RING_HALF, J.RING_HALF), ring_look(J, ticks), eps=0.0025, rings=18, segs=64,
                up=tuple(around(az + 90)))


def ribbon(k, J, name, bone, pts, r0, r1, colour, flat=0.42):
    """A flat drifting ribbon arm from its root through pts, tapering, its face turned to the front."""
    pts = np.array(pts, float)
    radii = np.linspace(r0, r1, len(pts))
    # flattened through its depth (y), so its broad face shows to the cameras in front and behind
    V, F = blade(pts, radii, (0, 1, 0), flat)
    k.add(name, outward((V, F)), colour, bone, fuse=False, surface="shell")


def ribbons(k, J, bones, curl_bone=None):
    """The hanging ribbon arms, one from each tentacle joint, drifting a little; one curling out to +X."""
    arm = k.arm.data.bones                          # each ribbon starts on its own joint, inside the bell
    rz = rim_z(J)
    for j, b in enumerate(bones):
        h = np.array(arm[b].head_local)
        out = unit(h[0] + 1e-4, h[1] + 1e-4, 0)
        sway = np.array([out[1], -out[0], 0]) * (0.02 if j % 2 else -0.02)
        if b == curl_bone:
            pts = [h, h + np.array([0.01, -0.005, -(h[2] - rz) - 0.02]), h * np.array([1, 1, 0]) + np.array([0.05, -0.01, rz - 0.1]),
                   np.array([0.14, -0.02, rz - 0.17]), np.array([0.19, -0.015, rz - 0.15]), np.array([0.2, -0.012, rz - 0.12])]
        else:
            low = J.LOW + 0.012 * (j % 3)
            length = rz - low
            pts = [h, h * np.array([0.9, 0.9, 1]) + np.array([0, 0, -(h[2] - rz) - 0.03]),
                   h * np.array([1.1, 1.1, 0]) + out * 0.015 + sway + np.array([0, 0, rz - length * 0.4]),
                   h * np.array([0.9, 0.9, 0]) + out * 0.03 - sway + np.array([0, 0, rz - length * 0.72]),
                   h * np.array([1.0, 1.0, 0]) + out * 0.05 + sway + np.array([0, 0, low])]
        ribbon(k, J, f"ribbon{j}", b, pts, J.RIB0, J.RIB1, J.RIBBON if j % 2 else J.RIBBONEDGE)


def eyes(k, J):
    INKC, WHITEC, L = srgb(J.INK), np.array([1.0, 1.0, 1.0]), srgb(J.INNER)
    def dot(u, v):
        c = np.tile(INKC, (len(u), 1))
        c[(u + 0.3) ** 2 + (v - 0.34) ** 2 < 0.3 ** 2] = WHITEC
        return c
    def lay(state):
        d = unit(J.EYE_X / J.IR[0] * 0.9, -1.0, (J.EYE_Z - J.IC[2]) / J.IR[2] * 0.9)
        p = on_ell(J.IC, J.IR, d)
        k.decal(p, d, J.EYE, eye_look(state, dot, L, INKC, line=0.2), eps=0.0018, rings=20, segs=56, mirror=True)
    return lay


def meduzel(src, out_path, J=Jelly):
    k = Kit(src, coat="shell")
    bell(k, J)
    fringe(k, J)
    tints(k, J)
    rings(k, J)
    ribbons(k, J, [f"tentacle{i}" for i in range(5)], curl_bone="tentacle1")
    k.build(voxel=J.VOXEL, band=0.006, wband=0.04)
    board(k)
    k.eye_states(eyes(k, J))
    k.export(out_path)


if __name__ == "__main__":
    meduzel(*args())
