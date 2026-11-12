# A kit for sculpting a Pokémon from nothing in Blender with no window, on the skeleton and clips of the game's own
# export of the species: parts (ellipsoids, tubes) fused by a voxel remesh, coloured by part and by painted rules,
# pieces left unfused, and patches laid over the surface (eyes, marks). Everything is vertex colours.
#
# Axes: front -Y, up +Z, the model's left -X ("L" bones), about one unit tall, standing on z = 0.
import bpy, bmesh, math, sys
import numpy as np
from mathutils import Vector, Euler
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree


def srgb(h):
    """A colour written as on screen ("#F6C427") as the linear floats Blender keeps."""
    h = h.lstrip("#")
    c = np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)])
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def inside(d, edge=0.006):
    """1 where a signed measure d is below 0, eased over an edge: a painted border one cell soft."""
    return 1 - smoothstep(-edge, edge, d)


_ICO = {}


def _ico(sub):
    if sub not in _ICO:
        bm = bmesh.new()
        bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=1.0)
        V = np.array([v.co[:] for v in bm.verts])
        F = [[v.index for v in f.verts] for f in bm.faces]
        bm.free()
        _ICO[sub] = (V, F)
    V, F = _ICO[sub]
    return V.copy(), [list(f) for f in F]


def ell(c, r, rot=(0, 0, 0), deform=None, sub=4):
    """An ellipsoid: centre, radii, a turn in degrees (x, y, z), and a deform of the unit sphere's points."""
    V, F = _ico(sub)
    if deform is not None:
        V = deform(V)
    V = V * np.array(r)
    if any(rot):
        m = np.array(Euler([math.radians(a) for a in rot]).to_matrix())
        V = V @ m.T
    return V + np.array(c), F


