# Slakoth sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/slakoth.py -- <the export's Slakoth.glb> <out.glb>
#
# Written to be grown from (vigoroth.py and slaking.py import it with sibling("slakoth")): the line's way of making the
# sleepy eye in its brown patch, the pink pig's nose with two nostrils, the hull claws, the flat tufts of fur.
#
# The design, from the references: a grey-beige sloth lying flat on its belly, the big round head laid forward on the
# ground; a large dark-brown patch round each eye, nearly meeting over the nose, inside it a heavy-lidded eye (a pink lid
# over its top half, a white below with a black pupil against the lid's edge); a big pink pig's nose with two nostrils
# low on the face, a small wavy mouth under it; a fan of cream-white tufts on top of the head; two brown stripes across
# the back and a few spiky tufts of fur over the shoulders; the arms flopped out along the ground to the sides, the hind
# legs stretched out behind, each paw with two big white claws; a long thick tail curled up into a ring beside the head
# on its left (-X) side, its plane facing the front three-quarter (the pictures: the ring stands at the head's left and
# behind it, open in the middle).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, boxy, eye_look, args, sibling

t = sibling("teddiursa")
hit, claw, poly_mask, round_about, mirrored = t.hit, t.claw, t.poly_mask, t.round_about, t.mirrored

FUR, STRIPE, PATCH, PINK, LID = "#CFC5B5", "#8A6447", "#875C44", "#E99FA6", "#E7B3B5"
CLAW, TUFT, INK, NOSTRIL, MOUTH = "#F5F2EC", "#F2EEE6", "#211816", "#6A3438", "#5A3632"
WHITE = np.array([1.0, 1.0, 1.0])

H = np.array([0.0, -0.18, 0.094])          # the head's middle (its bone is at 0, -0.15, 0.11)
HR = np.array([0.106, 0.095, 0.094])


def tuft(k, name, root, d, length, width, thick, bone, color, fuse=False):
    """A flat tapering spike of fur from root along d, its broad face square to side: a hull."""
    root, d = np.array(root, float), unit(*d)
    side = np.cross(d, (0, 1.0, 0))
    if np.linalg.norm(side) < 1e-3:
        side = np.array([1.0, 0, 0])
    side /= np.linalg.norm(side)
    n = np.cross(side, d)
    P = []
    for s_ in (-1, 1):
        P += [root + side * s_ * width + n * thick * 0.5, root + side * s_ * width - n * thick * 0.5,
              root + d * length * 0.55 + side * s_ * width * 0.6 + n * thick * 0.35,
              root + d * length * 0.55 + side * s_ * width * 0.6 - n * thick * 0.35]
    P.append(root + d * length)
    k.add(name, hull(P), color, bone, fuse=fuse, surface="fur")


def two_claws(k, name, paw, d, bone, length=0.05, width=0.013, spread=0.017):
    """Two big white claws side by side out of a paw's end along d, curving a little down."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0)); a /= np.linalg.norm(a)
    for i, o in enumerate((-1, 1)):
        claw(k, f"{name}{i}", np.array(paw) + a * o * spread, d + a * o * 0.12 + np.array([0, 0, -0.18]),
             length, width, bone, CLAW)


def nose(k, c, r, bone, color=PINK, nostril=NOSTRIL, tilt=0.15):
    """A pig's nose: a squarish pink ball standing forward, two dark oval nostrils on its flat front."""
    c, r = np.array(c, float), np.array(r, float)
    NOSE = ell(c, r, deform=boxy(2.6))
    k.add("nose", NOSE, color, bone)
    for s in (1, -1):
        p = hit(NOSE, (c[0] + s * r[0] * 0.38, c[1] - 1, c[2] + r[2] * 0.05), (0, 1, 0))
        k.decal(p, unit(s * 0.15, -1, tilt), (r[0] * 0.2, r[2] * 0.3), nostril, eps=0.003, rings=8, segs=32)
    return NOSE


