# Drampa-Mega sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/drampamega.py -- <the export's Drampa-Mega.glb> <out.glb>
# Grown from drampa.py (sibling("drampa")): its on_ell, along and torus helpers, its way of a head (a skull, a snout
# and a jaw fused, the eye a small dark patch with a glint high on the snout) and of a cloud of fused lobes.
#
# From the references (10302): an old dragon risen out of a storm. Its masses, largest first: (1) a dark storm cloud
# lying on the ground, navy lobes in front and slate-blue ones behind, each lobe rounded and ending in a hooked point
# that curls up, some 0.4 of the height and the whole of the length; (2) a long neck rising tall out of the cloud's
# front in an S, green down its back and cream down its front, the head at the top held forward; (3) behind the head a
# great crest of mane, five or six thick dark spikes fanning up and back, slate at their tips, setting the height;
# (4) two big dark paws lying in front of the cloud, grey claws; (5) a green chest at the neck's root with a ruff of
# green leafy spikes and a white diamond of four squares on it. The head: emerald green, a cream muzzle and jaw, the
# mouth open and red with white fangs, a small dark eye, two yellow crescent horns on the crown, a pink cheek, white
# whisker curls from the snout, a dark beard hanging under the jaw.
# The game's sculptor (PokemonModels.Alola2.cs) gives a white mane and white horns: the pictures overrule it (a dark
# crest of mane, yellow crescent horns, a dark beard); the base's teal is emerald here.
# Head / height measured on the 3D picture 0.17 (snout to nape over 0.62); built a third larger, as advised.
# Bones (the form's own): body (0, 0, 0.15), head (0, -0.08, 0.22), tail (0, 0.4, 0.14): neck, head and mane on head,
# the rear lobes on tail, the paws and the rest of the cloud on body.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, smoothstep, unit, eye_look, axes, lumpy, outward, \
    sibling, args

D = sibling("drampa")
on_ell, along, torus = D.on_ell, D.along, D.torus

GREEN, GREEN_DK, CREAM = "#1F8F55", "#156B3F", "#ECEEDC"
NAVY, SLATE, SLATE_LT = "#2A3141", "#56658A", "#7484AC"
YELLOW, PINK, RED, WHITE = "#F2D23A", "#E7A3C8", "#C23038", "#F4F4F0"
INK, CLAW = srgb("#15161C"), "#B8BCC6"

# --- the frame ----------------------------------------------------------------------------------------------------------
NECK = np.array([(0, -0.12, 0.13), (0, -0.14, 0.23), (0, -0.165, 0.31), (0, -0.17, 0.41), (0, -0.2, 0.48)])
H, HR = np.array([0.0, -0.255, 0.52]), np.array([0.07, 0.085, 0.065])       # the skull
S, SR = np.array([0.0, -0.335, 0.515]), np.array([0.05, 0.065, 0.042])         # the snout
J, JR = np.array([0.0, -0.32, 0.462]), np.array([0.045, 0.065, 0.025])       # the dropped jaw


def horn(k, name, pts, widths, colour, bone, flat=0.6):
    """A curved horn, spike or claw as a chain of hulls, a kite section at each point, to a point."""
    pts = [np.asarray(p, float) for p in pts]
    secs = []
    for i, (p, w) in enumerate(zip(pts, widths)):
        d = pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]
        a, b = axes(d)
        secs.append([p + a * w, p - a * w, p + b * w * flat, p - b * w * flat] if w > 0 else [p])
    for i in range(len(secs) - 1):
        k.add(f"{name}{i}", hull(secs[i] + secs[i + 1]), colour, bone, fuse=False, surface="scales")


