# Glastrier sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/glastrier.py -- <the export's Glastrier.glb> <out.glb>
#
# The design, from the references: a great heavy war horse, snow white with a cool lavender shade under its belly,
# down the front of its chest and on its lower legs. Its whole head is a mask of pale blue ice with flat facets, the
# muzzle pointing forward and down to a dark nostril, a dark slanting eye-hole on each side with a pale eye in it; a
# tall ice horn rising from the forehead and a crown of ice crystals fanning back and out from behind the mask. Its
# legs are thick, each sunk in a great cluster of ice crystals (the front ones tallest, to a third of the leg). Its
# tail is a short lavender stem curving back and down to a mace of hanging ice crystals.
# The trunk, the legs and the crystals are written to be shared: spectrier.py grows from them (sibling("glastrier")).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, polygon, eye_look, args, axes, facets

# --- colours ----------------------------------------------------------------------------------------------------------
WHITE, SHADE = "#F2F2F8", "#C9C6E8"
ICE, ICEDEEP, ICEPALE = "#A8CBF0", "#7EA6E0", "#D6E8FA"
SOCKET, NOSTRIL = "#4C5266", "#3A3E50"
EYEW, EYEI, INK = srgb("#E8F2FF"), srgb("#5C7CC8"), srgb("#262A3A")

# --- the frame (shared with spectrier.py) -----------------------------------------------------------------------------
TRUNK = dict(z=0.47, half_w=0.15, half_d=0.175, half_l=0.235, y=0.0)   # a deep heavy barrel, the belly at 0.29
FORE_Y, HIND_Y, LEG_X = -0.2, 0.22, 0.09
H = np.array([0.0, -0.37, 0.72])                   # the head's middle


def on_ell(c, r, d):
    d = np.array(d, float)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


def crystal(p0, p1, prof, n=6, turn=0.0):
    """A crystal along p0 -> p1: rings of n sides at shares t of the way, (t, radius) each, made a flat-faced hull."""
    p0, p1 = np.array(p0, float), np.array(p1, float)
    d = p1 - p0
    a, b = axes(d)
    pts = []
    for t, r in prof:
        c = p0 + d * t
        if r <= 0:
            pts.append(c)
            continue
        for j in range(n):
            th = 2 * math.pi * j / n + math.radians(turn)
            pts.append(c + r * (math.cos(th) * a + math.sin(th) * b))
    return hull(np.array(pts))


def shard(p0, p1, r):
    return crystal(p0, p1, [(0, r * 0.8), (0.12, r), (0.72, r * 0.95), (1.0, 0)], n=5, turn=17)


def trunk(k, color, t=TRUNK, name=""):
    """The barrel with a round chest before and a round rump behind, as one mass from the side and from behind."""
    z, w, dd, L, y = t["z"], t["half_w"], t["half_d"], t["half_l"], t["y"]
    k.add(name + "barrel", ell((0, y, z), (w, L, dd)), color, "body")
    k.add(name + "chest", ell((0, y - L * 0.62, z + dd * 0.12), (w * 0.95, L * 0.48, dd * 1.02)), color, "body")
    k.add(name + "rump", ell((0, y + L * 0.62, z + dd * 0.12), (w * 1.02, L * 0.5, dd * 0.98)), color, "body")


def legs(k, color, x=LEG_X, r=0.07, name=""):
    """Thick legs: shoulders and thighs fused over their upper halves, the lower leg tapering to the ground."""
    k.add(name + "shoulder", ell((x * 0.86, FORE_Y, 0.42), (r * 0.95, r * 1.3, r * 1.75)), color, "leg{s}",
          mirror=True)
    k.add(name + "thigh", ell((x * 0.9, HIND_Y + 0.01, 0.43), (r * 1.0, r * 1.55, r * 1.85)), color, "hind{s}",
          mirror=True)
    k.add(name + "fore", tube([(x, FORE_Y, 0.42), (x + 0.004, FORE_Y - 0.01, 0.25), (x + 0.004, FORE_Y, 0.11),
                               (x + 0.004, FORE_Y - 0.005, r * 0.62)], [r, r * 0.74, r * 0.66, r * 0.66]),
          color, "leg{s}", mirror=True)
    k.add(name + "hind", tube([(x, HIND_Y, 0.42), (x + 0.004, HIND_Y + 0.045, 0.26), (x + 0.004, HIND_Y + 0.01, 0.11),
                               (x + 0.004, HIND_Y + 0.005, r * 0.62)], [r * 1.05, r * 0.76, r * 0.66, r * 0.66]),
          color, "hind{s}", mirror=True)


