# Gulpin sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gulpin.py -- <the export's Gulpin.glb> <out.glb>
#
# From the references (both drawn from the front three-quarter, the face turned to the viewer's left): a green blob
# of a stomach longer than it is tall, flat on the ground and spreading at its foot, a soft lobe of it trailing behind;
# its eyes always shut, two curved dark lines on the upper front; a small puckered lip low at the front; two stubby
# hands of three round lumps on its front sides; a black diamond on its right (+X) flank toward the back; a long
# yellow feather of a crest rising from the top and leaning back and out. (The brief's "big lips": the pictures show a
# small puckered nub at the front's foot and no open mouth; the dark shape at the side is the diamond Swalot inherits.)
# Swalot is grown from this script (sibling("gulpin")): the hands, the lips, the diamond, the shut eye.
# Skeleton: body (0, 0, 0.1), head (0, 0, 0.18), arms (+-0.1, -0.06, 0.08).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, leaf, outward, srgb, inside, smoothstep, unit, eye_look, polygon, args

GREEN, LINE, MARK, FEATHER = "#A6CA86", "#34482C", "#1C1E22", "#F2D058"

BODY, BODYR = np.array([0.0, 0.01, 0.102]), np.array([0.135, 0.165, 0.104])
DIAMOND = polygon([(1, 0), (0, 1), (-1, 0), (0, -1)])


# --- the line's makes ----------------------------------------------------------------------------------------------
def foot(V):
    """A blob's sole: flat on the ground and spreading a little at its foot."""
    V = V.copy()
    s = 1 + 0.1 * smoothstep(-0.2, -0.85, V[:, 2])
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.maximum(V[:, 2], -0.88)
    return V


def on(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in the direction d, and its normal there."""
    d = unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def hand(k, c, r, d, size, colour, bone):
    """A stubby hand of three round lumps (a palm and two fingers) lying on the side of the ellipsoid (centre c,
    radii r) toward d, on +X; mirrored."""
    p, n = on(np.array(c, float), np.array(r, float), d)
    t1 = np.cross(n, (0, 0, 1.0)); t1 /= np.linalg.norm(t1)          # along the surface, toward the front
    if t1[1] > 0:
        t1 = -t1
    t2 = np.cross(t1, n); t2 /= np.linalg.norm(t2)
    if t2[2] < 0:
        t2 = -t2
    base = p + n * size * 0.35
    k.add("palm", ell(base - t1 * size * 0.35, (size,) * 3), colour, bone, mirror=True)
    for i, (a, b) in enumerate(((0.75, 0.55), (0.8, -0.6))):
        k.add(f"finger{i}", ell(base + t1 * size * a + t2 * size * b + n * size * 0.1, (size * 0.78,) * 3), colour,
              bone, mirror=True)


def lips(k, c, r, colour, bone):
    """A small puckered lip: one soft round lump."""
    k.add("lip", ell(np.array(c, float), (r * 1.25, r, r)), colour, bone)


def diamond(k, c, r, d, half, colour):
    """A black diamond laid on the flank of the body ellipsoid (centre c, radii r) toward d."""
    p, n = on(c, r, d)
    k.decal(p, n, half, colour, eps=0.003, outline=DIAMOND, rings=12, segs=64)


def slit(colour, ink):
    """A shut eye's open state: a dark line arching a little."""
    C, I = srgb(colour), srgb(ink)
    def look(u, v):
        c = np.tile(C, (len(u), 1))
        c[(np.abs(u) < 0.92) & (np.abs(v - (0.4 * (1 - u * u) - 0.2)) < 0.24)] = I
        return c
    return look


def shut_eyes(colour, ink):
    """eye_look's states on the slit; Fierce the same slit tilted down toward the middle."""
    C, I = srgb(colour), srgb(ink)
    def states(state):
        if state != "Fierce":
            return eye_look(state, slit(colour, ink), C, I, line=0.24)
        def look(u, v):
            c = np.tile(C, (len(u), 1))
            c[(np.abs(u) < 0.92) & (np.abs(v - (0.45 * u - 0.02)) < 0.24)] = I
            return c
        return look
    return states


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")

    k.add("body", ell(BODY, BODYR, deform=foot), GREEN, "body")
    k.add("crown", ell((0, 0.01, 0.182), (0.06, 0.06, 0.03)), GREEN, "head")
    k.add("trail", ell((0.035, 0.165, 0.03), (0.06, 0.075, 0.03)), GREEN, "body")
    lips(k, (0.0, -0.158, 0.042), 0.024, GREEN, "body")
    hand(k, BODY, BODYR, (0.85, -0.55, -0.22), 0.025, GREEN, "arm{s}")
    diamond(k, BODY, BODYR, (1, 0.6, 0.12), (0.042, 0.05), MARK)
    # The feather: long and yellow, rising from the crown, leaning out to +X so its face shows to the game's cameras
    k.add("feather", outward(leaf((0.0, 0.02, 0.17), unit(0.5, -0.06, 0.82), 0.19, 0.068, thick=0.017)), FEATHER,
          "head", fuse=False)

    k.build(voxel=0.004, smooth=4, band=0.006)

    states = shut_eyes(GREEN, LINE)

    def eyes(state):
        for s in (1, -1):
            p, n = on(BODY, BODYR, (0.42 * s, -0.78, 0.5))
            look = states(state)
            lk = look if s > 0 else (lambda f: (lambda a, b: f(-a, b)))(look)
            k.decal(p, n, (0.036, 0.02), lk, eps=0.004, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)
