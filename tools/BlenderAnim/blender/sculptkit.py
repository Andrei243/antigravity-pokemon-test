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
    def __init__(self, src=None, name=None, skeleton=None, sides=("R", "L"), coat=None):
        """
        src: the game's export of the species, whose armature and clips are kept. With none, skeleton makes the
        armature: (bone, parent or None, the joint's place) each, and name names the mesh; it has no clips.
        sides: what "{s}" is on +X and on -X (a Pokémon's left is -X; a person's "L" bones are on +X).
        """
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.context.scene.render.fps = 60          # every clip of the game's a whole number of frames
        self.sides = sides
        self.coat = coat
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
        # The old mesh's materials too, or a new one of the same name is given a number ("body.001")
        for m in list(bpy.data.materials):
            bpy.data.materials.remove(m)
        self.bones = [b.name for b in self.arm.data.bones]

    # --- authoring -------------------------------------------------------------------------------------------
    def add(self, name, shape, color, bone, fuse=True, mirror=False, glow=None, surface=None, carve=False):
        """
        A part. mirror: authored on +X and copied to -X; "{s}" in the bone is the kit's side name for each.
        carve: the part is cut out of the fused body (an open mouth, a socket) and its colour lines the hollow.
        """
        V, F = shape
        col = srgb(color) if isinstance(color, str) else color
        if not mirror:
            self.parts.append(dict(name=name, V=V, F=F, color=col, bone=bone, fuse=fuse, glow=glow, surface=surface, carve=carve))
            return
        self.parts.append(dict(name=name + "R", V=V, F=F, color=col, bone=bone.format(s=self.sides[0]), fuse=fuse, glow=glow, surface=surface, carve=carve))
        self.parts.append(dict(name=name + "L", V=V * np.array([-1, 1, 1]), F=[f[::-1] for f in F], color=col,
                               bone=bone.format(s=self.sides[1]), fuse=fuse, glow=glow, surface=surface, carve=carve))

    def paint(self, mask, color, on=None):
        """Paint over the fused body: mask(P) gives 0..1 for points P (N x 3); on: only parts whose names start so."""
        self.paints.append((mask, srgb(color) if isinstance(color, str) else color, on))

    def decal(self, center, normal, half, color, up=(0, 0, 1), eps=0.004, outline=None, rings=14, segs=44, surface=None,
              mirror=False, flip=False):
        """
        A patch laid over the fused surface, seen along -normal: an ellipse of half sizes (a, b) about center, or
        outline(theta) times it. color(u, v) gives each point's linear colour for u, v in -1..1 (or one colour).
        mirror: also at -X, with u mirrored so that a picture keeps its inner and outer sides.
        """
        self.decals.append(dict(c=np.array(center, float), n=np.array(normal, float), half=half, color=color,
                                up=np.array(up, float), eps=eps, outline=outline, rings=rings, segs=segs, mx=False,
                                surface=surface))
        if mirror:
            m = np.array([-1, 1, 1.0])
            self.decals.append(dict(c=np.array(center, float) * m, n=np.array(normal, float) * m, half=half,
                                    color=color, up=np.array(up, float) * m, eps=eps, outline=outline, rings=rings,
                                    segs=segs, mx=True, surface=surface))

    # --- building --------------------------------------------------------------------------------------------
    def _fuse(self, voxel, smooth):
        V, F, off = [], [], 0
        for p in self.parts:
            if p["fuse"] and not p["carve"]:
                V.extend(p["V"].tolist()); F.extend([[i + off for i in f] for f in p["F"]]); off += len(p["V"])
        me = bpy.data.meshes.new("raw")
        me.from_pydata(V, [], F)
        ob = bpy.data.objects.new("raw", me)
        bpy.context.scene.collection.objects.link(ob)
        m = ob.modifiers.new("fuse", "REMESH")
        m.mode = "VOXEL"; m.voxel_size = voxel; m.adaptivity = 0; m.use_smooth_shade = True
        cutters = []
        for p in self.parts:
            if p["carve"]:
                cm = bpy.data.meshes.new("cutter"); cm.from_pydata(p["V"].tolist(), [], p["F"])
                co_ = bpy.data.objects.new("cutter", cm); bpy.context.scene.collection.objects.link(co_)
                b = ob.modifiers.new("carve", "BOOLEAN"); b.operation = "DIFFERENCE"; b.solver = "EXACT"; b.object = co_
                cutters.append(co_)
        if smooth:
            s = ob.modifiers.new("soften", "SMOOTH")
            s.factor = 0.5; s.iterations = smooth
        dg = bpy.context.evaluated_depsgraph_get()
        out = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
        n = len(out.vertices)
        co = np.empty(n * 3, np.float32); out.vertices.foreach_get("co", co)
        faces = [list(p.vertices) for p in out.polygons]
        bpy.data.objects.remove(ob, do_unlink=True)
        for c_ in cutters:
            bpy.data.objects.remove(c_, do_unlink=True)
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
        fused = [p for p in self.parts if p["fuse"] and not p["carve"]]
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
        # A hollow takes the colour of the part that carved it, whatever was painted there
        for p in self.parts:
            if p["carve"]:
                tree = BVHTree.FromPolygons(p["V"].tolist(), p["F"])
                dc = np.array([tree.find_nearest(v)[3] for v in P.tolist()])
                m = np.clip(1 - (dc - voxel * 0.6) / (voxel * 0.8), 0, 1)
                C = C * (1 - m[:, None]) + p["color"][None] * m[:, None]
        self.surfaces[name or self.name] = (P, F, W)
        fs = [fused[near[f[0]]]["surface"] for f in F]          # each face the surface of the part it is nearest
        if cut is not None:
            mid = np.array([P[f].mean(axis=0) for f in F])
            keep = cut(mid) <= 0.5
            F, fs = [f for f, k in zip(F, keep) if k], [s for s, k in zip(fs, keep) if k]
            used = np.unique(np.concatenate([np.array(f) for f in F]))
            remap = np.full(n, -1); remap[used] = np.arange(len(used))
            P, C, W = P[used], C[used], W[used]
            F = [[int(remap[i]) for i in f] for f in F]
            n = len(P)
        return self._finish(P, F, C, W, nb, bi, name, props, self.surfaces[name or self.name], fs)

    def _loose(self, nb, bi, name, props, surface):
        """A piece with nothing fused: its loose parts, and its decals on a piece built before."""
        return self._finish(np.zeros((0, 3)), [], np.zeros((0, 3)), np.zeros((0, nb)), nb, bi, name, props,
                            self.surfaces[surface] if surface else None, [])

    def _finish(self, P, F, C, W, nb, bi, name, props, under, fs):
        n = len(P)
        Vs, Fs, Cs, Ws, Ms = [P], [F], [C], [W], [fs]
        off = n
        glowing = []            # (first face, faces, colour) of each piece that gives its own light
        # Pieces left as they are
        for p in self.parts:
            if p["fuse"] or p["carve"]:
                continue
            k = len(p["V"])
            Vs.append(p["V"]); Fs.append([[i + off for i in f] for f in p["F"]]); Ms.append([p["surface"]] * len(p["F"]))
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
            if not faces:
                print("KIT: !! a decal at", d["c"].round(3).tolist(), "missed the surface and was left out")
                continue
            # Seen along -normal the faces must turn counter-clockwise
            a, b, c = pts[faces[0][0]], pts[faces[0][1]], pts[faces[0][2]]
            if np.dot(np.cross(b - a, c - a), nrm) < 0:
                faces = [f[::-1] for f in faces]
            if callable(d["color"]):
                col = d["color"](-uv[:, 0] if d["mx"] else uv[:, 0], uv[:, 1])
            else:
                col = np.tile(srgb(d["color"]) if isinstance(d["color"], str) else d["color"], (len(uv), 1))
            # Only the points a face uses: one that missed the surface is left where its ray began, far off the model
            used = sorted({i for f in faces for i in f})
            remap = {v: j for j, v in enumerate(used)}
            pts, col = pts[used], col[used]
            faces = [[remap[i] for i in f] for f in faces]
            w = np.array([uW[kd.find(p)[1]] for p in pts.tolist()])
            Vs.append(pts); Fs.append([[i + off for i in f] for f in faces]); Cs.append(col); Ws.append(w)
            Ms.append([d["surface"]] * len(faces))
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
            if glow is None and name in bpy.data.materials:        # one material a surface, whatever piece wears it
                me.materials.append(bpy.data.materials[name])
                return
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
        # A material a surface, named as the game names it (fur, scales, shell, leaf...: ImportedModels.SurfaceNamed),
        # the kit's coat for a part that names none
        plain = self.coat or "body"
        faces = [s or plain for ms in Ms for s in ms]
        order = [plain] + sorted(set(faces) - {plain})
        for s in order:
            material(s)
        if len(order) > 1:
            me.polygons.foreach_set("material_index", [order.index(s) for s in faces])
        # One material a colour of light, however many pieces give it
        lights = []
        for first, count, glow in glowing:
            g = tuple(np.round(srgb(glow) if isinstance(glow, str) else glow, 5).tolist())
            if g not in lights:
                lights.append(g)
                material(f"glow{len(lights) - 1}", g)
            for f in range(first, first + count):
                me.polygons[f].material_index = len(order) + lights.index(g)
        # Skinning: a group a bone, each vertex its four strongest
        groups = [ob.vertex_groups.new(name=b) for b in self.bones]
        order = np.argsort(-W, axis=1)[:, :4]
        top = np.take_along_axis(W, order, axis=1)
        top /= top.sum(1, keepdims=True)
        for g in range(nb):
            for k in range(order.shape[1]):          # fewer than four bones in a one-body skeleton
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

    def eye_states(self, lay, surface=None):
        """
        The eyes in the game's five states, each a piece of its own ("eyes.Open", "eyes.Shut", "eyes.Squeeze",
        "eyes.Fierce", "eyes.Smile") on the body built before: the game draws the one the pose asks for. lay(state)
        adds that state's decals (eye_look gives a colour function for each from the open eye's).
        """
        for state in EYE_STATES:
            lay(state)
            self.build(name=f"eyes.{state}", surface=surface or self.name)

    def export(self, out):
        bpy.ops.export_scene.gltf(
            filepath=out, export_format="GLB", export_yup=True,
            export_vertex_color="MATERIAL", export_all_vertex_colors=False,
            export_skins=True, export_animations=True, export_animation_mode="ACTIONS", export_force_sampling=True,
            export_draco_mesh_compression_enable=False, export_morph=False,
            export_cameras=False, export_lights=False, export_extras=True)
        print("KIT: wrote", out)


