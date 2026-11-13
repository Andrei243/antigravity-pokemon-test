# The grown people's files (style guide, "Characters from files" and "Grown characters"): one grown build's body,
# face, hair cuts, hats, garments and things held, each a piece of zones on the engine's 26-bone skeleton for the build.
#   blender --background --factory-startup --python sculpts/people_grown.py -- <Teen|Teen-curved|Grown|...> <out.glb> [only,pieces]
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from peoplekit import People, E, Box, T, lathe, torus, cone, V, P, A, W, K, STRIPE, SHADE, THIRD, inside, smoothstep, unit

argv = sys.argv[sys.argv.index("--") + 1:]
build, out = argv[0], argv[1]
only = set(argv[2].split(",")) if len(argv) > 2 else None

pp = People(build)
f = pp.f
H = V(f["HeadC"]); R = f["HeadR"]
SZ, G = f["Size"], f["Girth"]
CURVED = f["Curved"]
HIP, WAIST, CHESTM, CHESTJ, SHY, NECKF, CHIN, CROWN = f["HipY"], f["WaistY"], f["ChestMidY"], f["ChestJointY"], f["ShoulderY"], f["NeckFootY"], f["Chin"], f["Crown"]
SH, EL, WR = V(f["Shoulder"]), V(f["Elbow"]), V(f["Wrist"])
LT, KN, AN, BALL = V(f["LegTop"]), V(f["Knee"]), V(f["Ankle"]), V(f["Ball"])
CW, CD, WW, WD, PW, PD = f["ChestW"], f["ChestD"], f["WaistW"], f["WaistD"], f["PelvisW"], f["PelvisD"]
HL, FL, FWD = f["HandLength"], f["FootLength"], f["Forward"]
ARM = (WR - EL) / np.linalg.norm(WR - EL)
PALM = WR + ARM * HL * 0.45
MIR = np.array([-1.0, 1, 1])
EYE_Y = H[1]; EYE_X = 0.30 * R
# The hair and the hats go by a ball over the skull: its middle and its radius (the chibi kit's head, at a grown head's size)
HH = H + (0, 0.24 * R, -0.12 * R)
RH = 0.9 * R
U = RH / 0.27                    # a chibi head's absolute sizes at this head's


def want(name):
    return only is None or any(name == o or name.startswith(o + ".") or o.startswith(name + ".") for o in only)


def lerp(a, b, t):
    return a + (b - a) * t


def head_dir(az, el):
    return np.array([math.sin(az) * math.cos(el), math.sin(el), math.cos(az) * math.cos(el)])


def on_head(az, el, r=1.0):
    return HH + head_dir(az, el) * RH * r


def piece(name, tint, hides="", colours="zones", cut=None, surface=None, voxel=0.008, smooth=3, wband=0.045):
    cutb = None
    if cut is not None:
        cutb = lambda Pb: cut(np.stack([Pb[:, 0], Pb[:, 2], -Pb[:, 1]], axis=1))
    pp.k.build(voxel=voxel, smooth=smooth, wband=wband, band=0.009, name=name,
               props={"colours": colours, "tint": tint, "hides": hides}, cut=cutb, surface=surface)
    pp.made.append(name)


# ---------------------------------------------------------------------------------------------------------------- body
def torso_parts(grow=0.0, pre="", top=True, zone=P):
    """The torso's own shapes, made bigger by `grow` for a garment: the pelvis, the waist, the rib cage, the shoulders' slopes."""
    pp.add(pre + "pelvis", E((0, HIP + 0.035 * SZ, 0), (PW + grow, 0.1 * SZ, PD + grow), sub=4), zone, "hips")
    pp.add(pre + "waist", E((0, WAIST, 0.002), (WW + grow, (CHESTM - HIP) * 0.5, WD + grow), sub=4), zone, "spine")
    pp.add(pre + "ribs", E((0, CHESTM, 0), (CW + grow, (SHY - WAIST) * 0.56, CD + grow), sub=4), zone, "chest")
    if top:
        pp.add(pre + "trap", T([(0.03 * SZ, NECKF + 0.012 * SZ, -0.02 * SZ + FWD * 0.3), SH + (-0.04 * SZ, -0.012 * SZ, 0)], [0.04 * G * SZ + grow, 0.028 * G * SZ + grow * 0.7]),
               zone, "clavicle.{s}", mirror=True)
        if CURVED:
            pp.add(pre + "bust", E((CW * 0.46, CHESTM - 0.012 * SZ, CD * 0.62), (0.046 * SZ + grow * 0.8, 0.044 * SZ + grow * 0.8, 0.04 * SZ + grow * 0.8)), zone, "chest", mirror=True)


AU0, AU1, AF0, AF1 = 0.044 * G * SZ, 0.031 * G * SZ, 0.033 * G * SZ, 0.025 * G * SZ
TH0, TH1, SHN0, SHN1 = 0.074 * G * SZ, 0.047 * G * SZ, 0.05 * G * SZ, 0.031 * G * SZ
CALF = lerp(KN, AN, 0.3) + (0, 0, -0.008 * SZ)


def arm_parts(grow=0.0, pre="", to=1.0, zone=P):
    pp.add(pre + "delt", E(SH + (-0.004 * SZ, -0.02 * SZ, 0), (AU0 * 1.04 + grow * 0.7, AU0 * 1.2 + grow * 0.5, AU0 * 1.04 + grow * 0.7)), zone, "upperArm.{s}", mirror=True)
    if to <= 0.5:
        pp.add(pre + "upper", T([SH, lerp(SH, EL, 0.62)], [AU0 + grow, lerp(AU0, AU1, 0.62) + grow * 1.2]), zone, "upperArm.{s}", mirror=True)
        return
    pp.add(pre + "upper", T([SH, EL], [AU0 + grow, AU1 + grow]), zone, "upperArm.{s}", mirror=True)
    pp.add(pre + "fore", T([EL, lerp(EL, WR, to)], [AF0 + grow, AF1 + grow]), zone, "foreArm.{s}", mirror=True)


def leg_parts(grow=0.0, pre="", to=1.0, zone=P):
    top = LT + (0, 0.015 * SZ, 0)
    if to <= 0.5:
        end = lerp(LT, KN, to * 2)
        pp.add(pre + "thigh", T([top, end], [TH0 + grow, lerp(TH0, TH1, to * 2) + grow * 1.3]), zone, "thigh.{s}", mirror=True)
        return
    pp.add(pre + "thigh", T([top, KN], [TH0 + grow, TH1 + grow]), zone, "thigh.{s}", mirror=True)
    pp.add(pre + "shin", T([KN, CALF, lerp(KN, AN, to)], [TH1 + grow, SHN0 + grow, SHN1 + grow * (1.6 if grow else 1)]), zone, "shin.{s}", mirror=True)


def seat_part(grow=0.0, pre="", zone=P):
    k = 1.0 if grow else 0.84
    pp.add(pre + "seat", E((0, HIP - 0.01 * SZ, 0), (PW * 0.9 * k + grow, 0.075 * SZ * k, PD * 0.92 * k + grow)), zone, "hips")


