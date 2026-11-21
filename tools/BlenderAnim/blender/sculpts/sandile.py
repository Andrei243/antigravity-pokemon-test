# Sandile sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sandile.py -- <the export's Sandile.glb> <out.glb>
# From the references: a small tan crocodile low on four short splayed legs, most of its length a great head: a round
# skull with two eye domes standing on top of it, each face of a dome covered by a black oval joined across the brow
# like dark glasses, a dark grey eye with a big white glint in each; a long flat snout ending in a bulb of a nose with
# two bumps and two black nostrils on it, a dark grey band across the top of the snout, a long shut mouth curving up
# at the back; a pink belly; two dark grey bands round the back, a thick tail rising steeply behind, banded once and
# grey at its point; three dark grey claws on each foot.
#
# Written to be grown from (krokorok.py and krookodile.py import it with sibling("sandile")): the line's colours, its
# head (a skull, domes, a snout in a frame of its own that may be pitched down), the mask and eyes painted and laid
# on the domes, the snout's band, the mouth's line, the nostrils, the claws and the banded tail are the functions
# below, placed by a spec of named numbers; the calls that build Sandile are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from types import SimpleNamespace
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, boxy, eye_look, args

# --- the line's colours ---------------------------------------------------------------------------------------------
TAN = "#DDB27A"
BAND = "#5B5C63"
MASK = "#18181D"
PINK = "#D98E9C"
CLAW = "#4C4D55"
LINE = "#5A3B32"
NOSTRIL = "#2A2226"
EYE_GREY = srgb("#50525B")
WHITE = np.array([1.0, 1.0, 1.0])
SHUT_LINE = srgb("#A4A6AE")             # the shut eye's line on the black mask
VOXEL = 0.0045


