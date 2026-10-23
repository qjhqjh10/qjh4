"""audit_effect_index_r29.py — R29 分因（**只读**，不改任何数据）

任务出处：`资料/普查产出_1022/A1_自造与兜底_特效动画.md:58`（R29）
  · 4 件非「对得上」逐条查
  · 254 件「两边全程空」按根因分档

## 判据从哪来（全部是数出来的，不是猜的）

| 事实 | 从哪读 |
|---|---|
| 判定 / 亮度比 / 有内容时段 / 技术构成 | `资料/特效还原台账.tsv`（sweep 产物，2026-10-21 22:00） |
| 原版 prefab 的 `ParticleSystem` **个数** | `d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/`：按 GameObject→Transform 树走子树，数落在 `ParticleSystem/` 目录里的组件 pid |
| **我们**导出的 `ParticleSystem` 个数 | `MyGame/Assets/WarpforgeVFX/Prefabs/<名>.prefab` 里 `--- !u!198`（Unity 类 ID 198 = ParticleSystem）的条数 |
| 模块 / 控制器 | `数据/游戏数据/effect_index.json` |

⚠️ **原版侧为什么不用 `grep` 数 pid**：pid 是**分包局部**的，同一个数字在别的类型里也可能存在；
   所以先按「文件名（= pathID）落进 `ParticleSystem/` 目录」建集合，再拿 GameObject 的
   `m_Component` 去交集 —— 这样不会把 MeshRenderer 的 pid 误算成粒子系统。

## 分档口径（三档 + 一个兜底档）

- `scripted` 纯脚本驱动：**原版 0 个粒子系统** ⇒ 本来就没有看得见的东西（屏震 / 后期 / 换材质 / 卡背…）
- `lost` 导出丢了：**原版 > 0、我们 0** ⇒ 真丢件
- `no-hit` sweep 覆盖不到：**两边都有粒子**，但 sweep 全程空 ⇒ 需要触发条件 / 出画 / 太暗低于阈值
- `invented` 我们多出来：原版 0、我们有 > 0 ⇒ 自造（正常应为 0 件；非 0 就要单独看）

## 用法
    "D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 工具/audit_effect_index_r29.py
    ... --out-dir _tmp_view/E4_r29     # 产物落点（默认就在这儿）
"""
import io
import json
import os
import re
import sys

ROOT = r"d:/4/Unity"
BUNDLE = r"d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all"
LEDGER = os.path.join(ROOT, "资料/特效还原台账.tsv")
INDEX = os.path.join(ROOT, "数据/游戏数据/effect_index.json")
PREFABS = os.path.join(ROOT, "MyGame/Assets/WarpforgeVFX/Prefabs")
OUTDIR = os.path.join(ROOT, "_tmp_view/E4_r29")


def pid_of(fn):
    m = re.search(r"_(-?\d+)\.json$", fn)
    return int(m.group(1)) if m else None


def read_json(p):
    with io.open(p, "r", encoding="utf-8") as f:
        return json.load(f)


def head(p, n=4096):
    with io.open(p, "rb") as f:
        return f.read(n).decode("utf-8", "replace")


COMP_RE = re.compile(r'"component"\s*:\s*\{\s*"m_FileID"\s*:\s*(-?\d+)\s*,\s*'
                     r'"m_PathID"\s*:\s*(-?\d+)\s*\}')


