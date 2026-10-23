#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""dump_animfx.py — 把原版特效 prefab 上的 AnimFX 组件（含参数值）导成 JSON

为什么需要
----------
958 个导出 prefab 里有 2,300+ 个 AnimFX 脚本组件是**缺失脚本**（方法体拿不到、
导不出来），所以特效不会按原版的节奏自己播。但**组件的字段值本身是能读的**
（UnityPy 能解出 m_Script 对应的类名和全部字段），这部分数据是还原的基石：

- 走「按语义重写模块」这条路：字段值就是重写后要喂的参数
- 走「Ghidra 反编译」这条路：反编译只给行为，参数还是得从这里来

做法
----
1. 加载全部 84 个源 bundle（AnimFX 组件分散在多个包里，且字段引用的 SO 在
   tweenandshakes 包里，必须全部加载才跟得下去）
2. 按**字段签名**识别组件类型 —— 这些类在 Assembly-CSharp 里，bundle 没有类型树，
   只能靠「字段集合完全匹配」来认（这套办法上一轮验证过，0 歧义）
3. 顺着 m_GameObject → Transform 父链往上走，找到顶层 GameObject = 效果名
4. 字段值里遇到 PPtr 就跟进去，能读出名字的就记名字

产出
----
- `d:/4/Unity/数据/游戏数据/animfx_components.json`
  按效果分组的组件清单 + 每个组件的字段值 + 统计摘要

用法
----
  "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/dump_animfx.py"

  # 🆕 2026-10-23：**干跑**（⛔ 不覆盖正式产物）—— 改深度护栏之后先这么验一遍
  "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/dump_animfx.py" --out _tmp_view/xxx/after.json
