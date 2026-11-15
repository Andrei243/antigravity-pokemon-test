# Huntail sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/huntail.py -- <the export's Huntail.glb> <out.glb>
#
# From the references: a long sky-blue eel in an S, its big round head low in front and its tail rising behind; the
# head's mouth gapes wide, pink inside with a paler tongue, white fangs (two big ones at the upper corners, two small
# between them, two below); round white eyes ringed black with a black pupil high on the head's sides; white spots
# ringed orange on the face and down both sides of the body; a big orange fan with ribs standing up behind the head;
# round orange lobes along the back and under the body, two orange fins under the jaw; and the tail ending in a lure
# like a little blue fish: a pointed snout, a white spot ringed orange, two fins forked where it joins the tail.
# The body is laid as dratini.py's (its Line, by sibling): one smooth line through the chain of joints, a tube a
# stretch on its own bone. The skeleton: body (0.04, 0.34, 0.264) with the tail on the same joint, seg1 (0.06, 0.24,
# 0.144), seg2 (0, 0.08, 0.204), seg3 (-0.04, -0.06, 0.144), head (0, -0.14, 0.104).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, eye_look, args, outward, sibling

BLUE, ORANGE, ORANGE2, WHITE = "#58AEE6", "#F29A40", "#E2832C", "#F8F8F4"
MOUTH, TONGUE, INK, RIB = "#C8566A", "#EE98A2", "#141418", "#B8662A"

dr = sibling("dratini")

# The line, tail's end first, through the joints to the head
LINE = [(0.03, 0.43, 0.36), (0.045, 0.395, 0.31), (0.04, 0.34, 0.264), (0.058, 0.29, 0.19), (0.06, 0.24, 0.144),
        (0.035, 0.16, 0.168), (0.0, 0.08, 0.204), (-0.026, 0.01, 0.186), (-0.04, -0.06, 0.15), (0.0, -0.14, 0.13)]
RADS = [0.021, 0.026, 0.031, 0.035, 0.039, 0.042, 0.045, 0.048, 0.052, 0.058]
BONES = ["tail", "tail", "body", "body", "seg1", "seg1", "seg2", "seg2", "seg3"]

SC = np.array([0.0, -0.21, 0.168])          # the skull (the head above the gape)
SR = np.array([0.088, 0.105, 0.066])
JC = np.array([0.0, -0.222, 0.056])         # the lower jaw
JR = np.array([0.08, 0.098, 0.038])


def on(c, r, d):
    d = unit(*d)
    return c + d * r, d


def ring_spot(rim=0.68):
    W, O = srgb(WHITE), srgb(ORANGE)
    def look(u, v):
        c = np.tile(W, (len(u), 1))
        c[u * u + v * v > rim * rim] = O
        return c
    return look


def fang(root, tip, w):
    root, tip = np.array(root, float), np.array(tip, float)
    d = tip - root; d /= np.linalg.norm(d)
    a = np.cross(d, (1.0, 0, 0)); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    return hull([root + w * b, root - w * b, root + 0.6 * w * a, root - 0.6 * w * a, tip])


def lobe(k, name, c, ex, up, r, bone, color=ORANGE, tilt=35):
    """A round orange lobe standing out from the body: a half disc along ex from its middle c, its plane turned
    tilt degrees about ex toward +X so its face shows from the front three-quarter and from behind."""
    ex = np.array(ex, float); ex /= np.linalg.norm(ex)
    up = np.array(up, float); up = up - ex * (up @ ex); up /= np.linalg.norm(up)
    side = np.cross(ex, up)
    if side[0] < 0:
        side = -side
    t = math.radians(tilt)
    ez = math.cos(t) * up + math.sin(t) * side
    pts = [(-r, -0.02)] + [(r * math.cos(a), r * 1.05 * math.sin(a)) for a in np.linspace(math.pi, 0, 15)] + [(r, -0.02)]
    k.add(name, slab(pts[::-1], c, ex, ez, 0.015), color, bone, fuse=False, surface="scales")


