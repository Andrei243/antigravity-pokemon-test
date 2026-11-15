# Totodile sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/totodile.py -- <the export's Totodile.glb> <out.glb>
# A small blue upright crocodile, most of it a great head: a skull behind, the long upper jaw raised over a dropped
# lower one, the mouth open wide (dark red within, a pink tongue on the floor of it), two white fangs down from the
# upper jaw's front and two up from the lower; bold red eyes high on the skull's sides in a white, each sitting at the
# front of a dark bar that runs back from it; two nostrils on the snout's tip. A pear of a body over thighs fused into
# it, short shins and broad feet of three round toes; a cream V on the chest under the jaw; short arms forward and a
# little down, three stubby fingers a hand; a thick short tail curving back and out to the model's +X side; red spines
# down the back and two along the tail.
#
# Written to be grown from (Croconaw and Feraligatr import it with sibling("totodile")): the line's colours, its jaw,
# teeth, spines, claws, eyes and hands are the functions below, placed by a frame of named numbers (L); the calls
# that build Totodile are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from types import SimpleNamespace
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, boxy, eye_look, args

# --- the line's colours ---------------------------------------------------------------------------------------------
BLUE, CREAM, RED = "#4FB6DA", "#F2E29A", "#D8455C"
MOUTH, TONGUE = "#8E2434", "#E8788A"
TOOTH, CLAW = "#F6F4EE", "#F4F2EE"
MARK = "#2B2F3A"
NOSTRIL = "#1F4F66"
INK, WHITE = srgb("#15161C"), np.array([1.0, 1.0, 1.0])
EYE_RED = srgb("#C8202E")
VOXEL = 0.005


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


def fang(base, d, length, width, thick=None, up=(0, 0, 1)):
    """A tooth, a claw or a spine: a flat-faced solid from a kite at its root (width across, thick the other way)
    to its point along d, its belly a little full."""
    d = unit(*d)
    a = np.cross(d, np.array(up, float)); a /= np.linalg.norm(a)
    b = np.cross(a, d)
    thick = thick if thick is not None else width * 0.8
    base = np.array(base, float)
    mid = base + d * length * 0.42
    pts = [base + a * width / 2, base - a * width / 2, base + b * thick / 2, base - b * thick / 2 - d * 0.003,
           mid + a * width * 0.3, mid - a * width * 0.3, mid + b * thick * 0.32, mid - b * thick * 0.32,
           base + d * length]
    return hull(pts)


def spine(k, name, base, d, length, width, thick, bone, color=RED, across=(1, 0, 0)):
    """A red spine standing out of the back or the tail: width along the body, thick across it (across)."""
    d = unit(*d)
    k.add(name, fang(base, d, length, width, thick, up=across), color, bone, fuse=False, surface="scales")