if want("body"):
    # The head: a skull, the face's plate, the jaw to a chin, a nose, the ears; the neck on its own bone
    pp.add("skull", E(H + (0, 0.2 * R, -0.12 * R), (0.76 * R, 0.8 * R, 0.88 * R), sub=4), P, "head")
    pp.add("plate", E(H + (0, -0.3 * R, 0.1 * R), (0.66 * R, 0.62 * R, 0.68 * R), sub=4), P, "head")
    pp.add("jaw", E(H + (0, -0.66 * R, 0.26 * R), (0.46 * R, 0.34 * R, 0.46 * R)), P, "head")
    pp.add("chin", E(H + (0, -0.86 * R, 0.5 * R), (0.2 * R, 0.14 * R, 0.2 * R)), P, "head")
    pp.add("bridge", T([H + (0, 0.02 * R, 0.74 * R), H + (0, -0.36 * R, 0.86 * R)], [0.07 * R, 0.085 * R], seg=8, per=2), P, "head")
    pp.add("tip", E(H + (0, -0.4 * R, 0.9 * R), (0.1 * R, 0.085 * R, 0.1 * R)), P, "head")
    pp.add("ear", E(H + (0.74 * R, -0.2 * R, -0.12 * R), (0.07 * R, 0.2 * R, 0.13 * R), pitch=-0.2), P, "head", mirror=True)
    pp.add("neck", T([(0, NECKF - 0.03 * SZ, -0.022 * SZ + FWD * 0.3), H + (0, -0.55 * R, -0.2 * R)], [0.055 * G * SZ, 0.05 * G * SZ]), P, "neck")
    piece("body.head", "skin", voxel=0.0055, wband=0.03)

    torso_parts()
    pp.paint(lambda Q: inside(Q[:, 1] - (HIP + 0.085 * SZ), 0.003), W, on=["pelvis", "waist"])       # underwear
    piece("body.torso", "skin", voxel=0.01)
    arm_parts()
    pp.add("elbow", E(EL, (0.032 * G * SZ,) * 3, sub=2), P, "foreArm.{s}", mirror=True)
    piece("body.arms", "skin", voxel=0.008)
    # Mitten hands: a palm, the fingers one mass a little curled, a thumb forward
    pp.add("palm", T([WR, WR + ARM * HL * 0.45], [0.023 * G * SZ, 0.025 * SZ], seg=10, per=2), P, "hand.{s}", mirror=True)
    pp.add("fingers", T([WR + ARM * HL * 0.45, WR + ARM * HL * 0.85 + (-0.014 * SZ, 0, 0.004)], [0.024 * SZ, 0.015 * SZ], seg=10, per=2), P, "hand.{s}", mirror=True)
    pp.add("thumb", T([WR + ARM * HL * 0.25 + (0, 0, 0.016 * SZ), WR + ARM * HL * 0.55 + (-0.004, 0, 0.032 * SZ)], [0.012 * SZ, 0.009 * SZ], seg=8, per=2), P, "hand.{s}", mirror=True)
    piece("body.hands", "skin", voxel=0.006, wband=0.02)
    leg_parts()
    pp.add("knee", E(KN, (0.047 * G * SZ,) * 3, sub=2), P, "shin.{s}", mirror=True)
    seat_part(zone=W)
    piece("body.legs", "skin", voxel=0.01)
    pp.add("heel", E((AN[0], 0.034 * SZ, AN[2] + 0.012 * SZ), (0.036 * SZ, 0.034 * SZ, 0.065 * SZ)), P, "foot.{s}", mirror=True)
    pp.add("instep", T([AN, (AN[0], 0.03 * SZ, BALL[2] - 0.02 * SZ)], [SHN1, 0.03 * SZ], seg=10, per=2), P, "foot.{s}", mirror=True)
    pp.add("toes", E((BALL[0], 0.02 * SZ, BALL[2] + 0.02 * SZ), (0.04 * SZ, 0.02 * SZ, 0.045 * SZ)), P, "toe.{s}", mirror=True)
    piece("body.feet", "skin", voxel=0.007, wband=0.03)

# ---------------------------------------------------------------------------------------------------------------- face
EW, EH = 0.17 * R, 0.1 * R
FZ = H[2] + 1.3 * R


def face_n(x, y):
    return unit(x * 0.6, (y - H[1]) * 0.15, R)


def patch(x, y, half, zone, turn=0.0, both=True, **kw):
    up = (-math.sin(turn), math.cos(turn), 0)
    pp.decal((x, y, FZ), face_n(x, y), half, zone, up=up, mirror=both and x != 0, **kw)


def arc(x, y, w, h, thick, zone, steps=5, both=True, eps=0.0022):
    for i in range(steps):
        t0, t1 = -1 + 2 * i / steps, -1 + 2 * (i + 1) / steps
        tm = (t0 + t1) / 2
        dy0, dy1 = h * (1 - t0 * t0), h * (1 - t1 * t1)
        turn = math.atan2(dy1 - dy0, (t1 - t0) * w)
        seg = math.hypot(dy1 - dy0, (t1 - t0) * w) / 2 + thick * 0.6
        patch(x + tm * w, y + h * (1 - tm * tm), (seg, thick), zone, turn=turn, both=both, rings=2, segs=10, eps=eps)


def eye_open(wide=False):
    def colour(u, v):
        c = np.tile(K, (len(u), 1))                                               # the lids' line
        inner = (np.abs(u) ** 1.6 / 0.9 ** 1.6 + (np.abs(v + 0.1) / 0.7) ** 1.6) < 1
        c[inner] = W
        iris = (u / 0.5) ** 2 + ((v + 0.02) / 0.86) ** 2 < 1
        c[inner & iris] = P
        c[inner & iris & (v < -0.4)] = A
        c[inner & ((u / 0.26) ** 2 + ((v + 0.02) / 0.46) ** 2 < 1)] = THIRD
        c[inner & iris & (v > 0.34)] = THIRD
        c[((u + 0.2) / 0.16) ** 2 + ((v - 0.3) / 0.24) ** 2 < 1] = W
        return c
    k = 1.14 if wide else 1.0
    patch(EYE_X, EYE_Y, (EW * k, EH * (1.25 if wide else 1.0)), colour, turn=0.06, rings=8, segs=28, eps=0.002)


