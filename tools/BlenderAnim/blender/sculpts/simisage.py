# Simisage sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/simisage.py -- <the export's Simisage.glb> <out.glb>
#
# The tall monkey of the elemental three, grown from Pansage's frame (sibling("pansage"): its ears with their loose
# linings, its face painted cream below an edge, its crisp waist, its way of laying eyes and mouths) and written to be
# grown from in turn: tall(src, out, P) takes the colours and the head's crest, so Simisear is this with its own
# dictionary (sibling("simisage")). From the references: a head wider than tall, cream over the whole face up to the
# brows; a tall leafy crest rising from the brow, a cone of leaf with jagged edges and three yellow seeds at its foot
# (laid back here, so the body stays large in the game); white bushy brows sweeping out over fierce eyes cut flat and
# sloping by a dark lid, pupils to the inner side; a small dark nose and a smirk; round green ears, pale green within;
# a ruff of white fur round the neck, ragged below; a cream chest; green arms with leafy cuffs at the wrists and cream
# hands, the thumb up (one arm flung out in the art, both carried forward and a little down here); green leafy trousers
# with a jagged top, a leafy tuft at each ankle, cream feet; a long green tail out to the +X side, rising behind the
# shoulder, tipped with a three-lobed tuft of leaf.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
import sculptkit
from sculptkit import Kit, ell, tube, hull, sheet, srgb, inside, unit, boxy, polygon, eye_look, tri, args

p = sculptkit.sibling("pansage")
CREAM, INK, WHITE = p.CREAM, p.INK, p.WHITE
FUR = "#F6F4EE"

H = np.array([0, -0.01, 0.415])          # the head's centre
HR = np.array([0.114, 0.1, 0.086])
CH, CR = H + np.array([0, -0.03, -0.03]), np.array([0.1, 0.07, 0.056])     # the lower face standing forward


def turn(rot):
    return np.array(Euler([math.radians(a) for a in rot]).to_matrix())


def spikes(k, name, c, axis, r, n, length, lean, color, bone, phase=0.0, width=0.016, mirror=True):
    """A ring of short solid leaf points round a limb: kites on the ring, each tip out and along lean."""
    a, b = sculptkit.axes(axis)
    ax = unit(*axis)
    for i in range(n):
        t = phase + 2 * math.pi * i / n
        e = math.cos(t) * a + math.sin(t) * b
        tg = np.cross(ax, e)
        base = np.array(c) + e * r
        tip = base + e * length + np.array(lean) * length
        pts = [base + tg * width, base - tg * width, base + ax * width * 0.9, base - ax * width * 0.9,
               base - e * 0.012, tip]
        k.add(f"{name}{i}", hull(pts), color, bone, fuse=False, mirror=mirror)


# --- the crests ---------------------------------------------------------------------------------------------------------
def leaf_crest(k, P):
    """A tall cone of leaf from the brow, laid back, jagged at its sides, three seeds at its foot."""
    main = P["main"]
    R = np.array([0.078, 0.062, 0.16])
    M = turn((-24, -7, 0))
    base = np.array([0, -0.018, 0.462])
    c = base + M @ np.array([0, 0, R[2]])
    def taper(V):
        V = V.copy()
        t = np.clip(V[:, 2], 0, 1)
        th = np.arctan2(V[:, 1], V[:, 0])
        s = (1 - 0.92 * t ** 1.15) * (1 + 0.05 * np.abs(np.sin(4 * th)))
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("crest", ell(c, R, rot=(-24, -7, 0), deform=taper), main, "head")
    # The jagged leaf points up each side
    for side in (1, -1):
        for j, f in enumerate((0.42, 0.6, 0.76)):
            vz = -1 + 2 * f
            w = math.sqrt(max(1 - vz * vz, 0)) * (1 - 0.92 * max(vz, 0) ** 1.15)
            edge = np.array([side * R[0] * w * 0.92, 0.0, R[2] * vz])
            tip = edge + np.array([side * 0.034, 0.004, 0.05])
            pts = [edge + (0, 0.018, -0.01), edge + (0, -0.018, -0.01), edge + (-side * 0.012, 0, 0.012),
                   edge + (-side * 0.012, 0, -0.03), tip]
            k.add(f"crestleaf{j}{'R' if side > 0 else 'L'}", hull([c + M @ q for q in pts]), main, "head", fuse=False)
    # The seeds: three yellow dashes at its foot, the middle one longest
    for i, (x, h) in enumerate(((0, 0.026), (0.03, 0.019), (-0.03, 0.019))):
        q = np.array([x, 0, -R[2] * 0.72])
        d = M @ unit(q[0] / R[0], -1, q[2] / R[2])
        at = p.on_ell(c, R, d)
        n = unit(*(M @ (((M.T @ (at - c)) / R ** 2))))
        k.decal(at, n, (0.0085, h), P["seed"], up=tuple(M @ (0, 0, 1.0)), eps=0.004, rings=8, segs=28)


