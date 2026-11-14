# Leafeon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/leafeon.py -- <the export's Leafeon.glb> <out.glb>
# Grown from eevee.py (the same frame of four legs and a trunk, the same head, nose, mouth and way of making eyes):
# a slim quadruped in cream-tan fur with brown paws; ears that are green leaves lined brown and cream at the root; a
# green leaf curling up from the forehead and over to one side; a crown of cream spikes and a tuft at each cheek; a
# tail that is a big jagged green leaf on a cream stalk; small green sprouts on the chest, the back of the neck and
# the legs above the paws; big brown eyes with a white glint.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, leaf, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

# --- colours ----------------------------------------------------------------------------------------------------------
CREAM = "#F2DC9C"          # the coat: the 3D model's warm cream
PAWS = "#A8642C"           # the paws' brown
GREEN = "#62C48E"          # the leaves: the drawing's and the model's mint green
VEIN = "#3E9C6C"           # the tail's midrib
LINING = "#B97834"         # inside the ears
EYE, PUPIL = "#9A5626", "#3E1E0C"
NOSE, INK = "#4A2A18", "#4A2A18"

# --- the frame (the skeleton's joints: body 0.26 up, head joint (0, -0.13, 0.41), ears (+-0.055, -0.13, 0.51),
#     forelegs (+-0.05, -0.08, 0.24), hind legs (+-0.05, 0.13, 0.24), tail (0, 0.16, 0.28)) ------------------------------
# Against Eevee's script: legs some 1.85 times as long (joints at 0.24 against 0.13), the body some 1.3 times as long,
# the head's radii the same as Eevee's on a model about 1.2 times as tall (so a little smaller of the whole).
H = np.array([0, -0.172, 0.488])
HR = np.array([0.12, 0.11, 0.108])
LEG_X = 0.05
FORE_Y, HIND_Y = -0.085, 0.13
BODY_Z = 0.272


def head_shape(V):
    V = V.copy()
    V[:, 0] *= 1 + 0.06 * np.clip(-V[:, 2], 0, 1) * (1 - np.clip(-V[:, 2], 0, 1))
    return V


def add_head():
    k.add("head", ell(H, HR, deform=head_shape), CREAM, "head")
    k.add("muzzle", ell(H + (0, -0.09, -0.04), (0.048, 0.038, 0.033)), CREAM, "head")
    # A short thick neck, so the head never sits on a pole
    k.add("neck", tube([(0, -0.07, 0.315), (0, -0.11, 0.375), (0, -0.15, 0.435)], [0.074, 0.068, 0.068]), CREAM, "head")


def add_body():
    # A deep sturdy trunk, its belly low enough that the legs show short under it
    k.add("chest", ell((0, -0.065, BODY_Z), (0.075, 0.082, 0.094)), CREAM, "body")
    k.add("middle", ell((0, 0.03, BODY_Z + 0.004), (0.068, 0.11, 0.082)), CREAM, "body")
    k.add("rump", ell((0, 0.12, BODY_Z), (0.074, 0.072, 0.09)), CREAM, "body")


def add_legs(color=CREAM):
    # A broad shoulder and thigh covering the upper half of each leg, the leg tapering under it, a clear paw
    k.add("shoulder", ell((LEG_X + 0.006, FORE_Y + 0.004, 0.2), (0.046, 0.056, 0.098)), color, "leg{s}", mirror=True)
    k.add("leg", tube([(LEG_X, FORE_Y + 0.005, 0.205), (LEG_X + 0.002, FORE_Y, 0.115), (LEG_X + 0.003, FORE_Y - 0.004, 0.035)],
                      [0.042, 0.034, 0.031]), color, "leg{s}", mirror=True)
    k.add("paw", ell((LEG_X + 0.003, FORE_Y - 0.016, 0.023), (0.036, 0.046, 0.025)), color, "leg{s}", mirror=True)
    k.add("thigh", ell((LEG_X + 0.006, HIND_Y + 0.008, 0.2), (0.05, 0.07, 0.102)), color, "hind{s}", mirror=True)
    k.add("hind", tube([(LEG_X + 0.002, HIND_Y + 0.012, 0.19), (LEG_X + 0.003, HIND_Y + 0.026, 0.115),
                        (LEG_X + 0.003, HIND_Y + 0.012, 0.065), (LEG_X + 0.003, HIND_Y, 0.035)],
                       [0.044, 0.034, 0.032, 0.031]), color, "hind{s}", mirror=True)
    k.add("hpaw", ell((LEG_X + 0.003, HIND_Y - 0.016, 0.023), (0.036, 0.046, 0.025)), color, "hind{s}", mirror=True)
    # Brown paws: a crisp line round each leg a little above the paw
    k.paint(lambda P: inside(P[:, 2] - 0.064, 0.0025), PAWS)


