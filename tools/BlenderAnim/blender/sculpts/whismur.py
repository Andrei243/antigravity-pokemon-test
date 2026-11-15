# Whismur sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/whismur.py -- <the export's Whismur.glb> <out.glb>
#
# The design, from the references: a lilac ball that is all head and body at once; two big round ears on short
# stalks out of the crown's shoulders, each a thick disc flopped out and up, lilac behind and its whole front face a
# flat yellow plate; eyes that are two dark four-pointed sparkles; a wide open mouth, dark red with a pink tongue,
# its outline lobed at the four corners; two small round hands as bumps low on the front; two oval feet under the
# ball, lilac with yellow toes.
#
# Written to be grown from (loudred.py imports it through sibling): the frame is in named constants and small
# functions (a part laid on a frame of its own, the disc of an ear on its stalk, the mouth's lobed outline).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, boxy, eye_look, polygon, args

# --- colours ----------------------------------------------------------------------------------------------------------
LILAC = "#D9B9E0"
YELLOW = "#F2D06A"
INK = "#2A2230"
MOUTH = "#8E2836"
TONGUE = "#E07486"

# --- the frame (the skeleton's joints: body (0, 0, 0.18), head (0, 0, 0.28), ears (+-0.06, 0, 0.29),
#     arms (+-0.11, -0.04, 0.16), legs (+-0.06, 0, 0.06)) -------------------------------------------------------------
C = np.array([0.0, 0.0, 0.198])          # the ball's middle
R = np.array([0.172, 0.158, 0.166])      # its radii
EAR_C = np.array([0.168, 0.018, 0.42])  # an ear's disc: its middle
EAR_LONG = unit(0.62, 0.0, 0.79)         # the disc's long axis, out and up
EAR_FACE = unit(0.2, -0.92, 0.33)        # the way its yellow face looks
EAR_R = (0.112, 0.092, 0.046)            # half sizes: along, across, and its thickness
EYE_X, EYE_Z = 0.066, 0.012              # an eye's middle: out from the face's middle, up from C
EYE_HALF = (0.03, 0.03)
MOUTH_Z, MOUTH_HALF = -0.048, (0.042, 0.026)


def frame(face, along):
    """A right-handed frame (along, across, face) for a part whose face looks along `face`."""
    f = unit(*face)
    a = np.array(along, float); a = a - f * (a @ f); a /= np.linalg.norm(a)
    return a, np.cross(f, a), f


def on_frame(c, r, ex, ey, ez, deform=None, sub=4):
    """An ellipsoid of half sizes r laid on the frame (ex, ey, ez) about c."""
    V, F = ell((0, 0, 0), (1, 1, 1), deform=deform, sub=sub)
    V = V * np.array(r)
    return V @ np.stack([ex, ey, ez]) + np.array(c, float), F


def flat_front(amount=0.3):
    """A lentil: its front half (+z of the unit sphere) pressed nearly flat, its back left round."""
    def f(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * amount, V[:, 2])
        return V
    return f


def front(x, z, lift=0.0, c=None, r=None):
    """The ball's front point at (x, z), lifted off it along -y."""
    c = C if c is None else c; r = R if r is None else r
    zn = (z - c[2]) / r[2]
    q = max(0.0, 1 - zn * zn - (x / r[0]) ** 2)
    return np.array([x, c[1] - r[1] * math.sqrt(q) - lift, z])


def normal_at(p, c=None, r=None):
    c = C if c is None else c; r = R if r is None else r
    n = (p - c) / r ** 2
    return n / np.linalg.norm(n)


def egg(V):
    """Round, a little fuller below its middle."""
    V = boxy(2.1)(V)
    s = 1 + 0.05 * np.clip(-V[:, 2], 0, 1) - 0.03 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V


def add_body(k, color=LILAC):
    k.add("body", ell(C, R, deform=egg), color, "body")
    # The upper half on the head's bone, inside the ball, so the face nods with the head
    k.add("crown", ell(C + (0, 0, 0.05), R * (0.86, 0.86, 0.7)), color, "head")


def add_ear(k, c=EAR_C, long=EAR_LONG, face=EAR_FACE, r=EAR_R, root=(0.055, 0.012, 0.315), stalk_r=(0.048, 0.042),
            back=LILAC, plate=YELLOW, name="ear"):
    """
    An ear: a thick disc on a short stalk out of the crown, round behind and pressed flat in front, its front face a
    plate of another colour (painted with a crisp edge in the disc's own frame, so nothing else is touched).
    Returns the disc's frame for whatever is painted on the plate after.
    """
    ex, ey, ez = frame(face, long)
    c = np.array(c, float)
    k.add(name, on_frame(c, r, ex, ey, ez, deform=flat_front(0.42)), back, name + "{s}", mirror=True)
    tip = c - ex * r[0] * 0.5 - ez * r[2] * 0.75          # into the disc's round back, never through its face
    k.add("stalk" + name, tube([root, (np.array(root) + tip) / 2, tip], [stalk_r[0], stalk_r[1] * 1.05, stalk_r[1]]),
          back, name + "{s}", mirror=True)

    if plate is not None:
        k.paint(lambda P: plate_mask(P, c, r, ex, ey, ez), plate, on=[name])
    return c, ex, ey, ez


