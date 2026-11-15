# Mankey sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mankey.py -- <the export's Mankey.glb> <out.glb>
#
# Written to be grown from (Primeape is ape() with its own numbers: sibling("mankey")). From the references: a ball
# of pale cream shaggy fur that is head and body in one, spiky tufts all round its outline but a smooth face; a pink
# pig's snout with two dark nostrils low on the face; furious eyes just above it, each cut straight across the top by
# a black brow sloping down to the nose, a red iris with a black pupil in the white; small pointed cat's ears high on
# the ball, cream with a tan lining. Thin cream arms ending in brown cuffs and big brown fists (raised in the art,
# carried forward and a little down here); cream thighs, brown shins and long-toed brown feet splayed wide; a long thin
# tail out of its back to the +X side, cream at the root and brown along its curl.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, srgb, unit, boxy, polygon, eye_look, smoothstep, inside, args

CREAM, BROWN, PINK, NOSTRIL = "#F0E2CE", "#A87650", "#E8AEB4", "#5E3236"
INK, RED, METAL = "#1A1214", "#D2462A", "#2C2A30"
WHITE = np.array([1.0, 1.0, 1.0])

MANKEY = dict(
    C=(0.0, 0.0, 0.38), R=(0.178, 0.166, 0.17), seed=56,
    tufts=96, tuft_len=0.052, tuft_r=(0.032, 0.015, 0.003), bumps=0.05,
    ear_base=(0.105, 0.0, 0.5), ear_tip=(0.17, 0.006, 0.605), ear_w=0.05, lining="#B88A68",
    eye_dir=(0.35, -0.935, -0.02), eye_half=(0.035, 0.029), iris=True,
    snout_z=-0.36, snout_r=(0.052, 0.04, 0.036),
    shoulder=(0.13, 0.0, 0.38), elbow=(0.24, -0.05, 0.33), wrist=(0.255, -0.15, 0.315),
    arm_r=(0.027, 0.023, 0.021), fist_r=(0.048, 0.052, 0.05), cuff=0.5, brown_arms=False,
    hip=(0.08, 0.0, 0.26), knee=(0.21, -0.05, 0.16), ankle=(0.19, -0.07, 0.06),
    leg_r=(0.04, 0.028, 0.022), brown_from=0.2, feet="long", shackles=False,
    tail=True, vein=False, lid=CREAM,
)


def fur(n, m, amp):
    """A deform of the unit sphere: lumps of fur all round it, fading out over the face."""
    def f(V):
        th = np.arctan2(V[:, 1], V[:, 0]); ph = np.arccos(np.clip(V[:, 2], -1, 1))
        face = smoothstep(0.35, 0.75, -V[:, 1])
        return V * (1 + amp * (1 - face) * np.abs(np.sin(n * th) * np.sin(m * ph)))[:, None]
    return f


def tufts(k, P):
    """Pointed tufts radiating from the ball everywhere but the face, each drooping a little."""
    rs = np.random.RandomState(P["seed"])
    C, R = np.array(P["C"]), np.array(P["R"])
    n = P["tufts"]
    golden = math.pi * (3 - math.sqrt(5))
    for i in range(n):
        z = 1 - 2 * (i + 0.5) / n
        ring = math.sqrt(1 - z * z)
        d = np.array([math.cos(i * golden) * ring, math.sin(i * golden) * ring, z])
        if (-d[1] > 0.5 and d[2] > -0.55) or d[2] < -0.8:
            continue
        d = unit(*(d + rs.normal(scale=0.08, size=3)))
        L = P["tuft_len"] * rs.uniform(0.8, 1.2)
        base = C + d * R * 0.86
        mid = C + d * (R + L * 0.45)
        tip = C + d * (R + L) + np.array([0, 0, -0.25 * L])
        k.add(f"tuft{i}", tube([base, mid, tip], list(P["tuft_r"])), CREAM, "body")


def ear(P):
    """A pointed cat's ear on +X and its lining, as flat-faced pieces (front faces -Y)."""
    B, T, w = np.array(P["ear_base"]), np.array(P["ear_tip"]), P["ear_w"]
    out = unit(T[0] - B[0], 0, T[2] - B[2])                 # up the ear, in its own plane
    across = unit(out[2], 0, -out[0])                        # across the ear's base, outward and down
    pts = [B - across * w, B + across * w * 0.9, T]
    shell = []
    for p, f in zip(pts, (1.0, 1.0, 0.35)):
        shell += [p + np.array([0, -0.022 * f, 0]), p + np.array([0, 0.026 * f, 0])]
    lin = [B - across * w * 0.55 + out * 0.012, B + across * w * 0.5 + out * 0.004, T - out * 0.022]
    lining = []
    for p, f in zip(lin, (1.0, 1.0, 0.45)):
        lining += [p + np.array([0, -0.028 * f, 0]), p + np.array([0, -0.012 * f, 0])]
    return hull(shell), hull(lining)


def shackle(c, axis, r, length):
    """A black iron cuff round a limb: a short drum with chamfered rims."""
    e = 0.005
    return lathe(c, axis, [(0, -length / 2), (r - e, -length / 2), (r, -length / 2 + e), (r, length / 2 - e),
                           (r - e, length / 2), (0, length / 2)])