def along(P, pts, n=200):
    """Nearest place on a polyline (|x| taken, so a mirrored part's both sides): its share of the length and the offset."""
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]); L = cum[-1]
    s = np.linspace(0, L, n)
    line = np.stack([np.interp(s, cum, pts[:, i]) for i in range(3)], axis=1)
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    j = np.concatenate([((Q[a:a + 4000, None, :] - line[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 4000)])
    return s[j] / L, Q - line[j]


# --- the ears: Eevee's ears made broad leaves, green with a cream root and a brown lining on their front faces -----
# Tall leaves, as tall as the head, their edges notched: the radius along many points steps back at every other one
EAR_A, EAR_B = np.array([0.06, -0.135, 0.56]), np.array([0.165, -0.105, 0.815])
_t = np.linspace(0, 1, 15)
EAR = [tuple(EAR_A + (EAR_B - EAR_A) * t + np.array([0.012, 0, 0]) * math.sin(math.pi * t)) for t in _t]
EAR_R = [float((0.062 * np.sin(np.pi * (0.18 + 0.82 * t)) ** 0.75 + 0.003) * (0.8 if (i % 2 and 0.3 < t < 0.95) else 1))
         for i, t in enumerate(_t)]


def add_ears():
    k.add("ear", blade(EAR, EAR_R, (0, 1, 0), 0.32), GREEN, "ear{s}", mirror=True)
    d = unit(*(np.array(EAR[-1]) - np.array(EAR[0])))
    across = np.cross(d, (0, 1.0, 0)); across /= np.linalg.norm(across)

    def root(P):
        t, off = along(P, EAR)
        return inside(t - 0.22, 0.003) * (np.linalg.norm(off, axis=1) < 0.07)
    k.paint(root, CREAM, on=["ear"])

    def lining(P):
        t, off = along(P, EAR)
        half = 0.034 * np.sin(np.pi * np.clip((t - 0.05) / 0.75, 0, 1)) ** 0.8
        w = off @ across
        front = -off[:, 1]
        return (inside(np.abs(w) - half, 0.0025) * inside(-front + 0.002, 0.002) * inside(t - 0.74, 0.01)
                * (P[:, 2] > 0.55))
    k.paint(lining, LINING, on=["ear"])


# --- the crown: cream spikes over the forehead and a tuft out at each cheek -----------------------------------------
def add_crown():
    for i, (x, h, lean) in enumerate(((0.0, 0.055, 0.0), (0.026, 0.05, 0.4), (0.05, 0.04, 0.8))):
        base = H + (x, -0.02, HR[2] - 0.014)
        d = unit(lean, -0.1, 1)
        k.add(f"crown{i}", blade([base, base + d * h * 0.6, base + d * h], [0.02, 0.011, 0.002], (0, 1, 0), 0.6),
              CREAM, "head", mirror=x > 0)
    base = H + (0.105, -0.02, -0.035)
    d = unit(1, -0.15, -0.25)
    k.add("cheek", blade([base, base + d * 0.022, base + d * 0.04], [0.022, 0.013, 0.002], (0, 1, 0), 0.55),
          CREAM, "head", mirror=True)


# --- the leaf on the forehead: rises from the crown, curls over to the model's -X side and down; its midrib in the
#     plane of the face, so its face shows from the front --------------------------------------------------------------
def curl_leaf():
    # A long ribbon: straight up from the forehead to about the ears' tips, then a turn of 200 degrees over to -X
    base = H + (0.0, -0.045, HR[2] - 0.035)
    rise, r, turn = 0.17, 0.045, math.radians(200)
    total = rise + r * turn
    def midrib(u):
        s = u * total
        phi = np.clip((s - rise) / r, 0, None)
        x = np.where(s < rise, 0.0, -r * (1 - np.cos(phi)))
        z = np.where(s < rise, s, rise + r * np.sin(phi))
        return x - 0.012 * np.sin(np.pi * u), z
    def at(u, v):
        uu = 0.015 + 0.97 * u
        x, z = midrib(uu)
        dx = np.gradient(x, axis=0); dz = np.gradient(z, axis=0)
        n = np.hypot(dx, dz)
        nx, nz = dz / n, -dx / n                  # across the midrib, in the plane of the face
        w = 0.032 * np.sin(np.pi * uu) ** 0.45 + 0.004
        s = (v - 0.5) * 2 * w
        y = base[1] - 0.025 * uu                 # leaning a little forward as it rises
        return np.stack([base[0] + x + nx * s, y + 0 * x, base[2] + z + nz * s], axis=2)
    k.add("curl", sheet(at, 80, 7, 0.012), GREEN, "head", fuse=False, surface="leaf")


# --- the tail: a big jagged leaf on a short cream stalk, running back and up and some 35 degrees out to +X so its
#     face shows from the front three-quarter and from behind ---------------------------------------------------------
TD = unit(0.6, 0.8, 0)                        # back and out
TB = np.array([0.03, 0.2, 0.31])              # where the leaf starts, on the stalk


def tail_mid(u):
    h = 0.05 * u + 0.05 * np.sin(np.pi * u) - 0.045 * np.sin(2 * np.pi * u)
    z = 0.42 * u
    return h, z


def tail_at(u0, u1, width):
    def at(u, v):
        uu = u0 + (u1 - u0) * (0.015 + 0.97 * u)
        h, z = tail_mid(uu)
        dh = np.gradient(h, axis=0); dz = np.gradient(z, axis=0)
        n = np.hypot(dh, dz)
        nh, nz = dz / n, -dh / n
        jag = 1 - 0.36 * (uu * 8 % 1)                   # notches cut into the edge, each tooth pointing to the tip
        grow = np.clip(uu / 0.36, 0, 1) ** 0.6 * np.clip((1 - uu) / 0.42, 0, 1) ** 0.85   # widening from the stalk
        w = width * grow * np.where(uu > 0.24, jag, 1) + 0.003
        s = (v - 0.5) * 2 * w
        hh = h + nh * s; zz = z + nz * s
        return TB[None, None] + hh[..., None] * TD[None, None] + zz[..., None] * np.array([0, 0, 1.0])
    return at


def add_tail():
    k.add("tail", tube([(0, 0.15, 0.28), (0.012, 0.18, 0.295), (0.03, 0.205, 0.315)], [0.03, 0.026, 0.022]), CREAM, "tail")
    k.add("tailbase", sheet(tail_at(0.0, 0.2, 0.085), 14, 7, 0.013), CREAM, "tail", fuse=False, surface="fur")
    k.add("tailleaf", sheet(tail_at(0.17, 1.0, 0.085), 150, 9, 0.009), GREEN, "tail", fuse=False, surface="leaf")
    pts = [TB + tail_mid(u)[0] * TD + np.array([0, 0, tail_mid(u)[1]]) for u in np.linspace(0.22, 0.92, 8)]
    k.add("tailvein", tube(pts, [0.0055] * 7 + [0.003], seg=8, per=4), VEIN, "tail", fuse=False, surface="leaf")


# --- sprouts: curled loose leaves, two on the chest and one on the outside of each leg above the paw, each about as
#     long as the leg is thick ----------------------------------------------------------------------------------------
def curly(name, base, d, out, length, width, bone):
    """A leaf that starts along d and curls toward out; its face turned toward out."""
    d, out = unit(*d), unit(*out)
    wd = np.cross(d, out); wd /= np.linalg.norm(wd)
    def at(u, v):
        uu = 0.015 + 0.97 * u
        mid = (np.array(base)[None, None] + length * (uu[..., None] * d[None, None]
                                                     + 0.55 * uu[..., None] ** 2 * out[None, None]))
        w = width * np.sin(np.pi * uu) ** 0.7 + 0.002
        return mid + ((v - 0.5) * 2 * w)[..., None] * wd[None, None]
    k.add(name, sheet(at, 16, 5, 0.008), GREEN, bone, fuse=False, surface="leaf")


def add_sprouts():
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        curly(f"chest{side}", (0.045 * s, -0.135, 0.285), (0.35 * s, -0.25, -1), (0.6 * s, -1, 0.3), 0.085, 0.022, "body")
        for y, bone in ((FORE_Y, "leg"), (HIND_Y + 0.01, "hind")):
            curly(f"sprout{bone}{side}", (s * (LEG_X + 0.03), y, 0.115), (0.35 * s, -0.15, -1), (s, -0.2, 0.2), 0.065,
                  0.019, bone + side)


# --- the face ---------------------------------------------------------------------------------------------------------
CB, CI = srgb(CREAM), srgb(INK)


def add_nose(at=H + (0, -0.13, -0.033), half=(0.012, 0.008), color=NOSE):
    k.decal(at, unit(0, -1, 0.1), half, color, eps=0.003, outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]),
            rings=6, segs=24)


