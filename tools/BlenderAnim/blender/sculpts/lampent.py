# Lampent sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lampent.py -- <the export's Lampent.glb> <out.glb>
#
# Grown from Litwick's script: its flame's colours, its gold eye and its lavender.
# The design, from the references: a lantern, the whole lamp leaning some 20 degrees, its top toward -X (so the hat's
# brim rises toward +X). A round pale glass head, a purple flame showing inside it as a band from its foot up the
# middle, blue below, with a small pale teardrop of a mouth; two yellow eyes in dark rims either side, level with the
# lamp's lean, just under the brim. Over it a broad black hat, a shallow cone with a lavender lining under the brim,
# its apex drawn up into a leaf of a point. Under the glass a black fitting: a flared collar, a waist and a five-
# pointed star of a flange. Two black flat arms from the fitting: the +X one sweeps out and up behind the brim to a
# broad end over the hat's right, the -X one sweeps out and down and curls into a hook under the lamp.
# The skeleton: body (0, 0, 0.228) at the fitting, head (0, 0, 0.308) at the glass, armR/armL (+-0.08, 0, 0.208).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, blade, lathe, slab, srgb, inside, unit, eye_look, polygon, sibling, args

lw = sibling("litwick")

GLASS, BLACK, LINING = "#C6D3EE", "#34333C", "#B4B2E6"
FLAME, FOOT, HEART, INK = lw.FLAME, lw.FLAMEFOOT, lw.FLAMEHEART, lw.EYERIM

LEAN = math.radians(20)
J = np.array([0.0, 0.0, 0.228])                          # the fitting, the lamp's pivot
LX = np.array([math.cos(LEAN), 0, math.sin(LEAN)])       # the lamp's own across (+X, risen)
LZ = np.array([-math.sin(LEAN), 0, math.cos(LEAN)])      # the lamp's own up (leaning to -X)
LY = np.array([0, 1.0, 0])
BALL_Z, BALL_R = 0.094, np.array([0.096, 0.093, 0.094])  # the glass, in the lamp's frame
BRIM = BALL_Z + 0.052                                    # the brim's plane


def at(x, y, z):
    return J + x * LX + y * LY + z * LZ


def way(x, y, z):
    d = x * LX + y * LY + z * LZ
    return d / np.linalg.norm(d)


def local(P):
    Q = P - J
    return Q @ LX, Q @ LY, Q @ LZ


