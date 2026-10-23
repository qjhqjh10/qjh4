"""gen_module_events.py — 把原版 `AnimFXModuleEvent.eventsToFire[i].eventToFire` 的
UnityEvent `m_Calls` **逐条**解出来，落成运行时能用的数据表。

产出：`数据/游戏数据/module_events.json`

## 为什么要有这个脚本

`AnimFXModuleEvent` 的全部行为就是「到某个时机 Invoke 一个 UnityEvent」。我们已有的
`数据/游戏数据/animfx_components.json` 里，那个 `eventToFire` 被写成了
`{"m_PersistentCalls": {"m_Calls": "<深>"}}` —— **不是原版没有，是 dump 的深度护栏截断了**
（`工具/dump_animfx.py` 的 `_depth_limit()`：默认 6，只对 `collisionEvent` / `references`
两条路径单独放宽过，`eventToFire` 没在名单里 ⇒ 整块写成 `<深>`，生成器再把它跳过）。
判据 → `资料/AnimFX_18类方法体_块2.md` §4 / §10「还没查到的三处」①。

⇒ 本脚本**绕开那份被截断的 dump**，直接读 `assets_full` 里的 prefab 原始 JSON。

## 数据从哪来（全部是原始序列化字段，零推断）

- 实例：`D:/2/新解包资源/assets_full/bundle_*/MonoBehaviour/*.json` 里
  `m_Script` → `bundle_Waprforge_monoscripts/MonoScript/MonoScript_<pid>.json` 的
  `m_ClassName == "AnimFXModuleEvent"` 的那些（**全库扫**，不只看一个包）。
- 字段：`eventsToFire[i].eventToFire.m_PersistentCalls.m_Calls[j]` 的
  `m_Target` / `m_TargetAssemblyTypeName` / `m_MethodName` / `m_Mode` /
  `m_Arguments.*` / `m_CallState`。
- 目标路径：`m_Target`(PPtr) → 组件的 `m_GameObject` → 该 GameObject 的 Transform 链，
  相对**事件模块自己所在 GameObject**算出一条名字路径。

## 幂等

不做时间戳、按固定键序输出 ⇒ 同一份资源跑两遍 **md5 相同**。

## 用法

    python -I gen_module_events.py                # 生成 json
    python -I gen_module_events.py --check        # 只比对 C# 里的生成区与 json 是否一致
    python -I gen_module_events.py --dump         # 打印解析结果，不写文件

`--check` 解析 `MyGame/Assets/WarpforgeVFX/Runtime/WFModuleEvent.cs` 里
`// >>> gen_module_events.py` 与 `// <<< gen_module_events.py` 之间的 `new TableCall(...)` 行，
逐字段与 json 比。**不一致就以退出码 1 退出**（防止数据与运行时表悄悄分叉）。
"""
import io
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)                       # d:/4/Unity
ASSETS = "D:/2/新解包资源/assets_full"
MONOSCRIPTS = os.path.join(ASSETS, "bundle_Waprforge_monoscripts", "MonoScript")
OUT = os.path.join(ROOT, "数据", "游戏数据", "module_events.json")
CS = os.path.join(ROOT, "MyGame", "Assets", "WarpforgeVFX", "Runtime", "WFModuleEvent.cs")

KIND = "AnimFXModuleEvent"

# `PersistentListenerMode`（Unity 引擎枚举，序列化进 `m_Mode`）
MODE_NAMES = {0: "EventDefined", 1: "Void", 2: "Object", 3: "Int", 4: "Float", 5: "String", 6: "Bool"}
# `UnityEventCallState`：0=Off / 1=EditorAndRuntime / 2=RuntimeOnly（**1 和 2 都会在运行期跑**）
CALLSTATE_NAMES = {0: "Off", 1: "EditorAndRuntime", 2: "RuntimeOnly"}