def jaws(k, L):
    """
    The head: a skull, cheeks, the upper jaw raised by L.gape_up from the hinge and the lower dropped by L.gape_down,
    the mouth between them carved and painted dark red with a pink tongue on its floor. Gives back the jaws' frames
    (hinge, direction, up) for the teeth.
    """
    H, HR = L.H, L.HR
    k.add("head", ell(H, HR, deform=boxy(2.2)), BLUE, "head")
    k.add("cheek", ell(H + L.cheek_at, L.cheek_r), BLUE, "head")
    hinge = np.array(L.hinge, float)
    au, al = math.radians(L.gape_up), math.radians(L.gape_down)
    du, upu = np.array([0, -math.cos(au), math.sin(au)]), np.array([0, math.sin(au), math.cos(au)])
    dl, upl = np.array([0, -math.cos(al), -math.sin(al)]), np.array([0, -math.sin(al), math.cos(al)])

    def taper(tip, flat=2.3):
        def f(V):
            V = boxy(flat)(V)
            t = (1 - V[:, 1]) / 2                    # 0 at the back, 1 at the front (local -y)
            V[:, 0] *= 1 - (1 - tip) * t ** 1.5
            V[:, 2] *= 1 - (1 - tip) * 0.6 * t ** 1.5
            return V
        return f
    cu = hinge + du * L.upper_len * 0.5 + upu * L.upper_r[2] * 0.75
    k.add("upperjaw", ell(cu, (L.upper_r[0], L.upper_len * 0.5, L.upper_r[2]), rot=(-L.gape_up, 0, 0),
                          deform=taper(L.upper_tip)), BLUE, "head")
    cl = hinge + dl * L.lower_len * 0.5 - upl * L.lower_r[2] * 0.75
    k.add("lowerjaw", ell(cl, (L.lower_r[0], L.lower_len * 0.5, L.lower_r[2]), rot=(L.gape_down, 0, 0),
                          deform=taper(L.lower_tip)), L.lower_colour, "head")
    J = SimpleNamespace(hinge=hinge, du=du, upu=upu, dl=dl, upl=upl)
    if not getattr(L, "open", True):
        return J                                   # a mouth shut: the jaws' own meeting is its line
    # The hollow: carved back from the gap into the crease where the jaws meet
    mid = math.radians((L.gape_up - L.gape_down) / 2)
    dm = np.array([0, -math.cos(mid), math.sin(mid)])
    half = math.radians((L.gape_up + L.gape_down) / 2)
    def wedge(V):
        V = V.copy()
        t = np.clip((1 - V[:, 1]) / 2, 0, 1)
        V[:, 2] *= 0.35 + 0.9 * t
        return V
    ccut = hinge + dm * (L.lower_len * 0.42) + np.array([0, 0.01, 0])
    k.add("maw", ell(ccut, (L.maw_w, L.lower_len * 0.5, L.lower_len * 0.5 * math.tan(half) * 0.9),
                     rot=(-math.degrees(mid), 0, 0), deform=wedge), MOUTH, "head", carve=True)

    def inner(P):
        rel = P - hinge
        fwd = -rel[:, 1]                                   # forward of the hinge
        dz = rel[:, 2] - fwd * math.tan(mid)               # above or below the mouth's middle line
        gap = np.maximum(fwd * math.tan(half), L.crease) - L.lip
        within = inside(np.abs(dz) - gap, 0.0025) * inside(np.abs(P[:, 0]) - L.maw_w * 0.95, 0.0025)
        return within * smoothstep(-0.04, -0.03, fwd)
    k.paint(inner, MOUTH)

    def tongue(P):
        rel = P - hinge
        fwd = -rel[:, 1]
        dz = rel[:, 2] - fwd * math.tan(mid)
        return inner(P) * inside(dz + L.tongue_drop, 0.003) * inside(np.abs(P[:, 0]) - L.maw_w * 0.85, 0.003)
    k.paint(tongue, TONGUE)
    return J


def teeth(k, L, J):
    """The teeth: a few large white fangs down from the upper jaw and up from the lower, seated in the rim."""
    for j, (t, x, ln, w) in enumerate(L.upper_teeth):
        for s, side in ((1, "R"), (-1, "L")):
            base = J.hinge + J.du * L.upper_len * t + np.array([x * s, 0, 0]) + J.upu * L.tooth_seat
            k.add(f"fangU{side}{j}", fang(base, -J.upu + J.du * 0.15, ln, w, w * 0.8, up=(0, 1, 0)), TOOTH, "head",
                  fuse=False, surface="shell")
    for j, (t, x, ln, w) in enumerate(L.lower_teeth):
        for s, side in ((1, "R"), (-1, "L")):
            base = J.hinge + J.dl * L.lower_len * t + np.array([x * s, 0, 0]) - J.upl * L.tooth_seat
            k.add(f"fangD{side}{j}", fang(base, J.upl + J.dl * 0.15, ln, w, w * 0.8, up=(0, 1, 0)), TOOTH, "head",
                  fuse=False, surface="shell")