def head(k):
    k.add("neck", tube(NECK, [0.05, 0.046, 0.043, 0.042, 0.042], seg=20), GREEN, "head", surface="scales")
    k.add("skull", ell(H, HR), GREEN, "head", surface="scales")
    k.add("snout", ell(S, SR, rot=(-4, 0, 0)), GREEN, "head", surface="scales")
    k.add("jaw", ell(J, JR, rot=(26, 0, 0)), CREAM, "head", surface="scales")
    k.add("mouth", ell((0, -0.325, 0.483), (0.038, 0.052, 0.022), rot=(16, 0, 0)), RED, "head", surface="skin")
    # the neck's front cream, its back green: by the side of the neck's centre line
    def front(P):
        cy = np.interp(P[:, 2], NECK[:, 2], NECK[:, 1])
        return inside(P[:, 1] - cy + 0.004, 0.004) * inside(P[:, 2] - 0.49, 0.004)
    k.paint(front, CREAM, on=["neck"])
    # the muzzle's underside cream
    k.paint(lambda P: inside(P[:, 2] - (0.505 + 0.12 * (P[:, 1] + 0.34)), 0.003) * inside(P[:, 1] + 0.26, 0.004),
            CREAM, on=["snout", "skull"])
    # the nostrils
    k.decal(on_ell(S, SR, (0.3, -1, 0.45)), unit(0.3, -1, 0.45), (0.006, 0.007), "#0E3A24", eps=0.003, rings=4,
            segs=20, mirror=True)
    # white fangs down from the upper jaw, up from the lower
    for s in (1, -1):
        b = np.array([0.026 * s, -0.378, 0.5])
        k.add(f"fang{'R' if s > 0 else 'L'}", hull([b + (0.008, 0, 0), b - (0.008, 0, 0), b + (0, 0.008, 0),
                                                   b + (0, -0.003, -0.026)]), WHITE, "head", fuse=False, surface="shell")
    # the crescent horns: two yellow curves rising from the crown and sweeping back
    for s in (1, -1):
        pts = [H + (0.03 * s, -0.025, 0.05), H + (0.045 * s, -0.04, 0.09), H + (0.05 * s, -0.02, 0.125),
               H + (0.045 * s, 0.02, 0.14)]
        horn(k, f"horn{'R' if s > 0 else 'L'}", pts, [0.016, 0.014, 0.011, 0.0], YELLOW, "head")
    # a pink cheek
    for s in (1, -1):
        k.add(f"cheek{'R' if s > 0 else 'L'}", ell(H + (0.064 * s, -0.035, -0.02), (0.016, 0.016, 0.016), sub=3), PINK,
              "head", fuse=False, surface="skin")
    # the white whiskers: short thick curls out from the snout's sides
    for s in (1, -1):
        pts = np.array([(0.04, -0.38, 0.505), (0.075, -0.385, 0.51), (0.09, -0.365, 0.535), (0.082, -0.345, 0.55)])
        pts[:, 0] *= s
        k.add(f"whisker{'R' if s > 0 else 'L'}", outward(tube(pts, [0.01, 0.01, 0.009, 0.006])), WHITE, "head",
              fuse=False, surface="fur")
    # the dark beard under the jaw
    k.add("beard", ell((0, -0.28, 0.425), (0.04, 0.04, 0.05), deform=lumpy(3, 2, 0.1)), NAVY, "head")
    k.add("beardtip", tube([(0, -0.28, 0.41), (0, -0.27, 0.36), (0, -0.25, 0.32)], [0.03, 0.02, 0.008]), NAVY, "head")