# 原版类名 → 我们这边的替身（**人工核过**，不是猜：两边的公开方法签名逐一比过）
#   · `AnimFXModuleChangeMaterial.ToggleMaterial(bool)` ↔ `WarpforgeVFX.WFModuleChangeMaterial.ToggleMaterial(bool)`
#   · `StoryProgramming.VATGPUPlayer.UpdateMaterials(bool)` ↔ `WarpforgeVFX.WarpforgeVatDriver.UpdateMaterials(bool)`
OUR_COMPONENT = {
    "AnimFXModuleChangeMaterial": "WarpforgeVFX.WFModuleChangeMaterial",
    "StoryProgramming.VATGPUPlayer": "WarpforgeVFX.WarpforgeVatDriver",
}


def load_json(p):
    with io.open(p, "r", encoding="utf-8") as f:
        return json.load(f)


def head(p, n=400):
    with io.open(p, "rb") as f:
        return f.read(n).decode("utf-8", "replace")


def read_pptr(d):
    """`{"m_FileID": 0, "m_PathID": X}` → (fileId, pathId)；形状不对返回 (None, None)。"""
    if not isinstance(d, dict):
        return (None, None)
    return (d.get("m_FileID"), d.get("m_PathID"))


def build_monoscripts():
    """MonoScript pathID → {name, ns, asm}。"""
    out = {}
    for fn in os.listdir(MONOSCRIPTS):
        if not fn.endswith(".json"):
            continue
        m = re.search(r"_(-?\d+)\.json$", fn)
        if not m:
            continue
        d = load_json(os.path.join(MONOSCRIPTS, fn))
        out[int(m.group(1))] = {
            "name": d.get("m_ClassName") or d.get("m_Name") or "",
            "ns": d.get("m_Namespace") or "",
            "asm": d.get("m_AssemblyName") or "",
        }
    return out