"""
import collections
import glob
import json
import os
import re
import sys

import UnityPy

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法\n----\n  "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/dump_animfx.py"\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

BUNDLE_DIR = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
OUT = r"d:/4/Unity/数据/游戏数据/animfx_components.json"

# 字段签名 → 类名。字段名取自 d:/2/Warpforge_code/Scripts/Assembly-CSharp/AnimFX*.cs
# 的 [SerializeField] 列表；每个 module 都继承 AnimFXModuleBase 的 actionStart。
BASE = {"actionStart"}

SIGNATURES = {
    # AnimFXController 直接继承 MonoBehaviour，没有 actionStart
    "AnimFXController": {"sounds", "exitSounds", "preventDestroy", "destroyTime",
                         "modules", "exitDestroyTime"},
    "AnimFXModuleScreenShake": BASE | {"cameraShakes", "manualTriggerCameraShakes"},
    "AnimFXModuleScaleByTarget": BASE | {"particleSystems", "doParentRelation",
                                         "particleSystemsShapeAngle"},
    "AnimFXModuleTween": BASE | {"tweenAnims", "playOnEnable", "triggerOnlyOnce"},
    "AnimFXModuleCardback": BASE | {"particleSystemsToAddCardback"},
    "AnimFXModuleDestroyInTime": BASE | {"destroyTime"},
    "AnimFXModuleDoDestroyAnimation": BASE | {"endAnimationName", "myAnimation",
                                              "destroyOnAnimationEnd", "timeToDestroy"},
    "AnimFXModuleAnimation": BASE | {"simpleAnimation"},
    "AnimFXModuleEvent": BASE | {"eventsToFire"},
    "AnimFXModulePostProcess": BASE | {
        "controlledByAnimation", "animateBloomIntensity", "bloomIntensity",
        "animateBloomThreshold", "bloomThreshold", "doLUTAnim", "lutBlend", "maxLUTBlend",
        "timeToOn", "timeOn", "timeToOff", "customLUT1", "customLUT2",
        "betweenLUTsBlend", "effectPriority"},
    "AnimFXModuleChangeMaterial": BASE | {
        "material", "isCardMaterial", "fade", "initializeWithCardImage",
        "toggleMaterialOffOnExit", "forceRecoverMaterialOnDestroy", "changeTexture",
        "textureMaterialProperty", "materialAnimations", "useCustomRenderers",
        "customRenderers"},
    "AnimFXModuleTransformModifier": BASE | {
        "target", "useEffectAnchorParent", "modifyPosition", "position", "modifyRotation",
        "rotation", "modifyScale", "scale", "transformModifiers",
        "alwaysMaintainWorldScale", "objective", "affectEnemyOnly", "updateContinuously",
        "aimToTargetInUpdate", "updateScaleByBoardPosition", "changeParent",
        "unParentAtStart", "parentAtExit", "setDefaultUnitTransformScaleWhenUnParenting"},
    "AnimFXModuleCollisions": BASE | {
        "particleSystem", "receiveCollisionMessage", "collisionEvent", "collisionPlane",
        "particleSystemsDefinition", "collisionAndParticles"},
    "AnimFxModuleMoveParticlesToTarget": BASE | {
        "delay", "particleSystems", "attractSpeed", "attractSpeedByLifetime",
        "guaranteeFinalPosition", "desiredZPosition"},
    "AnimFXModuleChangeVelocity": BASE | {
        "particleSystem", "applyToVelocityModule", "particleSystems"},
    "AnimFXInstanceParticleAdjacent": BASE | {"cardAnim", "delay", "playOnRetaliation"},
}

MONO_HEADER = {"m_Enabled", "m_GameObject", "m_Name", "m_Script", "object_reader"}


def classify(fields):
    """按「与声明字段的最大交集」认类，返回 (类名, 是否歧义, 交集大小)。

    ⚠️ 不能用「声明字段 ⊆ 实际字段」的全等/子集判定：实测有 375 个组件的字段
    比桩里声明的**少**（`AnimFXModuleCollisions` 只留下 actionStart + collisionAndParticles，
    `AnimFXModuleTransformModifier` 只留下 13 个字段中的部分），子集判定会全部漏掉。
    改用交集打分后这两类都能正确归位。"""
    if not fields:
        return None, False, 0
    scored = sorted(((len(sig & fields), cls) for cls, sig in SIGNATURES.items()), reverse=True)
    best, best_cls = scored[0]
    # 只有 actionStart 一个共同字段说明认不出来，别硬认
    if best < 2:
        return None, False, best
    tied = [c for n, c in scored if n == best]
    return best_cls, len(tied) > 1, best


# ---- 引用解析：把 PPtr 变成「以后还能重新找到它」的东西 --------------------------
#
# 🔴 2026-09-18 修的。原来这里是 `o = v.read(); nm = o.m_Name; ...` —— 而**组件没有 `m_Name`**，
#    于是全部退化成 `{"__ptr__": "ParticleSystem", "type": "ParticleSystem"}`：
#    **只说了类型，没说指向哪一个**。
#    实测 2346 个组件里约 **719 个**带 prefab 内部引用（ScaleByTarget 314 · Collisions 357 ·
#    Cardback 30 · TransformModifier 18），这批数据等于全丢 —— 光有「有个 ParticleSystem 引用」
#    是没法还原的。
#    ⇒ 现在解析成 **「根之下的节点路径 + 组件类型」**（外部资产则给类型与名字），
#      路径约定见 `_path_of`，C# 侧按同一约定解析。

def _go_of(o):
    """从任意对象走到它的 GameObject（组件有 m_GameObject；GameObject 就是它自己）。"""
    if o is None:
        return None
    if type(o).__name__ == "GameObject":
        return o
    p = getattr(o, "m_GameObject", None)
    if p is None:
        return None
    try:
        return p.read()
    except Exception:
        return None


def _transform_pptr(go):
    """拿 GameObject 上那个 Transform 的 **PPtr**（要靠它的 m_PathID 跟 m_Children 比）。"""
    for comp in getattr(go, "m_Component", []) or []:
        try:
            if type(comp.component.read()).__name__ == "Transform":
                return comp.component
        except Exception:
            continue
    return None


def _path_of(go):
    """**根 → 本节点** 的路径，形如 `Root/Card#0/Glow#1`。

    段里的 `#N` = **它在父节点全部子物体里的序号**（即 `Transform.GetSiblingIndex()`），
    为 0 时省略。C# 侧按同一约定解析（`WFEffectModule.ResolvePath`）。
    用「全部兄弟里的下标」而不是「同名兄弟里的第几个」：两边都直接拿
    `m_Children` / `transform.GetChild(i)` 的下标，不必再按名字筛一遍。"""
    parts = []
    cur = go
    guard = 0
    while cur is not None and guard < 256:
        guard += 1
        name = getattr(cur, "m_Name", None) or "?"
        idx = 0
        tr_pp = _transform_pptr(cur)
        parent = None
        if tr_pp is not None:
            try:
                f = getattr(tr_pp.read(), "m_Father", None)
                if f is not None and getattr(f, "m_PathID", 0):
                    ftr = f.read()
                    for i, ch in enumerate(getattr(ftr, "m_Children", []) or []):
                        if getattr(ch, "m_PathID", None) == getattr(tr_pp, "m_PathID", None):
                            idx = i
                            break
                    pg = getattr(ftr, "m_GameObject", None)
                    parent = pg.read() if pg is not None else None
            except Exception:
                parent = None
        parts.append("%s#%d" % (name, idx) if idx else name)
        cur = parent
    parts.reverse()
    return "/".join(parts)


def ptr_ref(pp):
    """PPtr → 可定位的引用。三种情况：
       · 指向 prefab 内部对象 → `{"kind":"node","path":"Root/Card#0/Glow#1","component":"ParticleSystem"}`
       · 指向资产（SO/材质/贴图） → `{"kind":"asset","type":"UnitTweenSO","name":"Impact Light Tween"}`
       · 解析不出来 → `{"kind":"unresolved","file":…,"path_id":…}`（**留着 file/path_id 好复查**）
    """
    fid = getattr(pp, "m_FileID", None)
    pid = getattr(pp, "m_PathID", None)
    try:
        o = pp.read()
    except Exception:
        return {"kind": "unresolved", "file": fid, "path_id": pid}
    tn = type(o).__name__
    if tn == "UnknownObject":
        return {"kind": "unresolved", "type": tn, "file": fid, "path_id": pid}
    go = _go_of(o)
    if go is not None:
        return {"kind": "node", "path": _path_of(go), "component": tn}
    return {"kind": "asset", "type": tn, "name": getattr(o, "m_Name", None)}


# ---- 深度护栏 ----------------------------------------------------------------
#
# 🔴 2026-09-18 从 4 提到 6：**原来 4 太浅了**，`collisionAndParticles[0].particleSystemsDefinition[0]`
#    `.particleSystem` 正好落在第 5 层被写成 `<深>` ⇒ **449 个碰撞定义里可见 0 个**，
#    `AnimFXModuleCollisions` 根本没有平面可以挂到粒子系统上（实现它的代理实测发现的）。
#    ⇒ 现在留到 6；再深说明结构变了，**宁可标出来也别给错值**
#      （`gen_animfx_modules.py` 见到 `<深>` 就把这个键跳过）。
DEFAULT_MAX_DEPTH = 6

# 🔴 2026-10-16（A828）**只对 `collisionEvent` 这一条路径放宽**：
#   实测（本件亲跑 UnityPy，见 `资料/普查产出_1016/W2_A828碰撞屏震.md`）：
#   `particleSystemsDefinition[i]` 这个值落在 depth 4、它那一级 dict 在 5，
#   于是 `collisionEvent`（一个 `UnityEvent`）**恰好落在 depth 6** —— 正好卡在护栏上
#   ⇒ 整棵子树被写成 `<深>`、生成器再把它跳过 ⇒ **原版那 563 条 PersistentCall
#     （559 条非空）一条都没进数据**，`AnimFXModuleCollisions` 装出来的全是空事件
#     （见 `Assets/WarpforgeVFX/Runtime/WFModuleCollisions.cs` 文件头 B）。
#   这棵子树的形状固定且有限：UnityEvent → PersistentCallGroup → PersistentCall[] → 6 个叶子，
#   最深的叶子（`m_Arguments.m_IntArgument`）在 **depth 15** ⇒ 给 18 = 「刚刚够 + 一点余量」。
#   ⛔ **别抬 `DEFAULT_MAX_DEPTH`** —— 上面 4→6 那次的教训就是「一抬就连带放宽几百个别的字段」。
COLLISION_EVENT_MAX_DEPTH = 18

# 🔴 2026-10-14（A1882）**再单独放宽 `references` 这一条路径**（同样⛔ 不抬 `DEFAULT_MAX_DEPTH`）：
#   实测（本件亲跑 UnityPy，读数见 `资料/普查产出_第二十八会话/W14_材质过渡数据链与多点落点.md`）：
#   `references` 是 Unity 的**内联托管引用注册表**（`ManagedReferencesRegistry`：
#   `version` + `RefIds[] = { rid, type{class,ns,asm}, data{…} }`），
#   `AnimFXModuleChangeMaterial.materialAnimations` 里那几个 `MaterialTween*` **就内联在这棵子树里**
#   （不在 `数据/游戏数据/tween/` 那 74 份 `UnitTweenSO` 目录里 —— 当年正是按那份目录搜，
#    才写成了「3 例都是空的」）。而 `data.materialProperty` / `type.class` 落在 **depth 7**，
#   正好卡在默认护栏 6 之外 ⇒ 整块被写成 `<深>` ⇒ **三条材质补间一条都进不了数据**
#   （`WFModuleChangeMaterial.TweenCount == 0`，那段代码是死码）。
#   这棵子树的形状固定且有限：注册表 → RefIds[] → {rid, type, data} → 6~10 个叶子，
#   最深的叶子（`data.startValue` 的 `r/g/b/a`）在 **depth 8** ⇒ 给 10 = 「刚好够 + 一点余量」。
#   ✅ **面**：全库只有 `AnimFXModuleChangeMaterial` 有 `references` 字段（本件扫了 84 个包 / 3 实例），
#      别的模块一条都没有 ⇒ 放宽它**不会连带放过任何别的字段**。
REFERENCES_MAX_DEPTH = 10

# 🔴 2026-10-23（特效线 A1·R25 · 代理 E）**第三条单独放宽的路径：`eventToFire`**。
#   实测（`资料/普查产出_1023/E_R25事件通道.md`）：`AnimFXModuleEvent.eventsToFire[i].eventToFire`
#   是一个 `UnityEvent`，它的 `m_PersistentCalls.m_Calls` 整块被写成 `<深>`
#   ⇒ 生成器跳过 ⇒ `数据/游戏数据/animfx_components.json` 里只剩 `"m_Calls": "<深>"`，
#   于是「这个事件到点到底调什么」一度被记成「原版没有/查不到」（**错**，是本护栏截断的）。
#   实际调什么（直接从 `assets_full` 的 prefab 原始 JSON 读出来的，全库 1 实例 / 2 条）：
#     `AnimFXModuleChangeMaterial.ToggleMaterial(bool=true)`（同一 GameObject）
#     `StoryProgramming.VATGPUPlayer.UpdateMaterials(bool=true)`（子节点 `Vanguard Frame Animated VAT`）
#   这棵子树与 `collisionEvent` **同形**（UnityEvent → PersistentCallGroup → PersistentCall[] → 6 个叶子），
#   最深的叶子 `m_Arguments.m_IntArgument` 同在该深度 ⇒ 同样给 18（只需放一条，别再放宽）。
#   ✅ **面**：`eventToFire`（单数）全库**只有** `AnimFXModuleEvent.FireEvent` 这一个字段
#      （`grep -rn "eventToFire" d:/2/Warpforge_code/Scripts/` 命中 1 处 = `AnimFXModuleEvent.cs:11`），
#      且它只出现在 `eventsToFire[i]` 之下 ⇒ 放宽它**不会连带放过任何别的字段**。
#   ⛔ **别抬 `DEFAULT_MAX_DEPTH`**（4→6 那次的教训就是「一抬就连带放宽几百个别的字段」）。
EVENT_TO_FIRE_MAX_DEPTH = 18


def _depth_limit(path):
    """这一条键路径允许用到第几层。
    **只放宽三条子树**（`collisionEvent` / `references` / `eventToFire`），其余默认值。"""
    if path:
        for seg in path.split("."):
            if seg.split("[", 1)[0] == "collisionEvent":
                return COLLISION_EVENT_MAX_DEPTH
            if seg.split("[", 1)[0] == "references":
                return REFERENCES_MAX_DEPTH
            if seg.split("[", 1)[0] == "eventToFire":
                return EVENT_TO_FIRE_MAX_DEPTH
    return DEFAULT_MAX_DEPTH


def _child(path, key):
    """子键的路径（`path` 为 None 时从这一层起算；顶层字段名由 main 传进来）。"""
    return path + "." + key if path else key


# ---- `AnimationCurve`：按结构逐键导出（W64 · B13）--------------------------------
#
# 🔴 2026-10-22（`W64_兜底审计_特效.md` **B13**）：
#   原来 `AnimationCurve` 落到 `short()` 最后那行 `{"__repr__": repr(v)[:120]}` ——
#   **被截断的文本**，只读得到第一个键 ⇒ 曲线上半段（含 `t=1, v=6` 那个端点）整块丢失，
#   运行时只能退成常值（`WFModuleMoveParticlesToTarget` 里那段 `ParseCurveRepr` + 警告）。
#   原版真值 = bundle 里**完整的 `AnimationCurve`**（UnityPy 1.24 把它读成 attrs 类：
#   `vars()` 不可用，但**字段可逐个取**：`m_Curve` 是 `Keyframe` 列表，
#   每个 `Keyframe` 有 `time/value/inSlope/outSlope/inWeight/outWeight/weightedMode`）。
#   ⇒ 这里按**字段**导出：浮点原样（UnityPy 给的是 float32 的精确 double，`json` 往返不丢），
#   **键序固定 = Unity `Keyframe` 的序列化顺序**（⛔ 不按字典序，别让下游以为顺序有意义）。
#
# ⚠️ **同时保留 `__repr__`（完整、不截断）**：既有消费者 `WFModuleMoveParticlesToTarget.Configure()`
#   读的是 `def.GetString("attractSpeedByLifetime.__repr__")`，删掉它会把曲线**静默**退成常值。
#   两路都写 = 老消费者零改动、结构化数据（`…m_Curve[i].time` 等键）随时可切。
#   📌 待办（不在本批文件所有权内）：那个 C# 改成读结构化键，然后把 `__repr__` 这半删掉。
CURVE_KEYFRAME_ATTRS = ("time", "value", "inSlope", "outSlope", "inWeight", "outWeight",
                        "weightedMode")


def _int_like(x):
    """`m_PreInfinity` / `m_RotationOrder` 这类枚举值：UnityPy 有时给 int 有时给枚举包装。"""
    try:
        return int(x)
    except (TypeError, ValueError):
        return x


def curve_to_dict(v):
    """`AnimationCurve` → 结构化字典（完整键 + 三档外插/旋转序）。键序固定。"""
    rows = []
    for kf in getattr(v, "m_Curve", None) or []:
        row = {}
        for a in CURVE_KEYFRAME_ATTRS:
            val = getattr(kf, a, None)
            row[a] = float(val) if isinstance(val, float) else _int_like(val)
        rows.append(row)
    return {
        "m_Curve": rows,
        "m_PostInfinity": _int_like(getattr(v, "m_PostInfinity", None)),
        "m_PreInfinity": _int_like(getattr(v, "m_PreInfinity", None)),
        "m_RotationOrder": _int_like(getattr(v, "m_RotationOrder", None)),
        # ⚠️ 完整 repr（**不截断**），逐字给上面那个既有消费者用 —— 见本条注释
        "__repr__": repr(v),
    }


def short(v, depth=0, path=None):
    """把字段值压成 JSON 友好的短表示；PPtr 解析成**可定位的引用**（见 ptr_ref）。

    `path` = 这个值在字段树里的点号键路径（**只用来决定深度护栏**，见上面两条常量）。"""
    if depth > _depth_limit(path):
        return "<深>"
    t = type(v).__name__
    if t in ("int", "float", "bool", "str"):
        return v
    if v is None:
        return None
    if t == "PPtr":
        return ptr_ref(v)
    # 🔴 2026-10-22（W64 · B13）：`AnimationCurve` **按结构导出**（完整曲线 + 完整 repr）——
    #   见 `curve_to_dict` 那段注释。放在 `vars()` 兜底**之前**：它没有 `__dict__`。
    if t == "AnimationCurve":
        return curve_to_dict(v)
    if t in ("list", "tuple"):
        return [short(x, depth + 1, path) for x in v[:64]]
    if t == "dict":
        return {k: short(vv, depth + 1, _child(path, k)) for k, vv in v.items()}
    if t == "UnknownObject":
        # 🔴 2026-09-18 修的：原来这里写的是 `{"__unknown__": repr(v)[:160]}` —— 当成「拿不到值」，
        #    实测**值就在 `v.__dict__` 里**（UnityPy 的 `__repr__` 自己就是遍历它打出来的，
        #    只是每个值截到 100 字符）。而这一批正是**最值钱的那些**：
        #    `cameraShakes` 508 · `collisionAndParticles` 450 · `sounds` 937（合计占 1919 处）。
        #    ⇒ 递归进 `__dict__`（剔掉 `__node__`，那是类型节点不是数据），照常走 short。
        d = {k: vv for k, vv in vars(v).items() if k != "__node__"}
        return short(d, depth + 1, path)
    try:
        d = vars(v)
    except Exception:
        d = None
    if d:
        out = {}
        for k, vv in d.items():
            if k in ("object_reader",) or k.startswith("m_") and k not in ("m_Name",):
                continue
            out[k] = short(vv, depth + 1, _child(path, k))
        if out:
            return out
    # UnityPy 的数学类型（`Vector3f(0.0, 0.0, 0.0)` / `Quaternionf(...)`）没有 `__dict__`，
    # 但 repr 是规整的 `名字(数, 数, 数)` —— 直接拆。原版拿这个存方向/位置/旋转。
    m = re.match(r"^([A-Za-z_]\w*)\(([-+0-9eE.,\s]*)\)$", repr(v)[:160])
    if m:
        nums = [x.strip() for x in m.group(2).split(",") if x.strip()]
        if 2 <= len(nums) <= 4:
            axes = ("x", "y", "z", "w")
            try:
                return {axes[i]: float(nums[i]) for i in range(len(nums))}
            except ValueError:
                pass
    # 🔴 2026-10-14（A1882）：**带名字的**那种 repr（`ColorRGBA(r=0.0, g=0.0, b=0.0, a=1.0)`）
    #   上面那条正则**匹配不上**（`r=` 的等号不在 `[-+0-9eE.,\s]` 里）⇒ 整块退成 `{"__repr__": …}`，
    #   而 `ColorRGBA` **没有 `__dict__`**（`vars()` 抛异常，本件实测）⇒ 连 `vars()` 兜底也接不住。
    #   `MaterialTweenColor` 的 `startValue`/`finalValue` 正是这个形状，C# 侧读的是
    #   `.startValue.r/.g/.b/.a`（键名约定 → `WFModuleChangeMaterial.WFMatTween` 的注释）
    #   ⇒ 不拆出来，那一条 Color 补间就只有一串文字、进不了数据。
    #   ⚠️ 只在**全部都是「名字=数」**时才认（`AnimationCurve(m_Curve=[Keyframe(…` 那种不匹配，
    #     照旧走 `__repr__` 兜底）—— 本件核过：全库现有 `__repr__` 只有 1 处，是 `AnimationCurve`。
    mn = re.match(r"^([A-Za-z_]\w*)\(((?:[A-Za-z_]\w*=[-+0-9eE.]+)"
                  r"(?:\s*,\s*[A-Za-z_]\w*=[-+0-9eE.]+)*)\)$", repr(v)[:160])
    if mn:
        try:
            # ⚠️ 分隔符是 `", "`（带空格）⇒ 键名**必须 strip**，否则会写出 `" g"` 这种键，
            #    C# 侧按 `.g` 取就取不到（本件踩过一次：先写成 `" g"`，回读才发现）。
            return {kv.split("=", 1)[0].strip(): float(kv.split("=", 1)[1])
                    for kv in mn.group(2).split(",")}
        except ValueError:
            pass
    return {"__repr__": repr(v)[:120]}


def main():
    # 🆕 2026-10-23（A1·R25）：`--out <路径>` ⇒ 干跑到别处，**不覆盖正式产物**
    #   （改深度护栏这种东西必须能先看「新增/变化了哪些键」再决定要不要落盘）。
    out = OUT
    argv = sys.argv[1:]
    for i, a in enumerate(argv):
        if a == "--out":
            if i + 1 >= len(argv):
                print("!! --out 后面要跟路径")
                return 2
            out = argv[i + 1]
    if os.path.abspath(out) == os.path.abspath(OUT):
        print("[0] ⚠ 输出 = 正式产物（会覆盖）。干跑请加 `--out _tmp_view/…`")

    files = sorted(glob.glob(os.path.join(BUNDLE_DIR, "*.bundle")))
    print(f"[1] 加载 {len(files)} 个 bundle …")
    env = UnityPy.load(*files)

    # 🔴 2026-10-07（A161 ③）：这里原来建了一张**全库 `path_id → GameObject` 索引**（`go_by_pid`），
    #   而它**从头到尾没有任何地方读**（死代码；`root_name_of()` 走的是 PPtr `.read()` 自己解析）。
    #   ⛔ **别按 pid 全局查 GameObject**：pid 是**分包局部**的，全 84 包里「同 pid 不同名」的
    #   GameObject 有 **1,291** 条（来源全是 `scenes_*`，每个场景包的主 CAB 都从 pid=1 重新编号）
    #   ⇒ 建了没用只是浪费，谁接上去用就会**静默取错**（上了膛的枪）。要反查请按 **`(包, pid)`**。
    #   判据：`资料/普查产出_1006/A152_pid陷阱普查.md` B9 · `资料/已知的坑.md`。

    def root_name_of(mb_obj):
        """顺 m_GameObject → Transform 父链往上找顶层 GameObject 名"""
        try:
            d = mb_obj.read()
            go_ptr = getattr(d, "m_GameObject", None)
            cur = go_ptr.read() if go_ptr else None
        except Exception:
            return None
        seen = 0
        while cur is not None and seen < 64:
            seen += 1
            parent = None
            for comp in getattr(cur, "m_Component", []) or []:
                try:
                    c = comp.component.read()
                except Exception:
                    continue
                if type(c).__name__ == "Transform":
                    pt = getattr(c, "m_Father", None)
                    if pt and getattr(pt, "m_PathID", 0) != 0:
                        try:
                            ptc = pt.read()
                            pg = getattr(ptc, "m_GameObject", None)
                            if pg:
                                parent = pg.read()
                        except Exception:
                            parent = None
                    break
            if parent is None:
                return getattr(cur, "m_Name", None)
            cur = parent
        return None

    print("[2] 扫描 MonoBehaviour …")
    found = collections.Counter()
    ambiguous = collections.Counter()
    by_effect = collections.defaultdict(list)
    unknown_base = []
    for o in env.objects:
        if o.type.name != "MonoBehaviour":
            continue
        try:
            d = o.read()
        except Exception:
            continue
        names = set(vars(d).keys()) - MONO_HEADER
        cls, amb, _score = classify(names)
        if cls is None:
            if BASE in names or "actionStart" in names:
                unknown_base.append(sorted(names))
            continue
        if amb:
            ambiguous[cls] += 1

        eff = root_name_of(o) or "<无父级>"
        fields = {}
        for k, v in vars(d).items():
            if k in MONO_HEADER:
                continue
            # `path` 从**顶层字段名**起算 —— 深度护栏只认 `collisionEvent` 那一条路径（见文件上部）
            fields[k] = short(v, 0, k)
        found[cls] += 1
        # 🔴 2026-09-18 加的：**模块自己挂在哪个节点上也要记**。原版模块是挂在具体 GameObject 上的
        #    （多数就是根，但不保证），不记的话运行时只能全挂到根上 —— `TransformModifier` 这类
        #    要按「自己所在的 Transform」取值的模块就会取错，而且错得很安静。
        #    路径约定与引用解析一致（`_path_of`：根起、带兄弟序号）。
        go_self = _go_of(d)
        node_path = _path_of(go_self) if go_self is not None else ""
        by_effect[eff].append({"class": cls, "ambiguous": amb,
                               "nodePath": node_path, "fields": fields})

    print("\n[3] 组件统计（按字段签名识别）：")
    for k, v in found.most_common():
        flag = "  ⚠有歧义" if ambiguous.get(k) else ""
        print(f"      {v:6d}  {k}{flag}")
    print(f"      ------")
    print(f"      {sum(found.values()):6d}  合计")

    if unknown_base:
        u = collections.Counter(tuple(x) for x in unknown_base)
        print(f"\n[3b] 有 actionStart 但没匹配上任何类：{len(unknown_base)} 个，字段组合前 8：")
        for sig, n in u.most_common(8):
            print(f"      {n:5d}  {sig}")

    multi = {k: v for k, v in by_effect.items() if len(v) > 1}
    print(f"\n[4] 涉及 {len(by_effect)} 个效果；其中 {len(multi)} 个挂了不止一个模块")

    os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
    with open(out, "w", encoding="utf-8") as f:
        json.dump({
            "summary": dict(found),
            "effect_count": len(by_effect),
            "effects": {k: v for k, v in sorted(by_effect.items())},
        }, f, ensure_ascii=False, indent=1)
    print(f"\n[5] 已写出 {out}  ({os.path.getsize(out)/1024:.0f} KB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
