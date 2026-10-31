#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""animfx_module_field_audit.py — AnimFXModule 全量字段比对（只读工具，永不写盘）

用法
----
    python -I -X utf8 d:/4/Unity/工具/animfx_module_field_audit.py            # 只打印用法
    python -I -X utf8 d:/4/Unity/工具/animfx_module_field_audit.py --check    # 读—比—报告
    python -I -X utf8 d:/4/Unity/工具/animfx_module_field_audit.py --check --strict
    python -I -X utf8 d:/4/Unity/工具/animfx_module_field_audit.py --check --kind AnimFXModuleTween
    python -I -X utf8 d:/4/Unity/工具/animfx_module_field_audit.py --check --tsv

不带任何参数 = 只打印本段用法并退出（不会读任何资产、不会写任何文件）。
`--check` 是幂等的：只读不写，同一份输入每次输出逐字相同（无时间戳、全排序）。
`--strict`：存在「缺字段 / 同名不同义 / 名字对不上(待定)」时退出码 1（默认 0）。

判据（三个来源，缺一个就不下结论）
----------------------------------
1. 原版序列化名与值：`d:/4/Unity/数据/游戏数据/animfx_modules.json`
   （由 `工具/gen_animfx_modules.py` 生成：字段拍平成点号键，引用编码 `@asset:` / `@node:`）
