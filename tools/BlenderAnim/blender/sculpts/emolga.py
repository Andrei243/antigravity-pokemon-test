# Emolga sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/emolga.py -- <the export's Emolga.glb> <out.glb>
#
# From the references: a white flying squirrel, its round head much wider than the small pear of a body under it. A
# dark grey hood over the top, the sides and the back of the head, coming down to a point on the brow between the
# eyes. Big rounded ears leaning out, dark outside, yellow inside with white in their middle. Big black eyes with a
# white glint, set wide; round yellow cheeks; a wide open smile, red with a pink tongue; a dot of a nose. Short white
# arms with little hands, and from each arm down to the leg a flap of skin, white near the arm and yellow over its
# outer, lower part. Tiny grey feet. A long flat dark tail cut into three points along its lower edge.
# The export's joints: body (0, 0, 0.13), head (0, -0.01, 0.19), ears (+-0.045, -0.005, 0.29), arms (+-0.04, 0,
# 0.17), legs (+-0.03, 0, 0.06), tail (0, 0.04, 0.1). About 0.37 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, slab, outward, srgb, inside, unit, boxy, eye_look, args

WHITE, GREY, YELLOW, INK = "#F5F5F8", "#45464E", "#F6CD2E", "#1A1A22"
FEET, MOUTH, TONGUE, NOSE = "#B9B8C8", "#A42C3C", "#F0808C", "#2A2830"
H = np.array([0, -0.012, 0.228])          # the head's centre
HR = np.array([0.095, 0.081, 0.08])


def on_head(d, lift=0.0):
    d = unit(*d)
    return H + d * (1 / math.sqrt(((d / HR) ** 2).sum()) + lift)


