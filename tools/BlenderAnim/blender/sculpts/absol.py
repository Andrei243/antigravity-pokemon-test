# Absol sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/absol.py -- <the export's Absol.glb> <out.glb>
# A quadruped in shaggy white fur on long legs: a white hood of fur over the head, out of which looks a dark slate-blue
# face with red eyes under a white brow; a dark oval mark on the forehead; from the head's -X side (the viewer's left
# when the face looks at the viewer, as in both pictures) a great dark scythe of a horn rising and arching over the top
# toward +X; a long white lock falling down the +X side of the face; a deep ruff of fur round the neck and down the chest, spiky at its edges; a dark flat scythe of a tail
# rising up and back, swung out to +X, a spur on its back edge; three dark claws on each paw and a dark spur at the back
# of each leg.
# The export's joints: body (0, 0.04, 0.4), head (0, -0.2, 0.55), tail (0, 0.26, 0.44), legs (+-0.08, -0.12, 0.32),
# hinds (+-0.08, 0.18, 0.32); no ear bones.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, unit, eye_look, polygon, lumpy, args

src, out = args()
k = Kit(src, coat="fur")

# --- colours ----------------------------------------------------------------------------------------------------------
WHITE = "#ECEFF6"          # the coat: white with a cool cast, kept under pure white
SLATE = "#3A548C"          # the horn, the tail, the claws: darker than the face
FACE = "#6079B0"           # the face: the art's medium slate (the first, darker face read as a hole in the game)
MARK = "#34508A"           # the forehead's oval
RED, PUPIL, SCLERA = "#E2283A", "#1A0A10", "#F4F2EE"
INK = "#141A2C"            # the lids' edge, the nose, the mouth
LINE = "#1E2A48"           # the closed eyes' line: dark on the medium slate face

# --- the frame --------------------------------------------------------------------------------------------------------
H = np.array([0, -0.25, 0.64])          # the head's centre, forward of and over its joint
HR = np.array([0.12, 0.126, 0.138])      # head height / height without the horn some 0.36 (the 3D model's 0.38)
LEG_X = 0.086
FORE_Y, HIND_Y = -0.12, 0.18
BODY_Z = 0.405


def add_body():
    # A deep trunk: as deep as the leg showing under it is long
    # (deepened and filled out after the first look in the game: the body behind the ruff was thin against the hood)
    k.add("chest", ell((0, -0.1, BODY_Z - 0.01), (0.108, 0.112, 0.15)), WHITE, "body")
    k.add("middle", ell((0, 0.04, BODY_Z - 0.008), (0.104, 0.155, 0.138)), WHITE, "body")
    k.add("rump", ell((0, 0.172, BODY_Z - 0.008), (0.118, 0.112, 0.15)), WHITE, "body")
    # A short thick neck from the chest up into the head, hidden under the ruff
    k.add("neck", tube([(0, -0.12, 0.47), (0, -0.18, 0.54), (0, -0.23, 0.6)], [0.08, 0.076, 0.072]), WHITE, "head")


