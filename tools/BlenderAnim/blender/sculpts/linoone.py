# Linoone sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/linoone.py -- <the export's Linoone.glb> <out.glb>
# Zigzagoon grown (zigzagoon.py, whose spikes and feet it borrows). From the references: a long, low, sleek cream body
# close to the ground on short legs; a long narrow pointed head with a dark nose; a brown stripe from the muzzle back
# through each eye and a brown stripe down the middle of the crown and the back of the neck; on each flank a long
# brown arrow pointing forward that runs back along the long flowing tail; sharp blue eyes under a flat brow; a little
# tuft of cream spikes on the crown and spiky cream cheeks; long white claws.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, tri, unit, eye_look, polygon, args, sibling

z_ = sibling("zigzagoon")

CREAM, BROWN, NOSE, CLAW = "#E8DFD4", "#8E7462", "#2A2626", z_.CLAW
IRIS, PUPIL, WHITE, INK = "#3AA8E0", "#10202C", "#F6F4F0", "#2A2220"

# The frame (joints: body (0, 0, 0.16), head (0, -0.22, 0.145), ears (+-0.04, -0.25, 0.195), legs (+-0.06, -0.15, 0.1),
# hinds (+-0.06, 0.17, 0.1), tail (0, 0.23, 0.145))
HC = np.array([0.0, -0.255, 0.152])
HR = np.array([0.064, 0.075, 0.058])
LX, FY, HY = 0.06, -0.15, 0.17
BODY = [(0, -0.2, 0.152), (0, -0.1, 0.155), (0, 0.04, 0.152), (0, 0.17, 0.145), (0, 0.24, 0.15)]
BRAD = [0.064, 0.078, 0.084, 0.078, 0.05]
TB, TD = np.array([0.0, 0.24, 0.155]), unit(0.42, 0.85, 0.32)
TL = 0.25


def tail_frame():
    a = np.cross(TD, (0, 0, 1.0)); a /= np.linalg.norm(a)
    return a, np.cross(TD, a)            # a sideways, b up (square to the tail)


