# Carracosta sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Tirtouga's script (sibling("tirtouga"): its colours, mask, beak, mouth and eyes):
#   blender --background --factory-startup --python sculpts/carracosta.py -- <the export's Carracosta.glb> <out.glb>
#
# From the references: a big, heavy turtle standing hunched on two short legs. A large light blue head carried forward
# and low on the body with no neck to speak of: navy over the brow and down the front into a hooked navy beak, the eye
# white with a grey iris under the navy brow, a small open mouth under the beak, red inside. The chest is armour:
# navy faceted plates round a pale cyan belly. A tall rocky navy shell of blocks on the back, rising behind the head.
# The arms are great light blue flippers, from the shoulders out and down in an arc to the ground before the feet,
# three navy claws along the inner edge of each tip. Fat light blue thighs fused into the hips, short shins, navy feet
# with blunt toes. A pointed light blue tail out behind to the +X side.
# Reference ratios (3D picture): head height / total 0.28, head width / body width 0.6, flipper reach to 0.05 of the
# height off the ground, width / height 1.0, the shell's top 0.9 of the height.
# The export's joints: body (0, 0, 0.3), arms (+-0.13, -0.02, 0.42), head (0, -0.04, 0.46), legs (+-0.08, 0, 0.16),
# tail (0, 0.1, 0.18); the export stands 0.625 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, unit, args, sibling

t = sibling("tirtouga")
LIGHT, NAVY, PALE, IRIS = t.LIGHT, "#30508E", "#B8E2EC", "#9CA6B0"
ROCK = "#2C4A86"

H = np.array([0.0, -0.105, 0.522])            # the head's centre, a tenth over the measure
HR = np.array([0.098, 0.108, 0.094])
DE = unit(0.6, -0.7, 0.26)
EYE_HALF = (0.033, 0.03)
VOXEL = 0.0062
SHELL_C = np.array([0.0, 0.12, 0.38])        # the rocky shell on the back
SHELL_R = np.array([0.175, 0.15, 0.2])

# The arm-flipper's middle line from the shoulder out and down to its tip before the foot
ARM = [(0.13, -0.03, 0.45), (0.2, -0.07, 0.45), (0.245, -0.11, 0.37), (0.255, -0.15, 0.23), (0.235, -0.19, 0.11),
       (0.2, -0.22, 0.065)]
ARM_R = [0.064, 0.062, 0.064, 0.07, 0.066, 0.04]


def plate(c, n, ex, w, h, th=0.022, bevel=0.82):
    """A faceted armour plate: a six-sided slab of hard faces round c, its face along n, w wide along ex, h tall."""
    n = unit(*n); ex = np.array(ex, float); ex = ex - n * (ex @ n); ex /= np.linalg.norm(ex)
    ez = np.cross(n, ex)
    c = np.array(c, float)
    pts = []
    for a, b in ((-1, -0.5), (-1, 0.5), (0, 1), (1, 0.5), (1, -0.5), (0, -1)):
        p = c + ex * a * w + ez * b * h
        pts += [p - n * th, p + n * th * 0.3]
        pts.append(c + ex * a * w * bevel + ez * b * h * bevel + n * th)
    return hull(pts)


