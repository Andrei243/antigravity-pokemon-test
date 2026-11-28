# Privigale (Carpathia No. 10012) sculpted from nothing on the skeleton and clips of the game's own export (sculptkit.py):
#   blender --background --factory-startup --python sculpts/privigale.py -- <the export's Privigale.glb> <out.glb>
# From its picture (docs/art/reference/gemini/carpathia/10012-Privigale.jpg): a large, elegant nightingale standing
# proud, chest out, beak open in song; a small russet head with a pale face, the line's pale ring round a dark eye,
# and a tall crest of long feathers swept back and up, one of them silver; a great ruff of glowing white and silver
# throat feathers over the chest, dotted, falling in tufts; a cream breast speckled below it; wings half open and
# raised like a singer's arms, each a fan of overlapping pointed russet flight feathers crossed by dark bands and
# tipped silver; feathered brown drumsticks over dark legs bent at the heel; and its signature, a long train of
# ribbons sweeping down from the rump and out and back across the ground, russet with dark stave bands and every
# other one moonlight silver. The rings of light the picture draws round it are an effect, left to the battle; the
# ruff is what glows. The wings are carried raised in a V and forward of the body rather than flung wide, and the
# train fans out toward +X, the side the game's front camera sees.
#
# Grown from trillet.py and nightrill.py (sibling): their eyes and pale ring, open beak, speckles and the way of feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, hull, outward, srgb, inside, unit, args, sibling

t = sibling("trillet")
g = t.g

BROWN, DARK, FACE, CREAM = "#B26E3E", "#4E2A1A", "#E7C7A0", "#F2E2C0"
RUF, SILVER, SILVERBAND, RUFF, RUFFDOT = "#AE582D", "#D4DCE6", "#94A1B5", "#E8EEF6", "#9CAAC0"
RING, BEAK, MOUTH, LEG, CLAW = "#F1E3C2", "#4A4446", "#E89058", "#4A3A36", "#2A2224"

# --- the frame (skeleton: body (0, 0.02, 0.5), head (0, -0.02, 0.66), wings (+-0.12, 0.02, 0.6),
#     tail (0, 0.15, 0.42), legs (+-0.06, 0, 0.34)). The body is carried lower than the joints, so that the legs
#     show some 0.2 of the height ------------------------------------------------------------------------------------
H = np.array([0, -0.045, 0.685]); HR = np.array([0.09, 0.092, 0.084])
BODY = ((0, 0.03, 0.4), (0.13, 0.14, 0.175))
CHEST = ((0, -0.045, 0.465), (0.122, 0.105, 0.135))
RUFFE = ((0, -0.06, 0.55), (0.135, 0.1, 0.085))
EYEDIR = (0.62, -0.7, 0.33)
VOXEL = 0.0085


def blade_feather(k, name, base, d, side, n, ln, w, th, colour, bone, bands=(), tip=None, mirror=False):
    """
    A pointed flight feather: a flat-faced hull widest at two fifths of its length, tapering to a point; bands across
    it (share of the length, its width as a share, colour) and a tip of another colour over its last fifth, each a
    hull a little wider and thicker so its edge is hard on both faces.
    """
    base, d, side, n = (np.array(v, float) for v in (base, d, side, n))
    def hw(s):
        return w / 2 * (0.5 + 0.5 * s / 0.4 if s < 0.4 else max(0.0, (1 - s) / 0.6) ** 0.85)
    def piece(s0, s1, grow, thick):
        pts = []
        for s in np.linspace(s0, s1, 4):
            h = hw(s) * grow
            c = base + d * ln * s
            for a in (1, -1):
                for b in (1, -1):
                    pts.append(c + side * a * max(h, 0.002) + n * b * thick / 2)
        if s1 >= 0.999:
            pts.append(base + d * ln)
        return hull(pts)
    k.add(name, piece(0.0, 1.0, 1.0, th), colour, bone, fuse=False, mirror=mirror)
    for j, (at, bw, bc) in enumerate(bands):
        k.add(f"{name}b{j}", piece(at, at + bw, 1.04, th * 1.5), bc, bone, fuse=False, mirror=mirror)
    if tip is not None:
        k.add(f"{name}t", piece(0.8, 1.0, 1.06, th * 1.5), tip, bone, fuse=False, mirror=mirror)