class Frame:
    """A frame: origin, right (x), forward (f) and up (u), forward pitched down by `pitch` degrees about x."""
    def __init__(self, origin, pitch=0.0):
        p = math.radians(pitch)
        self.o = np.array(origin, float)
        self.pitch = pitch
        self.ex = np.array([1.0, 0, 0])
        self.ef = np.array([0, -math.cos(p), -math.sin(p)])
        self.eu = np.array([0, -math.sin(p), math.cos(p)])

    def p(self, x, f, u):
        return self.o + x * self.ex + f * self.ef + u * self.eu

    def d(self, x, f, u):
        return x * self.ex + f * self.ef + u * self.eu

    def local(self, P):
        q = P - self.o
        return q @ self.ex, q @ self.ef, q @ self.eu

    def ell(self, c, r, deform=None, sub=4):
        return ell(self.p(*c), r, rot=(self.pitch, 0, 0), deform=deform, sub=sub)


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre."""
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


def fang(base, d, length, width, thick=None, up=(0, 0, 1)):
    """A claw, a tooth or a spike: a flat-faced solid from a kite at its root to its point along d."""
    d = unit(*d)
    a = np.cross(d, np.array(up, float)); a /= np.linalg.norm(a)
    b = np.cross(a, d)
    thick = thick if thick is not None else width * 0.8
    base = np.array(base, float)
    mid = base + d * length * 0.42
    pts = [base + a * width / 2, base - a * width / 2, base + b * thick / 2, base - b * thick / 2 - d * 0.003,
           mid + a * width * 0.3, mid - a * width * 0.3, mid + b * thick * 0.32, mid - b * thick * 0.32,
           base + d * length]
    return hull(pts)


def spline(pts, per=12):
    """The centre line the kit's tube runs through (its Catmull-Rom), sampled densely."""
    pts = np.array(pts, float)
    ext = np.vstack([2 * pts[0] - pts[1], pts, 2 * pts[-1] - pts[-2]])
    out = []
    for i in range(len(pts) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for j in range(per):
            t = j / per
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out.append(pts[-1])
    return np.array(out)


def along(line, P):
    """For each point P: how far along the line (0..1 by length) its nearest point is, and its distance from it."""
    seg = np.linalg.norm(np.diff(line, axis=0), axis=1)
    s = np.concatenate([[0], np.cumsum(seg)]); s /= s[-1]
    d2 = ((P[:, None, :] - line[None]) ** 2).sum(2)
    i = d2.argmin(1)
    return s[i], np.sqrt(d2[np.arange(len(P)), i])


def banded_tail(k, name, pts, radii, col, bands, tip_from, band_col=BAND, bone="tail"):
    """A tail: a tube, rings of band_col at the fractions of its length in `bands` (centre, half width), grey from
    tip_from to its point."""
    k.add(name, tube(pts, radii), col, bone)
    line = spline(pts)
    reach = max(radii) + 0.03
    def mask(P):
        s, d = along(line, P)
        near = d < reach
        m = (s > tip_from).astype(float)
        for c, w in bands:
            m = np.maximum(m, inside(np.abs(s - c) - w, 0.004))
        return m * near
    k.paint(mask, band_col, on=[name])
    return line


# --- the head ----------------------------------------------------------------------------------------------------------
def head(k, H, col):
    """
    The skull with its two eye domes, the snout in a frame of its own (H.snout_pitch down from the skull's), the nose
    bulb with its two bumps, the lower jaw; the band across the snout and the mouth's line painted. Gives back the
    skull's frame and the snout's.
    """
    F = Frame(H.O)
    k.add("skull", F.ell((0, 0, 0), H.skull_r, deform=boxy(2.2)), col, "head")
    for s in (1, -1):
        dc = np.array(H.dome_c) * (s, 1, 1)
        k.add(f"dome{'R' if s > 0 else 'L'}", F.ell(dc, H.dome_r), col, "head")
    k.add("saddle", F.ell(H.saddle_c, H.saddle_r), col, "head")
    S = Frame(F.p(*H.snout_root), H.snout_pitch)
    k.add("snout", S.ell(H.snout_c, H.snout_r, deform=boxy(2.3)), col, "head")
    k.add("nose", S.ell(H.nose_c, H.nose_r), col, "head")
    for s in (1, -1):
        k.add(f"nostrilbump{'R' if s > 0 else 'L'}", S.ell(np.array(H.bump_c) * (s, 1, 1), H.bump_r), col, "head")
    k.add("jaw", S.ell(H.jaw_c, H.jaw_r, deform=boxy(2.3)), H.jaw_col or col, "head")
    if getattr(H, "cheek_r", None):
        k.add("cheek", F.ell(H.cheek_c, H.cheek_r), col, "head")

    def mouth_u(f):
        """Where the mouth's line runs at each station f of the snout (its own up): level, then up at the corner."""
        rise = np.clip((H.mouth_corner - f) / H.mouth_curl, 0, 1) ** 1.6
        return H.mouth_u + H.mouth_rise * rise

    # The band across the top of the snout, above the mouth's line
    if H.band_f is not None:
        def band(P):
            x, f, u = S.local(P)
            return inside(H.band_f[0] - f, 0.0025) * inside(f - H.band_f[1], 0.0025) \
                * inside(mouth_u(f) + 0.008 - u, 0.003) * inside(np.abs(x) - 0.09, 0.003)
        k.paint(band, H.band_col)

    # The mouth's line: from under the nose round its front, along each side and curling up at the back
    def line(P):
        x, f, u = S.local(P)
        return inside(np.abs(u - mouth_u(f)) - H.mouth_w, 0.0018) * inside(H.mouth_corner - H.mouth_curl - f, 0.002) \
            * inside(f - (H.nose_c[1] + H.nose_r[1] + 0.02), 0.002) * inside(np.abs(x) - 0.12, 0.002)
    k.paint(line, LINE)
    return F, S


def nostrils(k, H, S):
    """Two black nostrils on the front of the nose's bumps."""
    c = S.p(H.nostril[0], H.nostril[1], H.nostril[2])
    n = S.d(0.35, 1.0, 0.55)
    k.decal(c, n, (H.nostril_r, H.nostril_r * 0.75), NOSTRIL, eps=0.003, rings=5, segs=20, mirror=True)


def dome_frame(F, H, s):
    """A dome's centre and the way its eye looks (world), with the eye's right and up."""
    dc = F.p(*(np.array(H.dome_c) * (s, 1, 1)))
    de = unit(*F.d(H.eye_dir[0] * s, H.eye_dir[1], H.eye_dir[2]))
    right = np.cross((0, 0, 1.0), de); right /= np.linalg.norm(right)
    up = np.cross(de, right)
    return dc, de, right, up


def mask(k, H, F):
    """
    The black mask painted on the domes: an oval on each dome's face (H.mask_ab, turned by H.mask_tilt and stretched
    outward by H.mask_out), a bridge across the brow between them (H.bridge), and whatever more the species asks
    (H.mask_more(P) on the skull's frame).
    """
    shapes = []
    for s in (1, -1):
        dc, de, right, up = dome_frame(F, H, s)
        shapes.append((dc, de, right, up, s))
    a, b = H.mask_ab
    t = math.radians(H.mask_tilt)

    def m(P):
        out = np.zeros(len(P))
        for dc, de, right, up, s in shapes:
            q = P - dc
            n = q @ de
            x, y = q @ right, q @ up
            # turned so that the oval's outer end rises (s: right is outward on +X, inward on -X)
            xo = x * s
            xr = xo * math.cos(t) + y * math.sin(t)
            yr = -xo * math.sin(t) + y * math.cos(t)
            xr = np.where(xr > 0, xr / (1 + H.mask_out), xr)
            e = np.sqrt((xr / a) ** 2 + (yr / b) ** 2)
            out = np.maximum(out, inside(e - 1, 0.07) * (n > -0.004) * (np.linalg.norm(q, axis=1) < H.mask_reach))
        x, f, u = F.local(P)
        bx, bu, bh, bf = H.bridge
        out = np.maximum(out, inside(np.abs(x) - bx, 0.003) * inside(np.abs(u - bu) - bh, 0.0025) * (f > bf))
        if getattr(H, "mask_more", None):
            out = np.maximum(out, H.mask_more(P, F))
        return out
    k.paint(m, MASK)


def eye_open(eye=EYE_GREY, r=0.74, pupil=None):
    """The open eye on the mask: the mask's black round it, a grey oval, (a dark pupil in it)."""
    def look(u, v):
        rr = np.sqrt(u * u + v * v)
        c = np.tile(srgb(MASK), (len(u), 1))
        c[rr < r] = eye
        if pupil is not None:
            d = np.sqrt(((u - pupil[0]) / pupil[2]) ** 2 + ((v - pupil[1]) / pupil[3]) ** 2)
            c[(d < 1) & (rr < r)] = srgb(MASK)
        return c
    return look


def eyes(k, H, F):
    """The eyes' layer for a state: on each dome's face, inside the mask; a big white glint of its own when open."""
    lid = srgb(MASK)
    def lay(state):
        look = eye_look(state, H.eye_look, lid, SHUT_LINE, line=0.16)
        for s in (1, -1):
            dc, de, right, up = dome_frame(F, H, s)
            E = on_ell(dc, H.dome_r, de) if F.pitch == 0 else dc + de * H.dome_r[0]
            E = E + up * H.eye_up
            fn = look if s > 0 else (lambda u, v, look=look: look(-u, v))
            k.decal(E, de, H.eye_h, fn, eps=0.005, rings=20, segs=56)
            if state == "Open":
                g = E + up * H.glint[1] * H.eye_h[1] + right * s * H.glint[0] * H.eye_h[0]
                k.decal(g, de, (H.eye_h[0] * H.glint[2], H.eye_h[0] * H.glint[2] * 1.1), "#FFFFFF", eps=0.008,
                        rings=5, segs=20)
    return lay


def claw(k, name, base, d, length, width, bone, color=CLAW, up=(0, 0, 1)):
    k.add(name, fang(base, d, length, width, width * 0.8, up=up), color, bone, fuse=False, surface="shell")


# --- Sandile ----------------------------------------------------------------------------------------------------------
H = SimpleNamespace(
    O=(0.0, -0.13, 0.128), skull_r=(0.076, 0.073, 0.056),
    dome_c=(0.04, -0.01, 0.046), dome_r=(0.043, 0.044, 0.041),
    saddle_c=(0.0, -0.014, 0.04), saddle_r=(0.034, 0.032, 0.03),
    cheek_c=(0.0, 0.02, -0.025), cheek_r=(0.062, 0.05, 0.04),
    snout_root=(0.0, 0.05, -0.016), snout_pitch=0.0,
    snout_c=(0.0, 0.07, 0.0), snout_r=(0.047, 0.1, 0.03),
    nose_c=(0.0, 0.158, 0.004), nose_r=(0.054, 0.042, 0.035),
    bump_c=(0.02, 0.172, 0.027), bump_r=(0.019, 0.017, 0.015),
    jaw_c=(0.0, 0.065, -0.04), jaw_r=(0.042, 0.1, 0.021), jaw_col=None,
    mouth_u=-0.027, mouth_rise=0.02, mouth_corner=0.0, mouth_curl=0.035, mouth_w=0.003,
    band_f=(0.06, 0.115), band_col=BAND,
    nostril=(0.019, 0.19, 0.022), nostril_r=0.0085,
    eye_dir=(0.38, 1.0, 0.3), mask_ab=(0.04, 0.037), mask_tilt=0.0, mask_out=0.0, mask_reach=0.07,
    bridge=(0.04, 0.05, 0.012, 0.005),
    eye_h=(0.03, 0.029), eye_up=0.0, eye_look=eye_open(r=0.66), glint=(-0.18, 0.32, 0.36),
)


def sandile(src, out):
    k = Kit(src, coat="scales")

    # The body: a long round trunk low over the legs, a neck as thick as the skull
    def pear(V):
        V = V.copy()
        V[:, :2] *= (1 + 0.1 * np.clip(-V[:, 2], 0, 1))[:, None]
        return V
    k.add("torso", ell((0, 0.03, 0.1), (0.086, 0.135, 0.07), deform=pear), TAN, "body")
    k.add("neck", ell((0, -0.09, 0.11), (0.074, 0.06, 0.06)), TAN, "body")
    k.add("rump", ell((0, 0.12, 0.095), (0.075, 0.06, 0.06)), TAN, "body")

    # Two dark grey bands round the back, down to the pink belly
    def bands(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        m = np.zeros(len(P))
        for c, w in ((-0.005, 0.026), (0.1, 0.026)):
            m = np.maximum(m, inside(np.abs(y - c) - w, 0.0025))
        return m * inside(np.abs(x) - 0.095, 0.003) * inside(0.064 - z, 0.003)
    k.paint(bands, BAND)
    def belly(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        return inside(z - 0.066, 0.003) * inside(np.abs(x) - 0.082, 0.004) * inside(y - 0.17, 0.004) \
            * inside(-0.105 - y, 0.004)
    k.paint(belly, PINK)

    # Four short legs splayed out: a haunch fused into the flank, a stub of shin, a wide foot, three dark claws
    for name, y0, bone in (("fore", -0.06, "leg{s}"), ("hind", 0.095, "hind{s}")):
        for s in (1, -1):
            side = "R" if s > 0 else "L"
            b = bone.format(s=side)
            m = np.array([s, 1, 1.0])
            k.add(f"{name}haunch{side}", ell(np.array((0.075, y0, 0.075)) * m, (0.05, 0.05, 0.045)), TAN, b)
            k.add(f"{name}shin{side}", tube(np.array([(0.1, y0, 0.065), (0.122, y0 - 0.006, 0.04),
                                                    (0.128, y0 - 0.012, 0.022)]) * m, [0.034, 0.03, 0.027]), TAN, b)
            foot = np.array((0.13, y0 - 0.03, 0.013))
            k.add(f"{name}foot{side}", ell(foot * m, (0.032, 0.042, 0.014)), TAN, b)
            for j, dx in enumerate((-0.019, 0.0, 0.019)):
                base = foot + np.array((dx, -0.036, 0.004))
                claw(k, f"{name}claw{side}{j}", base * m, np.array((dx * 4, -1.0, -0.35)) * m, 0.026, 0.016, b)

    # The tail: thick at the rump, rising steeply behind and out to +X, a grey band round it and a grey point
    banded_tail(k, "tail", [(0, 0.13, 0.1), (0.012, 0.22, 0.115), (0.035, 0.29, 0.165), (0.052, 0.32, 0.235)],
                [0.06, 0.047, 0.032, 0.012], TAN, bands=[(0.5, 0.075)], tip_from=0.8)

    # The head, the snout's band, the mouth's line, the nostrils, the mask and the eyes
    F, S = head(k, H, TAN)
    nostrils(k, H, S)
    mask(k, H, F)
    lay = eyes(k, H, F)

    k.build(voxel=VOXEL, smooth=4, band=0.006)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    sandile(*args())
