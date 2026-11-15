# Squirtle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/squirtle.py -- <the export's Squirtle.glb> <out.glb>
#
# The design, from the references: a small light blue turtle on two short legs; a big round head with large dark red
# eyes high on it, cut along their tops by a dark line, and a wide smile across a short snout; a cream plastron down
# the whole front, parted by a few lines; a brown shell on the back rimmed in white, its plates parted by darker
# lines; short arms held out and forward with three blunt fingers, short feet with three toes; a tail that runs back
# and curls up into a spiral.
#
# Written to be grown from (Wartortle, Blastoise: sibling("squirtle")): the colours and every part are functions of a
# few named numbers, and the line's way of making a torso, its rim, its plates, a limb, a paw, an eye and a mouth is
# one function each.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, tri, unit, boxy, eye_look, polygon, args

BLUE, CREAM, SHELL, RIM, PLASTRON_LINE, PLATE_LINE = \
    "#88CBE6", "#EEDDA8", "#A9653E", "#F4F2E8", "#B89A68", "#6A3A22"
IRIS, PUPIL, SCLERA, INK, MOUTH = "#A8303E", "#1A1014", "#F8F6F4", "#22161E", "#3A2430"

# The frame: the head, the torso and the shell, the plane the rim lies in
H = np.array([0, -0.02, 0.475])          # the head's centre
HR = np.array([0.152, 0.142, 0.128])      # its radii
TC = np.array([0, -0.005, 0.25])          # the torso's centre
TR = np.array([0.135, 0.12, 0.13])
SC = np.array([0, 0.05, 0.275])           # the shell's centre: further back, so it stands off the back as a dome
SR = np.array([0.155, 0.118, 0.162])
RIM_Y, RIM_TILT, RIM_R = 0.02, 0.15, 0.024   # the rim's plane y = RIM_Y + RIM_TILT (z - SC.z), its tube's radius
VOXEL = 0.005


def plane(y0=RIM_Y, tilt=RIM_TILT, zc=SC[2]):
    """The rim's plane as y of z: where the shell meets the plastron."""
    return lambda z: y0 + tilt * (z - zc)


RIM_PLANE = plane()


def torso(k, tc=TC, tr=TR, sc=SC, sr=SR, rim=RIM_PLANE, rim_r=RIM_R, cream=CREAM, shell=SHELL, rim_col=RIM,
          collar=None):
    """The cream plastron, the brown shell behind it and the white rim between them, round the side in a plane."""
    k.add("torso", ell(tc, tr), cream, "body")
    k.add("shell", ell(sc, sr), shell, "body", surface="shell")
    # The rim: a fused ridge round the body where the shell meets the plastron, its seam under the crotch
    pts = []
    for a in np.linspace(math.pi, 3 * math.pi, 41):
        z = sc[2] + sr[2] * math.cos(a)
        y = rim(z)
        f = math.sqrt(max(0.0, 1 - ((y - sc[1]) / sr[1]) ** 2))
        pts.append((sr[0] * f * 1.03 * math.sin(a), y, sc[2] + (z - sc[2]) * f * 1.03))
    k.add("rim", tube(pts, [rim_r] * len(pts), seg=12, per=4), rim_col, "body", surface="shell")
    # Cream ahead of the rim's plane, brown behind it, whichever of the two parts is nearer
    k.paint(lambda P: inside(P[:, 1] - rim(P[:, 2]) + rim_r * 0.6, 0.003), cream, on=["torso", "shell"])
    k.paint(lambda P: inside(rim(P[:, 2]) + rim_r * 0.6 - P[:, 1], 0.003), shell, on=["torso", "shell"])
    if collar is not None:
        # A shell that rises over the plastron's top shows its white lip there, round the back of the neck
        k.paint(lambda P: inside(P[:, 1] - rim(P[:, 2]), 0.003) * inside(collar - P[:, 2], 0.004), rim_col,
                on=["shell"])


