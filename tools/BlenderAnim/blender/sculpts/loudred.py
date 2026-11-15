# Loudred sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Whismur's script (whismur.py, through sibling): the same disc ears on their stalks, now speakers; the same way of
# laying a face on the front of a ball.
#   blender --background --factory-startup --python sculpts/loudred.py -- <the export's Loudred.glb> <out.glb>
#
# The design, from the references: a stout periwinkle-blue biped whose whole front is one huge squarish open mouth,
# framed by a thick cream lip, a white peg of a tooth at each of its four corners, dark red inside with a big pink
# tongue on its floor, a dark throat and a little uvula; small eyes on the crown over the mouth's top, white with a
# black pupil under a heavy brow, two little nostril bumps between them; two ears like loudspeakers on thin stalks
# out of the crown, each a thick disc whose face is rings: a blue rim, a black ring, pink, a black middle; thick arms
# with round fists (raised in the art, carried forward and down here); short thick legs, big feet with three white
# claws each; a stub of a tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, outward, srgb, inside, unit, boxy, eye_look, polygon, args, sibling

W = sibling("whismur")

BLUE = "#7C84D2"
CREAM = "#F0D580"
WHITE = "#F6F4EE"
INK = "#1C1A26"
PINK = "#E8869E"
MOUTH = "#7A2030"
THROAT = "#3A0E18"
TONGUE = "#E8828E"

# --- the frame (the skeleton's joints: body (0, 0, 0.3), head (0, 0, 0.4), ears (+-0.08, 0, 0.4),
#     arms (+-0.12, 0, 0.32), legs (+-0.08, 0, 0.12), tail (0, 0.1, 0.18)) -------------------------------------------
B = np.array([0.0, 0.02, 0.3])            # the body's middle
BR = np.array([0.205, 0.185, 0.215])
H = np.array([0.0, -0.05, 0.3])           # the front's mass, which the mouth is cut into
HR = np.array([0.195, 0.15, 0.2])
MZ = 0.29                                 # the mouth's middle, its half width and half height
MW, MH = 0.148, 0.145
EAR_C = np.array([0.185, 0.015, 0.625])
EAR_LONG = unit(0.38, 0.0, 0.92)
EAR_FACE = unit(0.16, -0.94, 0.3)
EAR_R = (0.084, 0.08, 0.036)
EYE_X, EYE_Z = 0.064, 0.47


def pear(V):
    V = boxy(2.2)(V)
    s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1) - 0.1 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V


def on_front(x, z, lift=0.0):
    """A point of the front's mass at (x, z), lifted off it along -y."""
    return W.front(x, z, lift, c=H, r=HR)


