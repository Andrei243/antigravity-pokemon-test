# Type: Null sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/typenull.py -- "<the export's Type Null.glb>" <out.glb>
#
# From the references: a chimera on four legs. A charcoal beast's trunk and hind legs, the hind legs thick, marked with
# a chain of dark purple scales down their backs; a bird's forelegs, charcoal above and green and scaly below, with
# spurs on the shank and three great hooked talons forward and one back. A shaggy grey ruff round the neck and down the
# chest, cut into square-ended strips. The head is wholly a great copper helmet, the largest mass of the head: a
# chamfered box with a dark slot of a visor in its face (the eyes dark in it), a jutting chin plate, a round boss with a
# green cross on each side, spikes radiating from its rim with green studs. Over it a copper bracket clamps a great grey
# axe-blade, notched at its back end and hooked down at its front. A fish's tail: a blue spine arching up and back and a
# grey membrane under it, its back edge scalloped, its lower edge blue.
#
# The frame is written to be grown from: silvally.py imports it (sibling("typenull")) for the trunk, the legs, the
# ruff's strips and the tail, all of them placed by a frame F; nothing builds unless this file is run itself.
# Bones: body (0, 0, 0.32); head (0, -0.13, 0.38); tail (0, 0.18, 0.32); legs (+-0.055, -0.1, 0.3); hind legs
# (+-0.06, 0.14, 0.28).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, slab, lathe, outward, srgb, inside, tri, unit, axes, eye_look, boxy, lumpy,
                       args, sibling)

r = sibling("raikou")

# --- colours ------------------------------------------------------------------------------------------------------------
DARK = "#3A3337"
PURPLE = "#4C3E84"
GREEN = "#3B7A61"
TALON = "#448A6E"
FUR = "#BEC0C7"
FUR2 = "#A7A9B1"
HELM = "#CC8C6C"
HELM_D = "#B27358"
STUD = "#4E9E50"
BLADE = "#B9BBC1"
BRACKET = "#C78668"
SPINE = "#2552A0"
MEMBRANE = "#CBCFD8"
SLOT = "#221D20"
GLINT = "#D8C98E"
SLOT_LINE = "#6E5E5C"

# --- the frame: what Silvally grows on another skeleton -------------------------------------------------------------------
NULL = dict(x=0.058, fy=-0.1, hy=0.14, s=1.0, body_z=0.32)


def z(F, v):
    return v * F["s"]


# --- shapes ---------------------------------------------------------------------------------------------------------------
def spike(root, d, length, w):
    """A hard spike: a square root of half-width w and a point length along d."""
    root = np.array(root, float); d = unit(*d)
    a, b = axes(d)
    return hull([root + a * w, root - a * w, root + b * w, root - b * w, root - d * w * 0.6, root + d * length])


def box(c, h, ch):
    """A box of half sizes h with its edges chamfered by ch (a hull)."""
    c = np.array(c, float); hx, hy, hz = h
    pts = []
    for sx in (1, -1):
        for sy in (1, -1):
            for sz in (1, -1):
                pts += [c + (sx * (hx - ch), sy * hy, sz * (hz - ch)), c + (sx * hx, sy * (hy - ch), sz * (hz - ch)),
                        c + (sx * (hx - ch), sy * (hy - ch), sz * hz)]
    return hull(pts)


def strip(top, out, length, w, t, tilt=0.2):
    """A tuft of fur hanging from top, leaning out along out, its end pointed (a flat-faced hull)."""
    top = np.array(top, float); out = unit(out[0], out[1], 0)
    d = unit(*(out * math.sin(tilt) + np.array((0, 0, -1.0)) * math.cos(tilt)))
    across = np.cross(d, out); across /= np.linalg.norm(across)
    n = np.cross(across, d)
    mid = top + d * length * 0.55
    pts = []
    for c, ww, tt in ((top, w, t), (mid, w * 0.95, t * 0.85)):
        for sa in (1, -1):
            for sn in (1, -1):
                pts.append(c + across * sa * ww + n * sn * tt / 2)
    pts.append(top + d * length + n * t * 0.15)
    return hull(pts)


