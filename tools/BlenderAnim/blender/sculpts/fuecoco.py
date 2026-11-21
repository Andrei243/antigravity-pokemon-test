# Fuecoco sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/fuecoco.py -- <the export's Fuecoco.glb> <out.glb>
# A squat red-orange crocodile on two stubby legs, head and body one mass: a great round cream dome of a head whose
# broad upper jaw juts forward over a wide open mouth (dark plum above, pink on its floor), two dark triangular
# nostrils on the snout's top, a cream square tooth up from each corner of the red lower jaw; dark slate oval eyes
# high on the dome; a tuft of two yellow strands of flame off the crown (a long curling one and a short one behind);
# stubby red arms held at the chest; a pale cream belly low between the legs and under the short thick tail; a yellow
# square on the chest and another on the flank by the tail; dark grey claws, three a foot.
#
# Written to be grown from (Crocalor and Skeledirge import it with sibling("fuecoco")): the line's colours, its head
# (dome, jaws, carved mouth), teeth, claws, eyes, flame and limbs are the functions below, placed by a frame of named
# numbers (L); the calls that build Fuecoco are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from types import SimpleNamespace
import numpy as np
from sculptkit import (Kit, ell, tube, hull, blade, outward, srgb, inside, smoothstep, unit, boxy, eye_look, polygon,
                       args)

# --- the line's colours ---------------------------------------------------------------------------------------------
RED, CREAM, BELLY = "#E4532E", "#F4ECD4", "#E6E1D2"
YELLOW = "#EDC43C"
MOUTH, TONGUE = "#8A3E58", "#E8909A"
TOOTH = "#F6F0DE"
CLAW = "#4A4E5C"
NOSTRIL = "#2A2630"
FLAME, GLOW_YELLOW = "#F8D23A", "#FFD23A"
INK, WHITE = srgb("#1C1E26"), np.array([1.0, 1.0, 1.0])
SLATE = srgb("#343A4C")
VOXEL = 0.0042


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


def join(*shapes):
    """Several shapes as one piece (one material)."""
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(v); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def fang(base, d, length, width, thick=None, up=(0, 0, 1), blunt=0.0):
    """A tooth or a claw: a flat-faced solid from a kite at its root to its point along d; blunt > 0 squares the
    tip off (a square tooth)."""
    d = unit(*d)
    a = np.cross(d, np.array(up, float)); a /= np.linalg.norm(a)
    b = np.cross(a, d)
    thick = thick if thick is not None else width * 0.8
    base = np.array(base, float)
    mid = base + d * length * 0.45
    tip = base + d * length
    pts = [base + a * width / 2, base - a * width / 2, base + b * thick / 2, base - b * thick / 2 - d * 0.002,
           mid + a * width * 0.36, mid - a * width * 0.36, mid + b * thick * 0.34, mid - b * thick * 0.34]
    if blunt > 0:
        pts += [tip + a * width * blunt / 2, tip - a * width * blunt / 2, tip + b * thick * blunt * 0.4,
                tip - b * thick * blunt * 0.4]
    else:
        pts.append(tip)
    return hull(pts)


