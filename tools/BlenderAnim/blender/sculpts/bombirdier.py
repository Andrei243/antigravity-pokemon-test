# Bombirdier sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/bombirdier.py -- <the export's Bombirdier.glb> <out.glb>
# From the references: a tall white stork standing on thin red legs with red toes and black claws; a long white neck
# in a gentle S, a small head with a fan-shaped crest at its back and smug half-shut eyes under a heavy black lash; a
# long red beak, pink at its tip; a long white apron that hangs from its chest down to the ground and rolls forward
# into a round sack there (the pictures show it white like the body, not the grey the task expected); wings white
# over the shoulder with long black flight feathers. The pictures raise one wing; here both are folded at the sides,
# their black feathers trailing behind and down, as the rules for the game's cameras ask.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, slab, srgb, inside, unit, eye_look, args, sibling

r = sibling("rookidee")
spike, on_ell = r.spike, r.on_ell

WHITE, APRON, BLACK, RED, PINK = "#F4F2F2", "#DEDAE7", "#2E2C33", "#D6404C", "#F49A9A"
CLAW = "#2A282E"


def frame():
    F = SimpleNamespace()
    F.H = np.array([0, -0.035, 0.775])
    F.HR = np.array([0.068, 0.085, 0.07])
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.046, 0.31, 0.024, 0.012
    F.LEGR = (0.0165, 0.0145)
    F.TOE, F.HEEL, F.TOER = 0.062, 0.034, 0.0095
    F.EYEDIR = unit(0.78, -0.42, 0.46)
    F.EYE = (0.028, 0.02)
    F.VOXEL = 0.0075
    return F


def body(k, F):
    k.add("torso", ell((0, 0.04, 0.43), (0.118, 0.14, 0.165), rot=(-12, 0, 0)), WHITE, "body")
    k.add("chest", ell((0, -0.05, 0.51), (0.1, 0.09, 0.1)), WHITE, "body")
    k.add("neck", tube([(0, -0.05, 0.53), (0, -0.075, 0.62), (0, -0.045, 0.7), (0, -0.03, 0.76)],
                       [0.056, 0.044, 0.04, 0.042]), WHITE, "head")
    k.add("head", ell(F.H, F.HR), WHITE, "head")
    for s, b in ((1, "legR"), (-1, "legL")):
        k.add("thigh", ell((0.05 * s, 0.015, 0.315), (0.04, 0.05, 0.045)), WHITE, b)


def apron(k, F):
    """The long white apron from the chest to the ground, rolling forward into a sack there."""
    k.add("apron", blade([(0, -0.12, 0.5), (0, -0.15, 0.4), (0, -0.158, 0.29), (0, -0.16, 0.17), (0, -0.165, 0.08)],
                         [0.062, 0.075, 0.072, 0.064, 0.058], (0, 1, 0), 0.5), APRON, "body")
    k.add("sack", ell((0, -0.2, 0.045), (0.072, 0.1, 0.045), rot=(4, 0, 0)), APRON, "body")


def crest(k):
    """The fan at the back of the head: a thick plate with a scalloped rim."""
    o = np.array([0, 0.03, 0.815])
    pts = [(0.0, -0.006), (0.012, 0.0)]
    n = 11
    for i in range(n + 1):
        a = math.radians(-5 + 100 * i / n)
        rr = 0.07 if i % 2 == 0 else 0.063
        pts.append((rr * math.cos(a) * 0.9 + 0.012, rr * math.sin(a)))
    pts.append((0.0, 0.012))
    k.add("crest", slab(pts, o, (0, 0.62, 0.78), (0, -0.78, 0.62), 0.03), WHITE, "head", fuse=False)


def beak(k, F):
    c = F.H + np.array([0, -0.06, -0.012])
    d = unit(0, -1, -0.16)
    pts = [c + d * t for t in np.linspace(0, 0.205, 6)]
    k.add("beak", blade(pts, [0.024, 0.02, 0.016, 0.012, 0.009, 0.006], (0, 0, 1), 0.75), RED, "head", fuse=False,
          surface="shell")
    k.add("beaktip", ell(c + d * 0.188, (0.0105, 0.026, 0.0085), rot=(math.degrees(math.atan(0.16)), 0, 0)), PINK,
          "head", fuse=False, surface="shell")


def wings(k):
    k.add("wing", blade([(0.092, -0.01, 0.53), (0.118, 0.05, 0.45), (0.116, 0.115, 0.36), (0.094, 0.165, 0.29)],
                        [0.04, 0.056, 0.046, 0.022], (1, 0, 0), 0.42), WHITE, "wing{s}", mirror=True)
    for j, (b, t, w) in enumerate((((0.112, 0.1, 0.38), (0.11, 0.25, 0.2), 0.058),
                                   ((0.104, 0.13, 0.36), (0.085, 0.27, 0.15), 0.054),
                                   ((0.118, 0.07, 0.41), (0.13, 0.2, 0.23), 0.056),
                                   ((0.096, 0.15, 0.34), (0.06, 0.25, 0.12), 0.05))):
        k.add(f"primary{j}", spike(np.array(b), np.array(t), w * 1.35, 0.03, side=(0.85, 0, 0.5), at=0.45), BLACK,
              "wing{s}", fuse=False, mirror=True)


def tail(k):
    for j, x in enumerate((-0.03, 0.0, 0.03)):
        b = np.array([x, 0.12, 0.33])
        k.add(f"tail{j}", spike(b, b + unit(x * 2, 1, -0.6) * 0.1, 0.05, 0.022, side=(1, 0, 0.2)), WHITE, "tail",
              fuse=False)


def smug_eye(u, v):
    c = np.tile(srgb(WHITE), (len(u), 1))
    lash = 0.32 - 0.2 * u * u
    eye = (u / 0.92) ** 2 + ((v + 0.08) / 0.62) ** 2 < 1
    c[eye & (v < lash)] = srgb(BLACK)
    c[(np.abs(u) < 0.98) & (np.abs(v - lash) < 0.16)] = srgb(BLACK)
    c[np.hypot(u + 0.25, v + 0.05) < 0.16] = srgb("#FFFFFF")
    return c


def eyes(k, F):
    E = on_ell(F.H, F.HR, F.EYEDIR)
    def lay(state):
        k.decal(E, F.EYEDIR, F.EYE, eye_look(state, smug_eye, srgb(WHITE), srgb(BLACK), line=0.18), eps=0.003,
                rings=20, segs=56, mirror=True)
    return lay


def bombirdier(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    apron(k, F)
    crest(k)
    beak(k, F)
    wings(k)
    tail(k)
    r.legs(k, F, foot=RED, claw=CLAW, clawlen=2.2)
    k.build(voxel=F.VOXEL, band=0.008, wband=0.06)
    k.eye_states(eyes(k, F))
    k.export(out_path)


if __name__ == "__main__":
    bombirdier(*args())