def jitter(i):
    """A fixed uneven factor 0.75..1.15 for the i-th tuft."""
    return 0.75 + 0.4 * ((math.sin(i * 12.9898 + 1.7) * 43758.5453) % 1.0)


# --- the trunk ------------------------------------------------------------------------------------------------------------
def add_trunk(k, F, color=DARK):
    s, fy, hy, bz = F["s"], F["fy"], F["hy"], F["body_z"]
    k.add("chest", ell((0, fy - 0.01 * s, bz + 0.01 * s), (0.084, 0.1 * s, 0.11 * s)), color, "body")
    k.add("middle", ell((0, (fy + hy) / 2, bz - 0.005 * s), (0.078, 0.13 * s, 0.1 * s)), color, "body")
    k.add("rump", ell((0, hy + 0.01 * s, bz + 0.01 * s), (0.086, 0.095 * s, 0.112 * s)), color, "body")


# --- a bird's forelegs: dark above, green scales below, three great talons forward and one back ----------------------------
def add_forelegs(k, F, upper=DARK, lower=GREEN, claw=TALON, edge_z=0.17):
    x, fy, s = F["x"], F["fy"], F["s"]
    k.add("shoulder", ell((x + 0.004, fy + 0.004, z(F, 0.27)), (0.056, 0.068 * s, 0.1 * s)), upper, "leg{s}", mirror=True)
    k.add("leg", tube([(x, fy, z(F, 0.3)), (x + 0.004, fy + 0.012 * s, z(F, 0.2)), (x + 0.008, fy - 0.004 * s, z(F, 0.11)),
                       (x + 0.01, fy - 0.012 * s, 0.045)], [0.046, 0.034, 0.027, 0.025]), upper, "leg{s}", mirror=True)
    a = np.array((x + 0.01, fy - 0.014 * s, 0.034))
    for j, t in enumerate((-1, 0, 1)):
        d = unit(0.42 * t, -1, -0.12)
        tip = a + d * 0.058 * s
        k.add(f"toe{j}", tube([a, a + d * 0.03 * s, tip], [0.019, 0.016, 0.013]), lower, "leg{s}", mirror=True)
        # The hooked talon: a hull out of the toe's end curving down to the ground
        c, b = axes(d)
        root = tip - d * 0.004
        k.add(f"talon{j}", hull([root + c * 0.013, root - c * 0.013, root + np.array((0, 0, 0.013)),
                                 root - np.array((0, 0, 0.011)), tip + d * 0.022 + np.array((0, 0, -0.008)),
                                 tip + d * 0.03 + np.array((0, 0, -0.03))]), claw, "leg{s}", fuse=False, mirror=True,
              surface="shell")
    d = unit(0, 1, -0.15)
    tip = a + d * 0.03 * s
    k.add("toeback", tube([a, tip], [0.017, 0.013]), lower, "leg{s}", mirror=True)
    k.add("talonback", hull([tip + (0.012, 0, 0), tip - (0.012, 0, 0), tip + (0, 0, 0.012), tip - (0, 0, 0.01),
                             tip + (0, 0.022, -0.03)]), claw, "leg{s}", fuse=False, mirror=True, surface="shell")
    # Spurs down the back of the shank
    for j, zz in enumerate((0.15, 0.095)):
        root = np.array((x + 0.007, fy + 0.012 * s + 0.018, z(F, zz)))
        k.add(f"spur{j}", spike(root, (0.2, 1, 0.55), 0.03 * s, 0.011), claw, "leg{s}", fuse=False, mirror=True,
              surface="shell")
    # The green scales below a ragged line, kept to the forelegs by place
    def scales(P):
        near = (np.abs(P[:, 1] - fy) < 0.12 * s) & (np.abs(np.abs(P[:, 0]) - x) < 0.07) & (P[:, 2] < z(F, 0.3))
        ang = np.arctan2(P[:, 1] - fy, np.abs(P[:, 0]) - x)
        return inside(P[:, 2] - z(F, edge_z) - 0.014 * tri(ang * 3 / math.pi), 0.003) * near
    k.paint(scales, lower)