def plate_mask(P, c, r, ex, ey, ez, depth=0.002, reach=1.2):
    """The flat front face of a disc on its frame (on either side of the model), with a crisp edge: whatever of the
    disc lies in front of its middle by more than `depth`."""
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    q = Q - c
    a, b, n = q @ ex, q @ ey, q @ ez
    disc = inside(np.hypot(a / (r[0] * reach), b / (r[1] * reach)) - 1, 0.002) * inside(np.abs(n) - r[2] * 1.5, 0.002)
    return disc * (1 - inside(n - depth, 0.0022))


def add_hands(k, at=(0.118, -0.118, 0.142), r=(0.046, 0.04, 0.042), color=LILAC):
    """Small round hands, bumps low on the front of the ball."""
    k.add("hand", ell(at, r), color, "arm{s}", mirror=True)


def add_feet(k, at=(0.078, -0.045, 0.03), r=(0.056, 0.072, 0.032), turn=14, color=LILAC, toe=YELLOW):
    """Two oval feet under the ball, their toes turned a little out and yellow."""
    k.add("leg", tube([(0.065, 0.0, 0.08), (0.074, -0.03, 0.045)], [0.05, 0.045]), color, "leg{s}", mirror=True)
    k.add("foot", ell(at, r, rot=(0, 0, -turn)), color, "leg{s}", mirror=True)
    if toe is not None:
        k.paint(lambda P: toe_mask(P, at, r, turn), toe)


def toe_mask(P, at, r, turn, start=0.022, reach=1.25, top=0.075):
    """The front of a foot (on either side), from `start` ahead of its middle on, with a crisp edge."""
    at = np.array(at, float)
    d = unit(math.sin(math.radians(turn)), -math.cos(math.radians(turn)), 0)
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    q = Q - at
    return inside(np.linalg.norm(q / (np.array(r) * reach), axis=1) - 1, 0.003) * (1 - inside(q @ d - start, 0.0025)) \
        * (P[:, 2] < top)


# --- the face: patches on the fused ball, laid before the body's build ----------------------------------------------
CL, CI, CM, CT = srgb(LILAC), srgb(INK), srgb(MOUTH), srgb(TONGUE)


def sparkle(u, v):
    """A four-pointed sparkle: four tapering dark points out from a small gap in the middle."""
    c = np.tile(CL, (len(u), 1))
    au, av = np.abs(u), np.abs(v)
    arm = ((av < 0.34 * (1 - au)) & (au > 0.16)) | ((au < 0.34 * (1 - av)) & (av > 0.16))
    c[arm & (np.hypot(u, v) < 0.98)] = CI
    return c


MOUTH_OUTLINE = polygon([(1.0, 0.1), (0.86, 0.98), (0.4, 0.86), (0.0, 0.5), (-0.4, 0.86), (-0.86, 0.98),
                         (-1.0, 0.1), (-0.84, -0.98), (-0.4, -0.86), (0.0, -0.62), (0.4, -0.86), (0.84, -0.98)])


def add_mouth(k, z=MOUTH_Z, half=MOUTH_HALF):
    """The open mouth, dark red in its lobed outline, and its tongue a lighter pink oval of its own low in it."""
    at = front(0.0, C[2] + z)
    k.decal(at, normal_at(at), half, MOUTH, eps=0.003, outline=MOUTH_OUTLINE, rings=14, segs=96)
    t = front(0.0, C[2] + z - half[1] * 0.5)
    k.decal(t, normal_at(t), (half[0] * 0.55, half[1] * 0.36), TONGUE, eps=0.005, rings=8, segs=48)


SPARKLE = polygon([(1.0, 0.0), (0.27, 0.27), (0.0, 1.0), (-0.27, 0.27), (-1.0, 0.0), (-0.27, -0.27), (0.0, -1.0),
                   (0.27, -0.27)])


def lay_eyes(k, state, x=EYE_X, z=EYE_Z, half=EYE_HALF):
    """Open: the sparkle as a patch whose own outline is the four-pointed star, so its points are crisp (drawn
    inside a colour function they step at the rings). The other states: eye_look's lines on lilac in an oval."""
    p = front(x, C[2] + z)
    if state == "Open":
        k.decal(p, normal_at(p), (half[0] * 1.1, half[1] * 1.1), CI, eps=0.004, outline=SPARKLE, rings=12, segs=120,
                mirror=True)
        return
    k.decal(p, normal_at(p), half, eye_look(state, sparkle, CL, CI, line=0.2), eps=0.004, rings=20, segs=56,
            mirror=True)


def whismur(src, out):
    k = Kit(src, coat="fur")
    add_body(k)
    add_ear(k)
    add_hands(k)
    add_feet(k)
    add_mouth(k)
    k.build(voxel=0.0055, band=0.008)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    whismur(*args())