class Level(object):
    """一个 bundle 里的对象图索引（按需读，不预扫全库 —— 只有命中的包才建）。"""

    def __init__(self, bundle_dir):
        self.dir = bundle_dir
        self._comp_go = None        # 组件 pathID → gameObject pathID
        self._go_name = None        # gameObject pathID → 名字
        self._tr_of_go = None       # gameObject pathID → Transform pathID
        self._go_of_tr = None       # Transform pathID → gameObject pathID
        self._tr = None             # Transform pathID → (father, [children])
        self._pending = []          # (GameObject 名字, 它的 JSON 头文本)

    COMP_RE = re.compile(r'"component"\s*:\s*\{\s*"m_FileID"\s*:\s*(-?\d+)\s*,\s*'
                         r'"m_PathID"\s*:\s*(-?\d+)\s*\}')

    # 组件所在的目录（`assets_full` 按资产类型分目录，一个包里的组件类型是有限的）
    COMP_DIRS = ("MonoBehaviour", "Transform", "Animation", "Animator", "MeshRenderer",
                 "MeshFilter", "ParticleSystem", "ParticleSystemRenderer", "SpriteRenderer",
                 "AudioSource", "Canvas", "CanvasGroup", "TrailRenderer", "VFXRenderer",
                 "VisualEffect", "SortingGroup", "SkinnedMeshRenderer", "SpriteMask")

    def _build_comp_go(self):
        """组件 pathID → gameObject pathID（只读文件头 300 B）。"""
        self._comp_go = {}
        for sub in self.COMP_DIRS:
            d = os.path.join(self.dir, sub)
            if not os.path.isdir(d):
                continue
            for fn in os.listdir(d):
                m = re.search(r"_(-?\d+)\.json$", fn)
                if not fn.endswith(".json") or not m:
                    continue
                # ⚠️ 必须锚在 `m_GameObject` 上取 pathID —— 不能拿「文件里第一个 m_PathID」，
                #    那对 `m_GameObject: {m_PathID: 0}` 的资产级组件会静默取到 m_Script 的号。
                t = head(os.path.join(d, fn), 300)
                mm = re.search(r'"m_GameObject"\s*:\s*\{\s*"m_FileID"\s*:\s*-?\d+\s*,\s*'
                               r'"m_PathID"\s*:\s*(-?\d+)\s*\}', t)
                if mm:
                    self._comp_go[int(m.group(1))] = int(mm.group(1))

    def _build_go_index(self):
        """扫 GameObject/*.json 的 m_Component。
        ⚠️ UnityEngine 的 GameObject 文件里**没有它自己的 pathID** ⇒ 只能反查：
           该 Go 的某个组件 pi → `_comp_go[pi]` = 这个 GameObject 的 pid ⇒ `_go_name[pid] = 名字`。"""
        gdir = os.path.join(self.dir, "GameObject")
        if not os.path.isdir(gdir):
            return
        for fn in os.listdir(gdir):
            if not fn.endswith(".json"):
                continue
            p = os.path.join(gdir, fn)
            t = head(p, 4096)
            if '"m_Name"' not in t:
                with io.open(p, "r", encoding="utf-8") as f:
                    t = f.read()
            self._pending.append((self._name_of(fn, t), t))

    @staticmethod
    def _name_of(fn, t):
        nm = re.search(r'"m_Name"\s*:\s*"((?:[^"\\]|\\.)*)"', t)
        if not nm:
            return fn[:-5]
        try:
            return json.loads('"%s"' % nm.group(1))
        except Exception:
            return nm.group(1)

    def _build_all(self):
        if self._go_name is not None:
            return
        self._build_comp_go()
        self._build_go_index()
        self._go_name = {}
        for name, t in self._pending:
            for m in self.COMP_RE.finditer(t):
                if int(m.group(1)) != 0:
                    continue
                go = self._comp_go.get(int(m.group(2)))
                if go:
                    self._go_name[go] = name
        self._pending = []
        # Transform：pid → (father, children) 与 gameObject ↔ transform
        self._tr = {}
        self._tr_of_go = {}
        self._go_of_tr = {}
        tdir = os.path.join(self.dir, "Transform")
        if os.path.isdir(tdir):
            for fn in os.listdir(tdir):
                m = re.search(r"_(-?\d+)\.json$", fn)
                if not fn.endswith(".json") or not m:
                    continue
                pid = int(m.group(1))
                d = load_json(os.path.join(tdir, fn))
                go = read_pptr(d.get("m_GameObject"))[1]
                fa = read_pptr(d.get("m_Father"))[1]
                kids = []
                for c in d.get("m_Children") or []:
                    k = read_pptr(c)
                    if k[1] is not None:
                        kids.append(k[1])
                self._tr[pid] = (fa or 0, kids)
                if go is not None:
                    self._tr_of_go[go] = pid
                    self._go_of_tr[pid] = go

    def go_name(self, go_pid):
        self._build_all()
        return self._go_name.get(go_pid)

    def transform_of_go(self, go_pid):
        self._build_all()
        return self._tr_of_go.get(go_pid)


_LEVELS = {}


def level_for(bundle_dir):
    if bundle_dir not in _LEVELS:
        _LEVELS[bundle_dir] = Level(bundle_dir)
    return _LEVELS[bundle_dir]


def comp_go_map(bundle_dir):
    """组件 pathID → gameObject pathID（只建一次，按需）。"""
    lv = level_for(bundle_dir)
    lv._build_all()
    return lv._comp_go


def chain_to_root(lv, tr_pid):
    """Transform pid → [(名字, 在父节点里的下标)]，从根往下的顺序。"""
    lv._build_all()
    chain = []
    cur = tr_pid
    seen = set()
    while cur and cur in lv._tr and cur not in seen:
        seen.add(cur)
        father, _ = lv._tr[cur]
        # 找它在父节点里的下标
        idx = -1
        if father and father in lv._tr:
            kids = lv._tr[father][1]
            if cur in kids:
                idx = kids.index(cur)
        go = lv._go_of_tr.get(cur)
        chain.append((lv.go_name(go) if go is not None else "?", idx))
        cur = father
    chain.reverse()
    return chain


