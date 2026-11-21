# Tapu Koko sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tapukoko.py -- <the export's Tapu Koko.glb> <out.glb>
#
# The four guardians share one frame, made here and imported by tapulele.py, tapubulu.py and tapufini.py (sibling):
# a small dark figure (a body, a head with a face of patches, thin arms with round hands) and the halves of a painted
# shell, each half a loose dome with a dark lining and a dark rim (half_shell), with patches that lie on the dome
# (dome_patch) and solid points (spike). Each guardian gives its own colours and its own shell.
#
# From the references, Tapu Koko: a small black figure; the head black with white stripes up its forehead and a white
# mask round fierce teal eyes, a small orange beak; a great crest of orange flame-tongues, jagged on their edges,
# rising up and back from the head (more to its +X side); below the waist long orange feathers hang like a skirt;
# yellow rings at the wrists. Each arm holds out a half of the shell like the head of a cockerel: a yellow dome, its
# outer face turned out and forward, a white band with a black zigzag down its front edge, a black hooked beak at its
# top front, a black point at its foot and a tuft of yellow feathers at its top back.
# The skeleton: root, body (0, 0, 0.306), head (0, -0.02, 0.366), armR/L (+-0.035, -0.02, 0.346).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, sheet, slab, leaf, hull, ring_band, outward, axes, srgb, inside, unit,
                       tri, eye_look, polygon, args)

INK = "#1A181E"


# --- the shared frame ------------------------------------------------------------------------------------------------
def spike(base, tip, r, n=6, flat=1.0, turn=0.0):
    """A solid point: a ring of n corners round base (squashed across by flat) and the tip; a flat-faced hull."""
    base, tip = np.array(base, float), np.array(tip, float)
    a, b = axes(tip - base)
    pts = [base + r * (math.cos(t + turn) * a + flat * math.sin(t + turn) * b)
           for t in np.linspace(0, 2 * math.pi, n, endpoint=False)]
    return hull(pts + [tip])


class Dome:
    """A half of a shell: centre c, o the way its outer face looks, f and w the axes of its rim's oval, r the radii
    along o, f and w. at(t, e, lift) is a point on its outer face: t round the rim from f toward w, e from the rim (0)
    up to the dome's top (pi / 2)."""
    def __init__(self, c, o, f, w, r):
        self.c = np.array(c, float)
        self.o, self.f, self.w = (np.array(v, float) / np.linalg.norm(v) for v in (o, f, w))
        self.r = np.array(r, float)

    def at(self, t, e, lift=0.0):
        a, b, cc = self.r[0] + lift, self.r[1] * 0.985 + lift, self.r[2] * 0.985 + lift
        t, e = np.asarray(t, float), np.asarray(e, float)
        ce = np.cos(e)[..., None]
        return (self.c + ce * (np.cos(t)[..., None] * b * self.f + np.sin(t)[..., None] * cc * self.w)
                + np.sin(e)[..., None] * a * self.o)

    def rim(self, t, out=0.0):
        """A point on the rim's oval, t as in at; out moves it out along o."""
        return self.c + math.cos(t) * self.r[1] * self.f + math.sin(t) * self.r[2] * self.w + out * self.o


def half_shell(k, name, D, outer, lining, rim, bone, surface="shell"):
    """The dome (outer colour), a thin lens of the rim's colour standing just past its outline and in front of its
    open face, and the lining a little inside that: from the open side a lining in a dark rim, from outside the dome."""
    def piece(out_scale, rim_scale, shift):
        V, F = ell((0, 0, 0), (1, 1, 1), sub=4)
        x = np.clip(V[:, 0], 0, None) * D.r[0] * out_scale + shift
        P = (D.c + np.outer(x, D.o) + np.outer(V[:, 1] * D.r[1] * rim_scale, D.f)
             + np.outer(V[:, 2] * D.r[2] * rim_scale, D.w))
        return outward((P, F))
    k.add(name, piece(1.0, 0.985, 0.0), outer, bone, fuse=False, surface=surface)
    k.add(name + "rim", piece(0.25, 1.0, -0.0016), rim, bone, fuse=False, surface=surface)
    k.add(name + "lining", piece(0.2, 0.9, -0.0032), lining, bone, fuse=False, surface=surface)


def dome_patch(k, name, D, t0, t1, e0, e1, colour, bone, lift=0.004, thick=0.004, nu=48, nv=4, surface="shell"):
    """A patch lying on a dome's outer face: t from t0 to t1, e from e0 to e1(u) (a number or a function of u in
    0..1, for a zigzag or a tapering edge)."""
    def at(u, v):
        t = t0 + (t1 - t0) * u
        top = e1(u) if callable(e1) else np.full_like(u, float(e1))
        e = e0 + (top - e0) * v
        return D.at(t, e, lift)
    k.add(name, outward(sheet(at, nu, nv, thick)), colour, bone, fuse=False, surface=surface)