def white_brows(k, P):
    """Bushy white brows: from over each eye's inner corner out and up to the crest's side, a point at the end."""
    k.add("brow", sculptkit.outward(sculptkit.blade(
        [(0.018, -0.104, 0.452), (0.055, -0.092, 0.468), (0.088, -0.07, 0.49), (0.104, -0.05, 0.512)],
        [0.017, 0.021, 0.016, 0.004], unit(0.35, -1, 0.35), 0.62)), FUR, "head", fuse=False, mirror=True)


# --- the collars ----------------------------------------------------------------------------------------------------------
def ruff(k, P):
    """A ruff of white fur round the neck, flaring down to a ragged edge, its seam behind."""
    def at(u, v):
        a = 2 * math.pi * (0.02 + 0.96 * u)
        rag = (tri(a * 9 / (2 * math.pi)) + 1) / 2
        ztop, zbot = 0.342, 0.292 - 0.028 * rag - 0.012 * np.clip(-np.cos(a), 0, 1)
        r = 0.054 + v * (0.098 + 0.01 * rag - 0.054)
        z = ztop + v * (zbot - ztop)
        return np.stack([r * np.sin(a), -0.008 + 0.96 * r * np.cos(a), z], axis=-1)
    k.add("ruff", sculptkit.outward(sheet(at, 97, 7, 0.014)), FUR, "body", fuse=False, surface="fur")


# --- the hands ----------------------------------------------------------------------------------------------------------
def thumbs_up(k, P, W, fw):
    """A cream mitten, the thumb up."""
    C = W + fw * 0.03
    k.add("hand", ell(C, (0.036, 0.034, 0.033), deform=boxy(2.4)), CREAM, "arm{s}", mirror=True)
    k.add("thumb", tube([C + (-0.018, -0.012, 0.016), C + (-0.022, -0.022, 0.052)], [0.016, 0.0145]), CREAM, "arm{s}",
          mirror=True)


def leaf_tip(k, P, end, plane):
    """The tail's tuft: three big lobes of leaf in a plane facing back and out."""
    side = np.cross(plane, (0, 0, 1.0)); side /= np.linalg.norm(side)
    E = np.array(end)
    k.add("tiplobe0", ell(E + (0, 0, 0.06), (0.042, 0.03, 0.05)), P["main"], "tail")
    for i, s in enumerate((1, -1)):
        k.add(f"tiplobe{i + 1}", ell(E + side * s * 0.044 + (0, 0, 0.026), (0.038, 0.028, 0.042)), P["main"], "tail")


def long_tail(k, P):
    TP = [(0, 0.06, 0.17), (0.05, 0.15, 0.13), (0.14, 0.2, 0.15), (0.2, 0.2, 0.23), (0.23, 0.175, 0.32),
          (0.235, 0.165, 0.37)]
    k.add("tail", tube(TP, [0.022, 0.02, 0.019, 0.019, 0.019, 0.02]), P["main"], "tail")
    P["tip"](k, P, TP[-1], unit(0.6, 0.8, 0))


def leafy_waist(th):
    """Trousers of leaf: a jagged top all round, the points up."""
    return 0.205 + 0.026 * (tri(th * 8 / (2 * math.pi) + 0.5) + 1) / 2


def sage_top(ax):
    """Cream over the whole face, up to the brows."""
    return 0.05 - 1.0 * ax ** 2


# --- the eyes and the mouths ------------------------------------------------------------------------------------------
FIERCE = [(-1.0, -0.02), (-0.72, -0.72), (0.0, -0.96), (0.72, -0.74), (1.0, -0.08), (0.9, 0.62)]


def fierce_eye(u, v):
    """A white cut by a dark lid sloping down to the inner corner, a large black pupil to the inner side, low."""
    c = np.tile(WHITE, (len(u), 1))
    top = -0.02 + 0.64 * (u + 1) / 1.9
    c[v > top - 0.3] = srgb(INK)
    c[((u + 0.24) / 0.44) ** 2 + ((v + 0.3) / 0.52) ** 2 < 1] = srgb(INK)
    return c


def smirk():
    CC, CI = srgb(CREAM), srgb(INK)
    def look(u, v):
        c = np.tile(CC, (len(u), 1))
        line = -0.1 + 0.45 * u * u - 0.12 * u
        c[(np.abs(v - line) < 0.26) & (np.abs(u) < 0.9)] = CI
        return c
    return look, None, (0.032, 0.016), (0, -1, -0.5)


SIMISAGE = dict(
    main="#3FAE5A", lining="#9ADDA6", seed="#F0DC6A",
    ear_c=(0.168, 0.02, 0.45), ear_r=(0.066, 0.022, 0.074), ear_rot=(0, -14, 24), ear_outline=None,
    crest=leaf_crest, brows=white_brows, collar=ruff, cuffs=True, hand=thumbs_up, tail=long_tail, tip=leaf_tip,
    top=sage_top, waist=leafy_waist, arm_color=None, legs_cuffs=True,
    eye=fierce_eye, eye_outline=FIERCE, eye_half=(0.031, 0.027), eye_dir=(0.41, -0.9, 0.08), lid=CREAM,
    mouth=smirk, hips=(0.092, 0.076, 0.056),
)