def linoone(src, out):
    k = Kit(src, coat="fur")

    # The long sleek body, low between the short legs
    k.add("body", tube(BODY, BRAD, seg=24), CREAM, "body")
    k.add("belly", ell((0, 0.03, 0.11), (0.07, 0.16, 0.05)), CREAM, "body")
    for name, (x, y), bone in (("leg", (LX, FY), "leg{s}"), ("hind", (LX, HY), "hind{s}")):
        k.add(name + "top", ell((x - 0.01, y + 0.01, 0.12), (0.042, 0.05, 0.05)), CREAM, bone, mirror=True)
        k.add(name, tube([(x - 0.004, y, 0.1), (x + 0.002, y - 0.006, 0.03)], [0.034, 0.03]), CREAM, bone, mirror=True)
        k.add(name + "foot", ell((x + 0.002, y - 0.016, 0.02), (0.032, 0.042, 0.02)), CREAM, bone, mirror=True)
        for i, dx in enumerate((-0.015, 0.0, 0.015)):
            a = np.array([x + 0.002 + dx, y - 0.05, 0.014])
            k.add(f"claw{name}{i}", z_.spike(a, (dx * 2, -1, -0.45), 0.035, 0.008, 0.006), CLAW, bone, fuse=False,
                  mirror=True, surface="shell")

    # The long narrow head, a pointed muzzle
    k.add("head", ell(HC, HR), CREAM, "head")
    k.add("neck", ell((0, -0.2, 0.152), (0.062, 0.06, 0.06)), CREAM, "head")
    k.add("snout", ell(HC + (0, -0.075, -0.012), (0.034, 0.065, 0.032), rot=(4, 0, 0)), CREAM, "head")

    # The flowing tail, back and a little up, swung out to the model's right, flat across its swing
    a, b = tail_frame()
    pts = [TB + TD * t * TL + b * 0.04 * math.sin(t * 2.5) for t in (0, 0.3, 0.6, 0.85, 1.0)]
    k.add("tail", tube(pts, [0.045, 0.055, 0.048, 0.03, 0.008], seg=20), CREAM, "tail")
    tip = pts[-1]
    for i, (sa, sb) in enumerate(((0.0, 0.0), (0.5, 0.4), (-0.4, 0.5), (0.2, -0.5))):
        d = unit(*(TD + sa * a + sb * b))
        k.add(f"tailtip{i}", z_.spike(tip - TD * 0.04, d, 0.06, 0.02, 0.014), CREAM, "tail", fuse=False,
              surface="fur")

    # The stripes, crisp (no on): through the eyes from the muzzle back, down the crown and the back of the neck,
    # the arrows on the flanks and their run along the tail
    def face(P):
        d = (P - HC) / HR
        mid = 0.18 + 0.1 * d[:, 1]
        return (inside(np.abs(d[:, 2] - mid) - 0.26, 0.04) * (np.abs(d[:, 0]) > 0.38) * (d[:, 1] > -1.65)
                * (d[:, 1] < 0.7) * inside(np.linalg.norm(d, axis=1) - 2.0, 0.05))
    k.paint(face, BROWN)
    def crown(P):
        w = 0.022 * np.clip((P[:, 1] + 0.31) / 0.05, 0, 1) * np.clip((-0.02 - P[:, 1]) / 0.08, 0, 1)
        return inside(np.abs(P[:, 0]) - w, 0.003) * (P[:, 2] > 0.16) * (P[:, 1] > -0.31) * (P[:, 1] < -0.02)
    k.paint(crown, BROWN)
    def arrow(P):
        y = P[:, 1]
        w = 0.03 * np.clip((y + 0.06) / 0.14, 0, 1)
        zc = 0.17 + 0.012 * np.clip((y + 0.06) / 0.14, 0, 1)
        flank = inside(np.abs(P[:, 2] - zc) - w, 0.003) * (np.abs(P[:, 0]) > 0.035) * (y > -0.06) * (y < 0.26)
        q = P - TB
        s = q @ TD
        up = q @ b
        side = q @ a
        tail = (inside(np.abs(up + 0.004) - 0.022 * (1 - np.clip(s / TL, 0, 1) * 0.6), 0.003) * (s > 0.0)
                * (s < TL * 0.9) * (np.abs(side) > 0.012))
        return np.maximum(flank, tail)
    k.paint(arrow, BROWN)

    # A tuft of small cream spikes on the crown, spiky cream cheeks, a ruff of spikes under the chin
    for s in (1, -1):
        for j, (dx, dy, ln) in enumerate(((0.0, 0.0, 0.035), (0.018, 0.012, 0.03))):
            base = np.array([(0.03 + dx) * s, -0.255 + dy, 0.198])
            k.add(f"tuft{'R' if s > 0 else 'L'}{j}", z_.spike(base, (0.4 * s, 0.35, 1), ln, 0.016, 0.012), CREAM,
                  "ear" + ("R" if s > 0 else "L"), fuse=False, surface="fur")
        for j, dz in enumerate((0.0, -0.025)):
            base = HC + np.array([0.056 * s, 0.01, -0.02 + dz])
            k.add(f"cheek{'R' if s > 0 else 'L'}{j}", z_.spike(base, (s * 0.8, 0.6, -0.2 - dz * 6), 0.04, 0.018,
                                                                0.013), CREAM, "head", fuse=False, surface="fur")
    for j, x in enumerate((-0.03, 0.0, 0.03)):
        base = np.array([x, -0.19, 0.1])
        k.add(f"ruff{j}", z_.spike(base, (x * 4, -0.3, -1), 0.03, 0.018, 0.013), CREAM, "body", fuse=False,
              surface="fur")

    # The dark nose at the tip and a thin mouth line under it
    k.add("nose", ell(HC + (0, -0.138, -0.008), (0.016, 0.012, 0.012)), NOSE, "head", fuse=False, surface="skin")
    CC, CK = srgb(CREAM), srgb(INK)
    def mouth(u, v):
        c = np.tile(CC, (len(u), 1))
        c[(np.abs(v - 0.2 * u * u) < 0.25) & (np.abs(u) < 0.9)] = CK
        return c
    k.decal(HC + (0, -0.118, -0.036), unit(0, -1, -0.5), (0.022, 0.009), mouth, eps=0.003, rings=10, segs=40)

    k.build(voxel=0.0058, smooth=3, band=0.006)

    # Sharp blue eyes cut flat by a level brow, inside the brown stripe
    de = unit(0.62, -0.74, 0.2)
    E = HC + de / np.linalg.norm(de / HR) + (0, -0.012, 0.004)
    top = lambda u: 0.3 + 0.25 * u
    OUT = z_and_cut(top)
    WH, IR, PU, KK = srgb(WHITE), srgb(IRIS), srgb(PUPIL), srgb(INK)
    def look(u, v):
        c = np.tile(WH, (len(u), 1))
        c[np.hypot((u + 0.15) / 0.72, (v + 0.05) / 0.85) < 1] = IR
        c[np.hypot((u + 0.15) / 0.3, (v + 0.05) / 0.5) < 1] = PU
        c[np.hypot(u - 0.12, v - 0.12) < 0.16] = (1, 1, 1)
        c[v > top(u) - 0.16] = KK
        return c
    def eyes(state):
        f = eye_look(state, look, srgb(BROWN), srgb(INK), line=0.15)
        for side in (1, -1):
            nn = de * np.array([side, 1, 1.0])
            outline = polygon([(side * x, y) for x, y in OUT]) if state in ("Open", "Fierce") else None
            col = f if side == 1 else (lambda x, y, g=f: g(-x, y))
            k.decal(E * np.array([side, 1, 1.0]), nn, (0.022, 0.018), col, eps=0.004, outline=outline, rings=20,
                    segs=56)
    k.eye_states(eyes)
    k.export(out)


def z_and_cut(top, n=96):
    """The unit circle cut off above v = top(u)."""
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        du, dv = math.cos(th), math.sin(th)
        t = 1.0
        if dv > top(du):
            lo, hi = 0.0, 1.0
            for _ in range(40):
                t = (lo + hi) / 2
                if t * dv > top(t * du):
                    hi = t
                else:
                    lo = t
            t = lo
        pts.append((t * math.cos(th), t * math.sin(th)))
    return pts


if __name__ == "__main__":
    linoone(*args())