if want("face"):
    S = "body.head"
    eye_open(); piece("face.eyes.open", "eyes", surface=S)
    eye_open(True); piece("face.eyes.wide", "eyes", surface=S)
    arc(EYE_X, EYE_Y - EH * 0.3, EW, EH * 0.3, 0.014 * R, K, steps=5)
    piece("face.eyes.shut", "eyes", surface=S)
    for nm, k, dy in (("open", 1.0, 0.0), ("wide", 1.14, 0.0), ("shut", 1.0, -EH * 0.6)):
        patch(EYE_X + EW * k * 1.02, EYE_Y + EH * 0.5 + dy, (0.07 * R, 0.014 * R), K, turn=0.55, rings=2, segs=10, eps=0.0026)
        patch(EYE_X + EW * k * 0.7, EYE_Y + EH * 0.95 + dy, (0.05 * R, 0.012 * R), K, turn=0.75, rings=2, segs=10, eps=0.0026)
        piece("face.lashes." + nm, "eyes", surface=S)
    BY = EYE_Y + 0.24 * R
    brows = {"Neutral": (0.0, 0.08), "Happy": (0.02 * R, 0.12), "Surprised": (0.07 * R, 0.1), "Sad": (0.0, -0.34), "Angry": (-0.03 * R, 0.3)}
    for nm, (lift, turn) in brows.items():
        patch(EYE_X + 0.01 * R, BY + lift, (EW * 1.05, 0.03 * R), P, turn=turn, rings=2, segs=12, eps=0.0022)
        piece("face.brows." + nm, "brows", surface=S)
    MY = EYE_Y - 0.66 * R
    LIP, DARK_MOUTH, TONGUE = "#A04852", "#5A2630", "#E88A8E"
    arc(0, MY, 0.14 * R, -0.012 * R, 0.017 * R, LIP, steps=3, both=False)
    piece("face.mouth.Neutral", "", colours="true", surface=S)
    arc(0, MY + 0.03 * R, 0.19 * R, -0.07 * R, 0.019 * R, LIP, steps=5, both=False)
    piece("face.mouth.Happy", "", colours="true", surface=S)
    patch(0, MY - 0.01 * R, (0.07 * R, 0.085 * R), DARK_MOUTH, both=False, rings=4, segs=16, eps=0.002)
    piece("face.mouth.Surprised", "", colours="true", surface=S)
    arc(0, MY - 0.04 * R, 0.14 * R, 0.045 * R, 0.017 * R, LIP, steps=4, both=False)
    piece("face.mouth.Sad", "", colours="true", surface=S)
    arc(0, MY - 0.03 * R, 0.12 * R, 0.02 * R, 0.019 * R, LIP, steps=3, both=False)
    piece("face.mouth.Angry", "", colours="true", surface=S)
    patch(0, MY - 0.01 * R, (0.12 * R, 0.09 * R), DARK_MOUTH, both=False, rings=5, segs=18, eps=0.002)
    patch(0, MY - 0.05 * R, (0.07 * R, 0.035 * R), TONGUE, both=False, rings=3, segs=12, eps=0.003)
    piece("face.mouth.open", "", colours="true", surface=S)
    patch(EYE_X + 0.2 * R, EYE_Y - 0.34 * R, (0.16 * R, 0.09 * R), P, rings=4, segs=18, eps=0.0015)
    piece("face.Blush", "blush", surface=S)

    pp.add("stache", E((0.1 * R, EYE_Y - 0.54 * R, H[2] + 0.84 * R), (0.15 * R, 0.055 * R, 0.07 * R), roll=-0.3), P, "head", fuse=False, mirror=True)
    piece("face.Mustache", "hair")
    pp.add("beard", E(H + (0, -0.84 * R, 0.36 * R), (0.44 * R, 0.24 * R, 0.4 * R)), P, "head")
    pp.add("burn", T([H + (0.7 * R, -0.2 * R, 0.02 * R), H + (0.42 * R, -0.74 * R, 0.3 * R)], [0.09 * R, 0.14 * R]), P, "head", mirror=True)
    piece("face.Beard", "hair", voxel=0.0055, wband=0.03,
          cut=lambda Q: ((np.abs(Q[:, 0]) < 0.24 * R) & (Q[:, 1] > MY - 0.12 * R) & (Q[:, 2] > H[2] + 0.3 * R)).astype(float))
    pp.add("brow", E((EYE_X, EYE_Y + 0.25 * R, H[2] + 0.74 * R), (0.2 * R, 0.055 * R, 0.08 * R), roll=0.12), P, "head", fuse=False, mirror=True)
    piece("face.BushyBrows", "hair")
    GZ = H[2] + 0.86 * R
    pp.add("rim", torus((EYE_X + 0.02 * R, EYE_Y - 0.02 * R, GZ), 0.2 * R, 0.022 * R, pitch=math.pi / 2, seg=18, ring=6), P, "head", fuse=False, mirror=True)
    pp.add("bridge", T([(-EYE_X + 0.22 * R, EYE_Y, GZ), (EYE_X - 0.22 * R, EYE_Y, GZ)], [0.02 * R, 0.02 * R], seg=6, per=1), P, "head", fuse=False)
    pp.add("arm", T([(EYE_X + 0.22 * R, EYE_Y, GZ), (0.78 * R, EYE_Y + 0.02 * R, H[2] - 0.05 * R)], [0.02 * R, 0.02 * R], seg=6, per=2), P, "head", fuse=False, mirror=True)
    piece("face.Glasses", "glasses")

# ---------------------------------------------------------------------------------------------------------------- hair
def cap(k=1.0, low=0.36, wide=1.15, ear=-0.2, nape=-0.72, top=None, c=None, r=None, name="shell"):
    """The hair's cap over the skull, its points over the face, under the ears' line and above `top` drawn in under the skin."""
    def deform(Vb):
        Vb = Vb.copy()
        az = np.abs(np.arctan2(Vb[:, 0], -Vb[:, 1]))
        el = Vb[:, 2]
        face = (az < wide) & (el < low - 0.16 * (az / wide) ** 2)
        under = el < ear + (nape - ear) * np.clip((az - wide) / (math.pi - wide), 0, 1)
        gone = face | under
        if top is not None:
            gone |= el > top
        Vb[gone] *= 0.7
        return Vb
    rr = r if r is not None else (0.9 * R * k, 0.9 * R * k, 1.02 * R * k)
    pp.add(name, E(HH if c is None else c, rr, deform=deform, sub=5), P, "head")


def lock(a, b, ra, rb, bone="head", name="lock", mid=None, mirror=False):
    pts = [a, b] if mid is None else [a, mid, b]
    rs = [ra, rb] if mid is None else [ra, (ra + rb) * 0.55, rb]
    pp.add(name, T(pts, rs, seg=10, per=3), P, bone, mirror=mirror)


def fringe(spec, start=0.62):
    for i, (az, low, lean) in enumerate(spec):
        lock(on_head(az, math.asin(start), 0.98), on_head(az + lean, math.asin(low), 1.08), 0.075 * U, 0.02 * U,
             name=f"fringe{i}", mid=on_head(az + lean * 0.5, math.asin((start + low) / 2 + 0.06), 1.1))


def sides_and_back(hatted):
    for i, (az, el0, el1, r) in enumerate([(1.45, 0.25, -0.3, 0.065), (2.0, 0.2, -0.5, 0.07), (2.6, 0.15, -0.66, 0.075)]):
        lock(on_head(az, el0, 1.0), on_head(az, el1, 1.08), r * U, 0.03 * U, name=f"side{i}", mirror=True)
    lock(on_head(math.pi, 0.1, 1.05), on_head(math.pi, -0.74, 1.12), 0.08 * U, 0.04 * U, name="back")
    if not hatted:
        for i, az in enumerate((0.0, 0.9, -0.9, 2.2, -2.2)):
            lock(HH + (0, RH * 0.98, -RH * 0.1), on_head(az, 0.75, 1.05), 0.065 * U, 0.045 * U, name=f"top{i}")


# Bangs end a little over the brows: a grown face's brows show
BANGS = [(-0.9, 0.3, -0.12), (-0.55, 0.36, -0.1), (-0.2, 0.42, -0.06), (0.14, 0.36, 0.05), (0.5, 0.42, 0.06), (0.86, 0.3, 0.1)]
SHOULDER_BACK = V((0, SHY - 0.08 * SZ, -CD - 0.03 * SZ))