def figure(k, F):
    """The small dark figure: its body, neck, head and arms. F: the guardian's frame and colours."""
    col = F["body"]
    for i, (c, r) in enumerate(F["torso"]):
        k.add(f"torso{i}", ell(c, r), col, "body")
    k.add("neck", tube([F["neck"][0], F["neck"][1]], [F["neck_r"], F["neck_r"]]), col, "head")
    k.add("head", ell(F["head_c"], F["head_r"]), F.get("face", col), "head", surface=F.get("face_surface"))
    S, E, H = (np.array(p, float) for p in F["arm"])
    ar = F["arm_r"]
    k.add("arm", tube([S, E, H], [ar, ar, ar * 0.95]), col, "arm{s}", mirror=True)
    k.add("hand", ell(H, (F["hand_r"],) * 3), col, "arm{s}", mirror=True)


def eye_lay(k, F, open_look, lid, ink, line=0.16):
    """The eyes in a state: one patch a side, mirrored."""
    def lay(state):
        k.decal(F["eye_c"], F["eye_n"], F["eye_half"], eye_look(state, open_look, srgb(lid), srgb(ink), line=line),
                eps=0.0035, rings=20, segs=56, mirror=True, up=F.get("eye_up", (0, 0, 1)))
    return lay


def guardian(src, out, F, extras, eyes, voxel, band=0.004):
    """One guardian: its figure, extras(k, F) its shell and marks, eyes(k, F) the lay function of its eyes."""
    k = Kit(src, coat=F.get("coat", "shell"))
    figure(k, F)
    extras(k, F)
    k.build(voxel=voxel, smooth=3, band=band, wband=0.03)
    k.eye_states(eyes(k, F))
    k.export(out)


# --- Tapu Koko's own ------------------------------------------------------------------------------------------------
BLACK, YELLOW, LINING, RIM = "#2C2A32", "#EACF2C", "#3A3626", "#1E1C22"
ORANGE, ORANGE_D, WHITE, TEAL = "#F2782A", "#D8601C", "#F4F4EE", "#46C4D6"

KOKO = dict(
    body=BLACK,
    torso=[((0, -0.012, 0.318), (0.04, 0.034, 0.055)), ((0, -0.006, 0.262), (0.03, 0.027, 0.042))],
    neck=((0, -0.016, 0.352), (0, -0.024, 0.385)), neck_r=0.027,
    head_c=(0, -0.03, 0.412), head_r=(0.048, 0.045, 0.048),
    arm=((0.035, -0.02, 0.346), (0.078, -0.055, 0.312), (0.118, -0.046, 0.338)), arm_r=0.012, hand_r=0.017,
    eye_c=(0.021, -0.07, 0.417), eye_n=unit(0.42, -0.9, 0.05), eye_half=(0.0155, 0.0125),
)
KOKO_A = math.radians(22)          # how far each half's outer face is turned from straight out toward the front


def koko_dome(s):
    o = (s * math.cos(KOKO_A), -math.sin(KOKO_A), 0)
    f = (-s * math.sin(KOKO_A), -math.cos(KOKO_A), 0)
    return Dome((s * 0.17, 0.0, 0.33), o, f, (0, 0, 1), (0.082, 0.108, 0.17))


def koko_face(k, F):
    """The white mask round each eye, the white stripes up the forehead and the small orange beak."""
    k.decal((0.024, -0.069, 0.418), unit(0.45, -0.89, 0.05), (0.025, 0.02), WHITE, eps=0.0022, rings=12, segs=48,
            mirror=True, outline=polygon([(-1, 0.2), (-0.5, 1), (1, 0.9), (1, -0.7), (0.2, -1), (-0.9, -0.6)]))
    for x in (0.0, 0.017):
        k.decal((x, -0.064, 0.446), unit(x * 14, -0.78, 0.62), (0.0055, 0.016), WHITE, eps=0.0022, rings=8, segs=36,
                mirror=x > 0, outline=polygon([(-1, -1), (1, -1), (0.7, 1), (-0.7, 1)]))
    k.add("beak", spike((0, -0.071, 0.402), (0, -0.104, 0.386), 0.014, n=4, flat=0.8, turn=math.pi / 4), ORANGE,
          "head", fuse=False)


def flame(L, w, teeth):
    """An outline of a flame-tongue, b along its length 0..L, jagged on its +a edge."""
    pts = [(-w * 0.55, 0.0), (w * 0.55, 0.0)]
    for i in range(teeth):
        b0 = L * (0.18 + 0.68 * i / teeth)
        b1 = L * (0.18 + 0.68 * (i + 0.6) / teeth)
        pts.append((w * 0.55 * (1 - b0 / L) + w * 0.42, b0 + 0.004))
        pts.append((w * 0.55 * (1 - b1 / L), b1))
    pts.append((0.0, L))
    pts.append((-w * 0.45 * 0.3, L * 0.7))
    pts.append((-w * 0.5, L * 0.3))
    return pts