def sleepy_eye(ink=INK, pupil=(0.12, -0.35), size=0.2):
    """The heavy-lidded eye (u outward, v up): a pink lid over the top half, a white below with a black pupil
    against the lid's edge, the lid's edge a dark line."""
    def look(u, v, lid=srgb(LID), ink_=srgb(ink)):
        c = np.tile(WHITE, (len(u), 1))
        edge = -0.08 + 0.12 * u
        c[v > edge] = lid
        c[(np.abs(v - edge) < 0.11)] = ink_
        c[((u - pupil[0]) / size) ** 2 + ((v - pupil[1]) / (size * 1.25)) ** 2 < 1] = ink_
        return c
    return look


def patched(state, open_look, patch, ring=0.55, lid=LID, ink=INK, fierce_slope=0.45):
    """An eye sitting in a dark patch: the patch outside the radius ring, the eye's state inside it (eye_look on the
    inner disc, with the pink lid). Fierce is the lid slanting down toward the nose."""
    inner_lid, ink_, patch_ = srgb(lid), srgb(ink), srgb(patch)
    if state == "Fierce":
        def inner(u, v):
            c = open_look(u, v)
            edge = 0.05 + fierce_slope * u
            c[v > edge] = inner_lid
            c[np.abs(v - edge) < 0.12] = ink_
            return c
    else:
        inner = eye_look(state, open_look, inner_lid, ink_, line=0.15)
    def look(u, v):
        c = np.tile(patch_, (len(u), 1))
        m = u * u + v * v < ring * ring
        c[m] = inner(u[m] / ring, v[m] / ring)
        return c
    return look


