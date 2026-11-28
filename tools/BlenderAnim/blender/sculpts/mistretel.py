# Mistretel (No. 10087, Carpathia, Ground), sculpted from nothing on the skeleton and clips of the game's own export:
#   blender --background --factory-startup --python sculpts/mistretel.py -- <the export's Mistretel.glb> <out.glb>
# From the picture (docs/art/reference/gemini/carpathia/10087-Mistretel.jpg) and the brief (CP-dex-2b, 87): a chubby
# wild boar piglet, one big round brown barrel low over short legs; cream stripes running front to back along its back
# and down its flanks, broken and tapering; the head carried low, nose down, a short snout ending in a flat pinkish
# disc with two nostrils; tiny pale tusk buds at the snout's sides; a small dark eye with a glint; pointed upright ears
# with a darker lining; short thick legs, muddy dark hooves; a little curly tail.
#
# Written to be grown from (mistretor.py imports it with sibling("mistretel")): the snout, the ear, the leg and hoof,
# the stripes, the eye and the colours are the functions below; Mistretel's calls are under __main__.
#
# Reference ratios (picture): length snout to rump 1.19 of the height (built 1.2); the head 0.47 of the height tall
# (built 0.5 with the snout); legs showing under the belly 0.25 of the height (built 0.22); the eye 0.05 of the height
# (built 0.11: bold, for the game's size).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, slab, lathe, outward, srgb, inside, unit, eye_look, args, sibling)

lc = sibling("lechonk")            # the curly tail

COAT, STRIPE, DISC, LINING = "#A2805C", "#ECDDB4", "#C79A84", "#76563E"
HOOF, MUD, TUSK, NOSTRIL = "#3E3028", "#6E5640", "#F2EAD4", "#3A2420"
INK, GLINT = srgb("#140E0C"), np.array([1.0, 1.0, 1.0])


def on_ell(c, r, d):
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def snout(k, root, tip, radii, disc_r, coat, disc, nostril, bone="head"):
    """A short boar's snout from root to tip ending in a flat disc facing along it, two nostrils on the disc."""
    root, tip = np.array(root, float), np.array(tip, float)
    d = unit(*(tip - root))
    k.add("snout", tube([root, (root + tip) / 2, tip], radii), coat, bone)
    c = tip + d * (radii[-1] * 0.72 + disc_r[2])          # past the tube's rounded cap
    k.add("disc", lathe(c - d * disc_r[2], d, [(0, 0), (disc_r[0] * 0.95, 0), (disc_r[0], disc_r[2] * 0.6),
                                               (disc_r[0] * 0.9, disc_r[2] * 1.6), (0, disc_r[2] * 1.75)], seg=36),
          disc, bone, fuse=False, surface="skin")
    front = c - d * disc_r[2] + d * disc_r[2] * 1.75
    a = unit(1, 0, 0); b = np.cross(d, a); b = b / np.linalg.norm(b)
    for s in (1, -1):
        k.add(f"nostril{'R' if s > 0 else 'L'}",
              ell(front + a * s * disc_r[0] * 0.42 + d * 0.0005, (disc_r[0] * 0.2, disc_r[0] * 0.2, disc_r[0] * 0.28),
                  rot=(math.degrees(math.atan2(d[2], -d[1])), 0, 0), sub=3),
              nostril, bone, fuse=False, surface="skin")
    return front, d