def segments_mask(P2, segs, w, edge=0.0025):
    """1 within w of any of the segments (2D points P2, segments ((a0, b0), (a1, b1)) each)."""
    d = np.full(len(P2), 9.0)
    for p, q in segs:
        p, q = np.array(p, float), np.array(q, float)
        e = q - p
        t = np.clip(((P2 - p) @ e) / (e @ e), 0, 1)
        d = np.minimum(d, np.linalg.norm(P2 - (p + np.outer(t, e)), axis=1))
    return inside(d - w, edge)


def plastron_lines(k, tc=TC, tr=TR, rim=RIM_PLANE, color=PLASTRON_LINE, w=0.0032, scale=1.0):
    """The plastron's plates: a line down the middle and two across."""
    s = scale
    zc = tc[2]
    segs = [((0, zc - 0.1 * s), (0, zc + 0.11 * s)),
            ((-0.09 * s, zc + 0.045 * s), (0.09 * s, zc + 0.045 * s)),
            ((-0.1 * s, zc - 0.045 * s), (0.1 * s, zc - 0.045 * s))]
    def mask(P):
        front = (P[:, 1] < tc[1] - tr[1] * 0.45) * (P[:, 1] < rim(P[:, 2]) - 0.03)
        return segments_mask(P[:, [0, 2]], segs, w) * front
    k.paint(mask, color)


def shell_plates(k, sc=SC, sr=SR, rim=RIM_PLANE, rim_r=RIM_R, color=PLATE_LINE, w=0.0035, hexr=0.058, reach=0.2):
    """The shell's plates seen from behind: a hexagon in the middle and a line out from each of its corners."""
    zc = sc[2]
    corners = [(hexr * math.cos(a), zc + hexr * math.sin(a)) for a in np.radians(np.arange(0, 360, 60))]
    segs = [(corners[i], corners[(i + 1) % 6]) for i in range(6)]
    for (x, z) in corners:
        d = np.array([x, z - zc]); d /= np.linalg.norm(d)
        segs.append(((x, z), (x + d[0] * reach, z + d[1] * reach)))
    def mask(P):
        back = P[:, 1] > rim(P[:, 2]) + rim_r * 1.4
        on_shell = np.abs(np.sqrt((((P - sc) / sr) ** 2).sum(1)) - 1) < 0.06     # the dome's own surface alone
        return segments_mask(P[:, [0, 2]], segs, w) * back * on_shell
    k.paint(mask, color)


def limb(k, name, pts, radii, color, bone, mirror=True):
    k.add(name, tube(pts, radii), color, bone, mirror=mirror)


def toes(n=3, amp=0.16):
    """A deform: the front of a paw (toward -Y) scalloped into n blunt toes."""
    def f(V):
        ph = np.arctan2(V[:, 0], -V[:, 1])
        front = np.clip(-V[:, 1], 0, 1)
        s = 1 + amp * np.cos(n * ph) * front
        V = V.copy(); V[:, 0] *= s; V[:, 1] *= s
        return V
    return f


def paw(k, name, c, r, d, color, bone, n=3, amp=0.16, mirror=True):
    """A paw at c pointing along d (x, y), its toes at the front."""
    th = math.degrees(math.atan2(d[0], -d[1]))
    k.add(name, ell(c, r, rot=(0, 0, th), deform=toes(n, amp)), color, bone, mirror=mirror)


def round_eye(iris=IRIS, pupil=PUPIL, sclera=SCLERA, ink=INK, look_in=0.06):
    """A tall oval eye: a white edge low and outward, a big dark iris, a black pupil, a highlight, a dark lash on top."""
    IR, PU, SC_, IN = srgb(iris), srgb(pupil), srgb(sclera), srgb(ink)
    def eye(u, v):
        c = np.tile(SC_, (len(u), 1))
        c[((u + look_in) / 0.72) ** 2 + ((v + 0.02) / 0.8) ** 2 < 1] = IR
        c[((u + look_in) / 0.4) ** 2 + ((v + 0.02) / 0.5) ** 2 < 1] = PU
        c[((u - 0.18) / 0.22) ** 2 + ((v - 0.38) / 0.22) ** 2 < 1] = (1, 1, 1)
        c[(u * u + v * v > 0.66) & (v > -0.15)] = IN          # the lash over the top, a sixth of the eye wide
        return c
    return eye