def add_legs():
    # Broad shoulders and thighs fused over the upper half of each leg, the lower leg tapering to a clear paw
    # (the lower legs thickened by some 40 percent and the paws by half again after the game's shot: strong forearms
    # and shins widening into large paws, as the art's)
    k.add("shoulder", ell((LEG_X, FORE_Y + 0.004, 0.33), (0.062, 0.076, 0.135)), WHITE, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X, FORE_Y, 0.33), (LEG_X + 0.002, FORE_Y - 0.002, 0.17), (LEG_X + 0.004, FORE_Y - 0.008, 0.06)],
                      [0.062, 0.054, 0.052]), WHITE, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.005, FORE_Y - 0.03, 0.036), (0.064, 0.08, 0.04)), WHITE, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X + 0.004, HIND_Y + 0.008, 0.33), (0.07, 0.102, 0.15)), WHITE, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X, HIND_Y + 0.01, 0.3), (LEG_X + 0.003, HIND_Y + 0.04, 0.18),
                        (LEG_X + 0.004, HIND_Y + 0.03, 0.1), (LEG_X + 0.004, HIND_Y + 0.01, 0.06)],
                       [0.062, 0.054, 0.05, 0.05]), WHITE, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.005, HIND_Y - 0.016, 0.036), (0.064, 0.08, 0.04)), WHITE, "hind{s}", mirror=True)
    # Tufts of fur at the back of the elbows and the hocks
    for nm, bone, at, d in (("elbowtuft", "leg{s}", (LEG_X, FORE_Y + 0.03, 0.24), unit(0.2, 1, -0.6)),
                            ("hocktuft", "hind{s}", (LEG_X + 0.002, HIND_Y + 0.07, 0.2), unit(0.2, 1, -0.3))):
        a = np.array(at)
        k.add(nm, blade([a, a + d * 0.03, a + d * 0.05], [0.026, 0.016, 0.003], (1, 0, 0), 0.55), WHITE, bone,
              mirror=True)
    # Big dark claws, three to a paw: stout cones out of the paw's front, curving forward and down to the ground
    for bone, y in (("leg{s}", FORE_Y - 0.098), ("hind{s}", HIND_Y - 0.084)):
        for j, dx in enumerate((-0.036, 0.0, 0.036)):
            a = np.array([LEG_X + 0.005 + dx, y + 0.012, 0.042])
            k.add(f"claw{bone[0]}{j}", blade([a, a + [dx * 0.3, -0.03, 0.0], a + [dx * 0.45, -0.055, -0.03]],
                                             [0.022, 0.016, 0.003], (1, 0, 0), 0.75), SLATE, bone, fuse=False,
                  mirror=True, surface="shell")
    # A dark spur at the back of each lower leg
    for bone, at in (("leg{s}", (LEG_X + 0.004, FORE_Y + 0.04, 0.1)), ("hind{s}", (LEG_X + 0.004, HIND_Y + 0.075, 0.15))):
        a = np.array(at)
        k.add(f"spur{bone[0]}", blade([a, a + [0, 0.03, 0.018], a + [0, 0.055, 0.045]], [0.02, 0.013, 0.002],
                                      (1, 0, 0), 0.6), SLATE, bone, fuse=False, mirror=True, surface="shell")


# --- the ruff: one deep mass of white fur round the neck and down the chest, its edge in broad points ----------------
def add_ruff():
    k.add("ruff", ell((0, -0.205, 0.445), (0.118, 0.088, 0.13), deform=lumpy(6, 3, 0.05)), WHITE, "body")
    k.add("ruffcollar", ell((0, -0.15, 0.53), (0.104, 0.085, 0.08), deform=lumpy(5, 2, 0.05)), WHITE, "body")
    # Broad points hanging down the chest, the middle ones longest
    for i, a in enumerate(np.linspace(-1.1, 1.1, 5)):
        base = np.array((0.085 * math.sin(a), -0.205 - 0.07 * math.cos(a), 0.36))
        d = unit(0.4 * math.sin(a), -0.25 * math.cos(a), -1.0)
        ln = 0.1 - 0.02 * abs(a)
        k.add(f"rufftuft{i}", blade([base, base + d * ln * 0.55, base + d * ln], [0.036, 0.022, 0.003],
                                    (math.sin(a), -math.cos(a), 0.0), 0.5), WHITE, "body")
    # Points flaring out at each side of the neck and shoulder
    for j, (z, y, d) in enumerate(((0.52, -0.2, unit(1, -0.2, 0.25)), (0.46, -0.21, unit(1, -0.25, -0.15)),
                                   (0.39, -0.19, unit(1, -0.2, -0.55)))):
        base = np.array((0.105, y, z))
        k.add(f"ruffside{j}", blade([base, base + d * 0.03, base + d * 0.055], [0.032, 0.019, 0.003], (0, 1, 0), 0.5),
              WHITE, "body", mirror=True)


# --- the head: a white hood, the dark face looking out of it -----------------------------------------------------------
def add_head():
    k.add("head", ell(H, HR), WHITE, "head")
    # The face standing forward in the hood's opening, and a short cat's muzzle under it
    # Both white: the face's dark is paint with a crisp edge (a dark part blended into the white brow and ragged it)
    k.add("face", ell(H + (0, -0.045, -0.035), (0.09, 0.094, 0.09)), WHITE, "head")
    k.add("muzzle", ell(H + (0, -0.12, -0.078), (0.04, 0.038, 0.034)), WHITE, "head")
    # Spiky white fur at the -X cheek, under the horn's root
    for j, (z, d) in enumerate(((0.63, unit(-1, -0.15, 0.2)), (0.585, unit(-1, -0.2, -0.25)), (0.54, unit(-0.8, -0.2, -0.7)))):
        base = np.array((-0.095, -0.29, z))
        k.add(f"cheektuft{j}", blade([base, base + d * 0.025, base + d * 0.048], [0.026, 0.016, 0.003], (0, 1, 0), 0.5),
              WHITE, "head")