def mane(k):
    """The crest: thick dark spikes fanning up and back from the nape and down the neck's top."""
    root = H + np.array([0.0, 0.05, 0.02])
    fan = [(0.0, (0.0, 0.2, 1.0), 0.19, NAVY), (0.045, (0.35, 0.35, 0.9), 0.15, SLATE), (-0.045, (-0.35, 0.35, 0.9), 0.15, SLATE),
           (0.03, (0.3, 0.75, 0.6), 0.14, NAVY), (-0.03, (-0.3, 0.75, 0.6), 0.14, NAVY), (0.0, (0.0, 0.9, 0.3), 0.12, SLATE_LT),
           (0.05, (0.75, 0.25, 0.55), 0.11, SLATE_LT), (-0.05, (-0.75, 0.25, 0.55), 0.11, SLATE_LT)]
    for i, (x, d, L, col) in enumerate(fan):
        L *= 1.25
        d = unit(*d)
        b = root + np.array([x, 0, 0])
        up = unit(*(d + np.array([0, -0.4, 0])))
        pts = [b, b + d * L * 0.45, b + d * L * 0.8 + np.array([0, 0.02, -0.01]), b + d * L + np.array([0, 0.045, -0.03])]
        horn(k, f"mane{i}", pts, [0.034, 0.03, 0.017, 0.0], col, "head", flat=0.55)
    k.add("nape", ell(root + (0, -0.005, -0.01), (0.06, 0.05, 0.05)), NAVY, "head")
    # tufts down the back of the neck
    for i, z in enumerate((0.38, 0.32, 0.26)):
        cy = np.interp(z, NECK[:, 2], NECK[:, 1])
        b = np.array([0.0, cy + 0.03, z])
        horn(k, f"neckmane{i}", [b, b + (0, 0.05, 0.02), b + (0, 0.085, 0.0)], [0.026, 0.018, 0.0], NAVY, "head")


def chest(k):
    k.add("chest", ell((0, -0.14, 0.12), (0.1, 0.075, 0.085)), GREEN, "body", surface="scales")
    # a ruff of green leafy spikes round the neck's root
    for i, az in enumerate(np.linspace(-150, 150, 9)):
        a = math.radians(az)
        d = np.array([math.sin(a), -math.cos(a) * 0.9, 0.55])
        b = np.array([0.075 * math.sin(a), -0.13 - 0.06 * math.cos(a), 0.17])
        horn(k, f"ruff{i}", [b, b + d * 0.04, b + d * 0.07 + (0, 0, 0.015)], [0.022, 0.016, 0.0],
             GREEN if i % 2 else GREEN_DK, "body")
    # the white diamond of four squares on the chest
    CC, CR = np.array([0, -0.14, 0.12]), np.array([0.1, 0.075, 0.085])
    c = on_ell(CC, CR, (0.0, -1, -0.15))
    n = (c - CC) / CR ** 2
    n /= np.linalg.norm(n)
    ex = unit(1, 0, 0)
    ez = np.cross(n, ex)
    q = 0.013
    for i, (a, b) in enumerate(((0, 1), (1, 0), (0, -1), (-1, 0))):
        m = c + n * 0.004 + (ex * a + ez * b) * q * 1.15
        sq = [(-q, 0), (0, -q), (q, 0), (0, q)]
        k.add(f"diamond{i}", slab(sq, m, ex, ez, 0.006), WHITE, "body", fuse=False, surface="shell")