def carracosta(src, out):
    k = Kit(src, coat="shell")

    # The body: a broad pear, the belly low, the thighs fused into the hips
    k.add("chest", ell((0, -0.02, 0.4), (0.17, 0.15, 0.13)), LIGHT, "body", surface="fur")
    k.add("belly", ell((0, -0.02, 0.27), (0.16, 0.14, 0.13)), LIGHT, "body", surface="fur")
    k.add("thigh", ell((0.1, -0.01, 0.17), (0.095, 0.1, 0.1)), LIGHT, "leg{s}", mirror=True, surface="fur")
    k.add("shin", tube([(0.115, -0.02, 0.12), (0.12, -0.03, 0.075)], [0.07, 0.066]), LIGHT, "leg{s}", mirror=True,
          surface="fur")
    k.add("foot", ell((0.125, -0.06, 0.036), (0.075, 0.09, 0.034)), NAVY, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.04, 0.0, 0.04)):
        k.add(f"toe{j}", ell((0.125 + dx, -0.135, 0.03), (0.024, 0.026, 0.024), sub=3), NAVY, "leg{s}",
              mirror=True)
    # The pale belly, the plates' middle
    k.paint(lambda P: inside(np.linalg.norm((P[:, [0, 2]] - (0, 0.29)) / (0.07, 0.11), axis=1) - 1, 0.004)
            * (P[:, 1] < -0.08), PALE)

    # The head, carried forward and low, its back grown into the shoulders
    k.add("nape", ell((0, -0.04, 0.48), (0.1, 0.1, 0.09)), LIGHT, "head", surface="fur")
    k.add("head", ell(H, HR), LIGHT, "head", surface="fur")
    def hp(x, y, z):
        """A point placed about Tirtouga's head, grown to this one."""
        return H + (np.array([x, y, z], float) - t.H0) * (HR[0] / (t.HR[0] / t.HS))
    k.add("gape", ell(hp(0, -0.262, 0.088), np.array((0.036, 0.03, 0.017)) * 1.4, rot=(10, 0, 0)), t.MOUTH, "head",
          surface="fur")
    k.paint(t.head_mask(H, HR, high=0.55, drop=0.8, back=0.04, edge=0.003), NAVY, on=["head", "nape"])
    k.add("beak", hull([hp(*p) for p in [(0.03, -0.262, 0.135), (-0.03, -0.262, 0.135), (0, -0.27, 0.155),
                                         (0.027, -0.29, 0.1), (-0.027, -0.29, 0.1), (0, -0.312, 0.106),
                                         (0, -0.305, 0.068), (0.022, -0.27, 0.09), (-0.022, -0.27, 0.09)]]),
          NAVY, "head", fuse=False, surface="shell")

    # The arms: great flippers out and down in an arc, flat with their faces to the front, navy claws at the tips
    k.add("arm", blade(ARM, ARM_R, (0.1, 1, 0.15), 0.4), LIGHT, "arm{s}", mirror=True, surface="fur")
    tip, before = np.array(ARM[-1]), np.array(ARM[-2])
    d = unit(*(tip - before))
    for j, off in enumerate((-0.04, -0.008, 0.024)):
        b = tip - d * 0.075 + np.array([off - 0.01, -0.03, 0.01 * j])
        k.add(f"claw{j}", hull([b + (0.016, 0, 0), b - (0.016, 0, 0), b + (0, 0.022, 0), b + (0, -0.014, 0.01),
                                b + d * 0.08 + (0, -0.004, 0)]), NAVY, "arm{s}", fuse=False, mirror=True)

    # The chest's armour: faceted plates round the pale belly
    for c, n, ex, w, h in (((0.088, -0.142, 0.425), (0.35, -1, 0.3), (1, 0.3, -0.2), 0.072, 0.066),
                           ((0.128, -0.118, 0.3), (0.65, -1, 0.0), (0.6, 0.4, 0), 0.05, 0.07),
                           ((0.085, -0.13, 0.19), (0.3, -1, -0.35), (1, 0.25, 0), 0.066, 0.048)):
        k.add("plate", plate(c, n, ex, w, h), NAVY, "body", fuse=False, mirror=True)

    # The shell on the back: rocky navy blocks, the highest behind the head
    rs = np.random.RandomState(7)
    pts = []
    for i in range(26):
        z = 1 - 2 * (i + 0.5) / 26
        a = i * 2.39996
        r = math.sqrt(1 - z * z)
        d = np.array([r * math.cos(a), r * math.sin(a), z])
        if d[1] < -0.2:
            d[1] *= 0.3
        pts.append(SHELL_C + d * SHELL_R * (0.94 + 0.08 * rs.rand()))
    k.add("shell", hull(pts, merge=6.0), ROCK, "body", fuse=False)
    # its plates: a ridge down the middle and a row at each side, a little darker
    for c, n, ex, w, h in (((0.0, 0.265, 0.5), (0, 1, 0.5), (1, 0, 0), 0.07, 0.055),
                           ((0.0, 0.29, 0.36), (0, 1, 0.1), (1, 0, 0), 0.075, 0.06),
                           ((0.0, 0.27, 0.23), (0, 1, -0.3), (1, 0, 0), 0.07, 0.05),
                           ((0.12, 0.24, 0.43), (0.6, 1, 0.3), (0.85, -0.5, 0), 0.05, 0.065),
                           ((-0.12, 0.24, 0.43), (-0.6, 1, 0.3), (0.85, 0.5, 0), 0.05, 0.065),
                           ((0.13, 0.25, 0.27), (0.6, 1, -0.1), (0.85, -0.5, 0), 0.05, 0.06),
                           ((-0.13, 0.25, 0.27), (-0.6, 1, -0.1), (0.85, 0.5, 0), 0.05, 0.06)):
        k.add("shellplate", plate(c, n, ex, w, h, th=0.02), NAVY, "body", fuse=False)

    # The tail, back and out to +X, pointed
    k.add("tail", tube([(0.0, 0.1, 0.18), (0.08, 0.2, 0.1), (0.17, 0.29, 0.045), (0.24, 0.34, 0.03)],
                       [0.06, 0.045, 0.028, 0.012]), LIGHT, "tail", surface="fur")

    k.build(voxel=VOXEL, smooth=3, band=0.005)

    E = t.on_ell(H, HR, DE)
    cut = lambda u: 0.42 + 0.38 * u
    W, G, K = srgb(t.WHITE), srgb(IRIS), srgb(t.PUPIL)
    def eye(u, v):
        c = np.tile(W, (len(u), 1))
        c[((u + 0.15) / 0.5) ** 2 + ((v + 0.05) / 0.6) ** 2 < 1] = G
        c[((u + 0.18) / 0.24) ** 2 + ((v + 0.05) / 0.3) ** 2 < 1] = K
        c[((u + 0.02) / 0.13) ** 2 + ((v - 0.15) / 0.13) ** 2 < 1] = W
        return c
    outline = t.cut_outline(cut)
    k.eye_states(lambda state: t.lay_eyes(k, state, E, DE, EYE_HALF, eye, LIGHT, NAVY, cut, eps=0.004, line=0.0,
                                          outline=outline))
    k.export(out)


if __name__ == "__main__":
    carracosta(*args())