def emolga(src, out):
    k = Kit(src, coat="fur")

    # --- the head, the pear of a body ----------------------------------------------------------------------------
    k.add("head", ell(H, HR, deform=boxy(2.3)), WHITE, "head")

    def pear(V):
        V = V.copy(); s = 1 - 0.18 * V[:, 2]
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, 0.0, 0.112), (0.062, 0.056, 0.074), deform=pear), WHITE, "body")

    # --- tiny legs and grey feet, hanging ----------------------------------------------------------------------------
    k.add("leg", tube([(0.03, 0.0, 0.06), (0.032, -0.004, 0.03)], [0.017, 0.014]), WHITE, "leg{s}", mirror=True)
    k.add("foot", ell((0.033, -0.01, 0.018), (0.016, 0.02, 0.014)), FEET, "leg{s}", mirror=True)

    # --- short arms forward and a little down, little hands ----------------------------------------------------------
    SH, WR = np.array([0.045, -0.004, 0.168]), np.array([0.096, -0.066, 0.142])
    k.add("arm", tube([SH, (0.072, -0.035, 0.158), WR], [0.015, 0.013, 0.012]), WHITE, "arm{s}", mirror=True)
    k.add("hand", ell(WR + (0.008, -0.008, 0.002), (0.016, 0.014, 0.014)), WHITE, "arm{s}", mirror=True)

    # --- the flaps: from the arm down to the leg, bowed out; yellow, with white over the part near the arm ----------
    HIP, LOW = np.array([0.05, 0.006, 0.05]), np.array([0.112, -0.04, 0.038])
    TOPS, TOPW = SH + (0.0, 0.006, -0.004), WR + (-0.004, 0.006, -0.006)

    def flap(vmax):
        def at(u, v):
            u = 0.01 + 0.98 * u
            v = (0.01 + 0.98 * v) * vmax * (1 - 0.3 * u ** 4)
            top = TOPS[None, None] + (TOPW - TOPS)[None, None] * u[..., None]
            bot = HIP[None, None] + (LOW - HIP)[None, None] * u[..., None]
            p = top + (bot - top) * v[..., None]
            bow = (np.sin(np.pi * np.clip(v / max(vmax, 1e-6), 0, 1)) * (0.35 + 0.65 * u))[..., None]
            return p + bow * np.array([0.03, 0.012, 0.0])
        return at
    k.add("flap", outward(sheet(flap(1.0), 26, 18, 0.006)), YELLOW, "arm{s}", fuse=False, mirror=True, surface="skin")
    k.add("flapwhite", outward(sheet(flap(0.48), 26, 10, 0.0095)), WHITE, "arm{s}", fuse=False, mirror=True,
          surface="skin")

    # --- the ears: big, rounded, leaning out; a yellow lining with white in its middle, set in their front -----------
    def ear(s):
        rot = (0, 30 * s, 0)
        base = np.array([0.05 * s, -0.004, 0.285])
        up = np.array([0.5 * s, 0, 0.866])
        c = base + up * 0.04
        side = "R" if s > 0 else "L"
        k.add(f"ear{side}", ell(c, (0.034, 0.013, 0.046), rot=rot, sub=3), GREY, f"ear{side}", fuse=False)
        k.add(f"earlining{side}", ell(c + (0, -0.0055, -0.002), (0.028, 0.009, 0.039), rot=rot, sub=3), YELLOW,
              f"ear{side}", fuse=False)
        k.add(f"earwhite{side}", ell(c + up * -0.004 + np.array([-0.006 * s, -0.008, 0]), (0.018, 0.0075, 0.028),
                                     rot=rot, sub=3), WHITE, f"ear{side}", fuse=False)
    ear(1); ear(-1)

    # --- the tail: a flat dark blade back and out to +X, rising, three points along its lower edge -------------------
    TO = np.array([0.008, 0.05, 0.1])
    TD = unit(0.6, 0.8, 0)
    OUT = [(0.0, -0.016), (0.0, 0.014), (0.07, 0.056), (0.15, 0.11), (0.215, 0.16), (0.15, 0.092), (0.205, 0.098),
           (0.13, 0.05), (0.18, 0.035), (0.08, 0.004)]
    k.add("tail", slab(OUT, TO, TD, (0, 0, 1), 0.013), GREY, "tail", fuse=False)
    k.add("tailroot", ell(TO + (-0.002, -0.004, 0.0), (0.016, 0.018, 0.019), sub=3), GREY, "tail", fuse=False)

    # --- the hood: dark over the top, sides and back of the head, to a point on the brow -----------------------------
    def hood(P):
        d = P - H
        az = np.degrees(np.abs(np.arctan2(d[:, 0], -d[:, 1])))
        zl = np.interp(az, [0, 9, 20, 44, 62, 74, 86, 180], [0.3, 0.55, 0.8, 0.86, 0.55, 0.0, -1.6, -1.6]) * HR[2]
        near = (np.linalg.norm(d / (HR * 1.2), axis=1) < 1) * inside(0.168 - P[:, 2], 0.002)
        return inside(zl - d[:, 2], 0.0016) * near
    k.paint(hood, GREY)

    # --- the face: a dot of a nose, a wide open smile, round yellow cheeks --------------------------------------------
    k.add("nose", ell(on_head((0, -1, -0.12), -0.002), (0.005, 0.0035, 0.0035)), INK, "head", fuse=False,
          surface="skin")
    CW, CM, CT, CI = srgb(WHITE), srgb(MOUTH), srgb(TONGUE), srgb(INK)

    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.62) ** 2 + ((v + 0.78) / 0.58) ** 2 < 1] = CT
        return c

    def mouth_shape(th):
        s = math.sin(th)
        return min(1.0, 0.3 / s) if s > 0 else 1.0
    dm = unit(0, -1, -0.5)
    k.decal(on_head(dm), dm, (0.03, 0.03), mouth, eps=0.002, outline=mouth_shape, rings=22, segs=72)
    dc = unit(0.78, -0.55, -0.3)
    k.decal(on_head(dc), dc, (0.019, 0.019), YELLOW, eps=0.002, rings=10, segs=40, mirror=True)

    k.build(voxel=0.004, band=0.006)

    # --- big black eyes with a white glint, set wide ------------------------------------------------------------------
    def eye(u, v):
        c = np.tile(CI, (len(u), 1))
        c[((u - 0.1) / 0.36) ** 2 + ((v - 0.36) / 0.32) ** 2 < 1] = (1, 1, 1)
        return c
    de = unit(0.46, -0.87, 0.14)

    def eyes(state):
        k.decal(on_head(de), de, (0.019, 0.026), eye_look(state, eye, CW, CI), eps=0.0025, rings=20, segs=56,
                mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    emolga(*args())