def ice_boot(k, name, x, y, height, bone, r=0.07):
    """A cluster of ice crystals round a hoof: tall ones leaning out, short ones between, a ring of them."""
    big = [(0.0, 1.0), (0.9, 0.62), (1.7, 0.85), (2.5, 0.55), (3.3, 0.9), (4.1, 0.6), (4.9, 0.8), (5.6, 0.5)]
    for j, (a, h) in enumerate(big):
        a += 0.3
        root = np.array([x + math.cos(a) * r * 0.9, y + math.sin(a) * r * 0.9, 0.0])
        lean = np.array([math.cos(a) * 0.3, math.sin(a) * 0.3, 1.0])
        tip = root + unit(*lean) * height * h
        col = ICE if j % 3 else (ICEDEEP if j % 2 else ICEPALE)
        k.add(f"{name}crystal{j}", shard(root, tip, 0.028 + 0.008 * h), col, bone, fuse=False, surface="shell")
    k.add(name + "hoof", crystal((x, y, 0.0), (x, y, 0.05), [(0, r * 1.05), (1, r * 0.95)], n=7), ICEDEEP, bone,
          fuse=False, surface="shell")


def glastrier(src, out):
    k = Kit(src, coat="fur")
    trunk(k, WHITE)
    legs(k, WHITE)
    # the lavender shade: under the belly, down the chest's front, on the lower legs
    k.paint(lambda P: inside(P[:, 2] - (0.34 + 0.02 * np.cos(P[:, 1] * 9)), 0.025), SHADE,
            on=["barrel", "chest", "rump", "shoulder", "thigh"])
    k.paint(lambda P: inside(P[:, 2] - 0.27, 0.03), SHADE, on=["fore", "hind"])
    k.paint(lambda P: inside(P[:, 1] + 0.31, 0.03) * inside(P[:, 2] - 0.56, 0.03) * inside(0.36 - P[:, 2], 0.02)
            * inside(np.abs(P[:, 0]) - 0.1, 0.03), SHADE, on=["chest"])
    for s, side in ((1, "R"), (-1, "L")):
        ice_boot(k, "fore" + side, LEG_X * s + 0.004 * s, FORE_Y - 0.005, 0.2, f"leg{side}")
        ice_boot(k, "hind" + side, LEG_X * s + 0.004 * s, HIND_Y + 0.005, 0.13, f"hind{side}")

    # --- a thick neck up and forward to the head ---------------------------------------------------------------
    k.add("neck", tube([(0, -0.18, 0.55), (0, -0.25, 0.64), (0, -0.31, 0.71)], [0.12, 0.1, 0.08]), WHITE, "head")
    # --- the head: a mask of ice over all of it, the muzzle forward and down ------------------------------------
    HR = np.array([0.08, 0.098, 0.086])
    k.add("cranium", ell(H, HR, deform=facets(14, 0.35, 7, keep_front=0.5)), ICE, "head", surface="shell")
    FC = np.array([0.0, -0.453, 0.631])
    k.add("face", ell(FC, (0.061, 0.068, 0.116), rot=(-48, 0, 0), deform=facets(10, 0.3, 3)), ICE, "head",
          surface="shell")
    MZ = np.array([0.0, -0.523, 0.561])
    k.add("muzzle", ell(MZ, (0.054, 0.059, 0.053), rot=(-48, 0, 0)), ICEPALE, "head", surface="shell")
    for s in (1, -1):
        dn = unit(0.4 * s, -0.8, -0.45)
        k.decal(MZ + np.array([0.028 * s, -0.042, -0.021]), dn, (0.011, 0.014), NOSTRIL, up=unit(-0.3 * s, 0.3, 1),
                eps=0.003, rings=8, segs=40)

    # --- the horn and the crown of crystals fanning back from behind the mask -------------------------------
    k.add("horn", crystal((0, -0.4, 0.75), (0.0, -0.33, 0.99), [(0, 0.03), (0.15, 0.034), (0.75, 0.024), (1, 0)],
                          n=6, turn=30), ICE, "head", fuse=False, surface="shell")
    for s, side in ((1, "R"), (-1, "L")):
        for j, (root, d, L, r, col) in enumerate([
                ((0.045, -0.34, 0.78), (0.45, 0.55, 0.75), 0.15, 0.024, ICE),
                ((0.06, -0.31, 0.74), (0.75, 0.6, 0.3), 0.13, 0.022, ICEPALE),
                ((0.05, -0.29, 0.7), (0.55, 0.75, -0.1), 0.11, 0.02, ICEDEEP),
                ((0.03, -0.3, 0.79), (0.2, 0.7, 0.75), 0.12, 0.022, ICEPALE)]):
            p0 = np.array(root) * np.array([s, 1, 1])
            dd = unit(d[0] * s, d[1], d[2])
            k.add(f"crown{j}{side}", shard(p0, p0 + dd * L, r), col, "head", fuse=False, surface="shell")
    # a short crest of crystals down the top of the neck
    for j, t in enumerate(np.linspace(0, 1, 3)):
        p = np.array([0, -0.27, 0.73]) * (1 - t) + np.array([0, -0.17, 0.63]) * t
        k.add(f"crest{j}", shard(p - (0, 0, 0.02), p + np.array([0.0, 0.07, 0.07]) * (1 - 0.3 * t), 0.022),
              ICEDEEP if j % 2 else ICE, "head", fuse=False, surface="shell")

    # --- the tail: a lavender stem back and down to a mace of hanging ice -----------------------------------
    k.add("tailstem", tube([(0, 0.29, 0.57), (0, 0.39, 0.56), (0, 0.44, 0.5), (0, 0.455, 0.42)], [0.03, 0.026, 0.024,
                                                                                               0.022]), SHADE, "tail")
    T0 = np.array([0, 0.455, 0.43])
    for j, (d, L) in enumerate([((0, 0.2, -1), 0.16), ((0.6, 0.3, -0.8), 0.11), ((-0.6, 0.3, -0.8), 0.11),
                                ((0.3, -0.5, -0.8), 0.1), ((-0.2, 0.8, -0.4), 0.1)]):
        k.add(f"tailice{j}", shard(T0 - np.array(d) * 0.01, T0 + unit(*d) * L, 0.03), ICE if j % 2 else ICEDEEP,
              "tail", fuse=False, surface="shell")

    # --- the eyes: a dark slanting hole in the mask on each side, a pale eye in it -------------------------
    de = unit(0.74, -0.58, 0.22)
    E = on_ell(H + np.array([0, -0.035, -0.012]), HR, de)
    hole = polygon([(-1, -0.55), (0.25, -0.75), (1, -0.1), (0.85, 0.55), (-0.2, 0.25), (-1, 0.05)])
    k.decal(E, de, (0.042, 0.03), srgb(SOCKET), up=unit(0, 0.25, 1), eps=0.003, outline=hole, rings=10, segs=60,
            mirror=True)
    S = srgb(SOCKET)
    def open_eye(u, v):
        c = np.tile(S, (len(u), 1))
        r = ((u - 0.05) / 0.82) ** 2 + ((v + 0.05) / 0.62) ** 2
        c[r < 1] = EYEW
        c[((u - 0.12) / 0.36) ** 2 + ((v + 0.08) / 0.48) ** 2 < 1] = EYEI
        c[((u - 0.12) / 0.15) ** 2 + ((v + 0.08) / 0.24) ** 2 < 1] = INK
        c[(u + 0.12) ** 2 + (v - 0.15) ** 2 < 0.12 ** 2] = (1, 1, 1)
        return c
    def eyes(state):
        k.decal(E, de, (0.032, 0.023), eye_look(state, open_eye, S, srgb(ICEPALE), line=0.17),
                up=unit(0, 0.25, 1), eps=0.0055, rings=20, segs=56, mirror=True)

    k.build(voxel=0.0075, smooth=4, band=0.008)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    glastrier(*args())