def hair(cut, hatted):
    start = 0.52 if hatted else 0.66
    if cut == "Bald":
        cap(1.0, low=1.2, wide=1.5, ear=-0.25, nape=-0.7, top=0.3)
        return
    if cut == "Bowl":
        cap(1.0, low=0.3, wide=1.5, ear=-0.05, nape=-0.6, c=HH + (0, 0.02 * R, 0), r=(RH * 1.1, RH * 1.06, RH * 1.2))
        return
    if cut == "Afro":
        c = HH + (0, RH * 0.3, -RH * 0.1)
        rad = np.array([RH * 1.3, RH * 1.0, RH * 1.25])
        pp.add("mass", E(c, rad, sub=4), P, "head")
        dirs = [(0, 1, 0), (0, 0.75, 0.65), (0.5, 0.7, 0.5), (-0.5, 0.7, 0.5)]
        for y in (0.6, 0.1):
            for deg in (70, 115, 160, 200, 245, 290):
                a = math.radians(deg)
                dirs.append((math.sin(a), y, math.cos(a)))
        for i, d in enumerate(dirs):
            pp.add(f"curl{i}", E(c + unit(*d) * rad * 0.9, (RH * 0.28,) * 3, sub=2), P, "head")
        cap(1.0, low=0.34, wide=1.15)
        return
    if cut == "Curls":
        cap(1.0, low=0.34, wide=1.1)
        dirs = [] if hatted else [(0, 1, 0)]
        for y, count in ((0.75, 6), (0.4, 9), (0.02, 10), (-0.4, 8)):
            if hatted and y > 0.45:
                continue
            for i in range(count):
                a = (i + (0.5 if count % 2 == 0 else 0)) / count * 2 * math.pi
                d = (math.sin(a), y, math.cos(a))
                if d[2] > 0.3 and y < 0.38:
                    continue
                dirs.append(d)
        for x in (-0.55, -0.18, 0.18, 0.55):
            dirs.append((x, 0.56 + abs(x) * 0.15, 0.8))
        for i, d in enumerate(dirs):
            pp.add(f"curl{i}", E(HH + unit(*d) * np.array([0.9 * R, 0.9 * R, 1.02 * R]), (RH * 0.22,) * 3, sub=2), P, "head")
        return
    if cut == "Spiky":
        cap(1.0)
        spikes = [(0.0, 1.2), (0.9, 0.95), (-0.9, 0.95), (2.0, 0.9), (-2.0, 0.9), (3.14, 0.8), (1.5, 0.35), (-1.5, 0.35), (2.5, 0.2), (-2.5, 0.2), (3.14, 0.1)]
        for i, (az, el) in enumerate(spikes):
            if hatted and el > 0.5:
                continue
            d = head_dir(az, el)
            pp.add(f"spike{i}", cone(HH + d * RH * 0.8, HH + d * RH * 1.5 + (0, RH * 0.08, 0), RH * 0.3, 0.014 * U), P, "head")
        fringe([(-0.75, 0.36, -0.22), (-0.3, 0.3, -0.12), (0.2, 0.38, 0.14), (0.7, 0.32, 0.2)], start)
        sides_and_back(True)
        return
    if cut in ("Swept", "Bun"):
        cap(1.0, low=0.5, wide=1.05)
        if not hatted:
            # The hair's body combed back: one smooth swell over the top, a shallow groove each side of its middle
            pp.add("pomp", E(HH + (0, RH * 0.42, RH * 0.02), (RH * 0.82, RH * 0.72, RH * 1.02), pitch=-0.12, sub=4), P, "head")
            for i, x in enumerate((-0.42, 0.42)):
                lock(HH + (x * RH, RH * 0.72, RH * 0.7), HH + (x * RH * 1.15, RH * 0.45, -RH * 0.95), 0.06 * U, 0.05 * U, name=f"sweep{i}",
                     mid=HH + (x * RH * 1.2, RH * 0.98, -RH * 0.1))
        lock(HH + (RH * 0.78, RH * 0.4, RH * 0.5), HH + (RH * 0.62, -RH * 0.3, -RH * 0.85), 0.06 * U, 0.045 * U, name="sideA", mirror=True)
        lock(HH + (RH * 0.9, RH * 0.02, RH * 0.3), HH + (RH * 0.7, -RH * 0.5, -RH * 0.6), 0.05 * U, 0.04 * U, name="sideB", mirror=True)
        if cut == "Bun":
            back = unit(0, 0.5, -0.87)
            pp.add("knot", E(HH + back * RH * 1.3, (RH * 0.38,) * 3), P, "head")
        return
    cap(1.0)
    fringe(BANGS, start)
    sides_and_back(hatted)
    if cut == "Long":
        pp.add("fall", E(HH + (0, -RH * 0.9, -RH * 0.7), (RH * 0.95, RH * 1.1, RH * 0.5), sub=4), P, "hair")
        for i, x in enumerate((-0.55, -0.18, 0.18, 0.55)):
            lock(HH + (x * RH, -RH * 0.2, -RH * 0.95 + abs(x) * RH * 0.25), SHOULDER_BACK + (x * RH * 1.3, -0.05 * SZ, -0.01 + abs(x) * 0.02), 0.1 * U, 0.06 * U,
                 bone="hair", name=f"fall{i}", mid=HH + (x * RH * 1.1, -RH * 1.3, -RH * 1.0))
        lock(HH + (RH * 0.88, -RH * 0.1, RH * 0.2), (CW * 0.75, SHY - 0.05 * SZ, CD * 0.7), 0.06 * U, 0.035 * U, name="cheek", mirror=True,
             mid=HH + (RH * 0.95, -RH * 1.2, RH * 0.25))
    if cut == "Braids":
        root = HH + (RH * 0.85, -RH * 0.5, -RH * 0.05)
        sh = V((CW * 0.8, SHY + 0.0, CD * 0.9)); end = V((CW * 0.62, CHESTM - 0.02 * SZ, CD + 0.03 * SZ))
        lock(root, sh, 0.05 * U, 0.042 * U, name="plait", mirror=True)
        for i in range(4):
            at = lerp(sh, end, (i + 0.5) / 4)
            pp.add(f"bead{i}", E(at, (0.04 * U, np.linalg.norm(end - sh) / 4 * 0.62, 0.036 * U), sub=2), P, "chest", fuse=False, mirror=True)
        pp.add("tie", E(end + (0, -0.012 * U, 0), (0.026 * U,) * 3, sub=2), W, "chest", fuse=False, mirror=True)
        pp.add("tuft", cone(end + (0, -0.03 * U, 0), end + (0, -0.08 * U, 0), 0.026 * U, 0.01 * U), P, "chest", fuse=False, mirror=True)
    if cut == "Bunches":
        tie = HH + (RH * 0.9, RH * 0.3, -RH * 0.15)
        pp.add("bunch", T([tie, HH + (RH * 1.32, RH * 0.42, -RH * 0.22), HH + (RH * 1.45, -RH * 0.6, -RH * 0.16)], [RH * 0.2, RH * 0.18, RH * 0.07]),
               P, "hair", mirror=True)
        pp.add("tie", E(tie + (RH * 0.08, 0, 0), (RH * 0.13,) * 3, sub=2), W, "hair", fuse=False, mirror=True)
    if cut == "Ponytail":
        back = unit(0, 0.0, -1) if hatted else unit(0, 0.42, -0.9)
        tie = HH + back * RH * 1.14
        pp.add("tail", T([tie, tie + (0, -RH * 0.3, -RH * 0.42), SHOULDER_BACK + (0, 0.02, -0.02)], [RH * 0.2, RH * 0.19, RH * 0.06]), P, "hair")
        pp.add("tie", torus(tie + back * RH * 0.06, RH * 0.16, 0.022 * U, pitch=math.atan2(back[2], back[1]), seg=14, ring=6), W, "hair", fuse=False)


CUTS = ["Short", "Spiky", "Long", "Swept", "Afro", "Bun", "Braids", "Bunches", "Bowl", "Ponytail", "Curls", "Bald"]
for cut in CUTS:
    for hatted in (False, True):
        name = f"hair.{cut}" + (".hat" if hatted else "")
        if want(name):
            hair(cut, hatted)
            piece(name, "hair", voxel=0.0055, smooth=6, wband=0.03)

# ---------------------------------------------------------------------------------------------------------------- hats
def half_dome(at_z=-0.3):
    def fn(Vb):
        Vb = Vb.copy(); Vb[:, 2] = np.maximum(Vb[:, 2], at_z); return Vb
    return fn


