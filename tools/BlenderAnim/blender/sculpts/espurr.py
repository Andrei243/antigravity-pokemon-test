# Espurr sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/espurr.py -- <the export's Espurr.glb> <out.glb>
# From the references: a small grey-lavender kitten that is mostly head. A great round skull, about half its height
# and twice its body's width, with a flat face; on each top corner a big rounded ear folded over and down, white
# showing under the fold; three pointed tufts of fur fanning out under each ear over the cheeks; a low crown of
# curls on top. Huge round eyes set wide apart, each a broad pink-lilac ring round a violet iris and a dark pupil
# with a white glint; a dot of a nose and a small frowning mouth. Under the head a jagged collar of fur, a small
# pear of a body flaring at its hem into a point at each hip, tiny arms hanging at its sides with white tips, two
# stubby legs with white toes, and a short curled tail, darker, behind on the right.
#
# Written to be grown from (meowstic.py takes this frame): the line's colours, the eye, the legs, the collar and the
# tail are the functions below, each given the kit and its frame; Espurr's own calls are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, args

# --- the line's colours ---------------------------------------------------------------------------------------------
GREY = "#A3A0C6"           # the coat: between the drawing's grey-violet and the 3D model's pale lavender
DARK = "#716D99"           # the tail
WHITE = "#F3F2F9"
RING = "#E3A2E2"           # the eye's broad pink-lilac ring
IRIS, PUPIL = "#6A4DBE", "#25195A"
RIM = "#4D3B72"
NOSE, INK = "#2E2840", "#2E2840"