2. 原版类型声明：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/*.cs`（ILSpy 签名桩，含 [SerializeField]）
   交叉核对：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/*.cs`（Cpp2IL 反编译，方法体在）
3. 我方组件：`d:/4/Unity/MyGame/Assets/WarpforgeVFX/Runtime/WFModule*.cs`
   （另有 `CardPresentation/Battle/AnimFXController.cs` = 原版 AnimFXController 的对应物）

映射规则（不按名字猜）
----------------------
* 「同名」：原版序列化名与我方字段名逐字相同（仍会用类型判是否同义）。
* 「改名」：**只有在我方代码里找到读取点**才认 —— `字段 = def.GetXxx("原版名", …)`
  或对象初始化器里的 `字段 = def.GetXxx(p + ".原版名")`（我方文件:行号 即证据）。
* 读到了但找不到字段归属 ⇒ 一律进「待定」，不猜。
* 原版有、我方没有对应读取 ⇒ 缺字段。我方有、原版没有 ⇒ 多字段。
* 同名但类型不相容 ⇒ 同名不同义（引用被拍平成字符串路径属于已知的表示层差异，单列一类）。

输出口径
--------
`--tsv` 把映射表按制表符输出（列：类 / 原版名 / 原版类型 / 我方字段 / 我方类型 / 判定 / 证据），
便于落表；默认给人读的表格。
"""
import io
import json
import os
import re
import sys

DEFAULT_JSON = r"d:/4/Unity/数据/游戏数据/animfx_modules.json"
ORIG_STUB_DIR = r"d:/2/Warpforge_code/Scripts/Assembly-CSharp"
ORIG_ILSPY_DIR = r"d:/2/tools/cpp2il/out_cpp2il_dev_ilspy"
OURS_DIR = r"d:/4/Unity/MyGame/Assets/WarpforgeVFX/Runtime"
OUR_CONTROLLER = r"d:/4/Unity/MyGame/Assets/CardPresentation/Battle/AnimFXController.cs"

# 原版类 → (签名桩文件, ilspy 文件, 我方文件, 我方主类名, 数据里的 kind)
# 数据 kind 为 None = 该原版类在本 build 的数据里 0 实例（不能拿数据当判据，只能用类型桩）。
CLASSES = [
    ("AnimFXModuleBase", "AnimFXModuleBase.cs", "AnimFXModuleBase.cs", "WFEffectModule.cs", "WFEffectModule", None),
    ("AnimFXModuleAnimation", "AnimFXModuleAnimation.cs", "AnimFXModuleAnimation.cs", "WFModuleAnimation.cs", "WFModuleAnimation", "AnimFXModuleAnimation"),
    ("AnimFXModuleCardback", "AnimFXModuleCardback.cs", "AnimFXModuleCardback.cs", "WFModuleCardback.cs", "WFModuleCardback", "AnimFXModuleCardback"),
    ("AnimFXModuleChangeMaterial", "AnimFXModuleChangeMaterial.cs", "AnimFXModuleChangeMaterial.cs", "WFModuleChangeMaterial.cs", "WFModuleChangeMaterial", "AnimFXModuleChangeMaterial"),
    ("AnimFXModuleChangeVelocity", "AnimFXModuleChangeVelocity.cs", "AnimFXModuleChangeVelocity.cs", "WFModuleChangeVelocity.cs", "WFModuleChangeVelocity", None),
    ("AnimFXModuleCollisions", "AnimFXModuleCollisions.cs", "AnimFXModuleCollisions.cs", "WFModuleCollisions.cs", "WFModuleCollisions", "AnimFXModuleCollisions"),
    ("AnimFXModuleDestroyInTime", "AnimFXModuleDestroyInTime.cs", "AnimFXModuleDestroyInTime.cs", "WFModuleDestroyInTime.cs", "WFModuleDestroyInTime", "AnimFXModuleDestroyInTime"),
    ("AnimFXModuleDoDestroyAnimation", "AnimFXModuleDoDestroyAnimation.cs", "AnimFXModuleDoDestroyAnimation.cs", "WFModuleDoDestroyAnimation.cs", "WFModuleDoDestroyAnimation", "AnimFXModuleDoDestroyAnimation"),
    ("AnimFXModuleEvent", "AnimFXModuleEvent.cs", "AnimFXModuleEvent.cs", "WFModuleEvent.cs", "WFModuleEvent", "AnimFXModuleEvent"),
    ("AnimFXModulePostProcess", "AnimFXModulePostProcess.cs", "AnimFXModulePostProcess.cs", "WFModulePostProcess.cs", "WFModulePostProcess", "AnimFXModulePostProcess"),
    ("AnimFXModuleScaleByTarget", "AnimFXModuleScaleByTarget.cs", "AnimFXModuleScaleByTarget.cs", "WFModuleScaleByTarget.cs", "WFModuleScaleByTarget", "AnimFXModuleScaleByTarget"),
    ("AnimFXModuleScreenShake", "AnimFXModuleScreenShake.cs", "AnimFXModuleScreenShake.cs", "WFModuleScreenShake.cs", "WFModuleScreenShake", "AnimFXModuleScreenShake"),
    ("AnimFXModuleTransformModifier", "AnimFXModuleTransformModifier.cs", "AnimFXModuleTransformModifier.cs", "WFModuleTransformModifier.cs", "WFModuleTransformModifier", "AnimFXModuleTransformModifier"),
    ("AnimFXModuleTween", "AnimFXModuleTween.cs", "AnimFXModuleTween.cs", "WFModuleTween.cs", "WFModuleTween", "AnimFXModuleTween"),
    ("AnimFxModuleMoveParticlesToTarget", "AnimFxModuleMoveParticlesToTarget.cs", "AnimFxModuleMoveParticlesToTarget.cs", "WFModuleMoveParticlesToTarget.cs", "WFModuleMoveParticlesToTarget", "AnimFxModuleMoveParticlesToTarget"),
    ("AnimFXInstanceParticleAdjacent", "AnimFXInstanceParticleAdjacent.cs", "AnimFXInstanceParticleAdjacent.cs", "WFModuleInstanceParticleAdjacent.cs", "WFModuleInstanceParticleAdjacent", "AnimFXInstanceParticleAdjacent"),
    ("AnimFXParticleCollisionNotifier", "AnimFXParticleCollisionNotifier.cs", "AnimFXParticleCollisionNotifier.cs", "WFModuleParticleCollisionNotifier.cs", "WFModuleParticleCollisionNotifier", None),
    ("AnimFXController", "AnimFXController.cs", "AnimFXController.cs", None, "AnimFXController", "AnimFXController"),
]

# 嵌套结构对应关系：原版类型（可跨文件） ↔ 我方嵌套类。
# 依据 = 两边的**字段名与类型**逐个对得上 + 我方读取点（Configure 里读同一批键），不是按名字猜。
NESTED_PAIRS = [
    ("OverwriteCameraShakePreset.cs", "OverwriteCameraShakePreset", "WFModuleScreenShake.cs", "ShakeEntry",
     "WFModuleScreenShake.cs:456-471 逐个读 `.presetSO/.delay/.sustainTime/…`"),
    ("AnimFXModuleEvent.cs", "AnimFXModuleEvent.FireEvent", "WFModuleEvent.cs", "FireEvent",
     "WFModuleEvent.cs:521 读 `eventsToFire[i].whereToFire`（同名类）"),
    ("AnimFXModuleTransformModifier.cs", "AnimFXModuleTransformModifier.TransformModifier", "WFModuleTransformModifier.cs", "TransMod",
     "WFModuleTransformModifier.cs:311-321 读同一批顶层键；TransMod 字段与 TransformModifier 逐名相同"),
    ("AnimFXModuleCollisions.cs", "AnimFXModuleCollisions.CollisionAndParticles", "WFModuleCollisions.cs", "CollisionAndParticles",
     "WFModuleCollisions.cs:534 读 `collisionAndParticles[i].collisionPlane`（同名类）"),
    ("AnimFXModuleCollisions.cs", "AnimFXModuleCollisions.CollisionAndParticles.ParticleCollisionDefinition",
     "WFModuleCollisions.cs", "ParticleCollisionDefinition",
     "WFModuleCollisions.cs:559-560 读 `.particleSystem` / `.receiveCollisionMessage`"),
    ("MaterialTweenBase.cs", "MaterialTweenBase", "WFModuleChangeMaterial.cs", "WFMatTween",
     "WFModuleChangeMaterial.cs:271 读 `materialAnimations`；子类 Float/Color/Vector 拍平进 WFMatTween"),
    ("PlaySoundOnTime.cs", "PlaySoundOnTime", "AnimFXController.cs", "PlaySoundOnTime",
     "AnimFXController.cs:162-179 读同名字段（sounds[i].*）"),
]

# 原版字段名 → 我方字段名 的**手工对应**（只用于自动规则覆盖不到的场合；每条必须带我方代码行号）。
# 不进 ALIASES 而自动判定不出来的，一律按「待定」报，不猜。
ALIASES = [
    # (类, 原版序列化名, 我方字段, 证据, 说明)
    ("AnimFXModuleChangeMaterial", "materialAnimations", "tweens",
     "WFModuleChangeMaterial.cs:271-272", "原版 DynamicList<MaterialTweenBase> 拍平成 materialAnimations[i].*，由 ReadTweens 装配"),
    ("AnimFXModuleChangeMaterial", "materialAnimations", "materialAnimationCount",
     "WFModuleChangeMaterial.cs:271", "只记条数（诊断用）；真正承载数据的是 tweens"),
]

NESTED_RENAMES = {p[1].split(".")[-1]: p[3] for p in NESTED_PAIRS}

# 嵌套结构里的「改名」对应（原版名 → (我方名, 说明)）；每条都能在我方代码/数据里找到读取点。
NESTED_FIELD_ALIASES = {
    ("OverwriteCameraShakePreset", "presetSO"): ("preset", "改名：原版 CameraShakePresetSO 引用 → 我方只存名字串（WFModuleScreenShake.cs:456 读 `.presetSO`）"),
}

# 我方独有字段里，「不是原版字段、但有明确来路」的那些（只作说明，不改变归类）
EXTRA_NOTES = {
    "actingCard": "卡片上下文载体（原版从 AnimFXController.actingCard 读）",
    "targetCard": "卡片上下文载体（原版从 AnimFXController.targetCard 读）",
    "actingIsPlayer": "卡片上下文载体（原版 CardScript.isPlayer）",
    "targetIsPlayer": "卡片上下文载体（原版 CardScript.isPlayer）",
    "targetIsWarlord": "卡片上下文载体（原版 CardScript.IsWarlordEquivalent）",
    "myAnimationRef": "承接原版 `myAnimation` 的序列化引用（字符串形态），见改名表",
    "animationNode": "运行时解析出来的节点（原版 simpleAnimation 组件的宿主）",
    "rendererTargets": "运行时解析出来的 Renderer（原版 material 的应用目标）",
    "targets": "运行时解析出来的粒子系统（原版 particleSystemsToAddCardback 的宿主）",
    "cardback": "运行时解析出来的卡背 Sprite（由 CardbackResolver 提供）",
    "OnFinished": "原版同名字段（签名桩里是 Action 字段）——类型的 Action 被 type_category 归到 delegate",
    "materialAnimationCount": "只记条数（诊断用）；实体由 tweens 承载，见 materialAnimations 的改名行",
    "EffectName": "我方给每个效果起的名字（诊断/日志用，原版没有这个字段）",
    "boardScaleBase": "运行时状态：棋盘基准缩放（原版在 ApplyTransforms 内部按棋盘位置算）",
    "cardIsPlayer": "运行时状态：本次作用的卡是不是玩家方（原版从 CardScript.isPlayer 读）",
    "targetCardForAim": "运行时状态：缓存的对准目标（原版每次 Update 现取 controller.targetCard）",
}

REF_TYPES = {
    "Transform", "Material", "Animation", "SimpleAnimation", "ParticleSystem", "Renderer", "Texture",
    "UnitTweenSO", "DynamicList", "CameraShakePresetSO", "CameraShakePreset", "CameraShake",
    "CardScript", "EntityScript", "AnimFXController", "MonoBehaviour", "Component", "UnityEvent",
    "GameObject", "AudioCue", "Texture2D", "RenderTexture", "Mesh", "Sprite", "MaterialTweenBase",
    "PlaySoundOnTime", "AnimFXModuleBase", "AnimationCurve", "AssetReferenceTyped", "CardAnim",
    "OverwriteCameraShakePreset", "TransformModifier", "CollisionAndParticles", "ParticleCollisionDefinition",
}
PRIMS = {"string", "bool", "int", "float", "double", "Vector2", "Vector3", "Vector4", "Color", "Quaternion", "long", "short", "byte"}

FIELD_RE = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*"
    r"(?:(?:public|private|protected|internal|static|readonly|const|new|override|volatile|unsafe|extern|abstract|virtual|sealed)\s+)+"
    r"([A-Za-z_][\w\.]*(?:\s*<[^;{}]*?>)?(?:\s*\[\s*\])*)\s+"
    r"([A-Za-z_]\w*)\s*(?:=[^;{}]*)?;\s*$"
)
ASSIGN_RE = re.compile(
    r"([A-Za-z_]\w*)\s*=\s*(?:\([^;{}]*?\)\s*)?_?def\.(?:GetString|GetBool|GetInt|GetFloat|GetList|GetStringList|CountList)\(\s*\"([^\"]+)\""
)


def usage():
    print(__doc__.strip())


# ---------------------------------------------------------------- C# 解析
def strip_code(text):
    """去掉注释与字符串内容（保留行结构），避免 `//` 里的括号/分号污染块栈。"""
    out = []
    i, n = 0, len(text)
    in_str = None
    while i < n:
        c = text[i]
        if in_str:
            if c == "\\" and in_str != "'":
                out.append(" ")
                i += 2
                continue
            if c == in_str:
                in_str = None
            out.append("\n" if c == "\n" else " ")
            i += 1
            continue
        if c == "/" and i + 1 < n and text[i + 1] == "/":
            j = text.find("\n", i)
            if j < 0:
                break
            out.append(" " * (j - i))
            i = j
            continue
        if c == "/" and i + 1 < n and text[i + 1] == "*":
            j = text.find("*/", i + 2)
            j = n if j < 0 else j + 2
            out.append("".join("\n" if ch == "\n" else " " for ch in text[i:j]))
            i = j
            continue
        if c in ('"', "'"):
            in_str = c
            out.append(" ")
            i += 1
            continue
        out.append(c)
        i += 1
    return "".join(out)


