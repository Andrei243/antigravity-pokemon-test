# Scorbunny sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/scorbunny.py -- <the export's Scorbunny.glb> <out.glb>
#
# From the references: a small white rabbit, its head the largest mass, round and wide with white tufts pointing out
# at the temples and at the lower cheeks; two long ears rising in a V, white below with a yellow stripe up their fronts,
# a thin pale orange band and then orange over their top two fifths; a yellow plaster across the snout between the
# eyes; round eyes, black above and red below, with a white glint; a wide open mouth, dark within, a pink tongue; a little
# orange tuft at the throat. A small white body, short stubby arms, longish legs on long feet whose fronts and soles are
# orange with a yellow pad under the sole; a little white tuft of a tail.
# The export's joints: body (0, 0, 0.2), head (0, 0, 0.27), arms (+-0.045, 0, 0.23), legs (+-0.04, 0, 0.15); no ear or
# tail bone (the ears are on the head, the tail on the body). About 0.55 tall.
#
# Written to be grown from (raboot.py and cinderace.py import it with sibling("scorbunny")): the line's colours, its
# ears, tufts, feet, eyes and tail are the functions below; the calls that build Scorbunny are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import (Kit, ell, tube, hull, polygon, srgb, inside, smoothstep, unit, boxy, eye_look, args)

# --- the line's colours ---------------------------------------------------------------------------------------------
WHITE, ORANGE, PALE, YELLOW = "#F4F3EF", "#EE6A2A", "#F8B27A", "#F6CE58"
RED, INK, TONGUE, MOUTH, NOSE = "#E0432C", "#17141A", "#EE8A98", "#5A2238", "#F0A090"
GLINT = np.array([1.0, 1.0, 1.0])


def turn(rot):
    """A turn in degrees (x, y, z) as a matrix that takes the part's own frame into the model's."""
    return np.array(Euler([math.radians(a) for a in rot]).to_matrix())


def on_ell(c, r, d):
    """The point of an (unturned) ellipsoid along the direction d from its centre."""
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


# --- the ears ---------------------------------------------------------------------------------------------------------
def frame(d, front):
    """A right-handed frame (as a matrix of columns x, y, z) whose z runs along d and whose -y looks toward front."""
    z = unit(*d)
    y = -np.array(front, float); y = y - z * (y @ z); y /= np.linalg.norm(y)
    return np.stack([np.cross(y, z), y, z], axis=1)


def ear(k, base, length, width, thick, rot, bone, colour, widest=0.55, root=0.5, tipr=0.25, m=None):
    """A long rabbit's ear on +X (mirrored) from base, turned by rot (degrees: x tips it forward, y out to +X, z turns
    its face) or set in the frame m (frame(...)): narrow at the root, widest at `widest` of its length, drawn to a
    rounded point. Returns (base, m) so the caller can paint in the ear's own frame (z along it, -y its front)."""
    base = np.array(base, float)
    m = turn(rot) if m is None else m

    def shape(V):
        V = boxy(2.3)(V)
        V = V.copy()
        t = (V[:, 2] + 1) / 2
        f = np.where(t < widest, root + (1 - root) * np.sin(0.5 * math.pi * t / widest),
                     tipr + (1 - tipr) * np.cos(0.5 * math.pi * (t - widest) / (1 - widest)) ** 0.8)
        V[:, 0] *= f
        V[:, 1] *= 0.6 + 0.4 * f
        return V
    C = base + m @ np.array([0, 0, length / 2])
    V, F = ell((0, 0, 0), (width, thick, length / 2), deform=shape)
    k.add("ear", (V @ m.T + C, F), colour, bone, mirror=True)
    k.add("earroot", tube([base - m @ np.array([0, 0, 0.012]), base + m @ np.array([0, 0, length * 0.16])],
                          [width * 0.5, width * 0.55]), colour, bone, mirror=True)
    return base, m