def along(P, pts, n=200, absx=True):
    """Nearest place on a polyline (|x| taken for a mirrored part's both sides): its share of the length and the offset."""
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]); L = cum[-1]
    s = np.linspace(0, L, n)
    line = np.stack([np.interp(s, cum, pts[:, i]) for i in range(3)], axis=1)
    Q = P.copy()
    if absx:
        Q[:, 0] = np.abs(Q[:, 0])
    j = np.concatenate([((Q[a:a + 4000, None, :] - line[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 4000)])
    return s[j] / L, Q - line[j]


def ring_eye(ring=RING, iris=IRIS, pupil=PUPIL, rim=RIM, iris_r=0.64, pupil_r=0.34, rim_from=0.86):
    """Espurr's eye: a dark rim, a broad pink ring, a violet iris, a dark pupil and a white glint up and out."""
    cr, ci, cp, cm = srgb(ring), srgb(iris), srgb(pupil), srgb(rim)
    def eye(u, v):
        r = np.hypot(u, v)
        c = np.tile(cr, (len(u), 1))
        c[r > rim_from] = cm
        c[np.hypot(u - 0.02, v + 0.02) < iris_r] = ci
        c[np.hypot(u - 0.02, v + 0.02) < pupil_r] = cp
        c[np.hypot((u - 0.26) / 0.2, (v - 0.27) / 0.2) < 1] = (1, 1, 1)
        return c
    return eye


def legs(k, x, top, foot_y, color, toe=WHITE, r=0.017, bone="leg{s}"):
    """Stubby legs: a short thick shin from under the body to a wide round foot, its toes white."""
    k.add("leg", tube([(x - 0.002, foot_y + 0.004, top), (x, foot_y + 0.002, top * 0.5), (x + 0.001, foot_y, r * 0.9)],
                      [r, r * 0.95, r * 0.9]), color, bone, mirror=True)
    k.add("foot", ell((x + 0.002, foot_y - 0.006, r * 0.62), (r * 1.08, r * 1.3, r * 0.7)), color, bone, mirror=True)
    fy, fx = foot_y - 0.006, x + 0.002
    def toes(P):
        front = inside(P[:, 1] - (fy - r * 0.55), 0.0012)
        return (inside(P[:, 2] - r * 0.95, 0.0015) * front * inside(np.abs(np.abs(P[:, 0]) - fx) - r * 1.2, 0.002)
                * inside(P[:, 1] - fy + 0.2, 0.001))
    k.paint(toes, toe)


def collar(k, c, rx, ry, z, n, length, r, color, bone="body", front_only=False, droop=0.55):
    """A jagged collar of fur round the neck: pointed tufts leaning out and down, deepest in front."""
    for i in range(n):
        a = -math.pi + 2 * math.pi * (i + 0.5) / n
        if front_only and math.cos(a) < -0.2:
            continue
        base = np.array((c[0] + rx * math.sin(a), c[1] - ry * math.cos(a), z))
        d = unit(0.9 * math.sin(a), -0.9 * math.cos(a), -droop)
        ln = length * (0.75 + 0.25 * max(math.cos(a), 0))
        k.add(f"collar{i}", blade([base, base + d * ln * 0.5, base + d * ln], [r, r * 0.6, r * 0.12],
                                  (0, 0, 1), 0.5), color, bone)


def curled_tail(k, pts, radii, color, bone="tail", name="tail"):
    k.add(name, tube(pts, radii, seg=14), color, bone)


def espurr(src, out):
    k = Kit(src, coat="fur")

    # --- the head: a great round skull, a tenth larger than measured, its face flattened for the eyes ---------------
    H = np.array([0.0, -0.012, 0.188])
    HR = np.array([0.1, 0.088, 0.094])
    def skull(V):
        V = V.copy()
        V[:, 1] = np.where(V[:, 1] < 0, V[:, 1] * 0.86, V[:, 1])          # a flat face
        V[:, 0] *= 1 + 0.05 * np.clip(-V[:, 2], 0, 1)                      # full cheeks low down
        return V
    k.add("head", ell(H, HR, deform=skull), GREY, "head")
    k.add("neck", tube([(0, 0.0, 0.08), (0, -0.006, 0.11)], [0.03, 0.034]), GREY, "head")

    # The ears: big rounded lobes on the skull's top corners, folded over and down, white showing under the fold
    # One lobe leaning out from the skull's top corner and rolling down over its outer side
    k.add("ear", ell((0.088, -0.006, 0.228), (0.056, 0.046, 0.046), rot=(0, -38, 0)), GREY, "ear{s}", mirror=True)
    k.add("earfold", ell((0.108, -0.01, 0.208), (0.04, 0.04, 0.032), rot=(0, -50, 0)), GREY, "ear{s}", mirror=True)
    k.add("earwhite", blade([(0.096, -0.034, 0.188), (0.118, -0.032, 0.18), (0.138, -0.026, 0.181)],
                            [0.013, 0.012, 0.006], (0, 0.4, 1), 0.5), WHITE, "ear{s}", mirror=True)

    # Three pointed tufts under each ear, fanning out and down over the cheeks
    for i, (z, ln, dz) in enumerate(((0.168, 0.038, -0.25), (0.14, 0.036, -0.5), (0.114, 0.03, -0.8))):
        base = np.array((0.08 - 0.008 * i, -0.012, z))
        d = unit(1, 0.05, dz)
        k.add(f"tuft{i}", blade([base, base + d * ln * 0.55, base + d * ln], [0.017, 0.011, 0.0035], (0, 1, 0), 0.5),
              GREY, "head", mirror=True)

    # A low crown of curls: a crescent rising from the middle and curling over to the right, a smaller one beside it
    k.add("curl", tube([(0, -0.03, 0.27), (0.004, -0.032, 0.292), (0.018, -0.026, 0.303), (0.03, -0.016, 0.298),
                        (0.034, -0.01, 0.287)], [0.014, 0.013, 0.011, 0.008, 0.005]), GREY, "head")
    k.add("curl2", tube([(-0.02, -0.02, 0.27), (-0.028, -0.024, 0.286), (-0.04, -0.018, 0.29)],
                        [0.011, 0.009, 0.005]), GREY, "head")
    k.add("curl3", tube([(0.012, 0.02, 0.27), (0.022, 0.024, 0.288), (0.034, 0.03, 0.29)],
                        [0.011, 0.009, 0.005]), GREY, "head")

    # --- the body: a small pear under the head, flaring at its hem into a point at each hip ---------------------------
    def pear(V):
        V = V.copy()
        V[:, :2] *= (1 + 0.18 * np.clip(-V[:, 2], 0, 1))[:, None]          # wider at the hem
        return V
    k.add("torso", ell((0, 0.004, 0.055), (0.04, 0.034, 0.056), deform=pear), GREY, "body")
    for i, (x, z, d) in enumerate(((0.04, 0.03, unit(1, -0.2, -0.45)),)):
        base = np.array((x, 0.0, z))
        k.add("hem", blade([base, base + d * 0.016, base + d * 0.03], [0.014, 0.009, 0.003], (0, 1, 0), 0.5),
              GREY, "body", mirror=True)
    collar(k, (0, -0.002), 0.03, 0.03, 0.094, 9, 0.026, 0.011, GREY, droop=0.9)

    # Tiny arms hanging at the sides, a little forward, white at their pointed tips
    ARM = [(0.034, -0.008, 0.085), (0.045, -0.018, 0.066), (0.048, -0.026, 0.047)]
    k.add("arm", tube(ARM, [0.012, 0.011, 0.009]), GREY, "body", mirror=True)
    k.add("hand", blade([(0.048, -0.026, 0.048), (0.05, -0.03, 0.04), (0.051, -0.032, 0.033)], [0.0095, 0.007, 0.003],
                        (1, 0, 0), 0.6), WHITE, "body", mirror=True)

    legs(k, 0.024, 0.04, -0.004, GREY)

    # The tail: short, darker, curling up behind on the right (+X, the viewer's right in front)
    curled_tail(k, [(0.012, 0.03, 0.04), (0.04, 0.05, 0.036), (0.064, 0.056, 0.05), (0.072, 0.05, 0.072),
                    (0.062, 0.042, 0.086)], [0.011, 0.012, 0.011, 0.009, 0.004], DARK)

    # --- the face: a dot of a nose and a small frowning mouth ---------------------------------------------------------
    k.decal((0, -0.09, 0.148), unit(0, -1, 0.05), (0.0045, 0.0038), NOSE, eps=0.0018, rings=5, segs=20)
    CG, CI = srgb(GREY), srgb(INK)
    def mouth(u, v):
        c = np.tile(CG, (len(u), 1))
        c[(np.abs(v - (0.25 - 0.9 * u * u)) < 0.3) & (np.abs(u) < 0.85)] = CI
        return c
    k.decal((0, -0.088, 0.137), unit(0, -1, -0.05), (0.008, 0.0045), mouth, eps=0.0018, rings=8, segs=40)

    k.build(voxel=0.0029, band=0.005, wband=0.022)

    de = unit(0.5, -0.84, -0.2)
    E = np.array((0.046, -0.08, 0.164))
    look = ring_eye()
    def eyes(state):
        k.decal(E, de, (0.021, 0.022), eye_look(state, look, srgb(GREY), srgb(INK)), eps=0.002, rings=20, segs=56,
                mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    src, dest = args()
    espurr(src, dest)