# --- shapes the first batches needed (each written first inside a species' script) ---------------------------------

def blade(pts, radii, axis, f):
    """
    A tube squashed along an axis about its own line by f (0 flat, 1 round): a horn, a prong, a fin. Squashing a part
    about its whole middle instead pushes its far end off its line.
    """
    V, F = tube(pts, radii)
    a = np.array(axis, float); a /= np.linalg.norm(a)
    p = np.array(pts, float)
    line = np.vstack([p[i] + (p[i + 1] - p[i]) * t for i in range(len(p) - 1) for t in np.linspace(0, 1, 40)])
    near = line[np.argmin(((V[:, None, :] - line[None]) ** 2).sum(2), axis=1)]
    d = (V - near) @ a
    return V - np.outer(d * (1 - f), a), F


def sheet(at, nu, nv, thick):
    """A thick sheet (a collar, a cape): at(u, v) gives the middle surface's points for u, v in 0..1 (arrays nu x nv)."""
    u, v = np.meshgrid(np.linspace(0, 1, nu), np.linspace(0, 1, nv), indexing="ij")
    P = at(u, v)
    du = np.gradient(P, axis=0); dv = np.gradient(P, axis=1)
    N = np.cross(du, dv); N /= np.linalg.norm(N, axis=2, keepdims=True)
    A, B = (P + N * thick / 2).reshape(-1, 3), (P - N * thick / 2).reshape(-1, 3)
    V = np.vstack([A, B]); o = len(A)
    def i(a, b):
        return a * nv + b
    F = []
    for a in range(nu - 1):
        for b in range(nv - 1):
            F.append([i(a, b), i(a + 1, b), i(a + 1, b + 1), i(a, b + 1)])
            F.append([o + i(a, b), o + i(a, b + 1), o + i(a + 1, b + 1), o + i(a + 1, b)])
    for a in range(nu - 1):
        for b in (0, nv - 1):
            q = [i(a, b), i(a + 1, b), o + i(a + 1, b), o + i(a, b)]
            F.append(q if b else q[::-1])
    for b in range(nv - 1):
        for a in (0, nu - 1):
            q = [i(a, b), i(a, b + 1), o + i(a, b + 1), o + i(a, b)]
            F.append(q[::-1] if a else q)
    return V, F


