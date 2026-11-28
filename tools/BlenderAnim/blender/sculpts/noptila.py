# Noptila sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/noptila.py -- <the export's Noptila.glb> <out.glb>
# From its picture (docs/art/reference/gemini/carpathia/10049-Noptila.jpg) and its brief (CP-dex-1b, No. 49): a fat
# grey-brown caterpillar hunched in a high curve, its back made of plump rolls; its rear end down on the ground, its
# head a big round lobe at the front with its small dark face low at the front of it, a dark rounded snout with two
# tiny black eyes and short mandibles; on the roll just behind the head, high on each side, a big pale eye-spot with
# a dark middle and a dark rim that looks like a watching eye; soft pale bristles over the back; stubby pointed legs
# under the front rolls and round prolegs under the rear.
# Written for Fluturnopt to borrow from (sibling("noptila")): the palette, the eye-spot, the face's dark and its eyes.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, eye_look, args

# --- the line's colours -------------------------------------------------------------------------------------------
GREY, UNDER, BRISTLE, DARK = "#8C8580", "#756E6A", "#B8B0A8", "#3E3A3A"
SPOT, SPOTMID, SPOTRIM, CLAW = "#E8E1CD", "#3A3533", "#4A4442", "#3A3636"

# --- the body: a roll on each joint, the rear on the ground, the middle arched high, the head low at the front -----
CHAIN = [("tail", (0, 0.235, 0.07), 0.07), ("body", (0, 0.172, 0.088), 0.086), ("seg1", (0, 0.11, 0.118), 0.1),
         ("seg2", (0, 0.045, 0.144), 0.11), ("seg3", (0, -0.024, 0.152), 0.112), ("seg4", (0, -0.09, 0.138), 0.105),
         ("seg5", (0, -0.142, 0.104), 0.094)]
HC = np.array([0.0, -0.19, 0.082])
HR = np.array([0.086, 0.074, 0.08])
SNOUT = (np.array([0, -0.25, 0.052]), np.array([0.046, 0.032, 0.036]))
VOXEL = 0.0033


def tangent(i):
    a = np.array(CHAIN[max(i - 1, 0)][1], float); b = np.array(CHAIN[min(i + 1, len(CHAIN) - 1)][1], float)
    d = a - b                                              # the chain runs rear to front, -Y; the roll faces along it
    return math.degrees(math.atan2(d[2], d[1]))


def body(k):
    for i, (bone, at, r) in enumerate(CHAIN):
        k.add(f"roll_{bone}", ell(at, (r * 1.1, r * 0.9, r), rot=(tangent(i), 0, 0)), GREY, bone)
    k.add("head", ell(HC, HR), GREY, "head")
    k.add("snout", ell(*SNOUT), DARK, "head")
    # a little darker underneath
    k.paint(lambda P: inside(P[:, 2] - 0.045, 0.012) * inside(P[:, 1] + 0.16, 0.02), UNDER)


def legs(k):
    """Stubby pointed legs under the three front rolls, round prolegs under the two rear ones."""
    for bone, at, r in CHAIN:
        at = np.array(at)
        if bone in ("seg3", "seg4", "seg5"):
            top = at + np.array([0.05, -0.006, -r * 0.62])
            foot = np.array([0.064, at[1] - 0.012, 0.018])
            k.add(f"leg_{bone}", tube([top, (top + foot) / 2 + np.array([0.004, 0, 0]), foot], [0.024, 0.021, 0.017]),
                  GREY, bone, mirror=True)
            k.add(f"claw_{bone}", hull([foot + np.array([0.009, 0, 0.004]), foot + np.array([-0.009, 0, 0.004]),
                                        foot + np.array([0, 0.009, 0.004]), foot + np.array([0, -0.009, 0.004]),
                                        foot + np.array([0.004, -0.01, -0.012])]), CLAW, bone, fuse=False,
                  mirror=True, surface="shell")
        elif bone in ("body", "seg1"):
            c = np.array([0.045, at[1], max(at[2] - r * 0.78, 0.022)])
            k.add(f"proleg_{bone}", ell(c, (0.024, 0.026, 0.022)), GREY, bone, mirror=True)