class Bundle(object):
    """原版 bundle 的对象图（只建一次）。"""

    def __init__(self, d):
        self.d = d
        self.ps_pids = set()        # 是 ParticleSystem 的组件 pathID
        self.tr_pids = set()        # 是 Transform 的组件 pathID
        for sub, s in (("ParticleSystem", self.ps_pids), ("Transform", self.tr_pids)):
            p = os.path.join(d, sub)
            if os.path.isdir(p):
                for fn in os.listdir(p):
                    x = pid_of(fn)
                    if x is not None:
                        s.add(x)
        self.go = {}                # go pathID → 名字
        self.go_comps = {}          # go pathID → [组件 pid]
        self.go_tr = {}             # go pathID → Transform pid
        self.tr = {}                # Transform pid → (father, [children])
        self.tr_go = {}             # Transform pid → go pathID
        self._build()

    def _build(self):
        gdir = os.path.join(self.d, "GameObject")
        pending = []
        for fn in os.listdir(gdir):
            if not fn.endswith(".json"):
                continue
            p = os.path.join(gdir, fn)
            t = head(p)
            if '"m_Name"' not in t:
                with io.open(p, "r", encoding="utf-8") as f:
                    t = f.read()
            nm = re.search(r'"m_Name"\s*:\s*"((?:[^"\\]|\\.)*)"', t)
            name = fn[:-5]
            if nm:
                try:
                    name = json.loads('"%s"' % nm.group(1))
                except Exception:
                    name = nm.group(1)
            comps = [int(m.group(2)) for m in COMP_RE.finditer(t) if int(m.group(1)) == 0]
            pending.append((name, comps))

        # 组件 → GameObject 名字（GameObject 文件里没有自己的 pathID ⇒ 只能反查）
        for name, comps in pending:
            for c in comps:
                if c in self.tr_pids:
                    # 用 Transform 文件把它挂到具体 GO 上（见下）
                    continue
            self._pending = getattr(self, "_pending", [])
            self._pending.append((name, comps))

        tdir = os.path.join(self.d, "Transform")
        for fn in os.listdir(tdir):
            if not fn.endswith(".json"):
                continue
            pid = pid_of(fn)
            if pid is None:
                continue
            o = read_json(os.path.join(tdir, fn))
            go = (o.get("m_GameObject") or {}).get("m_PathID")
            fa = (o.get("m_Father") or {}).get("m_PathID") or 0
            kids = [(c or {}).get("m_PathID") for c in (o.get("m_Children") or [])]
            self.tr[pid] = (fa, [k for k in kids if k is not None])
            if go is not None:
                self.tr_go[pid] = go

        for name, comps in self._pending:
            go = None
            for c in comps:
                if c in self.tr_go:
                    go = self.tr_go[c]
                    break
            if go is None:
                continue
            self.go[go] = name
            self.go_comps[go] = comps
            for c in comps:
                if c in self.tr_pids:
                    self.go_tr[go] = c
                    break
        self._pending = []

    def ps_count(self, go):
        """该 GameObject 自己的 ParticleSystem 数。"""
        return sum(1 for c in self.go_comps.get(go, []) if c in self.ps_pids)

    def roots_named(self, name):
        """叫这个名字、且 Transform 没有父级的 GameObject。"""
        out = []
        for go, nm in self.go.items():
            if nm != name:
                continue
            tr = self.go_tr.get(go)
            if tr is None:
                continue
            father = self.tr.get(tr, (0, []))[0]
            if not father:
                out.append(go)
        return out

    def subtree_stats(self, root_go):
        """(粒子系统总数, GameObject 总数, 有粒子渲染器的节点数)"""
        tr = self.go_tr.get(root_go)
        if tr is None:
            return (0, 0, 0)
        ps = 0
        gos = 0
        stack = [tr]
        seen = set()
        while stack:
            cur = stack.pop()
            if cur in seen:
                continue
            seen.add(cur)
            g = self.tr_go.get(cur)
            if g is not None:
                gos += 1
                ps += self.ps_count(g)
            for k in self.tr.get(cur, (0, []))[1]:
                stack.append(k)
        return (ps, gos, 0)


def our_ps_counts():
    """我们导出的 prefab：名字 → ParticleSystem 条数（`--- !u!198`）。"""
    out = {}
    if not os.path.isdir(PREFABS):
        return out
    for fn in os.listdir(PREFABS):
        if not fn.endswith(".prefab"):
            continue
        n = 0
        with io.open(os.path.join(PREFABS, fn), "r", encoding="utf-8", errors="replace") as f:
            for line in f:
                if line.startswith("--- !u!198"):
                    n += 1
        out[fn[:-7]] = n
    return out


def load_ledger():
    rows = [l.rstrip("\n").split("\t") for l in io.open(LEDGER, encoding="utf-8-sig")]
    hdr = rows[0]
    out = {}
    for r in rows[1:]:
        if not r or not r[0]:
            continue
        d = dict(zip(hdr, r))
        out[d["效果名"]] = d
    return hdr, out