def add_face_paint():
    def face(P):
        Q = P - H
        r = np.hypot(Q[:, 0], Q[:, 1])
        a = np.abs(np.arctan2(Q[:, 0], -Q[:, 1]))
        ztop = 0.03 + 0.04 * np.minimum(a, 0.5)
        return (inside((a - 0.92) * r, 0.0025) * inside(Q[:, 2] - ztop, 0.0025) * inside(Q[:, 1] + 0.035, 0.0025)
                * inside(-0.13 - Q[:, 2], 0.003) * (np.linalg.norm(Q, axis=1) < 0.21))
    k.paint(face, FACE)
    # The brow: the paint's top edge came out ragged where the surface runs nearly along it, so the hood's edge over
    # the eyes is a white patch whose lower outline is the crisp line, dipping a little between the eyes
    n = unit(0, -1, 0.3)
    k.decal(H + (0, -0.13, 0.04), n, (0.104, 0.04), WHITE, eps=0.004, rings=12, segs=72,
            outline=polygon([(-1, 1), (1, 1), (1, -0.25), (0.55, -0.45), (0, -0.7), (-0.55, -0.45), (-1, -0.25)]))


# --- the lock: a long white lock falling down the +X side of the face, a loose piece so its edge stays crisp ----
def add_lock():
    pts = [(0.035, -0.27, 0.765), (0.1, -0.31, 0.725), (0.13, -0.335, 0.64), (0.126, -0.34, 0.55),
           (0.104, -0.32, 0.465)]
    k.add("lock", blade(pts, [0.04, 0.056, 0.054, 0.036, 0.004], unit(1, -0.45, 0), 0.5), WHITE, "head", fuse=False)


# --- flat plates along a curve: the horn and the tail ----------------------------------------------------------------
def plate(origin, e1, e2, curve, wf, wb, thick, nu=40, nv=6):
    """A flat strip in the plane of e1, e2: curve(u) gives (a, b) of its middle line for u in 0..1; wf(u), wb(u) its
    half-widths to the front side and the back side of that line (the front side is where cross(n, tangent) points)."""
    e1, e2 = np.array(e1, float), np.array(e2, float)
    n = np.cross(e1, e2); n /= np.linalg.norm(n)
    origin = np.array(origin, float)

    def at(u, v):
        u = 0.015 + 0.97 * u
        ab = curve(u)                                   # (2, nu, nv)
        c = origin + ab[0][..., None] * e1 + ab[1][..., None] * e2
        eps = 1e-3
        ab2 = curve(u + eps)
        t = (ab2[0] - ab[0])[..., None] * e1 + (ab2[1] - ab[1])[..., None] * e2
        t /= np.linalg.norm(t, axis=-1, keepdims=True)
        p = np.cross(n, t)
        return c + p * (-wb(u) + v * (wf(u) + wb(u)))[..., None]
    return sheet(at, nu, nv, thick)


# The horn: a crescent from the hood's side on -X, rising and arching over toward +X, in a plane
# tilted back some 21 degrees so its face shows from the front three-quarter and its back from behind
HORN_E1 = np.array([-1.0, 0, 0])
HORN_E2 = unit(0, 0.36, 0.93)
HORN_C = np.array([0.05, -0.245, 0.735])      # the arc's centre
HORN_R = 0.13


def add_horn():
    def curve(u):
        th = np.radians(-32 + 168 * u)
        return np.array([HORN_R * np.cos(th), HORN_R * np.sin(th)])
    w = lambda u: np.interp(u, [0, 0.15, 0.45, 0.75, 1], [0.016, 0.026, 0.03, 0.02, 0.002])
    k.add("horn", plate(HORN_C, HORN_E1, HORN_E2, curve, w, w, 0.02), SLATE, "head", fuse=False, surface="shell")