def edge(k, a, b, cut, r, near, far):
    """A crisp colour line across a limb (both sides, by |x|): along the segment a..b, the part before the share
    cut is painted near and the part after it far, within r of the segment's line."""
    a, b = np.array(a, float), np.array(b, float)
    ab = b - a
    def side(P, after):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        t = ((Q - a) @ ab) / (ab @ ab)
        d = np.linalg.norm(Q - (a + np.clip(t, 0, 1)[:, None] * ab), axis=1)
        s = (t - cut) * np.linalg.norm(ab)
        return inside(d - r, 0.003) * (inside(-s, 0.0025) if after else inside(s, 0.0025)) * (t > -0.1) * (t < 1.1)
    k.paint(lambda P: side(P, False), near)
    k.paint(lambda P: side(P, True), far)


def limbs(k, P):
    S, El, W = (np.array(P[x]) for x in ("shoulder", "elbow", "wrist"))
    a0, a1, a2 = P["arm_r"]
    upper = BROWN if P["brown_arms"] else CREAM
    k.add("arm", tube([S, (S + El) / 2 + np.array([0, 0, 0.01]), El], [a0, (a0 + a1) / 2, a1]), upper, "arm{s}",
          mirror=True)
    M = El + (W - El) * P["cuff"]
    if P["cuff"] > 0:
        k.add("fore", tube([El, M], [a1, (a1 + a2) / 2]), upper, "arm{s}", mirror=True)
    k.add("cuff", tube([M, W], [(a1 + a2) / 2, a2 * 1.12]), BROWN, "arm{s}", mirror=True)
    if P["cuff"] > 0:
        edge(k, El, W, P["cuff"], a1 + 0.012, upper, BROWN)
    fw = unit(*(W - El))
    F = W + fw * P["fist_r"][1] * 0.7
    k.add("fist", ell(F, P["fist_r"], deform=boxy(2.6)), BROWN, "arm{s}", mirror=True)
    k.add("thumb", ell(F + np.array([-P["fist_r"][0] * 0.75, -P["fist_r"][1] * 0.35, P["fist_r"][2] * 0.25]),
                       np.array(P["fist_r"]) * (0.36, 0.42, 0.4)), BROWN, "arm{s}", mirror=True)
    if P["shackles"]:
        k.add("wristiron", shackle(W - fw * 0.045, fw, a2 * 1.12 + 0.014, 0.05), METAL, "arm{s}", fuse=False,
              mirror=True, surface="metal")

    H, K, A = (np.array(P[x]) for x in ("hip", "knee", "ankle"))
    l0, l1, l2 = P["leg_r"]
    thigh = BROWN if P["brown_arms"] else CREAM
    k.add("thigh", tube([H, (H + K) / 2 + np.array([0.01, 0, 0.01]), K], [l0, (l0 + l1) / 2, l1]), thigh, "leg{s}",
          mirror=True)
    J = K + (A - K) * P["brown_from"]
    if P["brown_from"] > 0:
        k.add("knee", tube([K, J], [l1, l1 * 0.97]), thigh, "leg{s}", mirror=True)
    k.add("shin", tube([J, A], [l1 * 0.97, l2]), BROWN, "leg{s}", mirror=True)
    if P["brown_from"] > 0:
        edge(k, K, A, P["brown_from"], l1 + 0.012, thigh, BROWN)
    if P["shackles"]:
        sd = unit(*(K - A))
        k.add("ankleiron", shackle(A + sd * 0.035, sd, l2 * 1.05 + 0.014, 0.05), METAL, "leg{s}", fuse=False,
              mirror=True, surface="metal")
    if P["feet"] == "long":
        # A heel and three long knuckled toes splayed forward and out, a short one behind
        heel = A + np.array([0, -0.01, -0.03])
        k.add("heel", ell(heel, (0.032, 0.038, 0.026)), BROWN, "leg{s}", mirror=True)
        for j, ang in enumerate((-22, 6, 34)):
            d = unit(math.sin(math.radians(ang)), -math.cos(math.radians(ang)), 0)
            p0 = heel + d * 0.02
            p1 = heel + d * 0.05 + np.array([0, 0, 0.012])
            p2 = heel + d * 0.088 + np.array([0, 0, -0.013])
            k.add(f"toe{j}", tube([p0, p1, p2], [0.015, 0.013, 0.012]), BROWN, "leg{s}", mirror=True)
        k.add("backtoe", tube([heel, heel + np.array([-0.012, 0.045, -0.012])], [0.014, 0.012]), BROWN, "leg{s}",
              mirror=True)
    else:
        # A big rounded foot with three thick toes at its front
        Fo = A + np.array([0.005, -0.045, -0.035])
        k.add("foot", ell(Fo, (0.062, 0.09, 0.044)), BROWN, "leg{s}", mirror=True)
        for j, dx in enumerate((-0.034, 0.0, 0.034)):
            k.add(f"toe{j}", ell(Fo + np.array([dx, -0.075, -0.004]), (0.024, 0.03, 0.03)), BROWN, "leg{s}",
                  mirror=True)