def hat(kind):
    add = lambda name, shape, zone, bone="head": pp.add(name, shape, zone, bone, fuse=False)
    Hc, Rr = HH, RH * 1.06          # over the cap and its locks
    if kind == "Beret":
        at = Hc + (0, Rr * 0.8, -0.035 * U)
        add("crown", lathe(at, [(Rr * 0.6, -Rr * 0.3), (Rr * 1.0, -Rr * 0.22), (Rr * 1.16, -Rr * 0.02), (Rr * 1.08, Rr * 0.2), (Rr * 0.7, Rr * 0.38), (Rr * 0.25, Rr * 0.45)],
                           pitch=-0.2, sz=1.05), P)
        add("band", torus(at + (0, -Rr * 0.26, 0.012 * U), Rr * 0.98, 0.032 * U, pitch=-0.2, sz=1.08), A)
        add("stalk", cone(at + (0, Rr * 0.38, -0.012 * U), at + (0, Rr * 0.56, -0.03 * U), 0.022 * U, 0.014 * U), SHADE)
    elif kind == "Cap":
        at = Hc + (0, Rr * 0.3, -0.015 * U)
        add("crown", lathe(at, [(Rr * 1.07, 0), (Rr * 1.08, Rr * 0.2), (Rr * 0.98, Rr * 0.52), (Rr * 0.62, Rr * 0.78), (Rr * 0.2, Rr * 0.86)], pitch=-0.12, sz=1.12), P)
        add("peak", E(Hc + (0, Rr * 0.36, Rr * 1.3), (Rr * 0.76, 0.028 * U, Rr * 0.6), pitch=0.2), A)
        add("button", E(at + (0, Rr * 0.87, -0.03 * U), (0.026 * U, 0.016 * U, 0.026 * U), sub=2), A)
    elif kind == "Fedora":
        at = Hc + (0, Rr * 0.26, -0.015 * U)
        add("crown", lathe(at, [(Rr * 1.05, 0), (Rr * 1.03, Rr * 0.5), (Rr * 0.9, Rr * 0.86), (Rr * 0.5, Rr * 0.95), (Rr * 0.2, Rr * 0.9)], pitch=-0.1, sz=1.12), P)
        add("brim", lathe(at + (0, -Rr * 0.06, 0), [(Rr * 0.9, -0.016 * U), (Rr * 1.45, -0.018 * U), (Rr * 1.5, 0.004 * U), (Rr * 0.9, 0.018 * U)], pitch=-0.1, sz=1.08), P)
        add("band", torus(at + (0, Rr * 0.12, 0), Rr * 1.05, 0.03 * U, pitch=-0.1, sz=1.12), A)
    elif kind == "Cowboy":
        at = Hc + (0, Rr * 0.32, -0.015 * U)
        add("crown", lathe(at, [(Rr * 0.98, 0), (Rr * 0.95, Rr * 0.55), (Rr * 0.82, Rr * 0.92), (Rr * 0.4, Rr * 0.86), (Rr * 0.1, Rr * 0.74)], pitch=-0.08, sz=1.14), P)
        def curl(Vb):
            Vb = Vb.copy(); Vb[:, 2] += 0.9 * np.abs(Vb[:, 0]) ** 3; return Vb
        add("brim", E(at + (0, -Rr * 0.02, 0), (Rr * 1.62, Rr * 0.3, Rr * 1.5), deform=lambda Vb: curl(Vb * np.array([1, 1, 0.2])), sub=4), P)
        add("band", torus(at + (0, Rr * 0.1, 0), Rr * 1.0, 0.028 * U, pitch=-0.08, sz=1.14), A)
    elif kind == "WideHat":
        at = Hc + (0, Rr * 0.34, -0.02 * U)
        add("crown", lathe(at, [(Rr * 1.05, 0), (Rr * 1.06, Rr * 0.3), (Rr * 0.9, Rr * 0.66), (Rr * 0.5, Rr * 0.88), (Rr * 0.15, Rr * 0.92)], pitch=-0.14, sz=1.12), P)
        add("brim", lathe(at + (0, -Rr * 0.04, 0), [(Rr * 0.9, -0.014 * U), (Rr * 1.6, -0.016 * U), (Rr * 1.64, 0.004 * U), (Rr * 0.9, 0.018 * U)], pitch=-0.14, sz=1.06), P)
        add("band", torus(at + (0, Rr * 0.08, 0), Rr * 1.06, 0.03 * U, pitch=-0.14, sz=1.12), A)
    elif kind == "Helmet":
        at = Hc + (0, Rr * 0.4, -Rr * 0.02)
        add("shell", E(at, (Rr * 1.16, Rr * 0.8, Rr * 1.36), pitch=-0.12, deform=half_dome(-0.35), sub=4), P)
        add("stripe", E(at + (0, Rr * 0.02, 0), (Rr * 0.17, Rr * 0.815, Rr * 1.38), pitch=-0.12, deform=half_dome(-0.35), sub=3), A)
        add("peak", E(Hc + (0, Rr * 0.26, Rr * 1.3), (Rr * 0.62, 0.026 * U, Rr * 0.3), pitch=0.18), P)
    elif kind == "HardHat":
        at = Hc + (0, Rr * 0.45, 0)
        add("crown", E(at, (Rr * 1.12, Rr * 0.82, Rr * 1.28), deform=half_dome(-0.3), sub=4), P)
        add("brim", lathe(Hc + (0, Rr * 0.2, 0), [(Rr * 0.9, -0.016 * U), (Rr * 1.42, -0.016 * U), (Rr * 1.44, 0.006 * U), (Rr * 0.9, 0.018 * U)], sz=1.1), P)
        add("ridge", E(at + (0, Rr * 0.02, 0), (Rr * 0.12, Rr * 0.84, Rr * 1.26), deform=half_dome(-0.3), sub=3), A)
    elif kind == "Beanie":
        at = Hc + (0, Rr * 0.4, -Rr * 0.02)
        add("crown", E(at, (Rr * 1.12, Rr * 0.86, Rr * 1.28), pitch=-0.15, deform=half_dome(-0.3), sub=4), P)
        add("cuff", torus(Hc + (0, Rr * 0.2, 0), Rr * 1.06, 0.05 * U, pitch=-0.15, sz=1.14), A)
        add("pompom", E(at + (0, Rr * 0.9, -Rr * 0.14), (Rr * 0.22,) * 3), A)
    elif kind == "Bandana":
        at = Hc + (0, Rr * 0.34, -Rr * 0.02)
        add("cloth", E(at, (Rr * 1.1, Rr * 0.86, Rr * 1.26), deform=half_dome(-0.2), sub=4), P)
        knot = Hc + (0, Rr * 0.14, -Rr * 1.3)
        add("knot", E(knot, (Rr * 0.17,) * 3, sub=2), A)
        for s in (1, -1):
            add(f"tail{s}", cone(knot, knot + (s * Rr * 0.28, -Rr * 0.6, -Rr * 0.25), 0.034 * U, 0.02 * U), A, "hair")
    elif kind == "Headband":
        add("band", torus(Hc + (0, Rr * 0.3, 0), Rr * 1.02, 0.036 * U, pitch=0.18, sz=1.14), P)
        knot = Hc + (0, Rr * 0.52, -Rr * 1.2)
        add("knot", E(knot, (Rr * 0.13,) * 3, sub=2), A)
        for s in (1, -1):
            add(f"tail{s}", cone(knot, knot + (s * Rr * 0.22, -Rr * 0.75, -Rr * 0.3), 0.03 * U, 0.018 * U), A, "hair")
    elif kind == "Visor":
        add("band", torus(Hc + (0, Rr * 0.3, 0), Rr * 1.02, 0.034 * U, pitch=0.15, sz=1.14), A)
        add("peak", E(Hc + (0, Rr * 0.24, Rr * 1.3), (Rr * 0.72, 0.026 * U, Rr * 0.44), pitch=0.22), P)
    elif kind == "NurseCap":
        at = Hc + (0, Rr * 1.08, Rr * 0.5)
        add("cap", Box(at, (Rr * 0.52, 0.075 * U, 0.04 * U), pitch=-0.3), P)
        front = at + (0, 0.004 * U, 0.036 * U)
        add("crossV", Box(front, (0.014 * U, 0.038 * U, 0.012 * U), pitch=-0.3, sub=2), A)
        add("crossH", Box(front, (0.038 * U, 0.014 * U, 0.012 * U), pitch=-0.3, sub=2), A)
    elif kind == "Bow":
        normal = unit(0, 0.78, -0.62)
        knot = Hc + normal * np.array([Rr, Rr, Rr * 1.15]) * 1.12
        for s in (1, -1):
            add(f"loop{s}", E(knot + (s * Rr * 0.44, Rr * 0.06, Rr * 0.08), (Rr * 0.42, Rr * 0.25, Rr * 0.12), pitch=0.9, roll=s * 0.35), P)
            add(f"tail{s}", cone(knot, knot + (s * Rr * 0.28, -Rr * 0.55, -Rr * 0.42), 0.034 * U, 0.022 * U), A)
        add("knot", E(knot + normal * Rr * 0.1, (Rr * 0.2, Rr * 0.2, Rr * 0.15), pitch=0.9, sub=2), A)
    elif kind == "Mask":
        c = HH + (0, -RH * 0.1, 0)
        r = (RH * 1.1, RH * 1.2, RH * 1.24)
        cap(low=0.4, wide=1.0, ear=-0.9, nape=-0.95, c=c, r=r, name="hood")
        for i in range(7):
            a0, a1 = 0.62 - i * (2.6 / 7), 0.62 - (i + 1) * (2.6 / 7)
            p0 = c + (0, math.cos(a0) * r[1], math.sin(a0) * r[2])
            p1 = c + (0, math.cos(a1) * r[1], math.sin(a1) * r[2])
            pp.add(f"crest{i}", T([p0, p1], [0.04 * U, 0.04 * U], seg=8, per=1), A, "head", fuse=False)