def eye_patch(k, state, h, hr, d, half, eye, lid, ink):
    k.decal(h + d * hr, d, half, eye_look(state, eye, srgb(lid), srgb(ink)), eps=0.006, rings=20, segs=56, mirror=True)


def smile(k, c, n, half, color=MOUTH, curve=0.55, thick=0.42):
    """A wide smile: a thin crescent rising at both ends, its outline the patch's own."""
    us = np.linspace(-1, 1, 25)
    mid = lambda u: curve * (u * u - 0.25)
    wide = lambda u: thick * 0.5 * (1 - 0.55 * u * u)
    pts = [(u, mid(u) + wide(u)) for u in us] + [(u, mid(u) - wide(u)) for u in us[::-1]]
    k.decal(c, n, half, color, eps=0.004, rings=12, segs=120, outline=polygon(pts))


def tail_curl(start, d, length, curl_r, radii, turns=1.15, lift=0.0):
    """A tail from start back along d (a direction in the ground's plane), then curling up and over into a spiral."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    up = np.array([0, 0, 1.0])
    start = np.array(start, float)
    pts = [start, start + d * length * 0.45 + up * lift * 0.5, start + d * length + up * lift]
    cen = start + d * length + up * (lift + curl_r)
    for th in np.linspace(-math.pi / 2, -math.pi / 2 + 2 * math.pi * turns, 14)[1:]:
        t = (th + math.pi / 2) / (2 * math.pi * turns)
        r = curl_r * (1 - 0.55 * t)
        pts.append(cen + r * (math.cos(th) * d + math.sin(th) * up))
    n = len(pts)
    rr = np.interp(np.linspace(0, 1, n), np.linspace(0, 1, len(radii)), radii)
    return tube(pts, rr)


def squirtle(src, out):
    k = Kit(src, coat="scales")

    # Head: a big round ball, a short snout low on its front, and the neck under it
    k.add("head", ell(H, HR), BLUE, "head")
    k.add("snout", ell((0, H[1] - 0.088, H[2] - 0.048), (0.108, 0.075, 0.07)), BLUE, "head")
    k.add("neck", tube([(0, -0.01, 0.33), (0, -0.02, 0.42)], [0.08, 0.085]), BLUE, "head")

    torso(k)
    plastron_lines(k)
    shell_plates(k)

    # Arms down along the sides and a little forward, the hands before the plastron's edge, three blunt fingers
    limb(k, "arm", [(0.1, -0.03, 0.3), (0.145, -0.065, 0.25), (0.15, -0.115, 0.21)], [0.044, 0.04, 0.037], BLUE,
         "arm{s}")
    paw(k, "hand", (0.148, -0.135, 0.2), (0.04, 0.048, 0.034), (0.25, -1), BLUE, "arm{s}", amp=0.2)
    # Legs: short and thick, feet with three toes
    limb(k, "leg", [(0.075, 0.0, 0.16), (0.088, -0.015, 0.09), (0.092, -0.02, 0.05)], [0.06, 0.056, 0.05], BLUE,
         "leg{s}")
    paw(k, "foot", (0.095, -0.045, 0.03), (0.06, 0.085, 0.032), (0.12, -1), BLUE, "leg{s}", amp=0.14)

    # The tail: back and out to +X, curling up into a spiral whose face shows from the front three-quarter
    k.add("tail", tail_curl((0.0, 0.1, 0.14), (0.55, 0.83, 0), 0.13, 0.075, [0.042, 0.04, 0.034, 0.024]), BLUE, "tail")

    # The face: a wide smile across the snout
    smile(k, (0, H[1] - 0.16, H[2] - 0.055), unit(0, -1, 0.15), (0.072, 0.03))

    de = unit(0.5, -0.82, 0.24)
    eye = round_eye()
    def eyes(state):
        eye_patch(k, state, H, 0.142, de, (0.038, 0.05), eye, BLUE, INK)

    k.build(voxel=VOXEL, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    squirtle(*args())