def nostrils(k, L, J):
    """Two small dark dots on the top of the upper jaw's tip."""
    tip = J.hinge + J.du * L.upper_len * 0.9 + J.upu * L.upper_r[2] * 1.4
    k.decal(tip + np.array([L.nostril_x, 0, 0]), J.upu + J.du * 0.6 + np.array([0.3, 0, 0]),
            (L.nostril_r, L.nostril_r * 0.8), NOSTRIL, eps=0.003, rings=4, segs=16, mirror=True)


def eye_open(iris, rim=0.83, iris_r=0.7, iris_c=(-0.08, -0.04), pupil_r=0.34):
    """The open eye: a dark rim, white, a big coloured iris toward the nose, a black pupil in it."""
    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(WHITE, (len(u), 1))
        d = np.sqrt((u - iris_c[0]) ** 2 + (v - iris_c[1]) ** 2)
        c[d < iris_r] = iris
        c[d < pupil_r] = INK
        c[r >= rim] = INK
        return c
    return eye


def brow_cut(top, slope):
    """An eye's outline cut straight along the lid v = top + slope u (u back): its reach at each angle."""
    def cut(th):
        s = math.sin(th) - slope * math.cos(th)
        return min(1.0, top / s) if s > 1e-6 else 1.0
    return cut


def lidded_eye(iris, cut, band=0.3, rim=0.8, iris_c=(-0.12, -0.12), iris_r=0.52, pupil_r=0.27):
    """An eye under a heavy lid (the line's evolutions): its outline cut along the lid, a thick black band along
    that top edge, white below, the iris and pupil toward the nose."""
    def eye(u, v):
        reach = np.array([cut(t) for t in np.arctan2(v, u)])
        rho = np.sqrt(u * u + v * v) / reach
        c = np.tile(WHITE, (len(u), 1))
        d = np.sqrt((u - iris_c[0]) ** 2 + (v - iris_c[1]) ** 2)
        c[d < iris_r] = iris
        c[d < pupil_r] = INK
        c[rho >= rim] = INK
        c[(v > 0) & (rho > 1 - band)] = INK
        return c
    return eye


def eyes(k, L):
    """The eyes' layer for a state: a bold oval each side, its glint a patch of its own in the open state."""
    de = unit(*L.eye_dir)
    E = on_ell(L.H, L.HR, de) + np.array(L.eye_shift, float)
    look = L.eye_look
    lid = srgb(L.lid)
    right = np.cross((0, 0, 1.0), de); right /= np.linalg.norm(right)
    up = np.cross(de, right)
    def lay(state):
        fn = eye_look(state, look, lid, INK, line=0.16)
        cut = L.eye_outline
        if cut is None:
            k.decal(E, de, L.eye_h, fn, eps=0.006, rings=20, segs=56, mirror=True)
        else:
            # The outline is not mirrored by the kit: the left eye is laid by hand, its picture and outline turned
            m = np.array([-1, 1, 1.0])
            k.decal(E, de, L.eye_h, fn, eps=0.006, rings=20, segs=56, outline=cut)
            k.decal(E * m, de * m, L.eye_h, lambda u, v: fn(-u, v), eps=0.006, rings=20, segs=56,
                    outline=lambda th: cut(math.pi - th))
        if state == "Open" and L.glint:
            g = E - right * 0.3 * L.eye_h[0] + up * 0.42 * L.eye_h[1]
            k.decal(g, de, (L.eye_h[0] * 0.24, L.eye_h[0] * 0.26), "#FFFFFF", eps=0.009, rings=5, segs=20,
                    mirror=True)
    return lay, E, de


def mark(k, L, E, de):
    """The dark bar running back from each eye (before the body's build)."""
    right = np.cross((0, 0, 1.0), de); right /= np.linalg.norm(right)
    up = np.cross(de, right)
    c = E + right * L.mark_back + up * L.mark_up
    k.decal(c, de, L.mark_h, MARK, eps=0.0035, rings=12, segs=48, mirror=True)