def mouth(u, v):
    c = np.tile(CB, (len(u), 1))
    c[(np.abs(v - (-0.2 + 1.4 * (np.abs(u) - 0.45) ** 2)) < 0.26) & (np.abs(u) < 0.88)] = CI
    return c


def add_mouth(at=H + (0, -0.122, -0.066), half=(0.023, 0.014), look=mouth):
    k.decal(at, unit(0, -1, -0.2), half, look, eps=0.003, rings=16, segs=72)


# The eyes: Eevee's big ovals, a little rounder, brown with a darker middle and a white glint up and out
EYE_DIR = unit(0.47, -0.88, -0.02)
EYE_HALF = (0.031, 0.038)
CE, CP = srgb(EYE), srgb(PUPIL)


def eye(u, v):
    c = np.tile(CE, (len(u), 1))
    c[((u + 0.05) / 0.58) ** 2 + ((v + 0.08) / 0.66) ** 2 < 1] = CP
    c[((u - 0.2) / 0.3) ** 2 + ((v - 0.4) / 0.26) ** 2 < 1] = (1, 1, 1)
    return c
E = H + EYE_DIR * 0.11 + np.array([0, 0, 0.004])


def eyes(state):
    k.decal(E, EYE_DIR, EYE_HALF, eye_look(state, eye, CB, CI), eps=0.004, rings=20, segs=56, mirror=True)


add_body()
add_legs()
add_head()
add_ears()
add_crown()
curl_leaf()
add_tail()
add_sprouts()
add_nose()
add_mouth()

k.build(voxel=0.0058)
k.eye_states(eyes)
k.export(out)