def face(k, P):
    C, R = np.array(P["C"]), np.array(P["R"])
    s = R[0] / MANKEY["R"][0]
    # The pig's snout: a pink drum standing forward, flat at the front, two dark nostrils on it
    sr = np.array(P["snout_r"])
    Sn = np.array([0, C[1] - R[1] * 0.97 + sr[1] * 0.1, C[2] + P["snout_z"] * R[2]])
    k.add("snout", ell(Sn, sr, deform=boxy(2.8)), PINK, "body")
    front = Sn[1] - sr[1]
    for side in (1, -1):
        k.decal((side * sr[0] * 0.32, front, Sn[2] + sr[2] * 0.05), (side * 0.1, -1, 0),
                (0.0085 * s, 0.0135 * s), NOSTRIL, eps=0.003, rings=8, segs=32)
    if P["vein"]:
        d = unit(-0.2, -0.8, 0.56)
        at = C + d * R
        def mark(u, v):
            c = np.tile(srgb(CREAM), (len(u), 1))
            for sx in (1, -1):
                for sy in (1, -1):
                    dd = np.sqrt((u - sx * 1.0) ** 2 + (v - sy * 1.0) ** 2)
                    arc = (np.abs(dd - 0.72) < 0.17) & (u * sx > 0.12) & (v * sy > 0.12)
                    c[arc] = srgb("#8E3A2C")
            return c
        k.decal(at, d, (0.029 * s, 0.029 * s), mark, eps=0.003, rings=24, segs=72)


# An eye with u outward and v up: a black brow cuts its top straight across, sloping down to the inner corner
OUTLINE = [(-1.0, -0.3), (-0.55, -0.82), (0.25, -0.94), (0.82, -0.6), (1.0, 0.05), (1.0, 0.7)]


def eye_of(P):
    iris = P["iris"]
    def eye(u, v):
        top = 0.2 + 0.5 * u
        c = np.tile(srgb(INK), (len(u), 1))
        inner = ((u / 0.95) ** 2 + ((v + 0.14) / 0.72) ** 2 < 0.78) & (v < top - 0.3)
        c[inner] = WHITE
        if iris:
            c[inner & ((u + 0.1) ** 2 + (v + 0.3) ** 2 < 0.44 ** 2)] = srgb(RED)
            c[inner & ((u + 0.12) ** 2 + (v + 0.3) ** 2 < 0.2 ** 2)] = srgb(INK)
        else:
            c[inner & ((u + 0.18) ** 2 + ((v + 0.32) / 1.15) ** 2 < 0.22 ** 2)] = srgb(INK)
        return c
    return eye


def eyes(k, P, state):
    C, R = np.array(P["C"]), np.array(P["R"])
    look = eye_look(state, eye_of(P), srgb(P["lid"]), srgb(INK), line=0.17)
    for side in (1, -1):
        d = np.array(P["eye_dir"]) * np.array([side, 1, 1])
        at = C + d * R
        if side > 0:
            col, out = look, polygon(OUTLINE)
        else:
            col, out = (lambda u, v: look(-u, v)), polygon([(-a, b) for a, b in OUTLINE])
        k.decal(at, unit(*d), P["eye_half"], col, outline=out, eps=0.005, rings=20, segs=56)


def ape(k, P):
    C, R = np.array(P["C"]), np.array(P["R"])
    k.add("ball", ell(C, R, deform=fur(7, 5, P["bumps"])), CREAM, "body")
    tufts(k, P)
    shell, lining = ear(P)
    k.add("ear", shell, CREAM, "ear{s}", fuse=False, mirror=True)
    k.add("lining", lining, P["ear_lining"] if "ear_lining" in P else P["lining"], "ear{s}", fuse=False, mirror=True)
    limbs(k, P)
    if P["brown_arms"]:
        # Brown limbs fused into the ball tint the fur where they join: the fur's own cream back over the ball, the
        # limbs leaving it out of a clean edge (the snout kept pink)
        sr = np.array(P["snout_r"])
        Sn = np.array([0, C[1] - R[1] * 0.97, C[2] + P["snout_z"] * R[2]])
        k.paint(lambda P_: inside(np.sqrt((((P_ - C) / R) ** 2).sum(1)) - 1.06, 0.004)
                * (np.sqrt((((P_ - Sn) / (sr * 1.6)) ** 2).sum(1)) > 1), CREAM)
    if P["tail"]:
        TP = [(0, 0.14, 0.3), (0.06, 0.24, 0.25), (0.17, 0.3, 0.25), (0.27, 0.28, 0.31), (0.31, 0.22, 0.4),
              (0.285, 0.16, 0.45), (0.235, 0.165, 0.43)]
        k.add("tail", tube(TP[:4], [0.018, 0.016, 0.015, 0.014]), CREAM, "tail")
        k.add("tailtip", tube(TP[3:], [0.014, 0.014, 0.013, 0.013]), BROWN, "tail")
    face(k, P)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    ape(k, MANKEY)
    k.build(voxel=0.0065, smooth=3, band=0.004)
    k.eye_states(lambda s: eyes(k, MANKEY, s))
    k.export(out)