def leaf(base, d, length, width, thick=0.014):
    """A flat pointed piece from base along d (a leaf, a feather, a plate), its face turned to the sky's side of d."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    side = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(side) < 1e-3:
        side = np.array([1.0, 0, 0])
    side /= np.linalg.norm(side)
    face = np.cross(side, d)
    def shape(V):
        V = V.copy()
        t = (V[:, 2] + 1) / 2
        w = np.sqrt(np.clip(t, 0, 1)) * (1 - t ** 1.6) * 1.6
        V[:, 0] *= w; V[:, 1] *= w
        return V
    V, F = ell((0, 0, 0), (width, thick, length / 2), deform=shape, sub=2)
    return V @ np.stack([side, face, d], axis=1).T + np.array(base, float) + d * length / 2, F


def lumpy(n, m, amp):
    """A deform that raises n by m bumps on a sphere: a tree's crown, a bush, a cloud of fur."""
    def f(V):
        th = np.arctan2(V[:, 1], V[:, 0]); ph = np.arccos(np.clip(V[:, 2], -1, 1))
        return V * (1 + amp * np.abs(np.sin(n * th) * np.sin(m * ph)))[:, None]
    return f


def polygon(pts):
    """A decal's outline from a polygon in u, v, star-shaped about the middle: a patch with corners and points."""
    pts = np.array(pts, float)
    def r(th):
        d = np.array([math.cos(th), math.sin(th)])
        best = 9.0
        for a, b in zip(pts, np.roll(pts, -1, axis=0)):
            e = b - a
            m = np.array([[d[0], -e[0]], [d[1], -e[1]]])
            if abs(np.linalg.det(m)) < 1e-9:
                continue
            t, s = np.linalg.solve(m, a)
            if t > 0 and -1e-6 <= s <= 1 + 1e-6:
                best = min(best, t)
        return best
    return r