def hand(k, side, s, shoulder, elbow, wrist, radii, palm_r, fingers, finger_len, finger_r, color=BLUE, claws=None):
    """An arm forward and a little down: a tube from the shoulder to the wrist, a palm, short fingers forward."""
    m = np.array([s, 1, 1.0])
    k.add(f"arm{side}", tube(np.array([shoulder, elbow, wrist]) * m, radii), color, f"arm{side}")
    W = np.array(wrist, float)
    k.add(f"palm{side}", ell(W * m, palm_r), color, f"arm{side}")
    for j, d in enumerate(fingers):
        d = unit(*d)
        b = W + d * palm_r[1] * 0.7
        k.add(f"finger{side}{j}", tube(np.array([b, b + d * finger_len]) * m, [finger_r, finger_r * 0.85]), color,
              f"arm{side}")
        if claws:
            tip = b + d * (finger_len + finger_r * 0.4)
            k.add(f"claw{side}{j}", fang(tip * m, np.array(d) * m, claws[0], claws[1], claws[1] * 0.8), CLAW,
                  f"arm{side}", fuse=False, surface="shell")


def leg(k, side, s, thigh_c, thigh_r, shin, shin_r, foot_c, foot_r, toes, toe_r, color=BLUE, claws=None):
    """A leg: a thigh fused into the hips, a short shin, a broad foot with three toes (round, or white claws)."""
    m = np.array([s, 1, 1.0])
    k.add(f"thigh{side}", ell(np.array(thigh_c) * m, thigh_r), color, f"leg{side}")
    k.add(f"shin{side}", tube(np.array(shin) * m, shin_r), color, f"leg{side}")
    k.add(f"foot{side}", ell(np.array(foot_c) * m, foot_r), color, f"leg{side}")
    for j, t in enumerate(toes):
        t = np.array(t, float)
        k.add(f"toe{side}{j}", ell(t * m, toe_r), color, f"leg{side}")
        if claws:
            k.add(f"toeclaw{side}{j}", fang((t + (0, -toe_r[1] * 0.6, 0)) * m, np.array((0, -1, -0.3)) * m,
                                            claws[0], claws[1], claws[1] * 0.75), CLAW, f"leg{side}",
                  fuse=False, surface="shell")