def koko_crest(k):
    root = np.array([0.0, -0.005, 0.446])
    for i, (d, L, w) in enumerate([(unit(0.32, 0.42, 1), 0.17, 0.052), (unit(-0.12, 0.5, 1), 0.12, 0.04),
                                   (unit(0.72, 0.36, 0.72), 0.12, 0.04)]):
        ex = np.cross(d, (0, 1.0, 0)); ex /= np.linalg.norm(ex)
        k.add(f"crest{i}", slab(flame(L, w, 3), root + d * 0.005, -ex, d, 0.022), ORANGE, "head", fuse=False,
              surface="leaf")


def koko_skirt(k):
    """Long orange feathers hanging from the waist, their faces to the front, the middle ones longest."""
    waist = np.array([0.0, -0.008, 0.245])
    for i, (dx, L, w, col) in enumerate([(0.0, 0.18, 0.024, ORANGE), (0.22, 0.15, 0.021, ORANGE),
                                         (-0.22, 0.15, 0.021, ORANGE), (0.55, 0.1, 0.018, ORANGE_D),
                                         (-0.55, 0.1, 0.018, ORANGE_D)]):
        d = unit(dx, 0.04, -1)
        base = waist + np.array([dx * 0.04, -0.004 * abs(dx), 0.0])
        k.add(f"skirt{i}", outward(leaf(base, d, L, w, thick=0.0115)), col, "body", fuse=False, surface="leaf")


def koko_shell(k, s, side):
    D = koko_dome(s)
    bone = f"arm{side}"
    half_shell(k, f"shell{side}", D, YELLOW, LINING, RIM, bone)
    # the white band down the front edge and the black zigzag over it
    t0, t1 = -0.62 * math.pi, 0.46 * math.pi
    dome_patch(k, f"band{side}", D, t0, t1, 0.36, 0.82, WHITE, bone, lift=0.003, nu=90)
    dome_patch(k, f"zig{side}", D, t0 + 0.04, t1 - 0.04, 0.36,
               lambda u: 0.38 + 0.4 * (0.12 + 0.88 * (0.5 + 0.5 * tri(6.5 * u))), INK, bone, lift=0.0065, nu=160)
    # the hooked black beak at the top front, the black point at the foot
    top = D.rim(0.32 * math.pi)
    beak = [top + 0.03 * D.w + 0.012 * D.o, top - 0.035 * D.w + 0.012 * D.o, top - 0.035 * D.w - 0.01 * D.o,
            top + 0.03 * D.w - 0.01 * D.o, top - 0.03 * D.f + 0.02 * D.w,
            top + 0.07 * D.f - 0.035 * D.w - 0.012 * D.o]
    k.add(f"beak{side}", hull(beak), RIM, bone, fuse=False)
    foot = D.rim(-0.5 * math.pi)
    k.add(f"point{side}", spike(foot + 0.03 * D.w + 0.008 * D.o, foot - 0.085 * D.w + 0.03 * D.f, 0.028, n=5,
                                flat=0.6), RIM, bone, fuse=False)
    # the yellow tuft at the top back: a fan of three thick feathers
    back = D.rim(0.78 * math.pi, out=0.012)
    for j, (dw, df, L) in enumerate([(0.8, -0.6, 0.075), (1.0, -0.15, 0.085), (0.65, 0.3, 0.06)]):
        d = df * D.f + dw * D.w; d /= np.linalg.norm(d)
        k.add(f"tuft{side}{j}", spike(back + 0.012 * (j - 1) * D.f, back + d * L, 0.017, n=4, flat=0.5,
                                      turn=0.0), YELLOW, bone, fuse=False, surface="leaf")


def koko_extras(k, F):
    koko_face(k, F)
    koko_crest(k)
    koko_skirt(k)
    S, E, H = (np.array(p, float) for p in F["arm"])
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"cuff{side}", ring_band((E + (H - E) * 0.72) * m, (H - E) * m, 0.0145, 0.014, thick=0.007), YELLOW,
              f"arm{side}", fuse=False, surface="metal")
        koko_shell(k, s, side)


def koko_eyes(k, F):
    LID, IRIS, PUP, CI = srgb(WHITE), srgb(TEAL), srgb("#14232C"), srgb(INK)

    def open_eye(u, v):
        c = np.tile(LID, (len(u), 1))
        r = (u / 0.98) ** 2 + (v / 0.92) ** 2
        c[r < 1] = CI
        c[r < 0.66] = IRIS
        c[((u + 0.05) / 0.34) ** 2 + ((v + 0.08) / 0.46) ** 2 < 1] = PUP
        c[((u + 0.2) / 0.13) ** 2 + ((v - 0.2) / 0.13) ** 2 < 1] = (1, 1, 1)
        # the fierce brow: the eye cut by a line low at the nose
        c[v > 0.32 + 0.5 * u] = srgb(BLACK)
        return c
    return eye_lay(k, F, open_eye, WHITE, INK)


if __name__ == "__main__":
    src, out = args()
    guardian(src, out, KOKO, koko_extras, koko_eyes, voxel=0.0038)