def block_scan(code):
    """返回 [(line_no, stack_snapshot)] —— 每行开头时所在的块栈。"""
    lines = code.split("\n")
    pending = []
    stack = []
    snaps = []
    for ln, line in enumerate(lines, 1):
        snaps.append((ln, list(stack)))
        for ch in line:
            if ch == "{":
                header = "".join(pending)
                m = re.search(r"\b(class|struct|interface|enum)\s+([A-Za-z_]\w*(?:\s*<[^>]*>)?)", header)
                if m:
                    stack.append(("type", m.group(2).strip()))
                elif re.search(r"\bnamespace\s+", header):
                    stack.append(("ns", ""))
                else:
                    stack.append(("block", ""))
                pending = []
            elif ch == "}":
                if stack:
                    stack.pop()
                pending = []
            elif ch == ";":
                pending = []
            else:
                pending.append(ch)
    return lines, snaps


MODIFIERS = {"public", "private", "protected", "internal", "static", "readonly", "const", "new",
             "override", "volatile", "unsafe", "extern", "abstract", "virtual", "sealed", "partial"}
RESERVED = {"return", "if", "for", "foreach", "while", "switch", "case", "else", "using", "namespace",
            "class", "struct", "enum", "interface", "void", "get", "set", "new", "await", "yield", "throw", "goto"}