def ear(k, root, d, n, length, half, outer, inner, thick, bone="ear{s}"):
    """A pointed upright ear: a triangular plate from root along d, its face to n, a darker lining on its face."""
    d = unit(*d); n = np.array(n, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    ez = np.cross(n, d)
    L, W = length, half
    out_ = [(0, -W), (L * 0.45, -W * 0.85), (L, 0), (L * 0.45, W * 0.85), (0, W)]
    k.add("ear", slab(out_, root, d, ez, thick), outer, bone, fuse=False, mirror=True, surface="fur")
    lin = [(0.12 * L + 0.72 * a, 0.62 * b) for a, b in out_]
    k.add("earin", slab(lin, np.array(root) + n * (thick * 0.5 + 0.0015), d, ez, thick * 0.4), inner, bone,
          fuse=False, mirror=True, surface="skin")


def leg(k, name, mass_c, mass_r, pts, radii, bone, coat, hoof=HOOF):
    """A short thick leg: a haunch fused into the flank, a stub of shin, a dark cloven hoof."""
    k.add(name + "mass", ell(mass_c, mass_r), coat, bone, mirror=True)
    k.add(name, tube(pts, radii), coat, bone, mirror=True)
    x, y = pts[-1][0], pts[-1][1]
    hr = radii[-1] * 1.08
    h = hr * 0.9
    k.add(name + "hoof", lathe((x, y - 0.12 * hr, 0.0), (0, 0, 1),
                               [(0, 0), (hr, 0), (hr * 1.03, h * 0.35), (hr * 0.88, h), (0, h)], seg=24),
          hoof, bone, fuse=False, mirror=True, surface="shell")
    # the cleft down the hoof's front
    k.add(name + "cleft", outward(tube([(x, y - hr * 1.0, hr * 0.32), (x, y - hr * 0.95, h * 0.85)],
                                       [hr * 0.12, hr * 0.12], seg=8, per=2)), "#1E1612", bone, fuse=False,
          mirror=True, surface="shell")


def stripes(k, BC, BR, colour, angles, width, y0, y1, zmin, wave=0.03, edge=0.025):
    """Cream stripes running front to back along a barrel (BC, BR): at angles round its long axis from the top,
    tapering at both ends, wavering a little."""
    BC, BR = np.array(BC, float), np.array(BR, float)
    def m(P):
        q = (P - BC) / BR
        phi = np.arctan2(q[:, 0], q[:, 2])
        t = np.clip((P[:, 1] - y0) / (y1 - y0), 0, 1)
        taper = np.sqrt(np.clip(np.sin(math.pi * t), 0, 1))
        out_ = np.zeros(len(P))
        for i, a0 in enumerate(angles):
            a = a0 + wave * np.sin(P[:, 1] * 34 + i * 1.7)
            out_ = np.maximum(out_, inside(np.abs(phi - a) - width * taper, edge))
        return out_ * (P[:, 1] > y0) * (P[:, 1] < y1) * (P[:, 2] > zmin)
    k.paint(m, colour)


def boar_eye(rim=0.84):
    """The open eye: black, a dark brown edge, a white glint up and out."""
    I_ = srgb("#3A281E")
    def look(u, v):
        c = np.tile(INK, (len(u), 1))
        c[np.hypot(u, v) > rim] = I_
        c[np.hypot((u - 0.18) / 0.28, (v - 0.3) / 0.28) < 1] = GLINT
        c[np.hypot(u + 0.3, v + 0.36) < 0.1] = GLINT
        return c
    return look


def eyes_pair(k, E, de, half, look_open, lid, line=0.16, outline=None, eps=0.002):
    m = np.array([-1, 1, 1.0])
    def lay(state):
        lk = eye_look(state, look_open, lid, INK, line=line)
        k.decal(E, de, half, lk, eps=eps, rings=20, segs=56, outline=outline)
        k.decal(E * m, de * m, half, (lambda u, v, g=lk: g(-u, v)), eps=eps, rings=20, segs=56,
                outline=(lambda th, o=outline: o(math.pi - th)) if outline else None)
    return lay


def mistretel(src, out):
    k = Kit(src, coat="fur")

    # --- the barrel: big and round, low over short legs -----------------------------------------------------------
    BC, BR = np.array([0, 0.03, 0.228]), np.array([0.146, 0.222, 0.152])
    k.add("barrel", ell(BC, BR), COAT, "body")
    k.add("rump", ell((0, 0.14, 0.225), (0.136, 0.12, 0.14)), COAT, "body")
    k.add("chest", ell((0, -0.1, 0.205), (0.132, 0.12, 0.13)), COAT, "body")

    # --- legs ------------------------------------------------------------------------------------------------------
    leg(k, "fore", (0.088, -0.12, 0.155), (0.064, 0.07, 0.085), [(0.09, -0.13, 0.15), (0.092, -0.138, 0.07),
                                                                  (0.093, -0.14, 0.032)], [0.05, 0.045, 0.041],
        "leg{s}", COAT)
    leg(k, "hind", (0.09, 0.15, 0.17), (0.07, 0.088, 0.095), [(0.092, 0.152, 0.15), (0.094, 0.158, 0.07),
                                                               (0.095, 0.16, 0.032)], [0.052, 0.046, 0.042],
        "hind{s}", COAT)
    # mud on the little hooves' tops
    k.paint(lambda P: inside(P[:, 2] - (0.05 + 0.006 * np.sin(P[:, 1] * 140 + P[:, 0] * 90)), 0.003)
            * (np.abs(P[:, 0]) > 0.05), MUD, on=["fore", "hind"])

    # --- the stripes along the back and flanks ------------------------------------------------------------------------
    stripes(k, BC, BR, STRIPE, (0.22, -0.22, 0.62, -0.62, 1.02, -1.02), 0.1, -0.1, 0.235, 0.15)

    # --- the head, low and nose down ---------------------------------------------------------------------------------
    H, HR = np.array([0, -0.215, 0.2]), np.array([0.106, 0.104, 0.1])
    k.add("head", ell(H, HR), COAT, "head")
    k.add("jowl", ell((0, -0.2, 0.152), (0.11, 0.085, 0.075)), COAT, "head")
    k.add("brow", ell((0, -0.25, 0.245), (0.075, 0.06, 0.05)), COAT, "head")
    snout(k, (0, -0.27, 0.172), (0, -0.318, 0.128), [0.066, 0.06, 0.052], (0.052, 0, 0.014), COAT, DISC, NOSTRIL)
    # tiny pale tusk buds at the snout's sides
    for s in (1, -1):
        k.add(f"tusk{'R' if s > 0 else 'L'}",
              outward(tube([(0.04 * s, -0.315, 0.112), (0.052 * s, -0.322, 0.13), (0.056 * s, -0.326, 0.142)],
                           [0.009, 0.007, 0.003], seg=12, per=4)), TUSK, "head", fuse=False, surface="shell")
    # the mouth: a short dark line back from the snout along the jaw
    for s in (1, -1):
        md = unit(0.9 * s, -0.4, -0.3)
        k.decal(np.array([0.05 * s, -0.285, 0.12]), md, (0.022, 0.004), NOSTRIL, eps=0.0015, rings=3, segs=28,
                up=(0, -0.3, 1))

    # --- ears: pointed, upright, a little out ------------------------------------------------------------------------
    ear(k, (0.062, -0.19, 0.272), (0.35, 0.05, 1), unit(0.3, -1, 0.15), 0.078, 0.032, COAT, LINING, 0.014)

    # --- the curly tail, its loop turned out toward +X ----------------------------------------------------------------
    lc.curl_tail(k, (0.0, 0.245, 0.29), 0.026, (0.5, 0.86, 0.0), 0.0105, COAT)

    de = unit(0.74, -0.6, 0.3)
    E = on_ell(H, HR, de)
    k.build(voxel=0.0045, smooth=4, band=0.005, wband=0.03)
    k.eye_states(eyes_pair(k, E, de, (0.02, 0.023), boar_eye(), srgb(COAT)))
    k.export(out)


if __name__ == "__main__":
    mistretel(*args())