def ear_frame(base, m):
    """A function taking the model's points into the ear's own frame (on either side)."""
    def local(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        return (Q - base) @ m
    return local


def ear_band(k, base, m, length, width, thick, lo, hi, colour, on="ear", edge=0.0025):
    """Paint the ear from lo to hi of its length (both faces), kept to the ear by place."""
    local = ear_frame(base, m)

    def mask(P):
        L = local(P)
        s = L[:, 2] / length
        return (inside(lo - s, edge) * inside(s - hi, edge) * (np.abs(L[:, 1]) < thick * 3)
                * (np.abs(L[:, 0]) < width * 1.4) * (np.abs(P[:, 0]) > 0.006))
    k.paint(mask, colour, on=[on])


def ear_stripe(k, base, m, length, width, thick, lo, hi, half, colour, on="ear"):
    """A stripe up the ear's front (its -y face) from lo to hi of its length, half of the ear's width wide by `half`,
    narrowing at both ends."""
    local = ear_frame(base, m)

    def mask(P):
        L = local(P)
        s = L[:, 2] / length
        t = np.clip((s - lo) / (hi - lo), 0, 1)
        w = width * half * np.sin(math.pi * t) ** 0.5
        return (inside(np.abs(L[:, 0]) - w, 0.002) * inside(L[:, 1] + thick * 0.15, 0.002)
                * (s > lo) * (s < hi) * (np.abs(L[:, 1]) < thick * 3) * (np.abs(P[:, 0]) > 0.006))
    k.paint(mask, colour, on=[on])


# --- tufts, feet, tail ----------------------------------------------------------------------------------------------
def tuft(k, name, root, dirs, length, width, colour, bone, thick=None):
    """Pointed tufts of fur from root along each direction (on +X; mirrored): loose flat-faced spikes, flat across the
    model's length so the front and back cameras see their faces."""
    root = np.array(root, float)
    thick = thick or width * 0.7
    for i, d in enumerate(dirs):
        d = unit(*d)
        a = np.cross(d, (0, 1.0, 0)); a /= np.linalg.norm(a)
        b = np.cross(d, a)
        L = length[i] if hasattr(length, "__len__") else length
        mid = root + d * L * 0.35
        pts = [root + a * width, root - a * width, root + b * thick, root - b * thick,
               mid + a * width * 0.8, mid - a * width * 0.8, mid + b * thick * 0.7, mid - b * thick * 0.7,
               root + d * L]
        k.add(f"{name}{i}", hull(pts), colour, bone, fuse=False, mirror=True)


def foot(k, ankle, half, colour, cap, bone="leg{s}", cap_from=0.35, pad=None):
    """A long foot on +X (mirrored) centred under the ankle and forward of it: its front (the toes) and its sole in
    `cap`, the line between crisp on both sides; a pad colour under the sole."""
    a = np.array(ankle, float)
    c = a + np.array([0, -half[1] * 0.45, 0])
    c[2] = half[2]

    def toe(V):
        V = V.copy()
        f = np.clip(-V[:, 1], 0, 1)
        V[:, 2] *= 1 - 0.25 * f ** 2
        V[:, 2] += 0.12 * np.clip(V[:, 2], -1, 0) * -1          # a flatter sole
        return V
    k.add("foot", ell(c, half, deform=toe), colour, bone, mirror=True)
    front = c[1] - half[1] * cap_from                 # the toe cap's back edge, before the foot's middle

    def capmask(P):
        near = (np.abs(np.abs(P[:, 0]) - c[0]) < half[0] * 1.4) * (P[:, 2] < half[2] * 2.2)
        return near * np.maximum(inside(P[:, 1] - front, 0.002), inside(P[:, 2] - half[2] * 0.32, 0.002))
    k.paint(capmask, cap, on=["foot"])
    if pad:
        def padmask(P):
            near = np.abs(np.abs(P[:, 0]) - c[0]) < half[0] * 0.6
            return near * inside(P[:, 2] - 0.006, 0.002) * inside(np.abs(P[:, 1] - c[1]) - half[1] * 0.35, 0.002)
        k.paint(padmask, pad, on=["foot"])
    return c


def tail(k, c, r, colour, bone):
    """A little round tuft of a tail."""
    k.add("tail", ell(c, r, deform=boxy(1.8)), colour, bone)


# --- eyes ---------------------------------------------------------------------------------------------------------------
def eye_open(ground, top=None, low=None, split=-0.05, glint=(-0.25, 0.32, 0.26), pupil=None):
    """An open eye's colour (u outward, v up): its ground, the top above `split` in `top`, the iris below in `low`
    (each optional), a pupil (u, v, ru, rv) and a white glint (u, v, r)."""
    G = srgb(ground) if isinstance(ground, str) else ground
    T = None if top is None else (srgb(top) if isinstance(top, str) else top)
    Lo = None if low is None else (srgb(low) if isinstance(low, str) else low)

    def eye(u, v):
        c = np.tile(G, (len(u), 1))
        if Lo is not None:
            c[v < split] = Lo
        if T is not None:
            c[v >= split] = T
        if pupil is not None:
            c[((u - pupil[0]) / pupil[2]) ** 2 + ((v - pupil[1]) / pupil[3]) ** 2 < 1] = INK3
        if glint is not None:
            c[(u - glint[0]) ** 2 + (v - glint[1]) ** 2 < glint[2] ** 2] = GLINT
        return c
    return eye


INK3 = srgb(INK)


def lay_eyes(k, state, E, de, half, look_open, lid, ink, outline=None, eps=0.002, fierce=None):
    """Both eyes for a state: a patch a side at E (and its mirror) facing de, laid by hand so that an outline that is
    not symmetric (u outward) is mirrored too. fierce: a colour function to use in place of eye_look's Fierce."""
    for side in (1, -1):
        mm = np.array([side, 1, 1.0])
        c, n = np.array(E, float) * mm, np.array(de, float) * mm
        ol = None
        if outline is not None:
            ol = polygon([(side * u, v) for u, v in (outline if side == 1 else outline[::-1])])
        if state == "Fierce" and fierce is not None:
            base = fierce
        else:
            base = eye_look(state, look_open, lid, ink)
        look = base if side == 1 else (lambda f: (lambda u, v: f(-u, v)))(base)
        k.decal(c, n, half, look, eps=eps, outline=ol, rings=20, segs=56)


# --- Scorbunny's frame --------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.012, 0.345])            # the head's centre, a tenth over the measure
HR = np.array([0.09, 0.076, 0.08])
DE = unit(0.52, -0.82, 0.06)                 # where the eyes look out
EYE_HALF = (0.0195, 0.0215)
EAR_LEN, EAR_W, EAR_T = 0.175, 0.03, 0.012


def scorbunny(src, path):
    k = Kit(src, coat="fur")

    # --- the head: round and wide, a little snout, cheeks low -------------------------------------------------------
    def jaw(V):
        V = boxy(2.2)(V)
        V = V.copy()
        V[:, 0] *= 1 + 0.08 * np.clip(-V[:, 2], 0, 1)
        return V
    k.add("head", ell(H, HR, deform=jaw), WHITE, "head")
    k.add("snout", ell(H + (0, -0.056, -0.004), (0.036, 0.026, 0.025)), WHITE, "head")
    k.add("cheek", ell(H + (0.044, -0.032, -0.032), (0.038, 0.034, 0.032)), WHITE, "head", mirror=True)

    # --- a small body, a little round belly ---------------------------------------------------------------------------
    def pear(V):
        V = V.copy(); s = 1 + 0.12 * np.clip(-V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("torso", ell((0, 0.004, 0.205), (0.05, 0.044, 0.062), deform=pear), WHITE, "body")
    k.add("neck", ell((0, -0.004, 0.258), (0.034, 0.032, 0.026)), WHITE, "body")
    k.add("hips", ell((0, 0.006, 0.158), (0.05, 0.042, 0.032)), WHITE, "body")

    # --- the orange tuft at the throat: two little points under the chin --------------------------------------------
    for i, s in enumerate((1, -1)):
        k.add(f"throat{i}", hull([(0, -0.044, 0.262), (0, -0.04, 0.248), (0, -0.032, 0.256),
                                  (0.016 * s, -0.05, 0.246), (0.014 * s, -0.046, 0.236), (0.004 * s, -0.052, 0.252)]),
              ORANGE, "body", fuse=False)

    # --- short stubby arms bent forward, the paws before the chest ---------------------------------------------------
    sh, el, wr = np.array([0.044, -0.002, 0.236]), np.array([0.066, -0.022, 0.208]), np.array([0.052, -0.05, 0.19])
    k.add("arm", tube([sh, el, wr], [0.017, 0.016, 0.0155]), WHITE, "arm{s}", mirror=True)
    k.add("paw", ell(wr + (0.002, -0.006, -0.004), (0.019, 0.018, 0.019)), WHITE, "arm{s}", mirror=True)

    # --- longish legs, thighs fused into the hips, long feet orange in front and under ----------------------------------
    k.add("thigh", ell((0.036, 0.004, 0.148), (0.035, 0.037, 0.044)), WHITE, "leg{s}", mirror=True)
    k.add("leg", tube([(0.038, 0.002, 0.13), (0.041, -0.004, 0.075), (0.042, -0.008, 0.035)], [0.025, 0.022, 0.021]),
          WHITE, "leg{s}", mirror=True)
    foot(k, (0.042, -0.008, 0.03), (0.027, 0.052, 0.022), WHITE, ORANGE, pad=YELLOW)

    # --- the tail -----------------------------------------------------------------------------------------------------
    tail(k, (0, 0.048, 0.168), (0.02, 0.019, 0.019), WHITE, "body")

    # --- the ears: white, a yellow stripe up the front, a pale band and an orange top ---------------------------------
    base, m = ear(k, (0.034, 0.004, 0.41), EAR_LEN, EAR_W, EAR_T, (-4, 19, 6), "head", WHITE)
    ear_band(k, base, m, EAR_LEN, EAR_W, EAR_T, 0.6, 1.1, ORANGE)
    ear_band(k, base, m, EAR_LEN, EAR_W, EAR_T, 0.52, 0.6, PALE)
    ear_stripe(k, base, m, EAR_LEN, EAR_W, EAR_T, 0.1, 0.5, 0.32, YELLOW)

    # --- white tufts at the temples and at the lower cheeks -------------------------------------------------------------
    tuft(k, "temple", H + (0.074, 0.0, 0.03), [(1, 0.1, 0.55), (1, 0.15, 0.05)], [0.036, 0.028], 0.019, WHITE, "head")
    tuft(k, "cheektuft", H + (0.078, -0.012, -0.032), [(1, -0.1, 0.2), (1, -0.05, -0.35)], [0.03, 0.024], 0.018, WHITE,
         "head")

    # --- the face: the yellow plaster, a pink nose, the open mouth ----------------------------------------------------
    PLASTER = polygon([(-1, 0.55), (-0.9, 1), (0.9, 1), (1, 0.55), (1, -0.55), (0.9, -1), (-0.9, -1), (-1, -0.55)])
    k.decal(on_ell(H + (0, -0.056, -0.004), (0.036, 0.026, 0.025), (0, -0.85, 0.5)),
            unit(0, -0.85, 0.5), (0.019, 0.0105), YELLOW, up=(0, 0.5, 0.85), eps=0.002, outline=PLASTER, rings=10, segs=48)
    k.decal(on_ell(H + (0, -0.056, -0.004), (0.036, 0.026, 0.025), (0, -1, -0.05)), unit(0, -1, 0),
            (0.0055, 0.004), NOSE, eps=0.002, rings=6, segs=24)
    MO = polygon([(-1, 0.6), (-0.5, 0.95), (0.5, 0.95), (1, 0.6), (0.75, -0.3), (0.4, -0.85), (0, -1),
                  (-0.4, -0.85), (-0.75, -0.3)])
    CM, CT = srgb(MOUTH), srgb(TONGUE)

    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.62) ** 2 + ((v + 0.85) / 0.55) ** 2 < 1] = CT
        return c
    k.decal(H + (0, -0.08, -0.04), unit(0, -1, -0.25), (0.025, 0.018), mouth, eps=0.002, outline=MO,
            rings=12, segs=56)

    k.build(voxel=0.0045, band=0.005, wband=0.03)

    E = on_ell(H, HR, DE)
    look = eye_open(INK, top=INK, low=RED, split=-0.25, glint=(-0.2, 0.3, 0.27))
    lid = srgb(WHITE)
    k.eye_states(lambda state: lay_eyes(k, state, E, DE, EYE_HALF, look, lid, INK3))
    k.export(path)


if __name__ == "__main__":
    scorbunny(*args())