HATS = ["Beret", "Cap", "NurseCap", "Bow", "Mask", "Fedora", "Helmet", "HardHat", "Beanie", "Bandana", "Headband", "Cowboy", "Visor", "WideHat"]
for kind in HATS:
    if want("hat." + kind):
        hat(kind)
        piece("hat." + kind, "hat", voxel=0.0055, wband=0.03)

# ---------------------------------------------------------------------------------------------------------------- tops
OFF = 0.009 * max(SZ, 0.85)      # how far a garment stands off the body
HEM_Y = HIP + 0.06 * SZ


def neck_cut(depth=0.0):
    """Leaves a garment open round the neck's foot (and down the chest by `depth` in front)."""
    def cut(Q):
        d = np.hypot(Q[:, 0], (Q[:, 2] + 0.02 * SZ) * 1.0)
        open_neck = (d < 0.062 * G * SZ + 0.006) & (Q[:, 1] > NECKF - 0.02 * SZ)
        vee = (np.abs(Q[:, 0]) < (Q[:, 1] - (NECKF - depth)) * 0.5) & (Q[:, 2] > 0) & (Q[:, 1] > NECKF - depth) if depth else False
        return (open_neck | vee).astype(float)
    return cut


def garment_body(extra=0.0, hem=None, flare=0.0):
    """A garment's body: the torso's shapes made bigger, ending flat at its hem (the pelvis's ball would hang under it)."""
    g = OFF + extra
    torso_parts(g, "g")
    pp.k.parts = [p for p in pp.k.parts if p["name"] != "gpelvis"]
    hem = HEM_Y if hem is None else hem
    wide = PW + g
    low = max(hem, HIP + 0.03 * SZ)
    pp.add("gpelvis", lathe((0, 0, 0), [(wide * 0.98, low), (lerp(wide, WW + g, 0.5) - 0.002, lerp(low, WAIST, 0.5)), (WW + g - 0.004, WAIST)],
                            sz=(lerp(PD, WD, 0.5) + g) / lerp(wide, WW + g, 0.5), seg=22), P, "hips")
    if hem < HIP + 0.03 * SZ:
        pp.add("gskirts", lathe((0, 0, 0), [(wide + flare, hem), (wide + flare * 0.4, lerp(hem, HIP, 0.6)), (wide, HIP + 0.05 * SZ)],
                               sz=(max(PD, WD) + g + 0.006 * SZ) / wide, seg=22), P, "skirt")


def below(y):
    """Nothing: a garment ends at its hem by its own shape (a cut's rim is stepped)."""
    return lambda Q: np.zeros(len(Q))


def cuts(*fns):
    def cut(Q):
        m = np.zeros(len(Q))
        for fn in fns:
            m = np.maximum(m, fn(Q))
        return m
    return cut


def stripes_on(parts, period=0.05, lo=HIP, hi=SHY):
    period *= SZ
    pp.paint(lambda Q: inside(np.sin((Q[:, 1] - lo) * 2 * math.pi / period) * 0.01 + 0.002, 0.0025) * (Q[:, 1] > lo) * (Q[:, 1] < hi), STRIPE, on=parts)


def hem_band(y, on, depth=0.012):
    pp.paint(lambda Q: inside(Q[:, 1] - (y + depth * SZ), 0.002), SHADE, on=on)


def cuffs(on=("gfore",)):
    c = lerp(EL, WR, 0.8)
    pp.paint(lambda Q: inside(Q[:, 1] - c[1], 0.002) * (np.abs(Q[:, 0]) > CW + 0.03 * SZ), SHADE, on=list(on))


if want("top.Shirt"):
    garment_body(); arm_parts(OFF * 0.8, "g", to=0.96)
    stripes_on(["gribs", "gwaist", "gpelvis", "gupper", "gfore", "gdelt", "gbust", "gtrap"])
    hem_band(HEM_Y, ["gpelvis", "gwaist"])
    piece("top.Shirt", "top", "torso,arms", voxel=0.0095, cut=cuts(neck_cut(), below(HEM_Y)))
if want("top.Tee"):
    garment_body(); arm_parts(OFF * 0.9, "g", to=0.5)
    stripes_on(["gribs", "gwaist", "gpelvis", "gupper", "gdelt", "gbust", "gtrap"])
    hem_band(HEM_Y, ["gpelvis", "gwaist"])
    pp.paint(lambda Q: inside(np.hypot(Q[:, 0], Q[:, 2] + 0.02 * SZ) - (0.062 * G * SZ + 0.02 * SZ), 0.002) * (Q[:, 1] > NECKF - 0.04 * SZ), A, on=["gribs", "gtrap"])
    piece("top.Tee", "top", "torso", voxel=0.0095, cut=cuts(neck_cut(), below(HEM_Y)))
if want("top.Jacket"):
    garment_body(0.004 * SZ); arm_parts(OFF * 1.2, "g", to=0.96)
    hem_band(HEM_Y, ["gpelvis", "gwaist"], depth=0.03); cuffs()
    pp.paint(lambda Q: inside(np.abs(Q[:, 0]) - 0.005 * SZ, 0.0015) * (Q[:, 2] > 0) * (Q[:, 1] < NECKF - 0.03 * SZ), A, on=["gribs", "gwaist", "gpelvis"])
    pp.paint(lambda Q: inside(np.maximum(np.abs(np.abs(Q[:, 0]) - WW * 0.55) - 0.028 * SZ, np.abs(Q[:, 1] - (WAIST - 0.02 * SZ)) - 0.005 * SZ), 0.0015) * (Q[:, 2] > 0),
             SHADE, on=["gwaist", "gpelvis"])
    piece("top.Jacket", "top", "torso,arms", voxel=0.0095, cut=cuts(neck_cut(0.05 * SZ), below(HEM_Y)))
if want("top.Coat"):
    hem = KN[1] - 0.05 * SZ
    garment_body(0.006 * SZ, hem=hem, flare=0.04 * SZ); arm_parts(OFF * 1.3, "g", to=0.96)
    # The back of the skirts on the coat's tail
    pp.add("gtail", E((0, lerp(hem, HIP, 0.55), -PD * 0.7), (PW * 0.7, (HIP - hem) * 0.4, 0.03 * SZ)), P, "coatTail")
    pp.paint(lambda Q: inside(np.abs(Q[:, 0] - 0.004 * SZ) - 0.004 * SZ, 0.0015) * (Q[:, 2] > 0) * (Q[:, 1] < NECKF - 0.05 * SZ), SHADE, on=["gribs", "gwaist", "gpelvis", "gskirts"])
    cuffs(); hem_band(hem, ["gskirts"])
    piece("top.Coat", "top", "torso,arms", voxel=0.011, cut=cuts(neck_cut(0.06 * SZ), below(hem)))