# The tail: a dark flat scythe from the rump, rising up and back, swung 35 degrees out to +X; its front edge
# straight, its back edge with a spur near the root
TAIL_ROOT = np.array([0.0, 0.245, 0.45])
TAIL_H = unit(0.57, 0.82, 0)
UP = np.array([0, 0, 1.0])


def add_tail():
    def curve(u):
        # back and up in a gentle curve, the tip turning up
        return np.array([0.3 * u - 0.04 * u ** 2, 0.18 * u + 0.08 * u ** 2])
    wf = lambda u: np.interp(u, [0, 0.3, 0.7, 1], [0.014, 0.022, 0.018, 0.002])
    wb = lambda u: np.interp(u, [0, 0.12, 0.22, 0.3, 0.55, 0.85, 1], [0.014, 0.036, 0.058, 0.034, 0.05, 0.026, 0.002])
    k.add("tail", plate(TAIL_ROOT, TAIL_H, UP, curve, wf, wb, 0.018), SLATE, "tail", fuse=False, surface="shell")
    k.add("tailroot", ell(TAIL_ROOT + (0, -0.01, 0.0), (0.03, 0.03, 0.03)), WHITE, "tail")


# --- the forehead's mark: an upright dark oval on the hood over the face ----------------------------------------------
def add_mark():
    n = unit(0, -0.72, 0.69)
    k.decal(H + n * 0.13, n, (0.017, 0.03), MARK, eps=0.0065, rings=12, segs=48)


# --- the face's nose and mouth ----------------------------------------------------------------------------------------
def add_nose():
    k.decal(H + (0, -0.15, -0.062), unit(0, -1, 0.15), (0.012, 0.008), INK, eps=0.003,
            outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]), rings=6, segs=24)


def add_mouth():
    CS, CI = srgb(FACE), srgb(INK)

    def mouth(u, v):
        c = np.tile(CS, (len(u), 1))
        c[(np.abs(v - (-0.1 + 0.9 * (np.abs(u) - 0.4) ** 2)) < 0.3) & (np.abs(u) < 0.85)] = CI
        return c
    k.decal(H + (0, -0.145, -0.095), unit(0, -1, -0.3), (0.02, 0.011), mouth, eps=0.003, rings=12, segs=60)


# --- the eyes: red irises with a dark pupil in a white, the top cut straight by the lid, slanting down to the nose ----
HE = 0.037                  # 40 percent larger after the game's shot, where the eyes were sparks
EYE_DIR = unit(0.45, -0.88, 0.08)
E = np.array([0.053, -0.37, 0.631])


def eye_top(u):
    return 0.22 + 0.42 * u


def clipped(n=96):
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        du, dv = math.cos(th), math.sin(th)
        t = 1.0
        if dv > eye_top(du):
            lo, hi = 0.0, 1.0
            for _ in range(40):
                t = (lo + hi) / 2
                if t * dv > eye_top(t * du):
                    hi = t
                else:
                    lo = t
            t = lo
        pts.append((t * math.cos(th), t * math.sin(th)))
    return pts


OUT = clipped()
CR, CP, CW, CI = srgb(RED), srgb(PUPIL), srgb(SCLERA), srgb(INK)


def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    r = np.sqrt(((u + 0.14) / 0.64) ** 2 + ((v + 0.06) / 0.76) ** 2)
    c[r < 1] = CR
    c[r < 0.42] = CP
    c[((u - 0.08) / 0.16) ** 2 + ((v - 0.12) / 0.14) ** 2 < 1] = (1, 1, 1)
    c[v > eye_top(u) - 0.18] = CI                  # the lid's edge, the eye's own crisp top
    return c


def eyes(state):
    look = eye_look(state, eye, srgb(FACE), srgb(LINE), line=0.13)
    for side in (1, -1):
        n = EYE_DIR * np.array([side, 1, 1.0])
        outline = polygon([(side * a, b) for a, b in OUT]) if state in ("Open", "Fierce") else None
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(E * np.array([side, 1, 1.0]), n, (HE, HE * 0.85), col, eps=0.005, outline=outline, rings=20, segs=56)


add_body()
add_legs()
add_ruff()
add_head()
add_face_paint()
add_lock()
add_horn()
add_tail()
add_mark()
add_nose()
add_mouth()

k.build(voxel=0.0064, smooth=3)
k.eye_states(eyes)
k.export(out)