def tall(src, out, P):
    k = Kit(src, coat="fur")
    main = P["main"]
    arm = P["arm_color"] or main

    # --- the head, the lower face standing forward ------------------------------------------------------------------
    k.add("head", ell(H, HR), main, "head")
    k.add("cheeks", ell(CH, CR), main, "head")

    # --- the body: a cream chest broad at the top, the hips in the colour ------------------------------------------
    def chest(V):
        V = V.copy(); s = 1 + 0.12 * V[:, 2]
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("torso", ell((0, 0.0, 0.272), (0.076, 0.064, 0.082), deform=chest), CREAM, "body")
    k.add("neck", ell((0, -0.004, 0.335), (0.054, 0.05, 0.034)), CREAM, "body")
    k.add("hips", ell((0, 0.006, 0.19), P["hips"]), main, "body")

    # --- legs: haunches fused into the hips, short thick shins, cream feet ------------------------------------------
    k.add("thigh", ell((0.066, -0.004, 0.158), (0.052, 0.056, 0.062)), main, "leg{s}", mirror=True)
    k.add("shin", tube([(0.075, -0.008, 0.13), (0.088, -0.016, 0.085), (0.094, -0.018, 0.05)], [0.042, 0.035, 0.031]),
          main, "leg{s}", mirror=True)
    F = np.array([0.097, -0.04, 0.022])
    k.add("foot", ell(F, (0.044, 0.058, 0.024)), CREAM, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.026, 0.0, 0.026)):
        k.add(f"toe{j}", ell(F + (dx, -0.05, -0.002), (0.016, 0.018, 0.018)), CREAM, "leg{s}", mirror=True)
    p.waist_paint(k, P["waist"], main, CREAM, 0.12, band=0.06)
    if P["legs_cuffs"]:
        spikes(k, "ankleleaf", (0.093, -0.018, 0.058), (0, 0, 1), 0.026, 7, 0.026, (0, 0, 0.9), main, "leg{s}")

    # --- arms carried forward and a little down -----------------------------------------------------------------------
    S, El, W = (np.array(P.get(n, d)) for n, d in (("shoulder", (0.058, -0.006, 0.302)), ("elbow", (0.118, -0.034, 0.268)),
                                                     ("wrist", (0.152, -0.076, 0.236))))
    k.add("arm", tube([S, El, W], P.get("arm_r", [0.031, 0.027, 0.025])), arm, "arm{s}", mirror=True)
    if P.get("arm_paint"):
        k.paint(lambda Q: np.ones(len(Q)), arm, on=["arm"])          # the arm its own colour where the waist ran over it
    fw = unit(*(W - El))
    if P["cuffs"]:
        spikes(k, "cuffleaf", W - fw * 0.012, fw, 0.022, 7, 0.026, -fw * 0.8, arm, "arm{s}", phase=0.3)
    P["hand"](k, P, W, fw)

    # --- the ears, the crest, the brows, the collar, the tail --------------------------------------------------------
    p.ear(k, P["ear_c"], P["ear_r"], P["ear_rot"], main, P["lining"], P["ear_outline"])
    P["crest"](k, P)
    if P["brows"]:
        P["brows"](k, P)
    P["collar"](k, P)
    P["tail"](k, P)

    # --- the face: cream below its edge, the nose and the mouth ------------------------------------------------------
    p.face_paint(k, H, HR, P["top"], CREAM)
    dn = unit(0, -1, 0.05)
    k.decal(p.on_ell(CH, CR, dn), dn, (0.013, 0.009), "#3A2420", eps=0.004, rings=8, segs=32)
    look, shape, half, dm = P["mouth"]()
    dm = unit(*dm)
    k.decal(p.on_ell(CH, CR, dm), dm, half, look, eps=0.003, outline=shape, rings=20, segs=64)

    # --- the eyes ------------------------------------------------------------------------------------------------------
    look = lambda st: eye_look(st, P["eye"], srgb(P["lid"]), srgb(INK), line=0.17)
    def eyes(state):
        L = look(state)
        if P["eye_outline"] is None:
            de = unit(*P["eye_dir"])
            k.decal(p.on_ell(H, HR, de), de, P["eye_half"], L, eps=0.005, rings=20, segs=56, mirror=True)
            return
        for side in (1, -1):
            de = unit(*(np.array(P["eye_dir"]) * (side, 1, 1)))
            if side > 0:
                col, out_ = L, polygon(P["eye_outline"])
            else:
                col, out_ = (lambda u, v: L(-u, v)), polygon([(-a, b) for a, b in P["eye_outline"]])
            k.decal(p.on_ell(H, HR, de), de, P["eye_half"], col, outline=out_, eps=0.005, rings=20, segs=56)

    k.build(voxel=0.006, band=0.005)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    tall(*args(), SIMISAGE)