if want("top.Robe"):
    hem = AN[1] + 0.03 * SZ
    torso_parts(OFF + 0.006 * SZ, "g")
    pp.add("gskirts", lathe((0, 0, 0), [(PW * 1.25 + 0.03 * SZ, hem), (PW * 1.12 + 0.02 * SZ, lerp(hem, HIP, 0.5)), (PW + OFF + 0.006 * SZ, HIP + 0.05 * SZ)], sz=0.8, seg=22), P, "skirt")
    arm_parts(OFF * 1.8, "g", to=0.96)
    pp.paint(lambda Q: inside(np.abs(Q[:, 0] - 0.004 * SZ) - 0.007 * SZ, 0.0015) * (Q[:, 2] > 0) * (Q[:, 1] < NECKF - 0.05 * SZ), A, on=["gribs", "gwaist", "gpelvis", "gskirts"])
    pp.paint(lambda Q: inside(np.abs(Q[:, 1] - WAIST) - 0.014 * SZ, 0.002), A, on=["gwaist", "gribs", "gpelvis"])
    piece("top.Robe", "top", "torso,arms,legs", voxel=0.013, cut=cuts(neck_cut(0.05 * SZ), below(hem)))
if want("top.Swimsuit"):
    # A one-piece's top: the waist and the lower rib cage (and the bust), ending by its own shape under the collarbones
    g = 0.005
    pp.add("gwaist", E((0, WAIST, 0.002), (WW + g, (CHESTM - HIP) * 0.5, WD + g), sub=4), P, "spine")
    pp.add("gribs", E((0, CHESTM - 0.03 * SZ, 0), (CW + g * 0.6, (SHY - WAIST) * 0.4, CD + g), sub=4), P, "chest")
    if CURVED:
        pp.add("gbust", E((CW * 0.46, CHESTM - 0.012 * SZ, CD * 0.62), (0.046 * SZ + g, 0.044 * SZ + g, 0.04 * SZ + g)), P, "chest", mirror=True)
    piece("top.Swimsuit", "top", "")

# ---------------------------------------------------------------------------------------------------------------- bottoms
WAISTBAND = HIP + 0.1 * SZ


def trouser_top(grow=OFF * 0.7):
    pp.add("bpelvis", E((0, HIP + 0.035 * SZ, 0), (PW + grow, 0.1 * SZ, PD + grow), sub=4), P, "hips")
    seat_part(grow, "b")


if want("bottoms.Trousers"):
    trouser_top(); leg_parts(OFF * 0.6, "b", to=0.97)
    hem_band(lerp(KN, AN, 0.97)[1], ["bshin"])
    piece("bottoms.Trousers", "bottom", "legs", voxel=0.0095)
if want("bottoms.Shorts"):
    trouser_top(); leg_parts(OFF * 0.9, "b", to=0.25)
    hem_band(lerp(LT, KN, 0.5)[1], ["bthigh"])
    piece("bottoms.Shorts", "bottom", "")
if want("bottoms.Skirt"):
    hem = lerp(KN[1], HIP, 0.3)
    pp.add("skirt", lathe((0, 0, 0), [(PW * 1.35 + 0.03 * SZ, hem), (PW * 1.2 + 0.02 * SZ, lerp(hem, HIP, 0.55)), (PW + OFF, HIP + 0.05 * SZ), (PW * 0.96 + OFF, WAISTBAND + 0.01)],
                          sz=0.82, seg=24), P, "skirt")
    hem_band(hem, ["skirt"], depth=0.016)
    piece("bottoms.Skirt", "bottom", "")
if want("bottoms.Swim"):
    trouser_top(0.004); leg_parts(0.004, "b", to=0.08)
    piece("bottoms.Swim", "bottom", "")

# ---------------------------------------------------------------------------------------------------------------- shoes, bag, extras
if want("shoes"):
    def flat(Vb):
        Vb = Vb.copy(); Vb[:, 2] = np.maximum(Vb[:, 2], -0.8); return Vb
    g = 0.006 * SZ
    pp.add("heel", E((AN[0], 0.036 * SZ, AN[2] + 0.012 * SZ), (0.04 * SZ + g, 0.036 * SZ + g * 0.5, 0.07 * SZ + g), deform=flat), P, "foot.{s}", mirror=True)
    pp.add("instep", T([AN + (0, 0.012 * SZ, 0), (AN[0], 0.034 * SZ, BALL[2] - 0.02 * SZ)], [SHN1 + g, 0.034 * SZ + g], seg=10, per=2), P, "foot.{s}", mirror=True)
    pp.add("toes", E((BALL[0], 0.026 * SZ, BALL[2] + 0.02 * SZ), (0.044 * SZ + g, 0.026 * SZ + g * 0.5, 0.05 * SZ + g), deform=flat), P, "toe.{s}", mirror=True)
    pp.paint(lambda Q: inside(Q[:, 1] - 0.014 * SZ, 0.0015), A, on=["heel", "toes", "instep"])
    piece("shoes.Shoes", "shoes", "feet", voxel=0.0075, wband=0.03)
if want("bag"):
    by = lerp(WAIST, SHY, 0.45)
    pp.add("bag", Box((0, by, -CD - 0.06 * SZ), (CW * 0.72, (SHY - WAIST) * 0.4, 0.05 * SZ), n=4), P, "bag", fuse=False)
    pp.add("flap", Box((0, by + (SHY - WAIST) * 0.16, -CD - 0.085 * SZ), (CW * 0.66, (SHY - WAIST) * 0.2, 0.036 * SZ), n=4), A, "bag", fuse=False)
    pp.add("strap", T([(CW * 0.62, SHY - 0.02 * SZ, -CD * 0.9), (CW * 0.66, SHY + 0.03 * SZ, 0.0), (CW * 0.7, CHESTM, CD + OFF + 0.012 * SZ), (CW * 0.66, WAIST + 0.02, WD + OFF + 0.006 * SZ),
                       (CW * 0.6, by - (SHY - WAIST) * 0.3, -CD - 0.03 * SZ)], [0.012 * SZ] * 5, seg=8, per=3), P, "chest", fuse=False, mirror=True)
    piece("bag.Bag", "bag")