def wing(k):
    """A wing half open and raised: an arm out and up, a fan of overlapping pointed banded feathers facing forward."""
    S, W = np.array([0.112, 0.02, 0.53]), np.array([0.19, 0.06, 0.61])
    n = unit(0.38, -0.78, 0.3)
    k.add("arm", blade([S, (S + W) / 2 + np.array([0, 0, 0.012]), W], [0.048, 0.04, 0.028], n, 0.45), BROWN, "wing{s}",
          mirror=True)
    eo = np.array([1.0, 0, 0]) - n * n[0]; eo /= np.linalg.norm(eo)
    eu = np.cross(n, eo)
    if eu[2] < 0:
        eu = -eu
    feathers = [(-80, 0.19, 0.25), (-62, 0.205, 0.38), (-44, 0.22, 0.5), (-26, 0.23, 0.62), (-8, 0.24, 0.74),
                (10, 0.245, 0.84), (28, 0.245, 0.93), (46, 0.23, 1.0)]
    for i, (th, ln, at) in enumerate(feathers):
        a = math.radians(th)
        d = math.cos(a) * eo + math.sin(a) * eu
        base = S + (W - S) * at - n * (0.005 * i)
        side = np.cross(n, d)
        blade_feather(k, f"wingf{i}", base, d, side, n, ln * 0.88, 0.09, 0.012, RUF, "wing{s}",
                      bands=[(0.42, 0.07, DARK), (0.6, 0.07, DARK)], tip=SILVER, mirror=True)
    # coverts over the feathers' roots, brown, pointed too
    for i, (th, ln, at) in enumerate([(-62, 0.12, 0.3), (-30, 0.13, 0.55), (0, 0.13, 0.78), (30, 0.12, 0.95)]):
        a = math.radians(th)
        d = math.cos(a) * eo + math.sin(a) * eu
        base = S + (W - S) * at + n * 0.022
        blade_feather(k, f"covert{i}", base, d, np.cross(n, d), n, ln, 0.11, 0.016, BROWN, "wing{s}", mirror=True)


def train(k):
    """
    The long train: ribbons sweeping down from the rump and out toward +X and back over the ground, fanned to
    different lengths, each face turned up and out; russet with dark stave bands, every other one silver.
    """
    base = np.array([0.02, 0.11, 0.33])
    ribbons = [(-14, 0.42), (4, 0.52), (18, 0.55), (32, 0.5), (46, 0.45), (58, 0.38)]
    for i, (yaw, reach) in enumerate(ribbons):
        silver = i % 2 == 1
        col, band = (SILVER, SILVERBAND) if silver else (RUF, DARK)
        y = math.radians(yaw)
        hd = np.array([math.sin(y), math.cos(y), 0.0])
        out_ = np.array([math.cos(y), -math.sin(y), 0.0])          # across the ribbon, away from the body's middle
        lift = 0.005 * i
        def curve(u, y=y, reach=reach, lift=lift):
            u = np.asarray(u, float)
            h = reach * (1 - (1 - u) ** 1.6)
            yy = y + math.radians(10) * u ** 1.5
            hd = np.stack([np.sin(yy), np.cos(yy), np.zeros_like(yy)], axis=-1)
            z = (base[2] - 0.015) * (1 - u) ** 1.8 + 0.015 + lift * (1 - u)
            return np.stack([base[0] + hd[..., 0] * h, base[1] + hd[..., 1] * h, z], axis=-1)
        def width(u):
            return 0.06 * (0.75 + 0.25 * np.sin(np.pi * np.clip(u, 0, 1))) * (1 - u ** 5) + 0.006
        def strip(u0, u1, grow, thick, curve=curve, out_=out_):
            def at(u, v):
                uu = u0 + (u1 - u0) * u
                P = curve(uu)
                T = curve(np.minimum(uu + 0.01, 1.0)) - curve(np.maximum(uu - 0.01, 0.0))
                T /= np.linalg.norm(T, axis=-1, keepdims=True)
                side = np.cross(T, np.array([0, 0, 1.0]))
                side /= np.linalg.norm(side, axis=-1, keepdims=True)
                side = np.where((side @ out_)[..., None] < 0, -side, side)
                up = np.cross(side, T)
                up = np.where(up[..., 2:3] < 0, -up, up)
                # banked 45 degrees so the face turns out as well as up
                across = side * math.cos(math.radians(45)) + up * math.sin(math.radians(45))
                return P + across * ((v - 0.5) * width(uu) * grow)[..., None]
            return outward(sheet(at, max(6, int(56 * (u1 - u0))), 3, thick))
        k.add(f"train{i}", strip(0.0, 0.985, 1.0, 0.012), col, "tail", fuse=False)
        for j, u in enumerate((0.34, 0.48, 0.62, 0.76)):
            k.add(f"trainb{i}_{j}", strip(u, u + 0.05, 0.98, 0.022), band, "tail", fuse=False)


def legs(k):
    """Dark legs bent back at the heel under feathered drumsticks, three toes forward and one back."""
    for s, sd in ((1, "R"), (-1, "L")):
        x = 0.066 * s
        ank = np.array([x * 1.05, -0.005, 0.02])
        k.add(f"leg{sd}", tube([(x, 0.0, 0.25), (x * 1.02, 0.05, 0.12), ank], [0.018, 0.016, 0.014]), LEG,
              f"leg{sd}", fuse=False, surface="scales")
        for j, ang in enumerate((-32, 0, 32, 180)):
            rr = math.radians(ang)
            d = np.array([math.sin(rr) * s, -math.cos(rr), 0])
            ln = 0.075 if ang != 180 else 0.042
            tip = ank + d * ln + np.array([0, 0, -0.02 + 0.0095])
            k.add(f"toe{sd}{j}", tube([ank, (ank + tip) / 2 + np.array([0, 0, 0.004]), tip], [0.012, 0.0105, 0.0095]),
                  LEG, f"leg{sd}", fuse=False, surface="scales")
            cd = d + np.array([0, 0, -0.45])
            k.add(f"claw{sd}{j}", g.spike(tip, tip + cd * 0.022, 0.015, 0.014), CLAW, f"leg{sd}", fuse=False,
                  surface="shell")