# --- a beast's hind legs: thick, charcoal, a chain of purple scales down their backs -------------------------------------
def add_hindlegs(k, F, color=DARK, purple=PURPLE):
    x, hy, s = F["x"] + 0.004, F["hy"], F["s"]
    thigh_c = np.array((x + 0.006, hy + 0.014 * s, z(F, 0.255)))
    thigh_r = np.array((0.062, 0.088 * s, 0.115 * s))
    k.add("thigh", ell(thigh_c, thigh_r), color, "hind{s}", mirror=True)
    shank = [(x + 0.004, hy + 0.02 * s, z(F, 0.24)), (x + 0.006, hy + 0.0 * s, z(F, 0.16)),
             (x + 0.008, hy + 0.045 * s, z(F, 0.09)), (x + 0.008, hy + 0.03 * s, 0.045)]
    k.add("shank", tube(shank, [0.05, 0.04, 0.031, 0.03]), color, "hind{s}", mirror=True)
    k.add("hpaw", ell((x + 0.008, hy - 0.002 * s, 0.022), (0.038, 0.055 * s, 0.023)), color, "hind{s}", mirror=True)
    # The purple scales: ovals down the back of the thigh and of the shank
    for d, size in ((unit(0.55, 0.75, 0.38), (0.021, 0.026)), (unit(0.55, 0.83, 0.0), (0.023, 0.028)),
                    (unit(0.5, 0.8, -0.38), (0.021, 0.026))):
        p, n = r.on_ell(thigh_c, thigh_r, d)
        k.decal(p, n, size, purple, eps=0.003, rings=8, segs=36, mirror=True)
    for c, rad in ((shank[2], 0.031), ((np.array(shank[2]) + np.array(shank[3])) / 2, 0.03)):
        n = unit(0.6, 0.8, 0)
        k.decal(np.array(c) + n * rad, n, (0.016, 0.02), purple, eps=0.003, rings=8, segs=32, mirror=True)


# --- the ruff: a fused mass round the neck with square-ended strips hanging from it ----------------------------------------
def add_ruff(k, centre, radius, top, lengths, color=FUR, color2=FUR2, w=0.027, t=0.024, tilt=0.22, angles=None):
    """lengths(angle in degrees from the front) gives each strip's length; angles: where the strips hang."""
    centre = np.array(centre, float)
    k.add("ruff", ell(centre + (0, 0, top - centre[2] - radius[2] * 0.5), radius, deform=lumpy(6, 3, 0.04)), color, "body")
    angles = angles if angles is not None else (0, 24, 48, 72, 96, 120, 145)
    for i, ang in enumerate(angles):
        th = math.radians(ang)
        for sd in ((1,) if ang == 0 else (1, -1)):
            out = (math.sin(th) * sd, -math.cos(th))
            at = centre + (radius[0] * 0.92 * out[0], radius[1] * 0.92 * out[1], 0)
            at[2] = top - 0.01 * abs(math.sin(th))
            col = color if i % 2 == 0 else color2
            ln = lengths(ang) * jitter(2 * i + (sd < 0))
            k.add(f"strip{i}{'R' if sd > 0 else 'L'}", strip(at, out, ln, w, t, tilt), col, "body", fuse=False,
                  surface="fur")