if want("extra"):
    NK = V((0, NECKF + 0.02 * SZ, -0.02 * SZ + FWD * 0.3))
    pp.add("wrap", torus(NK, 0.058 * G * SZ, 0.03 * SZ, sx=1.1, seg=20), P, "neck", fuse=False)
    pp.add("tail", T([NK + (0.035 * SZ, 0, CD * 0.6), (0.04 * SZ, SHY - 0.06 * SZ, CD + OFF + 0.02 * SZ), (0.038 * SZ, CHESTM - 0.06 * SZ, CD + OFF + 0.022 * SZ)],
                     [0.026 * SZ, 0.024 * SZ, 0.018 * SZ], seg=10, per=3), P, "chest", fuse=False)
    piece("extra.Scarf", "accent")
    pp.add("collar", torus(NK + (0, -0.014 * SZ, 0), 0.06 * G * SZ, 0.016 * SZ, sx=1.12, seg=20, ring=6), P, "chest", fuse=False)
    piece("extra.Collar", "accent")
    bz = CD + OFF + 0.012 * SZ + (0.03 * SZ if CURVED else 0)
    for i in range(3):
        y = lerp(CHESTM - 0.03 * SZ, HIP + 0.02 * SZ, i / 2)
        zc = lerp(bz, PD + OFF + 0.014 * SZ, i / 2) if not CURVED else lerp(CD + OFF + 0.02 * SZ, PD + OFF + 0.014 * SZ, i / 2)
        pp.add(f"btn{i}", E((0.03 * SZ, y, zc), (0.012 * SZ, 0.012 * SZ, 0.008 * SZ), sub=2), P, "chest" if i == 0 else "spine" if i == 1 else "hips", fuse=False, mirror=True)
    piece("extra.Buttons", "accent")
    zc = CD + OFF + 0.006 * SZ
    pp.add("bib", Box((0, lerp(WAIST, CHESTM, 0.55), max(zc, WD + OFF + 0.006)), (CW * 0.6, (CHESTM - WAIST) * 0.75, 0.012 * SZ), n=5), P, "chest", fuse=False)
    pp.add("strap", T([(CW * 0.5, CHESTM + 0.02 * SZ, zc), (CW * 0.6, SHY + 0.035 * SZ, 0.0), (CW * 0.45, CHESTM, -zc - 0.004), (CW * 0.4, WAIST, -WD - OFF - 0.004)],
                      [0.012 * SZ] * 4, seg=8, per=3), P, "chest", fuse=False, mirror=True)
    pp.add("stud", E((CW * 0.46, CHESTM + 0.0 * SZ, zc + 0.012 * SZ), (0.011 * SZ, 0.011 * SZ, 0.006 * SZ), sub=2), A, "chest", fuse=False, mirror=True)
    piece("extra.Overalls", "bottom")
    pp.add("bib", Box((0, lerp(WAIST, CHESTM, 0.4), max(zc, WD + OFF + 0.006)), (CW * 0.62, (CHESTM - WAIST) * 0.8, 0.01 * SZ), n=5), P, "chest", fuse=False)
    pp.add("lap", Box((0, lerp(KN[1], HIP, 0.62), PD + OFF + 0.014 * SZ), (PW * 0.9, (HIP - KN[1]) * 0.36 + (WAIST - HIP) * 0.5, 0.012 * SZ), n=5), P, "skirt", fuse=False)
    pp.add("tie", torus((0, WAIST, 0.002), WW + OFF + 0.004, 0.008 * SZ, sz=(WD + OFF) / (WW + OFF), seg=20, ring=5), P, "spine", fuse=False)
    piece("extra.Apron", "apron")
    torso_parts(OFF + 0.01 * SZ, "v", top=True)
    piece("extra.Vest", "vest", cut=cuts(neck_cut(0.0), below(HEM_Y + 0.03 * SZ),
          lambda Q: ((np.abs(Q[:, 0]) < 0.02 * SZ + (Q[:, 1] - WAIST) * 0.22) & (Q[:, 2] > 0)).astype(float),
          lambda Q: (np.abs(Q[:, 0]) > CW * 1.02).astype(float)))
    mz = CD + OFF + 0.012 * SZ + (0.03 * SZ if CURVED else 0)
    pp.add("field", E((CW * 0.45, CHESTM + 0.03 * SZ, mz - 0.006 * SZ), (0.03 * SZ, 0.03 * SZ, 0.008 * SZ), sub=3), "#F2D23C", "chest", fuse=False)
    pp.add("facetA", Box((CW * 0.45, CHESTM + 0.03 * SZ, mz), (0.017 * SZ, 0.005 * SZ, 0.004 * SZ), roll=0.6, sub=2), "#2A2630", "chest", fuse=False)
    pp.add("facetB", Box((CW * 0.45, CHESTM + 0.03 * SZ, mz), (0.017 * SZ, 0.005 * SZ, 0.004 * SZ), roll=-0.6, sub=2), "#2A2630", "chest", fuse=False)
    piece("extra.GalacticMark", "", colours="true")

# ---------------------------------------------------------------------------------------------------------------- held
PR = PALM * MIR                  # the right hand, on -X
if want("held"):
    add = lambda name, shape, zone, bone: pp.add(name, shape, zone, bone, fuse=False)
    add("rod", cone(PR, PR + (-0.05, 0.5, 0.85), 0.011, 0.005), "#54423A", "hand.R")
    add("reel", E(PR + (0, 0.05, 0.085), (0.02,) * 3, sub=2), "#B0B4BE", "hand.R")
    piece("held.Rod", "", colours="true")
    tip = PR + (-0.02, 0.62, 0.42); hoop = tip + (0, 0.06, 0.09)
    add("pole", cone(PR, tip, 0.011, 0.009), "#B08854", "hand.R")
    add("hoop", torus(hoop, 0.1, 0.009, pitch=0.5, seg=16, ring=6), "#B0B4BE", "hand.R")
    add("bag", E(hoop + (0, -0.085, 0.02), (0.082, 0.1, 0.082)), "#EEEEE4", "hand.R")
    piece("held.Net", "", colours="true")
    can = V((-0.16, CROWN + 0.22, -0.2))
    add("shaft", T([PR, can], [0.011, 0.011], seg=8, per=1), K, "hand.R")
    add("canopy", lathe(can, [(0.4, -0.03), (0.37, 0.025), (0.22, 0.085), (0.03, 0.115)], pitch=-0.4, roll=0.3, seg=22), P, "hand.R")
    add("rim", torus(can + (0, -0.03, 0), 0.39, 0.015, pitch=-0.4, roll=0.3, seg=22, ring=6), A, "hand.R")
    piece("held.Parasol", "parasol")
    top = V((-SH[0] - 0.06, SHY + 0.3 * SZ, -0.1))
    add("handle", T([PR, top], [0.014, 0.013], seg=8, per=1), "#966C42", "hand.R")
    add("head", T([top + (0, -0.04, 0.2), top, top + (0, 0.04, -0.22)], [0.008, 0.024, 0.008], seg=8, per=3), "#969AA6", "hand.R")
    piece("held.Pickaxe", "", colours="true")
    along = unit(0.55, 0.84, 0); roll = -math.atan2(0.55, 0.84)
    gb = V((-0.04 * SZ, HIP + 0.06 * SZ, max(PD, CD) + OFF + 0.06 * SZ))
    add("body", E(gb, (0.15 * SZ, 0.13 * SZ, 0.04 * SZ), roll=roll), P, "spine")
    add("bout", E(gb + along * 0.17 * SZ, (0.11 * SZ, 0.09 * SZ, 0.038 * SZ), roll=roll), P, "spine")
    add("neck", T([gb + along * 0.24 * SZ, gb + along * 0.58 * SZ], [0.018 * SZ, 0.016 * SZ], seg=8, per=1), A, "spine")
    add("hole", E(gb + along * 0.08 * SZ + (0, 0, 0.034 * SZ), (0.035 * SZ, 0.035 * SZ, 0.01 * SZ), sub=2), K, "spine")
    piece("held.Guitar", "guitar")
    at = V((-SH[0] - 0.01, SHY + 0.1 * SZ, 0.03))
    add("box", Box(at, (0.06, 0.07, 0.15)), P, "chest")
    add("grip", T([at + (0, 0.08, -0.06), at + (0, 0.08, 0.06)], [0.011, 0.011], seg=8, per=1), P, "chest")
    add("lens", lathe(at + (0, 0.005, 0.14), [(0.042, 0), (0.042, 0.045), (0.032, 0.05)], pitch=math.pi / 2, seg=14), A, "chest")
    piece("held.Camera", "camera")
    ring = max(PW, WW) + 0.075 * SZ
    add("ring", torus((0, HIP + 0.08 * SZ, 0.004), ring, 0.05 * SZ, seg=32, ring=8), P, "hips")
    for i in range(4):
        a = (i + 0.5) * math.pi / 2
        add(f"stripe{i}", E((math.sin(a) * ring, HIP + 0.08 * SZ, 0.004 + math.cos(a) * ring), (0.034 * SZ, 0.053 * SZ, 0.053 * SZ), yaw=a, sub=2), A, "hips")
    piece("held.FloatRing", "float")

pp.export(out)
