# Lapras sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lapras.py -- <the export's Lapras.glb> <out.glb>
#
# From the references: a gentle blue plesiosaur. A broad low body carrying a grey dome of shell on its back, its rim a
# raised lip and low rounded knobs over it (grey in the art; lilac in the 3D picture: the art is followed); four
# broad flippers, blue with cream along their trailing edges and undersides; a long thick neck rising from the front of
# the body in a gentle S, cream down its front; a large head with a rounded snout, the lower jaw cream to a long smile,
# a small horn on the forehead, a curled ear on each side at the back of the head, and calm eyes, a brown iris under a
# heavy lid. A few dark blue spots on the cheek, the neck and the body's sides.
# Reference ratios (3D picture): head with ears 0.29 of the height, the body with its shell 0.51, the shell 0.54 long;
# the flippers spread about 0.8 of the height across.
# The export's joints: body (0, 0.04, 0.28), neck (0, -0.2, 0.36), head (0, -0.31, 0.79), legs (+-0.15, -0.16, 0.18),
# hind (+-0.15, 0.27, 0.18).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, slab, outward, args

src, out = args()
k = Kit(src, coat="scales")

BLUE, CREAM, SHELL, SPOT = "#5AAEE6", "#F2E6C2", "#A8A6AE", "#2E6CB4"
WHITE, IRIS, PUPIL, INK = "#FAF8F2", "#7A5236", "#1E1410", "#1C2A40"
H = np.array([0.0, -0.32, 0.815])       # the head's centre
HR = np.array([0.108, 0.135, 0.102])


def flat_bottom(V):
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.75, V[:, 2])
    return V


def dome(V):
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.3, V[:, 2])
    return V


# --- the body ---------------------------------------------------------------------------------------------------------
k.add("body", ell((0, 0.06, 0.2), (0.285, 0.32, 0.175), deform=flat_bottom), BLUE, "body")
k.add("chest", ell((0, -0.15, 0.24), (0.2, 0.15, 0.16)), BLUE, "body")

# --- the neck: thick, rising in a gentle S from the front of the body ------------------------------------------------
NECK = np.array([(0, -0.19, 0.3), (0, -0.255, 0.42), (0, -0.245, 0.54), (0, -0.245, 0.65), (0, -0.285, 0.75)])
NRAD = [0.14, 0.1, 0.082, 0.075, 0.074]
k.add("neck", tube(NECK, NRAD), BLUE, "neck")

# --- the head: large, a rounded snout, a small horn, curled ears ------------------------------------------------------
k.add("head", ell(H, HR), BLUE, "head")
k.add("snout", ell((0, -0.425, 0.79), (0.072, 0.085, 0.064), rot=(-8, 0, 0)), BLUE, "head")
k.add("horn", tube([(0, -0.37, 0.88), (0, -0.39, 0.925), (0, -0.405, 0.955)], [0.024, 0.014, 0.004]), BLUE, "head")
EC = np.array([0.088, -0.255, 0.885])
k.add("earbun", ell(EC, (0.035, 0.055, 0.055)), BLUE, "head", mirror=True)
# the curl: a thick coil spiralling in on the bun's outer face
th = np.linspace(0, 1.6 * math.pi, 14)
rr = 0.046 - 0.026 * th / th[-1]
coil = [EC + np.array([0.03 + 0.008 * (1 - t / th[-1]), -r * math.sin(t + 0.4), r * math.cos(t + 0.4)])
        for t, r in zip(th, rr)]
k.add("earcoil", tube(coil, np.linspace(0.016, 0.01, len(coil))), BLUE, "head", mirror=True)

# --- the shell: a grey dome with a raised lip and low rounded knobs -------------------------------------------------
SC = np.array([0.0, 0.11, 0.25])
SR = np.array([0.275, 0.285, 0.2])
k.add("shell", ell(SC, SR, deform=dome), SHELL, "body", surface="shell")
ang = np.linspace(0, 2 * math.pi, 41)
lip = [SC + np.array([SR[0] * 0.975 * math.sin(a), SR[1] * 0.975 * -math.cos(a), 0.012]) for a in ang]
k.add("shelllip", tube(lip, [0.016] * len(lip), seg=12, per=3), SHELL, "body", surface="shell")
KNOBS = [(0, 0, 1), (0.55, 0, 0.83), (-0.55, 0, 0.83), (0.4, -0.55, 0.73), (-0.4, -0.55, 0.73), (0.4, 0.55, 0.73),
         (-0.4, 0.55, 0.73), (0, 0.7, 0.7), (0, -0.62, 0.78), (0.82, 0.25, 0.5), (-0.82, 0.25, 0.5)]
for i, d in enumerate(KNOBS):
    d = unit(*d)
    p = SC + d * SR
    n = d / SR; n /= np.linalg.norm(n)
    k.add(f"knob{i}", tube([p - n * 0.02, p + n * 0.025, p + n * 0.05], [0.045, 0.032, 0.014], seg=14, per=4),
          SHELL, "body", surface="shell")

# --- the cream: under the body, down the neck's front, the lower jaw to a long smile --------------------------------
NC = []
for i in range(len(NECK) - 1):
    for t in np.linspace(0, 1, 20, endpoint=False):
        NC.append(NECK[i] * (1 - t) + NECK[i + 1] * t)
NC.append(NECK[-1])
NC = np.array(NC)
NT = np.gradient(NC, axis=0); NT /= np.linalg.norm(NT, axis=1, keepdims=True)