def ps_list_orig(b, root_go):
    """原版子树里每个 ParticleSystem 的关键参数（发射率 / 循环 / playOnAwake / maxNumParticles）。"""
    tr = b.go_tr.get(root_go)
    out = []
    if tr is None:
        return out
    pdir = os.path.join(BUNDLE, "ParticleSystem")
    stack = [tr]
    seen = set()
    while stack:
        cur = stack.pop()
        if cur in seen:
            continue
        seen.add(cur)
        g = b.tr_go.get(cur)
        if g is not None:
            for c in b.go_comps.get(g, []):
                if c not in b.ps_pids:
                    continue
                p = os.path.join(pdir, "ParticleSystem_%d.json" % c)
                if not os.path.isfile(p):
                    continue
                d = read_json(p)
                em = d.get("EmissionModule") or {}
                ro = (em.get("rateOverTime") or {})
                im = d.get("InitialModule") or {}
                out.append({
                    "pid": c,
                    "emEnabled": bool(em.get("enabled")),
                    "rate": float(ro.get("scalar") or 0.0),
                    "rateMin": float(ro.get("minScalar") or 0.0),
                    "looping": bool(d.get("looping")),
                    "playOnAwake": bool(d.get("playOnAwake")),
                    "maxParticles": float(im.get("maxNumParticles") or 0.0),
                    "length": float(d.get("lengthInSec") or 0.0),
                })
        for k in b.tr.get(cur, (0, []))[1]:
            stack.append(k)
    return out


def ps_list_ours(name):
    """我们导出 prefab 里的 ParticleSystem 关键参数（同一组字段，从 YAML 抠）。"""
    p = os.path.join(PREFABS, name + ".prefab")
    if not os.path.isfile(p):
        return []
    with io.open(p, "r", encoding="utf-8", errors="replace") as f:
        txt = f.read()
    parts = re.split(r"^--- !u!(\d+) &(-?\d+).*$", txt, flags=re.M)
    out = []
    for i in range(1, len(parts), 3):
        cls, fid, body = parts[i], parts[i + 1], parts[i + 2]
        if cls != "198":
            continue

        def num(field, default=0.0, where=body):
            m = re.search(r"^\s+%s:\s*([\d.eE+-]+)\s*$" % re.escape(field), where, re.M)
            return float(m.group(1)) if m else default

        def flag(field):
            m = re.search(r"^\s+%s:\s*(\d)" % re.escape(field), body, re.M)
            return bool(int(m.group(1))) if m else False

        em = re.search(r"^  EmissionModule:\n(.*?)(?=^  [A-Z]\w*:)", body, re.M | re.S)
        emseg = em.group(1) if em else ""
        ro = re.search(r"rateOverTime:\s*\n(.*?)(?=^\s{4}\w+:|\Z)", emseg, re.M | re.S)
        roseg = ro.group(1) if ro else ""
        out.append({
            "pid": int(fid),
            "emEnabled": bool(re.search(r"^\s+enabled:\s*1", emseg, re.M)),
            "rate": num("scalar", 0.0, roseg),
            "rateMin": num("minScalar", 0.0, roseg),
            "looping": flag("looping"),
            "playOnAwake": flag("playOnAwake"),
            "maxParticles": num("maxNumParticles"),
            "length": num("lengthInSec"),
        })
    return out


def summarize(ps):
    return {
        "n": len(ps),
        "emEnabled": sum(1 for x in ps if x["emEnabled"]),
        "rateSum": round(sum(x["rate"] if x["emEnabled"] else 0.0 for x in ps), 3),
        "rateNonZero": sum(1 for x in ps if x["emEnabled"] and x["rate"] > 0),
        "looping": sum(1 for x in ps if x["looping"]),
        "notPlayOnAwake": sum(1 for x in ps if not x["playOnAwake"]),
        "maxParticlesSum": round(sum(x["maxParticles"] for x in ps), 1),
    }


def classify(orig_ps, our_ps, prefab_exists):
    if not prefab_exists:
        return "lost", "我们根本没有这个 prefab"
    if orig_ps == 0 and our_ps == 0:
        return "scripted", "原版 0 个粒子系统（纯脚本/后期/屏震/材质类）"
    if orig_ps > 0 and our_ps == 0:
        return "lost", "原版 %d 个粒子系统，我们 0 个" % orig_ps
    if orig_ps == 0 and our_ps > 0:
        return "invented", "原版 0 个、我们有 %d 个" % our_ps
    return "no-hit", "两边都有粒子（原版 %d / 我们 %d），sweep 仍然全程空" % (orig_ps, our_ps)