def parse_field_line(s):
    """把一行（可能含多个 `;` 声明）解析成 [(type, name, static, const)]。
    `public float a, b;` 与 `public bool x; public float y;` 都支持。
    只接受**在类体层级**出现的声明（由调用方按块栈保证）；方法/属性/表达式体一律不认。"""
    out = []
    if not s or "{" in s or "=>" in s or "#" in s:
        return [("", "", False, False)]
    for stmt in s.split(";"):
        stmt = stmt.strip()
        if not stmt:
            continue
        body = stmt
        is_static = False
        is_const = False
        head = body.split("=", 1)[0]
        for k in re.split(r"[\s(<]+", head):
            if k == "static":
                is_static = True
            if k == "const":
                is_const = True
        if "=" in body:
            body = body.split("=", 1)[0].strip()
        toks = body.split()
        while toks and toks[0] in MODIFIERS:
            toks.pop(0)
        if len(toks) < 2:
            return [("", "", False, False)]
        ftype = toks[0]
        names = " ".join(toks[1:])
        if not re.match(r"^[A-Za-z_][\w\.]*(<[^;{}]*>)?(\[\])?$", ftype) or ftype in RESERVED:
            return [("", "", False, False)]
        for nm in names.split(","):
            nm = nm.strip()
            m2 = re.match(r"^([A-Za-z_]\w*)$", nm)
            if not m2:
                return [("", "", False, False)]
            out.append((ftype, m2.group(1), is_static, is_const))
    return out


def parse_cs(path):
    """解析一个 C# 文件 → {qualified_type: [dict(name,type,line,attrs,static,const,array)]}"""
    if not os.path.isfile(path):
        return None
    text = io.open(path, "r", encoding="utf-8-sig", errors="replace").read()
    raw_lines = text.split("\n")
    code = strip_code(text)
    lines, snaps = block_scan(code)
    types = {}
    last_attrs = []
    for ln, line in enumerate(lines, 1):
        s = line.strip()
        if not s:
            continue
        while s.startswith("["):
            e = s.find("]")
            if e < 0:
                break
            last_attrs.append(s[:e + 1])
            s = s[e + 1:].strip()
            if not s:
                break
        if not s:
            continue
        stack = snaps[ln - 1][1]
        if not stack or stack[-1][0] != "type":
            last_attrs = []
            continue
        type_names = [nm for kind, nm in stack if kind == "type"]
        qualified = ".".join(type_names)
        for ftype, fname, is_static, is_const in parse_field_line(s):
            if not fname:
                continue
            types.setdefault(qualified, []).append(dict(
                name=fname, type=ftype, line=ln,
                attrs=list(last_attrs), static=is_static, const=is_const,
                array=ftype.endswith("[]"),
                raw=raw_lines[ln - 1].strip() if ln - 1 < len(raw_lines) else s,
            ))
        last_attrs = []
    return types