def tube(pts, radii, seg=16, per=8):
    """A round tube through points, each with its radius, with rounded ends."""
    pts = np.array(pts, float)
    radii = np.array(radii, float)
    k = len(pts)
    ext = np.vstack([2 * pts[0] - pts[1], pts, 2 * pts[-1] - pts[-2]])
    P, R = [], []
    for i in range(k - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for j in range(per):
            t = j / per
            P.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                            + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
            R.append(radii[i] + (radii[i + 1] - radii[i]) * t)
    P.append(pts[-1]); R.append(radii[-1])
    P, R = np.array(P), np.array(R)
    T = np.gradient(P, axis=0)
    T /= np.linalg.norm(T, axis=1, keepdims=True)
    n = np.cross(T[0], [0, 0, 1.0])
    if np.linalg.norm(n) < 1e-3:
        n = np.cross(T[0], [1.0, 0, 0])
    n /= np.linalg.norm(n)
    rings = []          # (centre, radius, n, b)
    frames = []
    for i in range(len(P)):
        n = n - T[i] * np.dot(n, T[i]); n /= np.linalg.norm(n)
        frames.append((n.copy(), np.cross(T[i], n)))
    for a in (60, 30):
        s, c = math.sin(math.radians(a)), math.cos(math.radians(a))
        rings.append((P[0] - T[0] * R[0] * s, R[0] * c, *frames[0]))
    for i in range(len(P)):
        rings.append((P[i], R[i], *frames[i]))
    for a in (30, 60):
        s, c = math.sin(math.radians(a)), math.cos(math.radians(a))
        rings.append((P[-1] + T[-1] * R[-1] * s, R[-1] * c, *frames[-1]))
    ang = np.linspace(0, 2 * math.pi, seg, endpoint=False)
    V = []
    for c, r, nn, bb in rings:
        V.extend(c + r * (np.cos(a) * nn + np.sin(a) * bb) for a in ang)
    F = []
    for i in range(len(rings) - 1):
        for j in range(seg):
            a, b = i * seg + j, i * seg + (j + 1) % seg
            F.append([a, b, b + seg, a + seg])
    s, e = len(V), len(V) + 1
    V.append(P[0] - T[0] * R[0]); V.append(P[-1] + T[-1] * R[-1])
    last = (len(rings) - 1) * seg
    for j in range(seg):
        F.append([s, (j + 1) % seg, j])
        F.append([e, last + j, last + (j + 1) % seg])
    return np.array(V), F


class Kit:
    def __init__(self, src=None, name=None, skeleton=None, sides=("R", "L")):
        """
        src: the game's export of the species, whose armature and clips are kept. With none, skeleton makes the
        armature: (bone, parent or None, the joint's place) each, and name names the mesh; it has no clips.
        sides: what "{s}" is on +X and on -X (a Pokémon's left is -X; a person's "L" bones are on +X).
        """
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.context.scene.render.fps = 60          # every clip of the game's a whole number of frames
        self.sides = sides
        self.surfaces = {}
        self.parts, self.paints, self.decals = [], [], []
        if src is None:
            data = bpy.data.armatures.new("Armature")
            self.arm = bpy.data.objects.new("Armature", data)
            bpy.context.scene.collection.objects.link(self.arm)
            bpy.context.view_layer.objects.active = self.arm
            bpy.ops.object.mode_set(mode="EDIT")
            made = {}
            for bone, parent, at in skeleton:
                b = data.edit_bones.new(bone)
                b.head = at
                b.tail = (at[0], at[1], at[2] + 0.05)
                if parent:
                    b.parent = made[parent]
                made[bone] = b
            bpy.ops.object.mode_set(mode="OBJECT")
            self.name = name
            self.bones = [b.name for b in data.bones]
            return
        bpy.ops.import_scene.gltf(filepath=src)
        shapes = {b.custom_shape for o in bpy.data.objects if o.type == "ARMATURE"
                  for b in o.pose.bones if b.custom_shape}
        for o in shapes:
            bpy.data.objects.remove(o, do_unlink=True)
        self.arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
        old = [o for o in bpy.data.objects if o.type == "MESH"]
        self.name = old[0].name
        for o in old:
            bpy.data.objects.remove(o, do_unlink=True)
        self.bones = [b.name for b in self.arm.data.bones]

    # --- authoring -------------------------------------------------------------------------------------------
    def add(self, name, shape, color, bone, fuse=True, mirror=False, glow=None):
        """A part. mirror: authored on +X and copied to -X; "{s}" in the bone is the kit's side name for each."""
        V, F = shape
        col = srgb(color) if isinstance(color, str) else color
        if not mirror:
            self.parts.append(dict(name=name, V=V, F=F, color=col, bone=bone, fuse=fuse, glow=glow))
            return
        self.parts.append(dict(name=name + "R", V=V, F=F, color=col, bone=bone.format(s=self.sides[0]), fuse=fuse, glow=glow))
        self.parts.append(dict(name=name + "L", V=V * np.array([-1, 1, 1]), F=[f[::-1] for f in F], color=col,
                               bone=bone.format(s=self.sides[1]), fuse=fuse, glow=glow))

    def paint(self, mask, color, on=None):
        """Paint over the fused body: mask(P) gives 0..1 for points P (N x 3); on: only parts whose names start so."""
        self.paints.append((mask, srgb(color) if isinstance(color, str) else color, on))

    def decal(self, center, normal, half, color, up=(0, 0, 1), eps=0.004, outline=None, rings=14, segs=44,
              mirror=False, flip=False):
        """
        A patch laid over the fused surface, seen along -normal: an ellipse of half sizes (a, b) about center, or
        outline(theta) times it. color(u, v) gives each point's linear colour for u, v in -1..1 (or one colour).
        mirror: also at -X, with u mirrored so that a picture keeps its inner and outer sides.
        """
        self.decals.append(dict(c=np.array(center, float), n=np.array(normal, float), half=half, color=color,
                                up=np.array(up, float), eps=eps, outline=outline, rings=rings, segs=segs, mx=False))
        if mirror:
            m = np.array([-1, 1, 1.0])
            self.decals.append(dict(c=np.array(center, float) * m, n=np.array(normal, float) * m, half=half,
                                    color=color, up=np.array(up, float) * m, eps=eps, outline=outline, rings=rings,
                                    segs=segs, mx=True))

    # --- building --------------------------------------------------------------------------------------------
    def _fuse(self, voxel, smooth):
        V, F, off = [], [], 0
        for p in self.parts:
            if p["fuse"]:
                V.extend(p["V"].tolist()); F.extend([[i + off for i in f] for f in p["F"]]); off += len(p["V"])
        me = bpy.data.meshes.new("raw")
        me.from_pydata(V, [], F)
        ob = bpy.data.objects.new("raw", me)
        bpy.context.scene.collection.objects.link(ob)
        m = ob.modifiers.new("fuse", "REMESH")
        m.mode = "VOXEL"; m.voxel_size = voxel; m.adaptivity = 0; m.use_smooth_shade = True
        if smooth:
            s = ob.modifiers.new("soften", "SMOOTH")
            s.factor = 0.5; s.iterations = smooth
        dg = bpy.context.evaluated_depsgraph_get()
        out = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
        n = len(out.vertices)
        co = np.empty(n * 3, np.float32); out.vertices.foreach_get("co", co)
        faces = [list(p.vertices) for p in out.polygons]
        bpy.data.objects.remove(ob, do_unlink=True)
        return co.reshape(n, 3).astype(float), faces

    def build(self, voxel=0.008, smooth=4, band=0.012, wband=0.07, name=None, props=None, cut=None, surface=None):
        """
        Makes a mesh object of the parts, paints and decals added since the last build, skinned to the armature, and
        forgets them, so a model may be built a piece at a time (a body, each garment). name: the object's; props:
        custom properties, which the export writes as the node's extras; cut(P): faces of the fused surface whose
        middle it gives more than a half are left out (a window in a shell of hair); surface: the name of a piece
        built before, for decals to lie on when this piece has nothing fused of its own.
        """
        nb = len(self.bones)
        bi = {b: i for i, b in enumerate(self.bones)}
        fused = [p for p in self.parts if p["fuse"]]
        if not fused:
            return self._loose(nb, bi, name, props, surface)
        P, F = self._fuse(voxel, smooth)
        n = len(P)
        D = np.empty((n, len(fused)))
        for j, p in enumerate(fused):
            tree = BVHTree.FromPolygons(p["V"].tolist(), p["F"])
            D[:, j] = [tree.find_nearest(v)[3] for v in P.tolist()]
        dmin = D.min(axis=1, keepdims=True)
        near = D.argmin(axis=1)
        wc = np.clip(1 - (D - dmin) / band, 0, 1) ** 2
        C = (wc[:, :, None] * np.array([p["color"] for p in fused])[None]).sum(1) / wc.sum(1, keepdims=True)
        ww = np.clip(1 - (D - dmin) / wband, 0, 1) ** 2
        W = np.zeros((n, nb))
        for j, p in enumerate(fused):
            W[:, bi[p["bone"]]] += ww[:, j]
        names = [p["name"] for p in fused]
        for mask, col, on in self.paints:
            m = np.clip(mask(P), 0, 1)
            if on:
                # By the share of the point that is those parts': by the nearest part alone the paint's edge is
                # ragged wherever two parts meet
                ok = np.array([any(nm.startswith(o) for o in on) for nm in names], float)
                m = m * ((wc * ok[None]).sum(1) / wc.sum(1))
            C = C * (1 - m[:, None]) + col[None] * m[:, None]
        self.surfaces[name or self.name] = (P, F, W)
        if cut is not None:
            mid = np.array([P[f].mean(axis=0) for f in F])
            keep = cut(mid) <= 0.5
            F = [f for f, k in zip(F, keep) if k]
            used = np.unique(np.concatenate([np.array(f) for f in F]))
            remap = np.full(n, -1); remap[used] = np.arange(len(used))
            P, C, W = P[used], C[used], W[used]
            F = [[int(remap[i]) for i in f] for f in F]
            n = len(P)
        return self._finish(P, F, C, W, nb, bi, name, props, self.surfaces[name or self.name])

    def _loose(self, nb, bi, name, props, surface):
        """A piece with nothing fused: its loose parts, and its decals on a piece built before."""
        return self._finish(np.zeros((0, 3)), [], np.zeros((0, 3)), np.zeros((0, nb)), nb, bi, name, props,
                     self.surfaces[surface] if surface else None)

    def _finish(self, P, F, C, W, nb, bi, name, props, under):
        n = len(P)
        Vs, Fs, Cs, Ws = [P], [F], [C], [W]
        off = n
        glowing = []            # (first face, faces, colour) of each piece that gives its own light
        # Pieces left as they are
        for p in self.parts:
            if p["fuse"]:
                continue
            k = len(p["V"])
            Vs.append(p["V"]); Fs.append([[i + off for i in f] for f in p["F"]])
            if p["glow"]:
                glowing.append((sum(len(fs) for fs in Fs[:-1]), len(p["F"]), p["glow"]))
            Cs.append(np.tile(p["color"], (k, 1)))
            w = np.zeros((k, nb)); w[:, bi[p["bone"]]] = 1; Ws.append(w)
            off += k
        # Patches over the fused surface
        uP, uF, uW = under if under is not None else (P, F, W)
        tree = BVHTree.FromPolygons(uP.tolist(), uF) if len(uP) else None
        kd = KDTree(len(uP))
        for i, v in enumerate(uP.tolist()):
            kd.insert(v, i)
        kd.balance()
        for d in self.decals:
            nrm = d["n"] / np.linalg.norm(d["n"])
            right = np.cross(d["up"], nrm); right /= np.linalg.norm(right)
            up = np.cross(nrm, right)
            uv = [(0.0, 0.0)]
            for r in range(1, d["rings"] + 1):
                for s in range(d["segs"]):
                    th = 2 * math.pi * s / d["segs"]
                    k = (d["outline"](th) if d["outline"] else 1.0) * r / d["rings"]
                    uv.append((k * math.cos(th), k * math.sin(th)))
            uv = np.array(uv)
            pts, ok = [], []
            for u, v in uv:
                o = d["c"] + nrm * 0.35 + right * u * d["half"][0] + up * v * d["half"][1]
                hit = tree.ray_cast(Vector(o), Vector(-nrm))
                if hit[0] is None:
                    pts.append(o); ok.append(False)
                else:
                    pts.append(np.array(hit[0]) + np.array(hit[1]) * d["eps"]); ok.append(True)
            pts = np.array(pts)
            segs, rings = d["segs"], d["rings"]
            faces = []
            def idx(r, s):
                return 0 if r == 0 else 1 + (r - 1) * segs + s % segs
            for s in range(segs):
                faces.append([0, idx(1, s), idx(1, s + 1)])
                for r in range(1, rings):
                    faces.append([idx(r, s), idx(r + 1, s), idx(r + 1, s + 1), idx(r, s + 1)])
            faces = [f for f in faces if all(ok[i] for i in f)]
            # Seen along -normal the faces must turn counter-clockwise
            a, b, c = pts[faces[0][0]], pts[faces[0][1]], pts[faces[0][2]]
            if np.dot(np.cross(b - a, c - a), nrm) < 0:
                faces = [f[::-1] for f in faces]
            if callable(d["color"]):
                col = d["color"](-uv[:, 0] if d["mx"] else uv[:, 0], uv[:, 1])
            else:
                col = np.tile(srgb(d["color"]) if isinstance(d["color"], str) else d["color"], (len(uv), 1))
            w = np.array([uW[kd.find(p)[1]] for p in pts.tolist()])
            Vs.append(pts); Fs.append([[i + off for i in f] for f in faces]); Cs.append(col); Ws.append(w)
            off += len(pts)

        V = np.vstack(Vs); C = np.vstack(Cs); W = np.vstack(Ws)
        F = [f for fs in Fs for f in fs]
        nm = name or self.name
        me = bpy.data.meshes.new(nm)
        me.from_pydata(V.tolist(), [], F)
        me.polygons.foreach_set("use_smooth", [True] * len(me.polygons))
        loops = np.empty(len(me.loops), np.int32); me.loops.foreach_get("vertex_index", loops)
        attr = me.color_attributes.new("Color", "BYTE_COLOR", "CORNER")
        rgba = np.concatenate([C[loops], np.ones((len(loops), 1))], axis=1).astype(np.float32)
        attr.data.foreach_set("color", rgba.ravel())
        ob = bpy.data.objects.new(nm, me)
        for key, value in (props or {}).items():
            ob[key] = value
        bpy.context.scene.collection.objects.link(ob)
        def material(name, glow=None):
            mat = bpy.data.materials.new(name)
            mat.use_nodes = True
            nt = mat.node_tree
            bsdf = nt.nodes["Principled BSDF"]
            vc = nt.nodes.new("ShaderNodeVertexColor"); vc.layer_name = "Color"
            nt.links.new(vc.outputs["Color"], bsdf.inputs["Base Color"])
            bsdf.inputs["Roughness"].default_value = 1.0
            bsdf.inputs["Metallic"].default_value = 0.0
            if glow is not None:
                # The game lights a material as a glow once its emission is over 0.4 (ImportedModels)
                bsdf.inputs["Emission Color"].default_value = (*glow, 1)
                bsdf.inputs["Emission Strength"].default_value = 1.0
            me.materials.append(mat)
        material("body")
        for i, (first, count, glow) in enumerate(glowing):
            material(f"glow{i}", srgb(glow) if isinstance(glow, str) else glow)
            for f in range(first, first + count):
                me.polygons[f].material_index = i + 1
        # Skinning: a group a bone, each vertex its four strongest
        groups = [ob.vertex_groups.new(name=b) for b in self.bones]
        order = np.argsort(-W, axis=1)[:, :4]
        top = np.take_along_axis(W, order, axis=1)
        top /= top.sum(1, keepdims=True)
        for g in range(nb):
            for k in range(4):
                sel = np.nonzero((order[:, k] == g) & (top[:, k] > 0.01))[0]
                for i in sel.tolist():
                    groups[g].add([i], float(top[i, k]), "REPLACE")
        ob.parent = self.arm
        mod = ob.modifiers.new("Armature", "ARMATURE")
        mod.object = self.arm
        self.ob = ob
        self.parts, self.paints, self.decals = [], [], []
        print("KIT:", nm, len(V), "vertices,", len(F), "faces; height", round(float(V[:, 2].max()), 3),
              "width", round(float(V[:, 0].max() - V[:, 0].min()), 3), "length",
              round(float(V[:, 1].max() - V[:, 1].min()), 3), "lowest", round(float(V[:, 2].min()), 3))
        return ob

    def export(self, out):
        bpy.ops.export_scene.gltf(
            filepath=out, export_format="GLB", export_yup=True,
            export_vertex_color="MATERIAL", export_all_vertex_colors=False,
            export_skins=True, export_animations=True, export_animation_mode="ACTIONS", export_force_sampling=True,
            export_draco_mesh_compression_enable=False, export_morph=False,
            export_cameras=False, export_lights=False, export_extras=True)
        print("KIT: wrote", out)


def boxy(n):
    """A deform that squares a sphere up: 2 leaves it round, more makes it a rounded box."""
    def f(V):
        return V / (np.abs(V) ** n).sum(1, keepdims=True) ** (1 / n)
    return f


def tri(t):
    """A triangle wave of period 1 between -1 and 1."""
    return 2 * np.abs(2 * (t - np.floor(t + 0.5))) - 1


def unit(x, y, z):
    d = np.array([x, y, z], float)
    return d / np.linalg.norm(d)


def args():
    a = sys.argv[sys.argv.index("--") + 1:]
    return a[0], a[1]