def scan():
    """全库扫 `AnimFXModuleEvent` 实例。返回 (records, stats)。"""
    ms = build_monoscripts()
    records = []
    instances = set()
    scanned_files = 0
    bundles = sorted(d for d in os.listdir(ASSETS)
                     if d.startswith("bundle_") and os.path.isdir(os.path.join(ASSETS, d)))
    for b in bundles:
        mdir = os.path.join(ASSETS, b, "MonoBehaviour")
        if not os.path.isdir(mdir):
            continue
        for fn in sorted(os.listdir(mdir)):
            if not fn.endswith(".json"):
                continue
            p = os.path.join(mdir, fn)
            t = head(p, 300)
            mm = re.search(r'"m_Script"\s*:\s*\{\s*"m_FileID"\s*:\s*(-?\d+)\s*,\s*'
                           r'"m_PathID"\s*:\s*(-?\d+)\s*\}', t)
            if not mm:
                continue
            scanned_files += 1
            cls = ms.get(int(mm.group(2)))
            if not cls or cls["name"] != KIND:
                continue
            instances.add((b, fn))
            d = load_json(p)
            bundle_dir = os.path.join(ASSETS, b)
            lv = level_for(bundle_dir)
            cg = comp_go_map(bundle_dir)
            lv._build_all()

            own_go = read_pptr(d.get("m_GameObject"))[1]
            own_chain = chain_to_root(lv, lv.transform_of_go(own_go)) if own_go else []

            for i, ev in enumerate(d.get("eventsToFire") or []):
                ue = (ev or {}).get("eventToFire") or {}
                calls = ((ue.get("m_PersistentCalls") or {}).get("m_Calls")) or []
                for j, call in enumerate(calls):
                    tfid, tpid = read_pptr(call.get("m_Target"))
                    tgo = cg.get(tpid)
                    chain = chain_to_root(lv, lv.transform_of_go(tgo)) if tgo else []
                    # 相对事件模块自己的 GameObject 算路径
                    rel = chain
                    if own_chain and chain[:len(own_chain)] == own_chain:
                        rel = chain[len(own_chain):]
                    asm_ty = call.get("m_TargetAssemblyTypeName") or ""
                    parts = asm_ty.split(",")
                    bare = parts[0].strip()
                    args = call.get("m_Arguments") or {}
                    mode = call.get("m_Mode")
                    records.append({
                        "effect": own_chain[-1][0] if own_chain else os.path.basename(bundle_dir),
                        "index": i,
                        "whereToFire": (ev or {}).get("whereToFire"),
                        "callIndex": j,
                        "targetPath": "/".join(n for n, _ in rel),
                        "targetSiblingIndex": rel[-1][1] if rel else -1,
                        "targetComponent": bare,
                        "targetAssembly": parts[1].strip() if len(parts) > 1 else "",
                        "targetComponentOur": OUR_COMPONENT.get(bare, ""),
                        "methodName": call.get("m_MethodName") or "",
                        "argMode": MODE_NAMES.get(mode, "Unknown(%s)" % mode),
                        "argModeValue": mode,
                        "argBool": bool(args.get("m_BoolArgument")),
                        "argInt": args.get("m_IntArgument"),
                        "argFloat": args.get("m_FloatArgument"),
                        "argString": args.get("m_StringArgument") or "",
                        "argObjectAssemblyType": args.get("m_ObjectArgumentAssemblyTypeName") or "",
                        "callState": call.get("m_CallState"),
                        "callStateName": CALLSTATE_NAMES.get(call.get("m_CallState"), "?"),
                        # 出处（可复查坐标）
                        "sourceBundle": b,
                        "sourceFile": "MonoBehaviour/" + fn,
                        "sourceTargetFileId": tfid,
                        "sourceTargetPathId": tpid,
                    })
    stats = {"bundles": len(bundles), "monoBehavioursScanned": scanned_files,
             "instances": len(instances), "calls": len(records)}
    return records, stats