def bristle(k, name, base, d, ln, w, colour, bone):
    """A short soft tuft: a cone with a flat-faced base."""
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a); b = np.cross(d, a)
    pts = [base + (a * math.cos(t) + b * math.sin(t)) * w for t in np.linspace(0, 2 * math.pi, 6, endpoint=False)]
    k.add(name, hull(pts + [base + d * ln]), colour, bone, fuse=False)


def bristles(k):
    for i, (bone, at, r) in enumerate(CHAIN[1:], 1):
        at = np.array(at)
        for j, ang in enumerate((-60, 0, 60)):
            t = math.radians(ang)
            d = np.array([math.sin(t), 0.15 * ((j % 2) * 2 - 1), math.cos(t)])
            d /= np.linalg.norm(d)
            base = at + d * np.array([r * 1.1, r * 0.78, r]) * 0.9
            bristle(k, f"hair_{bone}_{j}", base, unit(*(d + np.array([0, 0.25, 0.1]))), 0.022, 0.0055, BRISTLE, bone)
    for j, ang in enumerate((-50, 0, 50)):
        t = math.radians(ang)
        d = unit(math.sin(t), -0.3, math.cos(t))
        bristle(k, f"hair_head_{j}", HC + d * HR * 0.9, unit(*(d + np.array([0, 0.15, 0.1]))), 0.02, 0.0055,
                BRISTLE, "head")


# --- the eye-spots and the eyes -----------------------------------------------------------------------------------
def spot(u, v, ground=SPOT, mid=SPOTMID, rim=SPOTRIM):
    """A watching eye: a pale disc, a dark middle a little toward the front, a dark rim."""
    r = np.hypot(u, v)
    c = np.tile(srgb(ground), (len(u), 1))
    c[np.hypot(u + 0.05, v) < 0.42] = srgb(mid)
    c[r > 0.86] = srgb(rim)
    return c


def eye_spots(k):
    at, r = np.array(CHAIN[5][1]), CHAIN[5][2]
    n = unit(0.84, -0.18, 0.52)
    k.decal(at + n * np.array([r * 1.1, r * 0.78, r]), n, (0.031, 0.031), spot, eps=0.0022, rings=16, segs=48,
            mirror=True)


INK, GLINT = srgb("#0E0C0C"), np.array([1.0, 1.0, 1.0])


def eye_open(u, v):
    c = np.tile(INK, (len(u), 1))
    c[np.hypot(u + 0.25, v - 0.3) < 0.36] = GLINT
    return c


def eyes(k):
    c, r = SNOUT
    d = unit(0.55, -0.62, 0.56)
    E = c + d * r
    m = np.array([-1, 1, 1.0])

    def lay(state):
        look = eye_look(state, eye_open, srgb(DARK), srgb("#BDB4AA"), line=0.2)
        k.decal(E, d, (0.0095, 0.0095), look, eps=0.0018, rings=20, segs=56)
        k.decal(E * m, d * m, (0.0095, 0.0095), lambda u, v: look(-u, v), eps=0.0018, rings=20, segs=56)
    return lay


def mandibles(k):
    c, r = SNOUT
    for s in (1, -1):
        b = c + np.array([0.013 * s, -0.012, -0.024])
        k.add(f"mandible{'R' if s > 0 else 'L'}", hull([b + np.array([0.006, 0, 0]), b - np.array([0.006, 0, 0]),
                                                        b + np.array([0, 0.006, 0]), b + np.array([0, -0.005, 0.004]),
                                                        b + np.array([-0.003 * s, -0.006, -0.018])]),
              CLAW, "head", fuse=False, surface="shell")


def noptila(src, out):
    k = Kit(src, coat="fur")
    body(k)
    legs(k)
    bristles(k)
    mandibles(k)
    eye_spots(k)
    k.build(voxel=VOXEL, band=0.006, wband=0.04)
    k.eye_states(eyes(k))
    k.export(out)


if __name__ == "__main__":
    noptila(*args())