# --- the fish's tail: a blue spine arching up and back, a membrane scalloped behind, its lower edge blue -------------------
def band(path, half):
    """A polygon round a path of (a, b) points, half its width each side."""
    P = np.array(path, float)
    T = np.gradient(P, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    N = np.stack([-T[:, 1], T[:, 0]], 1)
    hw = np.array(half, float)[:, None]
    return [tuple(p) for p in np.vstack([P + N * hw, (P - N * hw)[::-1]])]


def add_tail(k, root, F, spine=SPINE, membrane=MEMBRANE, color=DARK, size=1.0, spikes=None):
    root = np.array(root, float)
    s = size
    ex = unit(0.57, 0.82, 0)
    ez = np.array((0, 0, 1.0))
    # A short dark tail running back from the rump, rising gently, the fan at its end
    o = root + ex * 0.05 * s + ez * 0.012 * s
    k.add("tailroot", tube([root - (0, 0.06 * s, 0.045 * s), root - (0, 0.02 * s, 0.01 * s), root + ex * 0.03 * s,
                            o + ex * 0.008], [0.042, 0.036, 0.029, 0.024]), color, "tail")
    # The fan leaned back: its spine 35 degrees above level, not upright as a sail
    th = math.radians(-30)
    cs, sn = math.cos(th), math.sin(th)

    def rot(pts):
        return [(s * (a * cs - b * sn), s * (a * sn + b * cs)) for a, b in pts]
    fin = [(0.0, 0.03), (0.03, 0.11), (0.07, 0.19), (0.115, 0.25), (0.135, 0.205), (0.118, 0.172), (0.152, 0.135),
           (0.135, 0.103), (0.172, 0.068), (0.152, 0.04), (0.205, -0.012), (0.12, -0.012), (0.05, -0.014), (0.0, -0.02)]
    k.add("fin", slab(rot(fin), o, ex, ez, 0.022), membrane, "tail", fuse=False, surface="scales")
    up = [(-0.005, 0.0), (0.03, 0.11), (0.07, 0.19), (0.118, 0.256)]
    k.add("spine", slab(band(rot(up), [0.02 * s, 0.017 * s, 0.013 * s, 0.006 * s]), o, ex, ez, 0.034), spine, "tail",
          fuse=False, surface="scales")
    low = [(0.0, -0.01), (0.1, -0.014), (0.205, -0.014)]
    k.add("finlow", slab(band(rot(low), [0.012 * s, 0.011 * s, 0.005 * s]), o, ex, ez, 0.03), spine, "tail", fuse=False,
          surface="scales")
    if spikes:
        # Small grey spikes standing on the spine's top
        for j, (a, b) in enumerate(rot(((0.02, 0.085), (0.05, 0.155), (0.085, 0.22)))):
            at = o + ex * a + ez * b
            dd = unit(*(ez * 0.85 - ex * 0.3))
            k.add(f"finspike{j}", spike(at, dd, 0.035 * s, 0.011), spikes, "tail", fuse=False, surface="shell")


# ======================================================================================================================
# Type: Null's own: the helmet
# ======================================================================================================================
HC = np.array([0.0, -0.265, 0.535])          # the helmet's middle
HH = np.array([0.1, 0.106, 0.122])           # its half sizes
CH = 0.032
FRONT = HC[1] - HH[1]
VISOR = np.array([0.0, FRONT + 0.02 - 0.0015, 0.537])


def add_neck(k):
    k.add("neck", tube([(0, -0.1, 0.36), (0, -0.16, 0.43), (0, -0.22, 0.48)], [0.062, 0.058, 0.055]), DARK, "head")


def add_helmet(k):
    k.add("helmet", box(HC, HH, CH), HELM, "head", fuse=False, surface="metal")
    # The chin plate jutting forward under the visor, and its spike
    k.add("chin", hull([(0.07, FRONT + 0.01, 0.46), (-0.07, FRONT + 0.01, 0.46), (0.07, FRONT + 0.01, 0.415),
                        (-0.07, FRONT + 0.01, 0.415), (0.05, FRONT - 0.04, 0.45), (-0.05, FRONT - 0.04, 0.45),
                        (0.04, FRONT - 0.03, 0.425), (-0.04, FRONT - 0.03, 0.425)]), HELM_D, "head", fuse=False,
          surface="metal")
    k.add("chinspike", spike((0, FRONT - 0.01, 0.42), (0, -0.35, -1), 0.06, 0.022), HELM, "head", fuse=False,
          surface="metal")
    # The dark visor: a plate standing just proud of the helmet's face, for the eyes
    k.add("visor", ell(VISOR, (0.06, 0.02, 0.044), deform=boxy(6)), SLOT, "head")
    # A round boss with a green cross on each side
    k.add("boss", lathe((HC[0] + HH[0] - 0.004, HC[1] + 0.005, HC[2] + 0.01), (1, 0, 0),
                        [(0, 0), (0.042, 0), (0.042, 0.012), (0.034, 0.022), (0, 0.022)]), HELM, "head", fuse=False,
          mirror=True, surface="metal")
    cross = [(-0.011, -0.03), (0.011, -0.03), (0.011, -0.011), (0.03, -0.011), (0.03, 0.011), (0.011, 0.011),
             (0.011, 0.03), (-0.011, 0.03), (-0.011, 0.011), (-0.03, 0.011), (-0.03, -0.011), (-0.011, -0.011)]
    k.add("cross", slab(cross, (HC[0] + HH[0] + 0.02, HC[1] + 0.005, HC[2] + 0.01), (0, -1, 0), (0, 0, 1), 0.012), STUD,
          "head", fuse=False, mirror=True, surface="metal")
    # Spikes radiating from the helmet's rim, green studs on them
    for j, (root, d, ln, w) in enumerate((((0.085, -0.2, 0.63), (0.65, 0.45, 0.62), 0.11, 0.024),
                                          ((0.095, -0.17, 0.5), (0.85, 0.5, -0.05), 0.1, 0.024),
                                          ((0.075, -0.25, 0.425), (0.55, 0.15, -0.8), 0.08, 0.02),
                                          ((0.07, -0.34, 0.43), (0.55, -0.35, -0.6), 0.055, 0.017))):
        k.add(f"hspike{j}", spike(root, d, ln, w), HELM, "head", fuse=False, mirror=True, surface="metal")
        if j < 3:
            k.add(f"stud{j}", ell(np.array(root) + unit(*d) * ln * 0.36, (0.013, 0.013, 0.013), sub=2), STUD, "head",
                  fuse=False, mirror=True, surface="metal")


def add_blade(k):
    """The axe-blade over the helmet, clamped by a copper bracket, turned 25 degrees so both cameras see its face."""
    th = math.radians(25)
    ea = np.array((-math.sin(th), -math.cos(th), 0.0))           # toward the face
    eb = np.array((0, 0, 1.0))
    o = np.array((0.0, -0.245, 0.69))
    sc = 0.00175
    out = [(-112, 72), (-60, 62), (-10, 50), (45, 25), (88, -5), (106, -35), (100, -60), (88, -42), (68, -30),
           (40, -28), (10, -22), (-30, 6), (-70, 12), (-112, 14), (-112, 26), (-86, 28), (-86, 33), (-112, 35),
           (-112, 47), (-90, 49), (-90, 54), (-112, 56)]
    k.add("blade", slab([(a * sc, b * sc) for a, b in out], o, ea, eb, 0.03), BLADE, "head", fuse=False,
          surface="metal")
    n = np.cross(ea, eb)
    pts = []
    for a in (-0.034, 0.034):
        for b in (-0.085, 0.07):
            for t in (-0.027, 0.027):
                pts.append(o + ea * a + eb * b + n * t)
    k.add("bracket", hull(pts), BRACKET, "head", fuse=False, surface="metal")


# --- the eyes: dark in the visor's slot, a dim glint in each ------------------------------------------------------------
def eye_open(u, v):
    c = np.tile(srgb("#2E272B"), (len(u), 1))
    c[((u + 0.05) / 0.62) ** 2 + (v / 0.42) ** 2 < 1] = srgb("#4A3E44")
    c[((u + 0.05) / 0.32) ** 2 + ((v - 0.02) / 0.22) ** 2 < 1] = srgb(GLINT)
    return c


def eyes(k, state):
    look = eye_look(state, eye_open, srgb(SLOT), srgb(SLOT_LINE), line=0.15)
    for side in (1, -1):
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(VISOR * np.array([side, 1, 1.0]) + np.array([0.028 * side, -0.0185, 0.0]), (0, -1, 0), (0.024, 0.03),
                col, eps=0.0015, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    F = NULL
    add_trunk(k, F)
    add_forelegs(k, F)
    add_hindlegs(k, F)
    add_neck(k)
    add_ruff(k, (0, -0.15, 0.42), (0.1, 0.094, 0.07), 0.45,
             lambda a: 0.215 - 0.1 * (a / 145) ** 1.5)
    add_tail(k, (0, 0.235, 0.39), F)
    add_helmet(k)
    add_blade(k)
    k.build(voxel=0.007, band=0.004)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)