def throat(P):
    j = ((P[:, None, :] - NC[None]) ** 2).sum(2).argmin(1)
    rad = P - NC[j]
    rad -= NT[j] * (rad * NT[j]).sum(1, keepdims=True)
    rad /= np.linalg.norm(rad, axis=1, keepdims=True) + 1e-9
    front = np.array([0, -1.0, 0])[None] - NT[j] * (NT[j] @ np.array([0, -1.0, 0]))[:, None]
    front /= np.linalg.norm(front, axis=1, keepdims=True)
    a = np.arccos(np.clip((rad * front).sum(1), -1, 1))
    return inside(a - 0.5, 0.05) * (P[:, 2] > 0.2) * (P[:, 2] < 0.77) * (P[:, 1] < -0.12)


k.paint(throat, CREAM)
k.paint(lambda P: inside(P[:, 2] - (0.765 + 0.22 * (P[:, 1] + 0.45)), 0.003) * (P[:, 1] < -0.27) * (P[:, 2] > 0.62),
        CREAM)
k.paint(lambda P: inside(P[:, 2] - 0.075, 0.003) * (P[:, 2] < 0.2), CREAM)

# --- flippers: long paddles, broad at the body, curving back and tapering to a rounded point, arched down to the tip;
# blue on top, a cream paddle a little wider under each showing as a thin edge -------------------------------------
def paddle(o, ex, ey, L, w, th, sweep, droop, grow=1.0, thin=1.0, drop=0.0, back=0.0):
    """ex along the paddle (level), ey across toward its trailing side, up +z: a loose rounded paddle."""
    V, F = ell((0, 0, 0), (1, 1, 1), sub=4)
    t = np.clip((V[:, 0] + 1) / 2, 0, 1)
    wt = w * grow * (0.8 + 0.35 * np.sin(np.pi * np.minimum(t / 0.7, 1) * 0.5)) * np.sqrt(np.clip(1 - t ** 2.2, 0, 1))
    tt = th * thin * (1 - 0.65 * t) * np.sqrt(np.clip(1 - t ** 2.2, 0, 1)) + 0.002
    a = t * L
    b = V[:, 1] * wt + sweep * L * t ** 2 + back
    c = V[:, 2] * tt - droop * t ** 2 - drop
    P = np.array(o)[None] + a[:, None] * ex[None] + b[:, None] * ey[None] + c[:, None] * np.array([0, 0, 1.0])[None]
    return outward((P, F))


for name, root, d, L, w, bone in (("fore", (0.12, -0.13, 0.11), (0.85, -0.5, 0), 0.26, 0.085, "leg{s}"),
                                  ("hind", (0.12, 0.3, 0.1), (0.72, 0.7, 0), 0.22, 0.078, "hind{s}")):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        ex = unit(*d) * m
        ey = np.cross((0, 0, 1.0), ex); ey /= np.linalg.norm(ey)
        if ey[1] < 0:                       # across toward the back: the trailing side
            ey = -ey
        o = np.array(root) * m
        k.add(f"{name}{side}", paddle(o, ex, ey, L, w, 0.0175, 0.25, 0.08), BLUE, bone.format(s=side), fuse=False,
              surface="scales")
        k.add(f"{name}cream{side}", paddle(o, ex, ey, L * 0.99, w, 0.0175, 0.25, 0.08, grow=1.07, thin=0.6,
                                           drop=0.008, back=0.004), CREAM, bone.format(s=side), fuse=False,
              surface="scales")
        # the paddle's root, fused, so it grows out of the body
        k.add(f"{name}root{side}", blade([o - ex * 0.06 + np.array([0, 0, 0.02]), o + ex * 0.03, o + ex * 0.09],
                                         [0.06, 0.045, 0.026], (0, 0, 1), 0.5), BLUE, bone.format(s=side))

# --- spots: dark blue, on the cheeks, the neck and the body's sides ------------------------------------------------
for c, n, r in (((0.095, -0.255, 0.8), (1, 0.1, -0.1), 0.016),
                ((0.07, -0.27, 0.42), (1, -0.4, 0), 0.018),
                ((0.26, -0.08, 0.17), (1, -0.3, 0), 0.022), ((0.27, 0.02, 0.14), (1, 0, -0.1), 0.017)):
    k.decal(c, unit(*n), (r, r), SPOT, eps=0.003, rings=4, segs=24, mirror=True)

k.build(voxel=0.0075, band=0.004)

# --- the eyes: calm, a large brown iris under a heavy blue lid, a dark line along the lid --------------------------
CB, CW, CI, CP, CK = srgb(BLUE), srgb(WHITE), srgb(IRIS), srgb(PUPIL), srgb(INK)


def lid(u):
    return 0.28 - 0.08 * u


def eye(u, v):
    c = np.tile(CB, (len(u), 1))
    inn = (u * u + v * v < 0.92) & (v < lid(u))
    c[inn] = CW
    c[inn & (((u + 0.12) / 0.5) ** 2 + ((v + 0.05) / 0.62) ** 2 < 1)] = CI
    c[inn & (((u + 0.12) / 0.24) ** 2 + ((v + 0.08) / 0.32) ** 2 < 1)] = CP
    c[inn & (((u + 0.28) / 0.12) ** 2 + ((v - 0.08) / 0.12) ** 2 < 1)] = (1, 1, 1)
    c[(np.abs(v - lid(u)) < 0.13) & (u * u + v * v < 1.0)] = CK
    return c


de = unit(0.66, -0.68, 0.22)
E = H + np.array([0, -0.035, 0.0]) + de * 0.01


def eyes(state):
    k.decal(E, de, (0.042, 0.04), eye_look(state, eye, CB, CK, line=0.15), eps=0.005, rings=20, segs=56,
            mirror=True)


k.eye_states(eyes)
k.export(out)