def lamp(k):
    # --- the glass -------------------------------------------------------------------------------------------------
    k.add("glass", ell(at(0, 0, BALL_Z), BALL_R, rot=(0, -math.degrees(LEAN), 0)), GLASS, "head", surface="shell")

    # the flame inside, a band from the foot up the middle (front and back), blue below, purple above
    def band(P):
        x, y, z = local(P)
        zr = np.clip((z - BALL_Z) / BALL_R[2], -1, 1)
        a = np.arctan2(x, -y)
        a = np.where(np.abs(a) > math.pi / 2, np.sign(a) * math.pi - a, a)        # the back seen as the front
        mid = 0.13 * np.sin(math.pi * zr) - 0.04
        half = 0.5 - 0.36 * (zr + 1) / 2
        return inside(np.abs(a - mid) - half, 0.03) * (np.linalg.norm(P - at(0, 0, BALL_Z), axis=1) < 0.1)
    k.paint(band, FLAME)
    k.paint(lambda P: band(P) * inside(local(P)[2] - (BALL_Z - 0.035), 0.006), FOOT)

    # the pale teardrop of a mouth, low in the middle of the band
    tear = polygon([(0.0, 1.0), (0.38, 0.25), (0.62, -0.25), (0.45, -0.75), (0.0, -0.95), (-0.45, -0.75),
                    (-0.62, -0.25), (-0.38, 0.25)])
    n = way(-0.03, -1, -0.12)
    k.decal(at(0, 0, BALL_Z) + n * 0.094, n, (0.018, 0.026), HEART, up=LZ, eps=0.003, outline=tear, rings=12,
            segs=64)

    # --- the fitting under the glass: a flared collar, a waist, a star -----------------------------------------------
    k.add("fitting", lathe(J, LZ, [(0, 0.016), (0.044, 0.014), (0.047, 0.003), (0.026, -0.008), (0.02, -0.022),
                                   (0.022, -0.032), (0.0, -0.034)], seg=40), BLACK, "body", fuse=False, surface="shell")
    star = []
    for i in range(10):
        t = math.pi / 2 + i * math.pi / 5
        r = 0.068 if i % 2 == 0 else 0.03
        star.append((r * math.cos(t), r * math.sin(t)))
    k.add("star", slab(star, at(0, 0, -0.028), LX, -LY, 0.018), BLACK, "body", fuse=False, surface="shell")

    # --- the hat: a shallow black cone, a lavender lining under it, a leaf of a point ---------------------------------
    c = at(0, 0, BRIM)
    k.add("hat", lathe(c, LZ, [(0, 0.115), (0.02, 0.1), (0.05, 0.068), (0.09, 0.038), (0.14, 0.014), (0.186, 0.0),
                               (0.186, -0.009), (0.14, 0.004), (0.09, 0.027), (0.05, 0.055), (0.0, 0.07)], seg=64),
          BLACK, "head", fuse=False, surface="shell")
    k.add("lining", lathe(c, LZ, [(0, 0.075), (0.05, 0.06), (0.09, 0.032), (0.14, 0.009), (0.18, -0.004),
                                  (0.18, -0.011), (0.14, 0.002), (0.09, 0.025), (0.05, 0.053), (0.0, 0.068)], seg=64),
          LINING, "head", fuse=False, surface="shell")
    tip = [at(0, 0, BRIM + 0.1), at(-0.004, 0, BRIM + 0.128), at(-0.012, 0, BRIM + 0.158), at(-0.026, 0, BRIM + 0.186)]
    k.add("point", blade(tip, [0.01, 0.019, 0.013, 0.003], LY, 0.6), BLACK, "head", fuse=False, surface="shell")

    # --- the arms: flat black bands ------------------------------------------------------------------------------------
    up = [(0.028, 0.0, 0.208), (0.09, 0.012, 0.212), (0.148, 0.028, 0.255), (0.183, 0.034, 0.325),
          (0.2, 0.03, 0.395), (0.208, 0.02, 0.448), (0.196, 0.01, 0.482)]
    k.add("armup", blade(up, [0.014, 0.015, 0.015, 0.016, 0.018, 0.02, 0.009], (0, 1, 0), 0.55), BLACK, "armR",
          fuse=False, surface="shell")
    down = [(-0.026, 0.0, 0.198), (-0.08, -0.01, 0.18), (-0.118, -0.014, 0.142), (-0.13, -0.015, 0.098),
            (-0.117, -0.015, 0.06), (-0.088, -0.015, 0.046), (-0.066, -0.015, 0.062), (-0.07, -0.015, 0.085),
            (-0.088, -0.015, 0.086)]
    k.add("armdown", blade(down, [0.014, 0.016, 0.016, 0.015, 0.014, 0.013, 0.012, 0.011, 0.009], (0, 1, 0), 0.55),
          BLACK, "armL", fuse=False, surface="shell")


EYE_A, EYE_DZ, EYE_HALF = 0.6, -0.004, (0.0172, 0.0232)


def eyes(k, state):
    for s in (1, -1):
        n = way(s * math.sin(EYE_A), -math.cos(EYE_A), 0.16)
        cen = at(0, 0, BALL_Z + EYE_DZ) + n * 0.092
        look = eye_look(state, lw.gold_eye, srgb(GLASS), srgb(INK), line=0.17)
        lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(cen, n, EYE_HALF, lk, up=LZ, eps=0.003, rings=20, segs=56)


def lampent(src, out):
    k = Kit(src, coat="shell")
    lamp(k)
    k.build(voxel=0.004, smooth=3, band=0.005)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    lampent(*args())