def head(k, L):
    """
    The head: a dome (L.dome_col), the broad upper jaw raised by L.gape_up from the hinge (L.upper_col), the lower jaw
    dropped by L.gape_down (L.lower_col), and when L.open the mouth between them carved and painted (plum above, a pink
    floor). Gives back the jaws' frames for the teeth and marks.
    """
    k.add("dome", ell(L.H, L.HR, deform=boxy(L.dome_box)), L.dome_col, "head")
    hinge = np.array(L.hinge, float)
    au, al = math.radians(L.gape_up), math.radians(L.gape_down)
    du, upu = np.array([0, -math.cos(au), math.sin(au)]), np.array([0, math.sin(au), math.cos(au)])
    dl, upl = np.array([0, -math.cos(al), -math.sin(al)]), np.array([0, -math.sin(al), math.cos(al)])

    def taper(tip, flat=2.3):
        def f(V):
            V = boxy(flat)(V)
            t = (1 - V[:, 1]) / 2                    # 0 at the back, 1 at the front (local -y)
            V[:, 0] *= 1 - (1 - tip) * t ** 1.5
            V[:, 2] *= 1 - (1 - tip) * 0.5 * t ** 1.5
            return V
        return f
    cu = hinge + du * L.upper_len * 0.5 + upu * L.upper_r[2] * 0.7
    k.add("upperjaw", ell(cu, (L.upper_r[0], L.upper_len * 0.5, L.upper_r[2]), rot=(-L.gape_up, 0, 0),
                          deform=taper(L.upper_tip, L.jaw_box)), L.upper_col, "head")
    cl = hinge + dl * L.lower_len * 0.5 - upl * L.lower_r[2] * 0.7
    k.add("lowerjaw", ell(cl, (L.lower_r[0], L.lower_len * 0.5, L.lower_r[2]), rot=(L.gape_down, 0, 0),
                          deform=taper(L.lower_tip, L.jaw_box)), L.lower_col, "head")
    J = SimpleNamespace(hinge=hinge, du=du, upu=upu, dl=dl, upl=upl, au=au, al=al)
    if not L.open:
        return J
    mid = math.radians((L.gape_up - L.gape_down) / 2)
    dm = np.array([0, -math.cos(mid), math.sin(mid)])
    half = math.radians((L.gape_up + L.gape_down) / 2)

    def wedge(V):
        V = V.copy()
        t = np.clip((1 - V[:, 1]) / 2, 0, 1)
        V[:, 2] *= 0.35 + 0.9 * t
        return V
    ccut = hinge + dm * (L.lower_len * 0.42) + np.array([0, 0.008, 0])
    k.add("maw", ell(ccut, (L.maw_w, L.lower_len * 0.5, L.lower_len * 0.5 * math.tan(half) * 0.9),
                     rot=(-math.degrees(mid), 0, 0), deform=wedge), MOUTH, "head", carve=True)

    def inner(P):
        rel = P - hinge
        fwd = -rel[:, 1]
        dz = rel[:, 2] - fwd * math.tan(mid)
        gap = np.maximum(fwd * math.tan(half), L.crease) - L.lip
        within = inside(np.abs(dz) - gap, 0.002) * inside(np.abs(P[:, 0]) - L.maw_w * 0.95, 0.002)
        return within * smoothstep(-0.03, -0.022, fwd)
    k.paint(inner, MOUTH)

    def floor(P):
        rel = P - hinge
        fwd = -rel[:, 1]
        dz = rel[:, 2] - fwd * math.tan(mid)
        return inner(P) * inside(dz + L.tongue_drop, 0.002)
    k.paint(floor, TONGUE)
    J.mid, J.half = mid, half
    return J


def teeth(k, L, J):
    """Square teeth up from the lower jaw's rim (and, where the frame lists them, down from the upper)."""
    for j, (t, x, ln, w, blunt) in enumerate(L.lower_teeth):
        for s, side in ((1, "R"), (-1, "L")):
            base = J.hinge + J.dl * L.lower_len * t + np.array([x * s, 0, 0]) - J.upl * L.tooth_seat
            k.add(f"toothD{side}{j}", fang(base, J.upl + J.dl * 0.08, ln, w, w * 0.75, up=(0, 1, 0), blunt=blunt),
                  TOOTH, "head", fuse=False, surface="shell")
    for j, (t, x, ln, w, blunt) in enumerate(L.upper_teeth):
        for s, side in ((1, "R"), (-1, "L")):
            base = J.hinge + J.du * L.upper_len * t + np.array([x * s, 0, 0]) + J.upu * L.tooth_seat
            k.add(f"toothU{side}{j}", fang(base, -J.upu + J.du * 0.08, ln, w, w * 0.75, up=(0, 1, 0), blunt=blunt),
                  TOOTH, "head", fuse=False, surface="shell")


def nostrils(k, L, J, color=NOSTRIL):
    """Two dark triangles on the top of the upper jaw near its tip, points toward the eyes."""
    top = J.upu * math.cos(0.0)
    c = J.hinge + J.du * L.upper_len * L.nostril_t + J.upu * L.upper_r[2] * L.nostril_lift
    n = J.upu + J.du * 0.35
    k.decal(c + np.array([L.nostril_x, 0, 0]), n, (L.nostril_r, L.nostril_r),
            color, up=J.du, eps=0.0018, rings=6, segs=36, mirror=True,
            outline=polygon([(-0.9, -0.55), (0.9, -0.55), (0.0, 0.95)]))


def eye_open(eye=SLATE, glint=None):
    """The open eye: a solid dark oval (Fuecoco's), the glint a patch of its own."""
    def look(u, v):
        return np.tile(eye, (len(u), 1))
    return look


def eye_frame(L):
    de = unit(*L.eye_dir)
    E = on_ell(L.H, L.HR, de) + np.array(L.eye_shift, float)
    right = np.cross((0, 0, 1.0), de); right /= np.linalg.norm(right)
    up = np.cross(de, right)
    return E, de, right, up