def build_doc(records, stats):
    return {
        "generated_by": "工具/gen_module_events.py",
        "source_tree": "D:/2/新解包资源/assets_full",
        "note": ("原版 `AnimFXModuleEvent.eventsToFire[i].eventToFire.m_PersistentCalls.m_Calls` 的逐条展开。"
                 "⛔ 别手改；改资源后重跑 `python -I gen_module_events.py`。"
                 "`m_Calls` 在 `数据/游戏数据/animfx_components.json` 里被写成了 \"<深>\"（dump 深度护栏截断），"
                 "不是原版没有 —— 本表直接读 prefab 原始 JSON。"),
        "whereToFireLegend": {"0": "Initialize", "5": "Exit", "10": "DoDestroy(原版无方法体)",
                              "15": "Manual(原版无方法体)"},
        "argModeLegend": MODE_NAMES,
        "callStateLegend": CALLSTATE_NAMES,
        "stats": stats,
        "calls": records,
    }


MARK_BEGIN = "// >>> gen_module_events.py"
MARK_END = "// <<< gen_module_events.py"


def cs_table_lines(records):
    out = []
    for r in records:
        out.append(
            '            new TableCall("%s", %d, %d, %d, %s, %d, "%s", "%s", ArgMode.%s, %s, %d),'
            % (r["effect"], r["index"], r["whereToFire"], r["callIndex"],
               _cs_str(r["targetPath"]), r["targetSiblingIndex"],
               r["targetComponent"], r["methodName"], r["argMode"],
               "true" if r["argBool"] else "false", r["callState"]))
    return out


def _cs_str(s):
    return '"' + s.replace("\\", "\\\\").replace('"', '\\"') + '"'


ROW_RE = re.compile(
    r'new TableCall\(\s*"((?:[^"\\]|\\.)*)"\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*'
    r'"((?:[^"\\]|\\.)*)"\s*,\s*(-?\d+)\s*,\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"\s*,\s*'
    r'ArgMode\.(\w+)\s*,\s*(true|false)\s*,\s*(-?\d+)\s*\)')


def check(records):
    if not os.path.isfile(CS):
        print("!! 找不到 %s" % CS)
        return 1
    txt = io.open(CS, "r", encoding="utf-8").read()
    b = txt.find(MARK_BEGIN)
    e = txt.find(MARK_END)
    if b < 0 or e < 0 or e < b:
        print("!! WFModuleEvent.cs 里找不到生成区标记 %s … %s" % (MARK_BEGIN, MARK_END))
        return 1
    block = txt[b:e]
    rows = ROW_RE.findall(block)
    exp = [(r["effect"], r["index"], r["whereToFire"], r["callIndex"], r["targetPath"],
            r["targetSiblingIndex"], r["targetComponent"], r["methodName"], r["argMode"],
            "true" if r["argBool"] else "false", r["callState"]) for r in records]
    got = [(a, int(b2), int(c), int(d), f, int(g), h, i, j, k, int(l))
           for (a, b2, c, d, f, g, h, i, j, k, l) in rows]
    if len(got) != len(exp):
        print("!! 行数不一致：json %d 条 / C# 表 %d 条" % (len(exp), len(got)))
    bad = 0
    for n in range(min(len(got), len(exp))):
        if got[n] != exp[n]:
            bad += 1
            print("!! 第 %d 行不一致：\n   json: %r\n   C#  : %r" % (n, exp[n], got[n]))
    if len(got) == len(exp) and not bad:
        print("== C# 生成区与 json 一致（%d 条）" % len(exp))
        return 0
    return 1


def main(argv):
    records, stats = scan()
    if "--dump" in argv:
        print(json.dumps(records, ensure_ascii=False, indent=1))
        print("stats:", stats)
        return 0
    if "--check" in argv:
        return check(records)
    if "--print-cs" in argv:
        # 打印可直接贴进 `WFModuleEvent.cs` 生成区的行（⛔ 脚本不改 .cs —— 那一行由人贴，
        # 然后 `--check` 负责「贴的和 json 是否一致」）
        for ln in cs_table_lines(records):
            print(ln)
        return 0
    doc = build_doc(records, stats)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    s = json.dumps(doc, ensure_ascii=False, indent=1) + "\n"
    with io.open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write(s)
    print("写 %s（%d 条 / %d 个实例 / 扫了 %d 个包 %d 个 MonoBehaviour）"
          % (OUT, stats["calls"], stats["instances"], stats["bundles"],
             stats["monoBehavioursScanned"]))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