def load_base_fields(path):
    """我方基类 `WFEffectModule` 的非静态字段（原版 `AnimFXModuleBase` 的对应物）。"""
    t = parse_cs(path) or {}
    out = {}
    for qname, flist in t.items():
        if qname.split(".")[-1] != "WFEffectModule":
            continue
        for f in flist:
            if f["static"] or f["const"]:
                continue
            out[f["name"]] = f
    return out


def read_assignments(path):
    """我方文件里 `字段 = def.GetXxx("键"` 的 (字段, 键, 行号) 列表。"""
    if not os.path.isfile(path):
        return []
    text = io.open(path, "r", encoding="utf-8-sig", errors="replace").read()
    lines = text.split("\n")
    out = []
    for i, line in enumerate(lines, 1):
        m = ASSIGN_RE.search(line)
        if m:
            out.append((m.group(1), m.group(2), i))
    return out


# ---------------------------------------------------------------- 数据
def load_json_keys(path):
    with io.open(path, "r", encoding="utf-8") as fh:
        data = json.load(fh)
    keys = {}
    for eff in data.get("effects", []):
        for mod in eff.get("modules", []):
            keys.setdefault(mod["kind"], set()).update(mod.get("keys", []))
    return data, keys


def top_level_names(keyset):
    names = set()
    for k in keyset:
        m = re.match(r"^([A-Za-z_]\w*)", k)
        if m:
            names.add(m.group(1))
    return names


# ---------------------------------------------------------------- 类型判定
def norm_type(t):
    t = t.replace(" ", "")
    t = re.sub(r"^(?:System\.)?", "", t)
    return t


def type_category(t):
    """→ (类别, 元素类型名, 是否数组)。`List<X>` 的元素名取 X（用于嵌套类改名判定）。"""
    t = norm_type(t)
    elem = t
    is_arr = False
    m = re.match(r"^(.*)\[\]$", t)
    if m:
        is_arr, elem = True, m.group(1)
    container = re.sub(r"<.*>$", "", elem)
    inner = re.search(r"<([^<>]+)>$", elem)
    if container in ("List", "DynamicList", "IReadOnlyList", "IList"):
        return "list", (inner.group(1).strip() if inner else container), is_arr
    if container in PRIMS:
        return "prim", container, is_arr
    if container in REF_TYPES:
        return "ref", container, is_arr
    if container in ("Action", "Func", "UnityAction"):
        return "delegate", container, is_arr
    return "other", container, is_arr


def type_verdict(orig_t, our_t):
    """'同义' / '同义*' / '同义(枚举改名)' / '同义(嵌套类改名)' / '同名不同义(单个↔数组)' / '类型不同' / '待核'"""
    if orig_t == "?":
        return "待定(原版类型桩里没有这个名)"
    oc, ob, oa = type_category(orig_t)
    mc, mb, ma = type_category(our_t)
    if norm_type(orig_t) == norm_type(our_t):
        return "同义"
    # 我方枚举/类型加了 WF 前缀（WFActionStart / WFVFXObjective / WFCollisionPlane …）
    if not oa and not ma and mb == "WF" + ob:
        return "同义(枚举改名)"
    if NESTED_RENAMES.get(ob) == mb:
        return "同义(嵌套类改名)"
    if oc == "ref" and mc == "prim" and mb in ("string",) and oa == ma:
        return "同义*"   # 引用被拍平成字符串路径（我们的表示层约定），语义相同
    if oc == "ref" and mb == "string" and not ma and oa:
        return "类型不同"
    if oc == "list" and mc in ("list",) and not oa:
        return "同义"
    if oc == "prim" and mc == "prim" and ob == mb:
        return "同义"
    if oc == "prim" and mc == "prim" and {ob, mb} == {"bool", "int"}:
        return "同义(bool↔0/1 表示差异)"
    if oc == "prim" and mc == "prim":
        return "类型不同"
    # 原版单个 vs 我方数组（或反之）：形状不同，单列
    if not oa and ma and ob == mb:
        return "同名不同义(单个↔数组)"
    if oa and not ma and ob == mb:
        return "同名不同义(单个↔数组)"
    return "待核"