def eyes(k, L):
    """The eyes' layer for a state: an oval each side, its glint a patch of its own in the open state."""
    E, de, right, up = eye_frame(L)
    lid = srgb(L.lid)
    def lay(state):
        fn = eye_look(state, L.eye_look, lid, INK, line=0.17)
        k.decal(E, de, L.eye_h, fn, eps=0.0018, rings=20, segs=56, mirror=True)
        if state == "Open" and L.glint:
            g = E + right * L.glint[0] * L.eye_h[0] + up * L.glint[1] * L.eye_h[1]
            k.decal(g, de, (L.eye_h[0] * L.glint[2], L.eye_h[0] * L.glint[2]), "#FFFFFF", eps=0.0032, rings=5,
                    segs=20, mirror=True)
    return lay


def flame_strand(pts, radii, flat_dir):
    """A strand of flame: a tapering tube flattened across flat_dir (its face shows the other way)."""
    return outward(blade(pts, radii, flat_dir, 0.42))


def arm(k, side, s, shoulder, wrist, radii, paw_r, color=RED, claws=None, claw_col=CLAW, paw_col=None):
    """A stubby arm forward and down to a round paw; claws: (length, width, spread) of three loose claws."""
    m = np.array([s, 1, 1.0])
    k.add(f"arm{side}", tube(np.array([shoulder, (np.array(shoulder) + np.array(wrist)) / 2, wrist]) * m, radii),
          color, f"arm{side}")
    W = np.array(wrist, float)
    k.add(f"paw{side}", ell(W * m, paw_r), paw_col or color, f"arm{side}")
    if claws:
        ln, w, spread, d0 = claws
        for j, a in enumerate((-1, 0, 1)):
            d = unit(*(np.array(d0) + np.array([a * spread, 0, 0])))
            b = W + d * paw_r[1] * 0.75 + np.array([a * paw_r[0] * 0.55, 0, 0])
            k.add(f"handclaw{side}{j}", fang(b * m, d * m, ln, w, w * 0.8), claw_col, f"arm{side}",
                  fuse=False, surface="shell")


def leg(k, side, s, thigh_c, thigh_r, foot_c, foot_r, claws, color=RED, claw_col=CLAW, bone=None, foot_col=None):
    """A leg: a fat haunch fused into the hips, a broad foot under it, loose claws along its front."""
    m = np.array([s, 1, 1.0])
    bone = bone or f"leg{side}"
    k.add(f"thigh{side}", ell(np.array(thigh_c) * m, thigh_r), color, bone)
    k.add(f"foot{side}", ell(np.array(foot_c) * m, foot_r), foot_col or color, bone)
    for j, (dx, ln, w, d) in enumerate(claws):
        base = np.array(foot_c) + np.array([dx, -foot_r[1] * 0.78, -foot_r[2] * 0.3])
        k.add(f"toeclaw{side}{j}", fang(base * m, np.array(unit(*d)) * m, ln, w, w * 0.8), claw_col, bone,
              fuse=False, surface="shell")


L = SimpleNamespace(
    H=np.array([0.0, 0.0, 0.272]), HR=np.array([0.118, 0.108, 0.1]), dome_box=2.2, dome_col=CREAM,
    hinge=(0.0, -0.035, 0.205), gape_up=12, gape_down=24,
    upper_len=0.15, upper_r=(0.1, 0, 0.042), upper_tip=0.78, upper_col=CREAM, jaw_box=2.4,
    lower_len=0.145, lower_r=(0.094, 0, 0.036), lower_tip=0.8, lower_col=RED,
    open=True, maw_w=0.066, crease=0.012, lip=0.005, tongue_drop=-0.006,
    lower_teeth=[(0.84, 0.046, 0.024, 0.02, 0.7)], upper_teeth=[], tooth_seat=0.006,
    nostril_t=0.86, nostril_lift=1.05, nostril_x=0.03, nostril_r=0.0105,
    eye_dir=(0.42, -0.72, 0.52), eye_shift=(0, 0, 0), eye_h=(0.0152, 0.0235), eye_look=eye_open(),
    lid=CREAM, glint=(-0.3, 0.45, 0.32),
)