def rim_points(w, h, rr, z, n=40, lift=0.006):
    """A rounded rectangle round the mouth, each point on the front's surface."""
    pts = []
    per = [(w - rr, h - rr, 0), (-(w - rr), h - rr, 90), (-(w - rr), -(h - rr), 180), (w - rr, -(h - rr), 270)]
    for cx, cz, a0 in per:
        for t in np.linspace(0, 90, n // 4, endpoint=False):
            a = math.radians(a0 + t)
            x, zz = cx + rr * math.cos(a), cz + rr * math.sin(a)
            pts.append(on_front(x, z + zz, lift))
    return pts


def add_mouth(k):
    """The cream lip all round, the hollow carved inside it and lined, the tongue on its floor, the throat and the
    uvula, and the four white pegs of teeth at the corners."""
    pts = rim_points(MW, MH, 0.055, MZ)
    k.add("lip", outward(tube(pts + pts[:3], [0.03] * (len(pts) + 3), seg=14, per=4)), CREAM, "head", fuse=False,
          surface="skin")
    k.add("mouth", ell((0, -0.2, MZ), (MW - 0.014, 0.12, MH - 0.014), deform=boxy(3.4)), MOUTH, "head", carve=True)

    def lining(P):
        """The hollow's walls: inside the lip's opening and behind its front."""
        return inside(np.abs(P[:, 0]) - (MW - 0.01), 0.003) * inside(np.abs(P[:, 2] - MZ) - (MH - 0.01), 0.003) \
            * (1 - inside(P[:, 1] + 0.205, 0.003)) * inside(P[:, 1] + 0.03, 0.003)
    k.paint(lining, MOUTH)
    k.paint(lambda P: inside(np.hypot(P[:, 0] / 0.06, (P[:, 2] - MZ - 0.03) / 0.05) - 1, 0.004)
            * (1 - inside(P[:, 1] + 0.12, 0.004)) * inside(P[:, 1] + 0.03, 0.003), THROAT)
    k.add("tongue", ell((0, -0.125, MZ - 0.088), (0.108, 0.06, 0.05)), TONGUE, "head", fuse=False, surface="skin")
    k.add("uvula", ell((0, -0.115, MZ + 0.075), (0.02, 0.018, 0.026)), TONGUE, "head", fuse=False, surface="skin")
    for zc, down in ((MZ + MH - 0.03, True), (MZ - MH + 0.03, False)):
        x = MW - 0.05
        p = on_front(x, zc, 0.0)
        s = -1 if down else 1
        peg = ell((x, p[1] + 0.006, zc + s * 0.022), (0.018, 0.014, 0.03), deform=boxy(2.6))
        k.add("tooth" + ("u" if down else "d"), peg, WHITE, "head", fuse=False, mirror=True, surface="shell")


# --- eyes: small, white with a black pupil low in it, under a heavy brow ---------------------------------------------
CB, CW, CI = srgb(BLUE), srgb(WHITE), srgb(INK)
EYE_OUTLINE = polygon([(1.0, 0.25), (0.6, 0.6), (-0.4, 0.75), (-0.95, 0.5), (-1.0, -0.2), (-0.6, -0.85),
                       (0.3, -0.95), (0.9, -0.5)])


def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    c[np.hypot((u + 0.12) / 0.42, (v + 0.1) / 0.55) < 1] = CI
    return c


def lay_eyes(k, state):
    p = on_front(EYE_X, EYE_Z, 0.0)
    n = W.normal_at(p, c=H, r=HR) + np.array([0, 0, 0.15])
    n /= np.linalg.norm(n)
    k.decal(p, n, (0.03, 0.026), eye_look(state, eye, CB, CI, line=0.2), eps=0.004, rings=20, segs=56, mirror=True,
            outline=EYE_OUTLINE if state == "Open" else None)


def add_speakers(k):
    """The ears: Whismur's discs on thinner, longer stalks, their faces rings like a loudspeaker's."""
    c, ex, ey, ez = W.add_ear(k, c=EAR_C, long=EAR_LONG, face=EAR_FACE, r=EAR_R, root=(0.07, 0.005, 0.47),
                              stalk_r=(0.03, 0.024), back=BLUE, plate=None)
    centre = c + ez * EAR_R[2] * 0.42
    CP, CK = srgb(PINK), srgb(INK)

    def rings(u, v):
        r = np.hypot(u, v)
        col = np.tile(CB, (len(u), 1))
        col[r < 0.97] = CK
        col[r < 0.625] = CP
        col[r < 0.27] = CK
        return col
    k.decal(centre, ez, (EAR_R[0] * 0.86, EAR_R[1] * 0.86), rings, eps=0.004, rings=20, segs=64, mirror=True,
            up=ex)


def loudred(src, out):
    k = Kit(src, coat="fur")
    k.add("body", ell(B, BR, deform=pear), BLUE, "body")
    k.add("front", ell(H, HR, deform=boxy(2.3)), BLUE, "head")
    k.add("crown", ell((0, -0.02, 0.43), (0.16, 0.15, 0.085)), BLUE, "head")
    for x in (-0.022, 0.022):
        k.add("nostril", ell(on_front(x, 0.462, -0.004), (0.014, 0.012, 0.011)), BLUE, "head")
    add_mouth(k)
    add_speakers(k)
    # Arms: thick, from the shoulders forward and down, round fists
    k.add("arm", tube([(0.15, 0.0, 0.34), (0.215, -0.05, 0.31), (0.255, -0.11, 0.27)], [0.065, 0.058, 0.052]), BLUE,
          "arm{s}", mirror=True)
    k.add("fist", ell((0.265, -0.13, 0.255), (0.06, 0.058, 0.058), deform=boxy(2.4)), BLUE, "arm{s}", mirror=True)
    # Legs: fat thighs fused into the body, short shins, big feet with three white claws
    k.add("thigh", ell((0.105, 0.0, 0.13), (0.1, 0.1, 0.1)), BLUE, "leg{s}", mirror=True)
    k.add("foot", ell((0.12, -0.07, 0.04), (0.08, 0.11, 0.042), rot=(0, 0, -12)), BLUE, "leg{s}", mirror=True)
    for i, dx in enumerate((-0.042, 0.0, 0.042)):
        cx, y = 0.125 + dx, -0.168 + abs(dx) * 0.4
        k.add(f"claw{i}", ell((cx, y, 0.02), (0.016, 0.022, 0.019), deform=boxy(2.6)), WHITE, "leg{s}", fuse=False, mirror=True, surface="shell")
    k.add("tail", ell((0, 0.19, 0.16), (0.05, 0.05, 0.045)), BLUE, "tail")
    k.build(voxel=0.007, band=0.005)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    loudred(*args())