def slakoth(src, out):
    k = Kit(src, coat="fur")

    # The long body lying flat, as deep as the head's lower two thirds, settled on the ground
    def flat_belly(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.75, V[:, 2])
        return V
    B = np.array([0.0, 0.08, 0.072])
    k.add("body", ell(B, (0.108, 0.205, 0.074), deform=flat_belly), FUR, "body")
    k.add("shoulders", ell((0, -0.07, 0.074), (0.1, 0.09, 0.07)), FUR, "body")
    k.add("rump", ell((0, 0.24, 0.064), (0.09, 0.085, 0.062)), FUR, "body")

    # The big round head laid forward on the ground
    def head_shape(V):
        V = boxy(2.25)(V)
        V[:, 0] *= 1 + 0.05 * np.clip(-V[:, 2], 0, 1)
        return V
    HEAD = ell(H, HR, deform=head_shape)
    k.add("head", HEAD, FUR, "head")

    # The right arm (+X) flopped out along the ground to the side and a little back; hind legs stretched out behind
    for s, bone in ((1, "legR"),):
        m = np.array([s, 1, 1.0])
        pts = [np.array(p) * m for p in [(0.06, -0.1, 0.062), (0.14, -0.06, 0.04), (0.23, -0.01, 0.033)]]
        k.add("arm" + bone[-1], tube(pts, [0.04, 0.036, 0.034]), FUR, bone)
        paw = np.array((0.262, 0.012, 0.03)) * m
        k.add("paw" + bone[-1], ell(paw, (0.036, 0.04, 0.03)), FUR, bone)
        two_claws(k, "clawf" + bone[-1], paw + unit(0.8 * s, 0.6, 0) * 0.026, (0.8 * s, 0.6, 0.0), bone)
    for s, bone in ((1, "hindR"), (-1, "hindL")):
        m = np.array([s, 1, 1.0])
        pts = [np.array(p) * m for p in [(0.06, 0.2, 0.055), (0.085, 0.3, 0.038), (0.1, 0.38, 0.032)]]
        k.add("hind" + bone[-1], tube(pts, [0.045, 0.038, 0.034]), FUR, bone)
        paw = np.array((0.105, 0.405, 0.03)) * m
        k.add("foot" + bone[-1], ell(paw, (0.036, 0.04, 0.03)), FUR, bone)
        two_claws(k, "clawh" + bone[-1], paw + np.array([0.15 * s, 1, 0]) * 0.03, (0.15 * s, 1, 0.0), bone)

    # The left arm (-X) raised from the shoulder, arched up and over in a wide curl beside the head, its paw laid on
    # the crown with its two white claws over the top (the pictures' great loop: an arm, not a tail)
    AP = [(-0.06, -0.1, 0.075), (-0.12, -0.08, 0.12), (-0.175, -0.07, 0.19), (-0.17, -0.1, 0.26),
          (-0.115, -0.145, 0.292), (-0.06, -0.175, 0.265), (-0.035, -0.19, 0.235)]
    k.add("armL", tube(AP, [0.04, 0.04, 0.039, 0.038, 0.037, 0.036, 0.035]), FUR, "legL")
    PAW = np.array([-0.03, -0.195, 0.212])
    k.add("pawL", ell(PAW, (0.036, 0.04, 0.03), rot=(0, 0, 30)), FUR, "legL")
    two_claws(k, "clawfL", PAW + unit(0.6, -0.7, -0.2) * 0.026, (0.6, -0.7, -0.25), "legL", length=0.05,
              spread=0.016)

    # A low hump over the hips, so the second stripe stands where the front three-quarter sees it
    k.add("hump", ell((0, 0.16, 0.098), (0.09, 0.085, 0.058)), FUR, "body")

    # Brown stripes across the back, down the flanks to half their depth: crisp, by distance along the body
    for y0, w in ((0.0, 0.022), (0.155, 0.024)):
        k.paint(lambda P, y0=y0, w=w: inside(np.abs(P[:, 1] - y0 - 0.12 * np.abs(P[:, 0])) - w, 0.0025)
                * inside(0.075 - P[:, 2], 0.003) * (P[:, 1] > -0.07) * (P[:, 1] < 0.24), STRIPE, on=["body", "shoulders", "hump"])

    # Spiky tufts of fur over the shoulders and along the first stripe
    for i, (x, y, ang) in enumerate([(0.03, -0.045, 15), (-0.03, -0.05, -15), (0.0, -0.035, 0), (0.05, 0.11, 25),
                                     (-0.05, 0.105, -25)]):
        root = np.array([x, y, 0.135])
        tuft(k, f"spike{i}", root, (math.sin(math.radians(ang)) * 0.6, 0.55, 0.75), 0.05, 0.016, 0.022, "body", FUR)

    # Cream-white tufts on top of the head, leaning back behind the paw laid there
    for i, (x, lean, L) in enumerate([(0.012, 0.15, 0.065), (0.04, 0.5, 0.05)]):
        root = H + np.array([x, 0.02, HR[2] * 0.82])
        tuft(k, f"crown{i}", root, (lean, 0.42, 1.0), L, 0.018, 0.024, "head", TUFT)

    # The pig's nose low on the face, a small wavy mouth under it
    NC = H + np.array([0, -HR[1] * 0.92, -0.028])
    nose(k, NC, (0.042, 0.026, 0.029), "head")
    CM, CF = srgb(MOUTH), srgb(FUR)
    def mouth(u, v):
        c = np.tile(CF, (len(u), 1))
        c[(np.abs(u) < 0.95) & (np.abs(v - 0.25 * np.cos(u * math.pi * 1.4)) < 0.5)] = CM
        return c
    MC = hit(HEAD, (0, -1, H[2] - 0.066), (0, 1, 0))
    k.decal(MC, unit(0, -1, -0.35), (0.028, 0.012), mouth, eps=0.003, rings=10, segs=56)

    # The eyes in their brown patches, wide apart, the patches nearly meeting over the nose
    de = unit(0.42, -0.9, 0.16)
    E = hit(HEAD, (0.046, -1, H[2] + 0.018), (0, 1, 0))
    open_look = sleepy_eye()
    def lay(state):
        k.decal(E, de, (0.05, 0.047), patched(state, open_look, PATCH), eps=0.004, rings=20, segs=56, mirror=True)

    k.build(voxel=0.0055, band=0.005)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    slakoth(*args())