def fuecoco(src, out):
    k = Kit(src, coat="scales")

    # The body: a squat pear under the head, round in the belly, the hips low over stubby legs
    def pear(V):
        V = V.copy()
        low = np.clip(-V[:, 2], 0, 1)
        V[:, :2] *= (1 + 0.12 * low)[:, None]
        return V
    k.add("torso", ell((0, 0.01, 0.15), (0.108, 0.1, 0.098), deform=pear), RED, "body")
    k.add("hips", ell((0, 0.02, 0.085), (0.11, 0.098, 0.062)), RED, "body")
    k.add("throat", ell((0, -0.03, 0.17), (0.09, 0.075, 0.06)), RED, "body")

    # The legs: fat haunches fused into the hips, broad feet, three dark claws each
    for s, side in ((1, "R"), (-1, "L")):
        leg(k, side, s, (0.066, -0.0, 0.058), (0.055, 0.06, 0.056), (0.068, -0.02, 0.018), (0.045, 0.058, 0.02),
            [(-0.026, 0.022, 0.017, (-0.15, -1, -0.45)), (0.0, 0.024, 0.018, (0, -1, -0.45)),
             (0.026, 0.022, 0.017, (0.15, -1, -0.45))])

    # The arms: stubby, held at the chest, forward and a little down, the paws notched by three small claws
    for s, side in ((1, "R"), (-1, "L")):
        arm(k, side, s, (0.078, -0.04, 0.165), (0.084, -0.098, 0.14), [0.03, 0.027, 0.025], (0.027, 0.026, 0.025),
            color=RED, claws=(0.012, 0.011, 0.25, (0.0, -1, -0.5)), claw_col=RED)

    # The tail: thick at the hips, short, back and out to the model's +X side
    TAIL = np.array([(0, 0.07, 0.085), (0.02, 0.13, 0.052), (0.045, 0.168, 0.036), (0.058, 0.178, 0.032)])
    TAIL_R = [0.06, 0.046, 0.033, 0.026]
    k.add("tail", tube(TAIL, TAIL_R), RED, "tail")

    # The pale belly low between the legs and under the tail
    def belly(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        under = inside(z - (0.072 - 0.18 * np.clip(y - 0.06, 0, None)), 0.003)
        front = inside(np.abs(x) - 0.075, 0.004) * (y < 0.06)
        tail = (y >= 0.06)
        return under * (front + tail * inside(np.abs(x - 0.25 * (y - 0.06)) - 0.05, 0.004))
    k.paint(belly, BELLY, on=["torso", "hips", "tail"])

    # The head, its open mouth, the two square teeth, the nostrils
    J = head(k, L)
    # The cream cap stops at the jaws' level behind the mouth: below it the head is red
    def cheeks(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        keep = np.maximum((y > -0.02).astype(float), (np.abs(x) > 0.075).astype(float))
        return inside(z - (0.205 + 0.25 * np.clip(y + 0.0, 0, None)), 0.0025) * keep * (z > 0.15)
    k.paint(cheeks, RED)
    teeth(k, L, J)
    nostrils(k, L, J)

    # The yellow squares: one on the chest, one on the flank by the tail
    k.decal((0.03, -0.098, 0.105), (0.25, -1, -0.15), (0.026, 0.024), YELLOW, eps=0.0018, rings=8, segs=40,
            outline=polygon([(-1, -0.85), (0.95, -1), (1, 0.9), (-0.9, 1)]))
    k.decal((0.085, 0.085, 0.075), (1, 0.35, 0.25), (0.022, 0.017), YELLOW, eps=0.0018, rings=8, segs=40,
            outline=polygon([(-1, -0.8), (1, -1), (0.9, 1), (-0.95, 0.85)]))

    # The flame: a long strand curling up and back off the crown, a short one leaning back behind it
    top = on_ell(L.H, L.HR, (0, 0.15, 1))
    longs = [top + (0.0, 0.0, -0.01), top + (0.006, -0.012, 0.03), top + (0.012, -0.004, 0.052),
             top + (0.016, 0.02, 0.068)]
    shorts = [top + (0.012, 0.022, -0.012), top + (0.022, 0.035, 0.018), top + (0.034, 0.052, 0.04)]
    flat = unit(0.7, -0.3, 0)
    fl = join(flame_strand(longs, [0.019, 0.018, 0.014, 0.008], flat),
              flame_strand(shorts, [0.015, 0.013, 0.007], flat))
    k.add("flame", fl, FLAME, "head", fuse=False, glow=GLOW_YELLOW)

    lay = eyes(k, L)
    k.build(voxel=VOXEL, smooth=4, band=0.0045, wband=0.035)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    fuecoco(*args())