def chevron(top, tip, half_w, slope, cy=0.0, reach=0.16, bottom=None):
    """A cream V on the chest: under top, over a V whose point is at tip, within reach of the body's axis (so the
    hands and the jaw in front of it keep their colour). bottom: a second V under it, for a chevron band."""
    def mask(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        front = inside(np.sqrt(x * x + (y - cy) ** 2) - reach, 0.004) * (y < cy)
        m = inside(z - top, 0.003) * inside(tip + slope * np.abs(x) - z, 0.003) * inside(np.abs(x) - half_w, 0.003)
        if bottom is not None:
            m = m * inside(z - (bottom + slope * np.abs(x)), 0.003)
        return front * m
    return mask


L = SimpleNamespace(
    H=np.array([0.0, -0.02, 0.44]), HR=np.array([0.124, 0.116, 0.1]),
    cheek_at=np.array([0.0, -0.045, -0.06]), cheek_r=(0.1, 0.085, 0.075),
    hinge=(0.0, -0.06, 0.39), gape_up=30, gape_down=12,
    upper_len=0.235, upper_r=(0.084, 0, 0.048), upper_tip=0.7,
    lower_len=0.205, lower_r=(0.08, 0, 0.042), lower_tip=0.72, lower_colour=BLUE,
    maw_w=0.058, crease=0.014, lip=0.006, tongue_drop=0.004,
    upper_teeth=[(0.76, 0.031, 0.046, 0.026)], lower_teeth=[(0.76, 0.032, 0.036, 0.022)], tooth_seat=0.012,
    nostril_x=0.022, nostril_r=0.008,
    eye_dir=(0.72, -0.42, 0.42), eye_shift=(0, 0, 0), eye_h=(0.034, 0.04), eye_look=eye_open(EYE_RED),
    lid=BLUE, glint=True, eye_outline=None,
    mark_back=0.03, mark_up=-0.008, mark_h=(0.046, 0.022),
)


def totodile(src, out):
    k = Kit(src, coat="scales")

    # The body: a pear, round in the belly, the hips low over short legs, a thick neck into the head
    def pear(V):
        V = V.copy()
        low = np.clip(-V[:, 2], 0, 1)
        V[:, :2] *= (1 + 0.14 * low)[:, None]
        return V
    k.add("torso", ell((0, 0.005, 0.19), (0.122, 0.112, 0.135), deform=pear), BLUE, "body")
    k.add("hips", ell((0, 0.015, 0.11), (0.122, 0.105, 0.075)), BLUE, "body")
    k.add("neck", tube([(0, 0.0, 0.27), (0, -0.025, 0.33), (0, -0.04, 0.37)], [0.095, 0.09, 0.085]), BLUE, "head")
    k.paint(chevron(0.335, 0.215, 0.12, 1.05, cy=0.0, reach=0.15), CREAM)

    # The legs: thighs fused into the hips, short shins, broad feet of three round toes
    for s, side in ((1, "R"), (-1, "L")):
        leg(k, side, s, (0.075, -0.005, 0.095), (0.062, 0.07, 0.068),
            [(0.08, -0.012, 0.06), (0.083, -0.02, 0.03)], [0.044, 0.042],
            (0.083, -0.045, 0.019), (0.046, 0.066, 0.021),
            [(0.083 + dx, -0.1, 0.017) for dx in (-0.026, 0.0, 0.026)], (0.016, 0.02, 0.015))

    # The arms: short, forward and a little down, three stubby fingers a hand
    for s, side in ((1, "R"), (-1, "L")):
        hand(k, side, s, (0.075, -0.02, 0.27), (0.11, -0.07, 0.24), (0.118, -0.13, 0.215), [0.036, 0.031, 0.029],
             (0.032, 0.03, 0.03), [(0.45, -1, 0.35), (0.1, -1, 0.1), (-0.25, -1, -0.15)], 0.024, 0.0125)

    # The tail: thick at the hips, back and down, curving out to the +X side; two red spines along it
    TAIL = np.array([(0, 0.08, 0.14), (0.02, 0.17, 0.09), (0.065, 0.25, 0.06), (0.11, 0.3, 0.05)])
    TAIL_R = [0.075, 0.055, 0.035, 0.018]
    k.add("tail", tube(TAIL, TAIL_R), BLUE, "tail")
    for j, (i, ln) in enumerate(((1, 0.06), (2, 0.05))):
        c = TAIL[i] + np.array([0, 0, TAIL_R[i] * 0.75])
        a = TAIL[i + 1] - TAIL[i - 1]
        spine(k, f"tailspine{j}", c, unit(*(a / np.linalg.norm(a) * 0.7 + np.array([0.15, 0, 1.0]))), ln, 0.045,
              0.024, "tail", across=tuple(np.cross(a, (0, 0, 1))))
    # Red spines down the back
    for j, (y, z, ln, lean) in enumerate([(0.1, 0.3, 0.055, 1), (0.12, 0.23, 0.06, -1), (0.13, 0.17, 0.05, 1)]):
        spine(k, f"spine{j}", (0.0, y, z), (0.12 * lean, 0.9, 0.55), ln, 0.05, 0.026, "body", across=(1, 0, 0))

    # The head, the open mouth, the teeth, the nostrils
    J = jaws(k, L)
    teeth(k, L, J)
    nostrils(k, L, J)

    # The eyes, each at the front of a dark bar running back
    lay, E, de = eyes(k, L)
    mark(k, L, E, de)

    k.build(voxel=VOXEL, smooth=4, band=0.006)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    totodile(*args())