def cloud(k):
    """The storm: navy lobes in front, slate behind, each rounded and ending in a point that curls up."""
    k.add("core", ell((0, 0.06, 0.1), (0.19, 0.27, 0.11), deform=lambda V: np.c_[V[:, :2], np.maximum(V[:, 2], -0.8)]),
          NAVY, "body")
    lobes = [  # centre, radius, tip direction, colour, bone
        ((0.15, -0.06, 0.1), 0.085, (0.5, -0.3, 0.8), NAVY, "body"),
        ((-0.15, -0.06, 0.1), 0.085, (-0.5, -0.3, 0.8), NAVY, "body"),
        ((0.18, 0.08, 0.11), 0.09, (0.6, 0.0, 0.8), NAVY, "body"),
        ((-0.18, 0.08, 0.11), 0.09, (-0.6, 0.0, 0.8), NAVY, "body"),
        ((0.0, 0.1, 0.16), 0.095, (0.0, 0.2, 1.0), NAVY, "body"),
        ((0.13, 0.25, 0.13), 0.1, (0.4, 0.3, 0.9), SLATE, "tail"),
        ((-0.13, 0.25, 0.13), 0.1, (-0.4, 0.3, 0.9), SLATE, "tail"),
        ((0.0, 0.36, 0.17), 0.11, (0.0, 0.5, 0.9), SLATE, "tail"),
        ((0.1, 0.46, 0.09), 0.08, (0.4, 0.7, 0.6), SLATE, "tail"),
        ((-0.1, 0.46, 0.09), 0.08, (-0.4, 0.7, 0.6), SLATE, "tail"),
    ]
    for i, (c, r, d, col, bone) in enumerate(lobes):
        c = np.array(c, float)
        k.add(f"lobe{i}", ell(c, (r, r, r * 0.85)), col, bone)
        d = unit(*d)
        # the hooked point: out along d, then curling over toward the middle
        tip = [c + d * r * 0.6, c + d * r * 1.05, c + d * r * 1.3 + np.array([-0.3 * d[0], -0.3 * d[1], 0.1]) * r,
               c + d * r * 1.35 + np.array([-0.8 * d[0], -0.8 * d[1], -0.1]) * r]
        k.add(f"lobecurl{i}", tube(tip, [r * 0.45, r * 0.32, r * 0.2, r * 0.06]), col, bone)
    # the swirl on each slate lobe's side: a lighter curl laid round it
    for i, (c, r, d, col, bone) in enumerate(lobes):
        if col != SLATE or abs(c[0]) < 0.05:
            continue
        c = np.array(c, float)
        s = np.sign(c[0])
        pts = [c + np.array([s * r * 0.92 * math.cos(t) * 0.35 + s * r * 0.75, r * 0.9 * math.sin(t) * 0.6,
                             r * 0.5 * math.cos(t)]) * np.array([1, 1, 1]) for t in np.linspace(0.2, 4.2, 9)]
        k.add(f"swirl{i}", outward(tube(pts, [0.016] * 8 + [0.008])), SLATE_LT, bone, fuse=False, surface="fur")
    k.paint(lambda P: smoothstep(0.08, 0.0, P[:, 2]) * 0.6, "#1C212D", on=["core", "lobe"])


def paws(k):
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        c = np.array([0.12 * s, -0.245, 0.045])
        k.add(f"paw{side}", ell(c, (0.06, 0.075, 0.045)), NAVY, "body")
        k.add(f"wrist{side}", tube([c + (0, 0.03, 0.01), (0.12 * s, -0.14, 0.09)], [0.045, 0.06]), NAVY, "body")
        for j, dx in enumerate((-0.03, 0.0, 0.03)):
            b = c + np.array([dx, -0.065, 0.005])
            horn(k, f"claw{side}{j}", [b, b + (0, -0.025, -0.008), b + (0, -0.035, -0.035)], [0.014, 0.01, 0.0], CLAW,
                 "body")


def eye_open(u, v):
    c = np.tile(INK, (len(u), 1))
    c[(u + 0.25) ** 2 + (v - 0.3) ** 2 < 0.1] = np.array([1.0, 1.0, 1.0])
    return c


def drampamega(src, out):
    k = Kit(src, coat="fur")
    head(k)
    mane(k)
    chest(k)
    cloud(k)
    paws(k)
    k.build(voxel=0.0058, smooth=4, band=0.004, wband=0.05)

    de = unit(0.62, -0.6, 0.5)
    E = on_ell(H, HR, de)
    m = np.array([-1, 1, 1.0])

    def eyes(state):
        look = eye_look(state, eye_open, srgb(GREEN), INK, line=0.18)
        k.decal(E, de, (0.02, 0.022), look, eps=0.003, rings=20, segs=56)
        k.decal(E * m, de * m, (0.02, 0.022), lambda u, v: look(-u, v), eps=0.003, rings=20, segs=56)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    drampamega(*args())
