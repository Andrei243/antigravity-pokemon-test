# Popplio sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/popplio.py -- <the export's Popplio.glb> <out.glb>
#
# Written to be grown from (Brionne, Primarina: sibling("popplio")): the eye, the flipper with its white stripes, the
# frilled collar, the nose and the open mouth are functions here. From the references: a blue sea lion pup sitting up,
# its round head nearly half its height and half again as wide as its body; big upright oval eyes, a large dark pupil
# with a white glint, wide apart above a white muzzle that stands forward; a round pink nose on the muzzle's tip and an
# open mouth under it, dark red with a pink tongue; rounded ear flaps standing out from the head's sides; a pale blue
# collar frilled at its edge round the neck, longer in front; broad front flippers with two white stripes at their
# ends, carried forward and down to the ground; two hind flippers spread out on the ground behind, striped the same.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, smoothstep, unit, eye_look, outward, args

BLUE, WHITE, PINK, COLLAR, INK = "#3D74D6", "#F4F6FA", "#EE8FAA", "#A6DCEE", "#1A1820"
PUPIL, MOUTH, TONGUE = "#2E2422", "#7E2232", "#E27A8E"


def at_surface(C, R, d):
    """The point of an ellipsoid (centre C, radii R) in a direction d from its centre."""
    d = np.array(d, float)
    return np.array(C, float) + d / np.linalg.norm(d / np.array(R, float))


def frame(d, roll=0.0):
    """A right-handed frame (side, d, normal) for a flat part lying along d, its face up, rolled about d by degrees."""
    d = unit(*d)
    s = np.cross(d, (0, 0, 1.0)); s /= np.linalg.norm(s)
    n = np.cross(s, d)
    r = math.radians(roll)
    s, n = s * math.cos(r) + n * math.sin(r), n * math.cos(r) - s * math.sin(r)
    return s, d, n


def popplio_eye(pupil=PUPIL, iris=None):
    """An upright oval eye: a thin dark rim, a white, a large dark pupil (an iris in it if given) and a white glint."""
    P, I = srgb(pupil), (srgb(iris) if iris else None)
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(np.array((1.0, 1.0, 1.0)), (len(u), 1))
        c[((u + 0.06) / 0.6) ** 2 + ((v + 0.04) / 0.66) ** 2 < 1] = P
        if I is not None:
            c[((u + 0.06) / 0.34) ** 2 + ((v + 0.1) / 0.4) ** 2 < 1] = I
        c[((u - 0.1) / 0.2) ** 2 + ((v - 0.28) / 0.22) ** 2 < 1] = (1.0, 1.0, 1.0)
        c[r > 0.85] = srgb(INK)
        return c
    return look


def eyes(k, spots, half, state, lid, open_look, eps=0.0015):
    """The two eyes as patches, the left one's colour function mirrored so u is outward on both."""
    look = eye_look(state, open_look, srgb(lid), srgb(INK), line=0.17)
    for at, d, sx in spots:
        col = look if sx > 0 else (lambda u, v, look=look: look(-u, v))
        k.decal(at, d, half, col, eps=eps, rings=20, segs=56)


def flat(c, d, half, roll=0.0, deform=None):
    """A flattened ellipsoid lying along d: half sizes (along, across, thick), its face turned up and rolled."""
    s, d, n = frame(d, roll)
    V, F = ell((0, 0, 0), (half[1], half[0], half[2]), deform=deform)
    return V @ np.stack([s, d, n]) + np.array(c, float), F


def flipper(k, name, root, d, half, color, bone, roll=0.0, stripes=WHITE, mirror=True, eps=0.0012):
    """
    A broad flipper fused at root, lying along d, wider toward its end, with two white stripes along its last third
    (each its own thin patch, cast down the flipper's face). Returns the flipper's frame.
    """
    def wider(V):
        V = V.copy(); V[:, 0] *= 0.78 + 0.32 * np.clip(V[:, 1] + 0.3, 0, 1)
        return V
    s, dd, n = frame(d, roll)
    c = np.array(root, float) + dd * half[0] * 0.9
    k.add(name, flat(c, d, half, roll, deform=wider), color, bone, mirror=mirror)
    if stripes:
        for off in (-0.3, 0.3):
            at = c + dd * half[0] * 0.55 + s * half[1] * off + n * half[2] * 0.95
            k.decal(at, n, (half[1] * 0.11, half[0] * 0.24), stripes, up=tuple(dd), eps=eps, rings=6, segs=28,
                    mirror=mirror)
    return s, dd, n