EYE_STATES = ("Open", "Shut", "Squeeze", "Fierce", "Smile")


def eye_look(state, open_look, lid, ink, line=0.14):
    """
    An eye's colour function (u outward, v up, each -1..1) in one of the game's states, from the open eye's: shut is
    the lid with a line curving down, a smile the lid with a line curving up, a squeeze two lines meeting at the
    inner corner, fierce the open eye with the lid slanting down over it toward the inner corner. lid and ink are
    linear colours.
    """
    def look(u, v):
        if state == "Open":
            return open_look(u, v)
        if state == "Fierce":
            c = open_look(u, v)
            edge = 0.2 + 0.5 * u
            c[v > edge] = lid
            c[np.abs(v - edge) < line] = ink
            return c
        c = np.tile(lid, (len(u), 1))
        wide = np.abs(u) < 0.82
        if state == "Shut":
            c[wide & (np.abs(v - (-0.05 - 0.3 * (1 - u * u))) < line)] = ink
        elif state == "Smile":
            c[wide & (np.abs(v - (-0.25 + 0.42 * (1 - u * u))) < line)] = ink
        else:
            c[wide & (np.abs(np.abs(v) - 0.42 * (u + 0.82) / 1.64) < line)] = ink
        return c
    return look


def facets(n, amount, seed, keep_front=None, depth=(0.9, 1.02)):
    """
    A deform that cuts a sphere into a rough stone: n flat faces (planes at random turns), mixed with the round by
    amount. keep_front leaves the faces whose normals point at the front (-Y) out, so a face there stays smooth.
    The remesh rounds weak facets off: depth of 0.7 to 0.93 and sub=4 keep them.
    """
    rs = np.random.RandomState(seed)
    N = rs.normal(size=(n, 3)); N /= np.linalg.norm(N, axis=1, keepdims=True)
    if keep_front is not None:
        N = N[N[:, 1] > -keep_front]
    d = depth[0] + (depth[1] - depth[0]) * rs.rand(len(N))
    def f(V):
        proj = (V @ N.T) / d[None]
        r = np.minimum(1 / np.maximum(proj.max(1), 0.6), 1.08)
        return V * (1 - amount + amount * r)[:, None]
    return f


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