def main(argv):
    outdir = OUTDIR
    compare = []
    for i, a in enumerate(argv):
        if a == "--out-dir" and i + 1 < len(argv):
            outdir = argv[i + 1]
        if a == "--compare" and i + 1 < len(argv):
            compare = [x for x in argv[i + 1].split(",") if x]

    print("[1] 读台账 / effect_index / 我们的 prefab …")
    hdr, led = load_ledger()
    idx = read_json(INDEX)
    ours = our_ps_counts()
    print("    台账 %d 行 · effect_index %d 件 · 我们 prefab %d 个"
          % (len(led), len(idx["effects"]), len(ours)))

    print("[2] 建原版 bundle 的对象图（GameObject/Transform/ParticleSystem）…")
    b = Bundle(BUNDLE)
    print("    GameObject %d · Transform %d · ParticleSystem %d 个 pid"
          % (len(b.go), len(b.tr), len(b.ps_pids)))

    print("[3] 逐个效果数原版子树里的粒子系统 …")
    rows = []
    for name in sorted(led):
        led_row = led[name]
        verdict_grp = led_row.get("分组", "")
        if verdict_grp not in ("W", "C", "D", "E"):
            continue
        roots = b.roots_named(name)
        if len(roots) == 1:
            orig_ps, orig_gos, _ = b.subtree_stats(roots[0])
            root_note = ""
        elif len(roots) == 0:
            orig_ps, orig_gos, root_note = -1, -1, "原版 bundle 里找不到同名根节点"
        else:
            orig_ps, orig_gos, root_note = -1, -1, "原版有 %d 个同名根节点（歧义）" % len(roots)
        op = ours.get(name)
        rows.append({
            "name": name,
            "group": verdict_grp,
            "verdict": led_row.get("判定", ""),
            "confidence": led_row.get("置信度", ""),
            "ratio": led_row.get("亮度比中位", ""),
            "peak": led_row.get("峰值亮点", ""),
            "orig_span": led_row.get("原版有内容时段", ""),
            "our_span": led_row.get("导出有内容时段", ""),
            "tech": led_row.get("技术构成", ""),
            "orig_ps": orig_ps,
            "orig_gos": orig_gos,
            "our_ps": op if op is not None else -1,
            "prefab": op is not None,
            "root_note": root_note,
        })

    # 分档
    for r in rows:
        if r["group"] != "W":
            r["bucket"], r["why"] = "", ""
            continue
        r["bucket"], r["why"] = classify(r["orig_ps"], r["our_ps"], r["prefab"])

    os.makedirs(outdir, exist_ok=True)
    with io.open(os.path.join(outdir, "r29_rows.json"), "w", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(rows, ensure_ascii=False, indent=1))

    cols = ["name", "group", "bucket", "orig_ps", "our_ps", "verdict", "confidence", "ratio",
            "peak", "orig_span", "our_span", "tech", "root_note", "why"]
    with io.open(os.path.join(outdir, "r29_rows.tsv"), "w", encoding="utf-8", newline="\n") as f:
        f.write("\t".join(cols) + "\n")
        for r in rows:
            f.write("\t".join(str(r.get(c, "")) for c in cols) + "\n")

    # 汇总
    import collections
    print("\n[4] 254 件「两边全程空」分档：")
    cnt = collections.Counter(r["bucket"] for r in rows if r["group"] == "W")
    for k, v in sorted(cnt.items(), key=lambda kv: -kv[1]):
        print("      %-9s %4d" % (k, v))
    print("      %-9s %4d" % ("合计", sum(cnt.values())))

    print("\n[5] 4 件非「对得上」：")
    for r in rows:
        if r["group"] in ("C", "D", "E"):
            print("      [%s] %-50s 原版粒子 %3s · 我们 %3s · 判定 %s %s"
                  % (r["group"], r["name"], r["orig_ps"], r["our_ps"], r["verdict"], r["ratio"]))

    miss = [r["name"] for r in rows if r["group"] == "W" and r["root_note"]]
    if miss:
        print("\n[6] 原版侧解不出的：%d 件" % len(miss))
        for n in miss[:20]:
            print("      " + n)

    if compare:
        print("\n[7] 逐件对比原版 / 我们 的粒子参数（发射率 / 循环 / playOnAwake / 上限）：")
        for name in compare:
            roots = b.roots_named(name)
            if len(roots) != 1:
                print("      %-52s 原版根节点解不出（%d 个）" % (name, len(roots)))
                continue
            o = ps_list_orig(b, roots[0])
            u = ps_list_ours(name)
            so, su = summarize(o), summarize(u)
            print("      %-52s" % name)
            print("          原版: %s" % so)
            print("          我们: %s" % su)

    print("\n[8] 产物：%s" % os.path.join(outdir, "r29_rows.tsv"))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