def privigale(src, out_path):
    k = Kit(src, coat="fur")
    k.add("body", ell(*BODY, rot=(-12, 0, 0)), BROWN, "body")
    k.add("chest", ell(*CHEST), BROWN, "body")
    k.add("neck", tube([(0, 0.0, 0.53), (0, -0.02, 0.6), (0, -0.035, 0.665)], [0.078, 0.07, 0.062]), BROWN, "head")
    k.add("head", ell(H, HR), BROWN, "head")
    k.add("ruff", ell(*RUFFE), RUFF, "body")
    for s in (1, -1):
        # feathered drumsticks fused to the belly
        k.add("thigh", ell((0.07 * s, 0.03, 0.255), (0.064, 0.07, 0.1), rot=(-15, 0, 0)), BROWN,
              "leg" + ("R" if s > 0 else "L"))
    for i in range(15):
        a = math.radians(-120 + 240 * i / 14)
        o = np.array([math.sin(a), -math.cos(a), 0])
        root = np.array([0, -0.045, 0.525]) + o * np.array([0.12, 0.09, 0])
        tip = root + o * 0.05 + np.array([0, 0, -0.085 + 0.02 * abs(math.sin(a * 2))])
        k.add(f"tuft{i}", g.spike(root, tip, 0.05, 0.02, side=np.cross(o, (0, 0, 1))), RUFF if i % 2 else SILVER,
              "body", fuse=False, glow="#E6ECF7")
    k.paint(lambda P: inside(P[:, 1] - (H[1] - 0.02), 0.01) * inside(P[:, 2] - 0.695, 0.012)
            * inside(0.585 - P[:, 2], 0.01), FACE, on=["head", "neck"])
    k.paint(lambda P: inside((P[:, 0] / 0.1) ** 2 + ((P[:, 2] - 0.4) / 0.115) ** 2 - 1, 0.06)
            * inside(P[:, 1] + 0.04, 0.008), CREAM, on=["chest", "body"])
    t.speckles(k, [(-0.05, 0.41), (-0.015, 0.425), (0.025, 0.415), (0.06, 0.4), (-0.07, 0.38), (-0.035, 0.385),
                   (0.0, 0.38), (0.04, 0.375), (-0.05, 0.345), (-0.015, 0.34), (0.02, 0.34), (0.055, 0.35),
                   (-0.025, 0.31), (0.012, 0.31)], [CHEST, BODY], 0.0075, on=["chest", "body"])
    t.speckles(k, [(-0.07, 0.575), (-0.03, 0.585), (0.01, 0.58), (0.05, 0.575), (0.085, 0.565), (-0.095, 0.56),
                   (-0.05, 0.55), (0.03, 0.55), (0.07, 0.545), (-0.01, 0.557)],
               [RUFFE], 0.006, colour=RUFFDOT, on=["ruff"])
    E = t.on_ell(H, HR, EYEDIR)
    k.decal(E, unit(*EYEDIR), (0.028, 0.027), RING, eps=0.004, rings=12, segs=48, mirror=True)
    # the crest: long feathers swept back and up from the crown, the middle one silver
    top = H + np.array([0, 0.0, HR[2] * 0.8])
    for j, (dx, ln, col) in enumerate(((0.0, 0.22, SILVER), (0.035, 0.19, BROWN), (-0.035, 0.19, BROWN),
                                       (0.065, 0.15, RUF), (-0.065, 0.15, RUF))):
        b = top + np.array([dx * 0.5, -0.03, -0.012])
        tip = b + unit(dx * 2.4, 1.0, 0.9) * ln
        k.add(f"crest{j}", g.spike(b, tip, 0.05, 0.026, side=(1, 0, 0)), col, "head", fuse=False)
    wing(k)
    train(k)
    root = t.on_ell(H, HR, (0, -1, -0.08)) + np.array([0, 0.009, 0])
    t.beak(k, root, (0, -1, 0.25), (0, 0, 1), 0.078, 0.032, 0.016, 30, colour=BEAK, mouth=MOUTH)
    legs(k)
    k.build(voxel=VOXEL, band=0.008, wband=0.06)
    k.eye_states(t.eyes(k, H, HR, EYEDIR, (0.021, 0.021), t.eye_ringed("#140F0C", "#33291F", "#FFFFFF"),
                        RING, "#3A2414", eps=0.009))
    k.export(out_path)


if __name__ == "__main__":
    privigale(*args())