# ---------------------------------------------------------------- 主流程
def audit(json_path, kind_filter=None, tsv=False, strict=False):
    data, jkeys = load_json_keys(json_path)
    base_fields = load_base_fields(os.path.join(OURS_DIR, "WFEffectModule.cs"))
    rows = []
    diffs = {"缺字段": [], "多字段": [], "同名不同义": [], "名字对不上": [], "待定": [], "运行态对应": [], "私有运行态": []}
    nested_rows = []
    crosscheck = []
    stats = {"classes": 0, "orig_serialized": 0, "mapped": 0, "missing": 0, "extra": 0,
             "same_name_diff_meaning": 0, "renamed": 0, "pending": 0}

    for kind, stub_f, ilspy_f, ours_f, ours_cls, jkind in CLASSES:
        if kind_filter and kind_filter not in (kind, jkind):
            continue
        stats["classes"] += 1
        stub_path = os.path.join(ORIG_STUB_DIR, stub_f)
        ilspy_path = os.path.join(ORIG_ILSPY_DIR, ilspy_f)
        ours_path = os.path.join(OURS_DIR, ours_f) if ours_f else OUR_CONTROLLER
        stub = parse_cs(stub_path) or {}
        ilspy = parse_cs(ilspy_path) or {}
        ours = parse_cs(ours_path) or {}

        # 原版顶层字段（含 actionStart 等基类字段由调用方另行补）
        orig_fields = {}
        for qname, flist in stub.items():
            if qname == kind or qname.endswith("." + kind):
                for f in flist:
                    orig_fields.setdefault(f["name"], f)
        ilspy_fields = {}
        for qname, flist in ilspy.items():
            if qname == kind or qname.endswith("." + kind):
                for f in flist:
                    ilspy_fields.setdefault(f["name"], f)

        # 原版基类 `AnimFXModuleBase` 的字段：类型补全用（数据里的 actionStart 就是基类字段）
        if kind not in ("AnimFXModuleBase", "AnimFXController"):
            for src in (parse_cs(os.path.join(ORIG_STUB_DIR, "AnimFXModuleBase.cs")) or {},
                        parse_cs(os.path.join(ORIG_ILSPY_DIR, "AnimFXModuleBase.cs")) or {}):
                for f in src.get("AnimFXModuleBase", []):
                    orig_fields.setdefault(f["name"], f)
                    ilspy_fields.setdefault(f["name"], f)

        # 原版序列化名集合：数据里有的用数据（第一判据）；数据 0 实例时退到 [SerializeField]
        serialized = set()
        data_backed = False
        if jkind and jkind in jkeys:
            data_backed = True
            serialized = set(top_level_names(jkeys[jkind]))
        for name, f in orig_fields.items():
            if any("SerializeField" in a for a in f["attrs"]):
                serialized.add(name)
        # 基类字段（原版 AnimFXModuleBase）对每个模块都存在
        if kind != "AnimFXModuleBase" and kind != "AnimFXController":
            serialized.add("actionStart")

        our_fields = {}
        if ours_cls in ours:
            for f in ours[ours_cls]:
                if f["static"] or f["const"]:
                    continue
                our_fields[f["name"]] = f
        elif ours:
            # 主类名对不上时取第一个非嵌套顶层类（并在交叉核对里报一声）
            top = [q for q in ours.keys() if "." not in q]
            if top:
                crosscheck.append("%s: 我方文件里找不到类 `%s`（实际顶层类 %s）" % (kind, ours_cls, ",".join(sorted(top))))
                for f in ours[top[0]]:
                    if f["static"] or f["const"]:
                        continue
                    our_fields[f["name"]] = f

        # 继承自 WFEffectModule 的字段（原版 `AnimFXModuleBase` 对应的那份）也算「我方字段」
        if kind not in ("AnimFXModuleBase", "AnimFXController") and base_fields:
            for bname, bf in base_fields.items():
                if bname not in our_fields:
                    bf = dict(bf)
                    bf["base"] = True
                    our_fields[bname] = bf

        assigns = read_assignments(ours_path)
        assign_by_field = {}
        for fld, key, ln in assigns:
            assign_by_field.setdefault(fld, []).append((key, ln))
        read_keys = {}
        for fld, key, ln in assigns:
            k = key.strip(".")
            k = k.split(".")[-1]
            read_keys.setdefault(k, []).append((fld, ln))

        used_our = set()
        for name in sorted(serialized):
            if name == "__node":
                continue
            stats["orig_serialized"] += 1
            of = orig_fields.get(name) or ilspy_fields.get(name)
            otype = of["type"] if of else "?"
            entry = None
            # A. 同名
            if name in our_fields:
                uf = our_fields[name]
                verdict = type_verdict(otype, uf["type"])
                evsrc = "WFEffectModule.cs" if uf.get("base") else os.path.basename(ours_path)
                entry = (uf["name"], uf["type"], "同名", "%s:%d" % (evsrc, uf["line"]), verdict)
                used_our.add(name)
                stats["mapped"] += 1
            # B. 手工对应（ALIASES）—— 优先于「按读取点自动配对」：一条原版名可能同时被
            #    好几个我方字段读到（如 `materialAnimations` 既读到条数、又经 ReadTweens 装配实体）。
            if entry is None:
                for akind, aname, afld, aev, anote in ALIASES:
                    if akind == kind and aname == name and afld in our_fields:
                        uf = our_fields[afld]
                        entry = (uf["name"], uf["type"], "改名(%s)" % anote, aev,
                                 type_verdict(otype, uf["type"]))
                        used_our.add(afld)
                        stats["renamed"] += 1
                        stats["mapped"] += 1
                        break
            # C. 改名（按我方代码里的读取点 `字段 = def.GetXxx("原版名"`）
            if entry is None and name in read_keys:
                for fld, ln in read_keys[name]:
                    if fld not in our_fields:
                        continue
                    uf = our_fields[fld]
                    verdict = type_verdict(otype, uf["type"])
                    entry = (uf["name"], uf["type"], "改名", "%s:%d" % (os.path.basename(ours_path), ln), verdict)
                    used_our.add(fld)
                    stats["renamed"] += 1
                    stats["mapped"] += 1
                    break
            # D. 嵌套拍平：我方按 `原版名.子键` 读（例 `cardAnim.m_AssetGUID`）
            if entry is None:
                found = None
                for fld, keys in sorted(assign_by_field.items()):
                    for key, ln in keys:
                        if key.strip(".").startswith(name + ".") and fld in our_fields:
                            found = (fld, key.strip("."), ln)
                            break
                    if found:
                        break
                if found:
                    fld, key, ln = found
                    uf = our_fields[fld]
                    entry = (uf["name"], uf["type"], "改名(嵌套子键 %s)" % key,
                             "%s:%d" % (os.path.basename(ours_path), ln),
                             type_verdict(otype, uf["type"]))
                    used_our.add(fld)
                    stats["renamed"] += 1
                    stats["mapped"] += 1
            if entry is None:
                stats["missing"] += 1
                diffs["缺字段"].append((kind, name, otype,
                                        "数据键" if data_backed and name in top_level_names(jkeys.get(jkind, set())) else "类型桩 [SerializeField]"))
                continue
            ufname, utype, how, ev, verdict = entry
            verdict_out = verdict
            if verdict.startswith("同名不同义"):
                stats["same_name_diff_meaning"] += 1
                diffs["同名不同义"].append((kind, name, otype, ufname, utype, ev + " · " + verdict))
                verdict_out = "同名不同义"
            elif verdict.startswith("待定") or verdict == "待核":
                stats["pending"] += 1
                diffs["待定"].append((kind, name, otype, ufname, utype, ev + " · " + verdict))
                verdict_out = "待定"
            rows.append((kind, name, otype, ufname, utype, how, ev, verdict_out))
            # 一对多①：同名字段之外，我方还有一个字段专门承接同一条序列化引用的**字符串形态**
            if how == "同名" and name in read_keys:
                for fld, ln in read_keys[name]:
                    if fld in our_fields and fld != ufname:
                        uf2 = our_fields[fld]
                        used_our.add(fld)
                        stats["renamed"] += 1
                        stats["mapped"] += 1
                        rows.append((kind, name, otype, uf2["name"], uf2["type"],
                                     "改名(同名字段之外另接引用)",
                                     "%s:%d" % (os.path.basename(ours_path), ln),
                                     type_verdict(otype, uf2["type"])))
                        break
            # 一对多②：嵌套引用的子键可能被拆进多个字段（`cardAnim.m_AssetGUID/.m_SubObjectName/…`）
            if how.startswith("改名(嵌套子键"):
                for fld, keys in sorted(assign_by_field.items()):
                    hit = None
                    for key, ln in keys:
                        k2 = key.strip(".")
                        if k2.startswith(name + ".") and fld in our_fields and fld not in used_our:
                            hit = (k2, ln)
                            break
                    if not hit:
                        continue
                    uf2 = our_fields[fld]
                    used_our.add(fld)
                    stats["renamed"] += 1
                    stats["mapped"] += 1
                    rows.append((kind, name, otype, uf2["name"], uf2["type"],
                                 "改名(嵌套子键 %s)" % hit[0],
                                 "%s:%d" % (os.path.basename(ours_path), hit[1]),
                                 type_verdict(otype, uf2["type"])))

        # 我方独有字段：先排除「原版同名运行态字段」（非序列化，本来就不在数据里）
        for fname in sorted(our_fields):
            if fname in used_our:
                continue
            f = our_fields[fname]
            if f.get("base"):
                continue
            ev = "%s:%d" % (os.path.basename(ours_path), f["line"])
            if fname in orig_fields or fname in ilspy_fields:
                diffs["运行态对应"].append((kind, fname, f["type"], ev, "原版同名运行态字段（非序列化）"))
                continue
            if fname.startswith("_"):
                diffs["私有运行态"].append((kind, fname, f["type"], ev, "我方私有实现状态（下划线前缀，不参与数据映射）"))
                continue
            note = EXTRA_NOTES.get(fname, "")
            stats["extra"] += 1
            diffs["多字段"].append((kind, fname, f["type"], ev, note))

        # 交叉核对：签名桩 vs ilspy
        if stub and ilspy:
            sn = set(orig_fields)
            inn = set(ilspy_fields)
            if sn and inn and sn != inn:
                crosscheck.append("%s: 签名桩∖ilspy=%s · ilspy∖签名桩=%s" %
                                  (kind, sorted(sn - inn), sorted(inn - sn)))

    # 嵌套类型比对
    for stub_rel, stub_cls, ours_rel, ours_cls, evidence in NESTED_PAIRS:
        if kind_filter and kind_filter.lower() not in stub_cls.lower() and kind_filter.lower() not in ours_cls.lower():
            continue
        stub = parse_cs(os.path.join(ORIG_STUB_DIR, stub_rel)) or {}
        ours = parse_cs(os.path.join(OURS_DIR if ours_rel != "AnimFXController.cs" else os.path.dirname(OUR_CONTROLLER), ours_rel)) or {}
        def _find(types, cls):
            if cls in types:
                return types[cls]
            for q in sorted(types):
                if q.split(".")[-1] == cls.split(".")[-1]:
                    return types[q]
            return []

        sf = _find(stub, stub_cls)
        of = _find(ours, ours_cls)
        if not sf and not of:
            continue
        sset = {f["name"]: f for f in sf if not f["static"] and not f["const"]}
        oset = {f["name"]: f for f in of if not f["static"] and not f["const"]}
        nested_rows.append(("==", stub_cls, ours_cls, "", "", evidence, ""))
        aliased = set()
        for n in sorted(sset):
            if n in oset:
                v = type_verdict(sset[n]["type"], oset[n]["type"])
                nested_rows.append(("ok", stub_cls, n, sset[n]["type"], oset[n]["type"], "", v))
            else:
                al = NESTED_FIELD_ALIASES.get((stub_cls.split(".")[-1], n))
                if al and al[0] in oset:
                    aliased.add(al[0])
                    nested_rows.append(("改", stub_cls, n, sset[n]["type"], oset[al[0]]["type"], al[1],
                                        type_verdict(sset[n]["type"], oset[al[0]]["type"])))
                else:
                    nested_rows.append(("缺", stub_cls, n, sset[n]["type"], "-", "", "原版有、我方无"))
        for n in sorted(oset):
            if n not in sset and n not in aliased:
                nested_rows.append(("多", stub_cls, n, "-", oset[n]["type"], "", "我方有、原版无"))

    # ---------------- 打印
    print("AnimFX module field audit（只读）")
    print("  数据: %s  effect_count=%s module_count=%s  kinds=%d" %
          (json_path, data.get("effect_count"), data.get("module_count"), len(jkeys)))
    print("  原版类型: %s  + 交叉核对 %s" % (ORIG_STUB_DIR, ORIG_ILSPY_DIR))
    print("  我方组件: %s" % OURS_DIR)
    print()
    print("== 汇总 ==")
    print("  模块类 %d | 原版序列化字段 %d | 已映射 %d（改名 %d）| 缺字段 %d | 多字段 %d | 同名不同义 %d | 待定 %d"
          % (stats["classes"], stats["orig_serialized"], stats["mapped"], stats["renamed"],
             stats["missing"], stats["extra"], stats["same_name_diff_meaning"], stats["pending"]))
    print()
    if tsv:
        print("类\t原版序列化名\t原版类型\t我方字段\t我方类型\t判定\t证据")
        for r in rows:
            print("\t".join(r))
    else:
        print("== 映射表 ==")
        print("  %-34s %-28s %-16s %-24s %-16s %s" % ("原版类", "原版序列化名", "原版类型", "我方字段", "我方类型", "判定"))
        cur = None
        for kind, name, otype, ufname, utype, how, ev, verdict in rows:
            if kind != cur:
                print("  -- %s" % kind)
                cur = kind
            print("    %-32s %-26s %-14s %-22s %-14s %s  [%s @ %s]"
                  % ("", name, otype, ufname, utype, verdict, how, ev))
    print()
    print("== 差异清单 ==")
    for title in ("缺字段", "多字段", "同名不同义", "名字对不上", "待定", "运行态对应", "私有运行态"):
        items = diffs[title]
        print("  [%s] %d" % (title, len(items)))
        for it in items:
            print("      " + " | ".join(str(x) for x in it))
    if not any(diffs.values()):
        print("      （无）")
    print()
    print("== 嵌套结构（对应关系见脚本 NESTED_PAIRS，依据=字段名+读取点） ==")
    for tag, cls, name, st, ot, ev, note in nested_rows:
        if tag == "==":
            print("  -- %s ↔ %s   (%s)" % (cls, name, ev))
        else:
            print("     %s %-26s %-16s %-16s %s %s" % (tag, name, st, ot, ev, note))
    print()
    print("== 交叉核对（签名桩 vs ilspy）==")
    if crosscheck:
        for c in crosscheck:
            print("  " + c)
    else:
        print("  （无差异；两源字段名一致）")

    bad = stats["missing"] + stats["same_name_diff_meaning"] + stats["pending"]
    if strict:
        return 1 if bad else 0
    return 0


def main(argv):
    if len(argv) <= 1 or argv[1] in ("-h", "--help"):
        usage()
        return 0
    check = "--check" in argv
    tsv = "--tsv" in argv
    strict = "--strict" in argv
    kind = None
    if "--kind" in argv:
        i = argv.index("--kind")
        if i + 1 < len(argv):
            kind = argv[i + 1]
    json_path = DEFAULT_JSON
    if "--json" in argv:
        i = argv.index("--json")
        if i + 1 < len(argv):
            json_path = argv[i + 1]
    if not check:
        usage()
        return 0
    if not os.path.isfile(json_path):
        sys.stderr.write("找不到数据文件: %s\n" % json_path)
        return 2
    return audit(json_path, kind_filter=kind, tsv=tsv, strict=strict)


if __name__ == "__main__":
    sys.exit(main(sys.argv))