def huntail(src, out):
    k = Kit(src, coat="scales")
    line = dr.Line(LINE, RADS)
    line.tubes(k, BONES, BLUE)

    # --- the head: a skull over a gaping mouth, the lower jaw tipped down in front -------------------------------
    k.add("nape", tube([LINE[-2], LINE[-1], (0, -0.16, 0.13)], [0.052, 0.058, 0.064]), BLUE, "head")
    k.add("skull", ell(SC, SR, rot=(-14, 0, 0)), BLUE, "head")
    k.add("cheeks", ell((0, -0.16, 0.12), (0.082, 0.075, 0.07)), BLUE, "head")
    k.add("jaw", ell(JC, JR, rot=(21, 0, 0)), BLUE, "head")
    # inside the gape: the pink of the mouth set back, a paler tongue on the jaw
    k.add("mouth", ell((0, -0.212, 0.1), (0.07, 0.066, 0.046)), MOUTH, "head")
    k.add("tongue", ell((0, -0.245, 0.078), (0.05, 0.052, 0.016), rot=(14, 0, 0)), TONGUE, "head")

    # fangs: two big at the upper corners, two small between, two below
    for s in (1, -1):
        k.add("fangbig" + "RL"[s < 0], fang((s * 0.052, -0.272, 0.138), (s * 0.05, -0.282, 0.098), 0.011),
              WHITE, "head", fuse=False, surface="shell")
        k.add("fangsmall" + "RL"[s < 0], fang((s * 0.02, -0.298, 0.142), (s * 0.02, -0.304, 0.117), 0.008),
              WHITE, "head", fuse=False, surface="shell")
        k.add("fanglow" + "RL"[s < 0], fang((s * 0.048, -0.27, 0.068), (s * 0.047, -0.28, 0.1), 0.009),
              WHITE, "head", fuse=False, surface="shell")

    # the fan behind the head: wedges of two oranges, standing up and back, turned toward +X
    ex, ez = unit(0.57, 0.82, 0), np.array([0, 0, 1.0])
    root = np.array([0.0, -0.17, 0.21])
    R = 0.14
    angs = np.linspace(math.radians(18), math.radians(112), 6)
    for i in range(5):
        a0, a1 = angs[i], angs[i + 1]
        arc = [(R * (1 - 0.06 * math.sin(math.pi * t)) * math.cos(a), R * (1 - 0.06 * math.sin(math.pi * t)) * math.sin(a))
               for t, a in zip(np.linspace(0, 1, 6), np.linspace(a0, a1, 6))]
        pts = [(0.0, 0.0)] + arc
        k.add(f"fan{i}", slab(pts, root, ex, ez, 0.016 + 0.003 * (i % 2)), ORANGE if i % 2 == 0 else ORANGE2, "head",
              fuse=False, surface="scales")
    k.add("fanroot", ell(root + (0, 0.005, -0.005), (0.03, 0.025, 0.02)), BLUE, "head")

    # the two fins under the jaw, down and out
    for s in (1, -1):
        exf = unit(s * 0.45, 0.35, -0.82)
        ezf = unit(0, 1, 0.3)
        pts = [(0.0, -0.025), (0.04, -0.03), (0.075, -0.012), (0.085, 0.01), (0.06, 0.03), (0.02, 0.028), (0.0, 0.02)]
        k.add("pec" + "RL"[s < 0], slab(pts, (s * 0.05, -0.19, 0.062), exf, ezf, 0.016), ORANGE, "head", fuse=False,
              surface="scales")

    # lobes along the back and under the body
    for name, s, below, r in (("lobe0", 1.2, False, 0.032), ("lobe1", 2.9, False, 0.036), ("lobe2", 4.6, False, 0.04),
                              ("lobe3", 6.3, False, 0.042), ("lobe4", 4.0, True, 0.04), ("lobe5", 7.4, True, 0.046)):
        p, t, rad = line.at(s)
        up = np.array([0, 0, -1.0 if below else 1.0])
        upn = up - t * (up @ t); upn /= np.linalg.norm(upn)
        bone = BONES[min(int(s), len(BONES) - 1)]
        lobe(k, name, p + upn * rad * 0.7, t, upn, r * 1.3, bone)

    # --- the lure: a little blue fish at the tail's end -------------------------------------------------------------
    p, t, rad = line.at(0.0)
    d = -t
    k.add("lure", tube([p, p + d * 0.03, p + d * 0.062, p + d * 0.1], [0.018, 0.028, 0.02, 0.006]), BLUE, "tail")
    for s in (1, -1):
        w = unit(s * 1.0, 0.0, 0.0)
        exl = d * -0.45 + w * 0.9; exl /= np.linalg.norm(exl)
        ezl = np.cross(exl, unit(0, -1, 0.3))
        pts = [(0.0, -0.012), (0.04, -0.016), (0.07, -0.004), (0.045, 0.01), (0.0, 0.014)]
        k.add("lurefin" + "RL"[s < 0], slab(pts, p + d * 0.022, exl, ezl, 0.014), BLUE, "tail", fuse=False,
              surface="scales")

    # --- spots: white ringed orange, on the face and down both sides ------------------------------------------------
    spot = ring_spot()
    for s in (1, -1):
        c, n = on(SC, SR, (s * 0.42, -0.9, -0.05))
        k.decal(c, n, (0.016, 0.016), spot, eps=0.003, rings=10, segs=40)
        c, n = on(SC, SR, (s * 0.96, -0.15, -0.1))
        k.decal(c, n, (0.016, 0.016), spot, eps=0.003, rings=10, segs=40)
        for sv in (1.6, 3.3, 5.3, 7.2):
            p, t, rad = line.at(sv)
            side = np.cross(t, (0, 0, 1.0)); side /= np.linalg.norm(side)
            if side[0] * s < 0:
                side = -side
            nrm = side * 0.85 + np.array([0, 0, 0.5]); nrm /= np.linalg.norm(nrm)
            k.decal(p + nrm * rad, nrm, (0.0115 + 0.001 * sv, 0.0115 + 0.001 * sv), spot, eps=0.003, rings=10, segs=40)
        lp = line.at(0.0)[0] + d * 0.035
        k.decal(lp + np.array([s * 0.025, 0, 0]), unit(s, -0.3, 0.2), (0.011, 0.011), spot, eps=0.003, rings=10,
                segs=40)

    k.build(voxel=0.004, smooth=4, band=0.004)

    # --- the eyes: round, white, ringed black, a black pupil, high on the head's sides ----------------------------------
    W, K, L = srgb(WHITE), srgb(INK), srgb(BLUE)

    def eye(u, v):
        c = np.tile(W, (len(u), 1))
        r2 = u * u + v * v
        c[r2 > 0.8 ** 2] = K
        c[(u + 0.08) ** 2 + (v - 0.02) ** 2 < 0.36 ** 2] = K
        return c

    def eyes(state):
        look = eye_look(state, eye, L, K, line=0.18)
        for s in (1, -1):
            lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
            c, n = on(SC, SR, (s * 0.6, -0.42, 0.68))
            k.decal(c, n, (0.026, 0.026), lk, eps=0.003, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    huntail(*args())