def collar(k, name, z_in, r_in, z_out, r_out, front, waves, color, bone, thick, wave=0.12, mirror=False):
    """
    A frilled collar round the neck: a sloping sheet from an inner ring (buried in the neck) out to a scalloped hem,
    reaching further in front by front. Its seam behind.
    """
    def at(u, v):
        th = math.pi / 2 + 2 * math.pi * u          # u 0 at the back (+Y)
        fr = np.clip(-np.sin(th), 0, 1) ** 1.5      # 1 at the front (-Y)
        hem = 1 + wave * np.cos(waves * th) * v
        r = (r_in + (r_out * (1 + front * fr) - r_in) * v) * hem
        z = z_in + (z_out - z_in) * v - 0.25 * wave * (r_out - r_in) * np.sin(waves * th) * v
        return np.stack([r * np.cos(th), r * np.sin(th), z], axis=-1)
    k.add(name, outward(sheet(at, 97, 6, thick)), color, bone, fuse=False, surface="skin")


def nose(k, at, d, r, color, bone):
    """A round nose standing proud of the muzzle: a loose ball."""
    k.add("nose", ell(np.array(at, float) + unit(*d) * r * 0.55, (r, r * 0.92, r * 0.95)), color, bone, fuse=False,
          surface="skin")


def open_mouth(k, at, d, half, eps=0.0014, tooth=False):
    """The open mouth as one patch on the muzzle: dark red, a pink tongue in its lower half, a fang if asked."""
    M, T = srgb(MOUTH), srgb(TONGUE)
    def look(u, v):
        c = np.tile(M, (len(u), 1))
        c[(u / 0.62) ** 2 + ((v + 0.62) / 0.55) ** 2 < 1] = T
        if tooth:
            c[(np.abs(u + 0.35) < 0.18 * (v - 0.25) / 0.75 + 0.0) & (v > 0.25)] = (1.0, 1.0, 1.0)
        return c
    k.decal(at, d, half, look, up=(0, 0, 1), eps=eps, rings=12, segs=48)


# --- Popplio's own frame ---------------------------------------------------------------------------------------------
H = np.array([0, -0.035, 0.188])            # the head's centre
HR = np.array([0.066, 0.06, 0.059])
MZ = H + np.array([0, -0.052, -0.022])      # the muzzle's centre
MR = np.array([0.034, 0.03, 0.026])


def popplio(src, out):
    k = Kit(src, coat="scales")
    k.add("head", ell(H, HR), BLUE, "head")
    k.add("muzzle", ell(MZ, MR, rot=(12, 0, 0)), WHITE, "head")
    k.add("neck", ell((0, -0.02, 0.135), (0.044, 0.042, 0.04)), BLUE, "body")
    # The body, a pear sitting on the ground, narrower than the head
    def pear(V):
        V = V.copy()
        s = 1 + 0.12 * np.clip(-V[:, 2], 0, 1) - 0.12 * np.clip(V[:, 2], 0, 1) ** 2
        V[:, 0] *= s; V[:, 1] *= s
        V[:, 2] = np.where(V[:, 2] < -0.75, -0.75 + (V[:, 2] + 0.75) * 0.4, V[:, 2])
        return V
    k.add("body", ell((0, 0.002, 0.082), (0.05, 0.05, 0.082), deform=pear), BLUE, "body")
    # Ear flaps standing out from the head's sides, a little down
    for sx in (1, -1):
        ec = at_surface(H, HR, unit(0.97 * sx, 0.12, 0.02)) + np.array([0.012 * sx, 0, -0.003])
        k.add(f"ear{'RL'[sx < 0]}", ell(ec, (0.027, 0.016, 0.017), rot=(0, 14 * sx, 0)), BLUE, "head")
    # Front flippers from the shoulders, forward and down to the ground; the hind pair spread behind on the ground
    flipper(k, "arm", (0.04, -0.03, 0.098), (0.3, -0.72, -0.62), (0.056, 0.027, 0.0105), BLUE, "leg{s}", roll=-18)
    flipper(k, "hind", (0.025, 0.035, 0.016), (0.82, 0.5, 0.06), (0.052, 0.025, 0.0105), BLUE, "body", roll=8)
    # The collar: pale blue, frilled at its hem, longer in front
    collar(k, "collar", 0.142, 0.036, 0.118, 0.07, 0.3, 7, COLLAR, "body", 0.0055)
    nose(k, at_surface(MZ, MR, unit(0, -0.92, 0.38)), unit(0, -0.9, 0.42), 0.0165, PINK, "head")
    md = unit(0, -0.78, -0.62)
    open_mouth(k, at_surface(MZ, MR, md), md, (0.017, 0.012))
    k.build(voxel=0.0026, smooth=4, band=0.003, wband=0.02)
    E = [(at_surface(H, HR, d), d, sx) for sx in (1, -1) for d in [unit(0.47 * sx, -0.84, 0.17)]]
    k.eye_states(lambda st: eyes(k, E, (0.0175, 0.025), st, BLUE, popplio_eye()))
    k.export(out)


if __name__ == "__main__":
    popplio(*args())
