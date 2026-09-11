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
"""
import collections
import glob
import json
import os
import sys

import UnityPy

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


def short(v, depth=0):
    """把字段值压成 JSON 友好的短表示；PPtr 尽量跟进去读名字。"""
    if depth > 2:
        return "<深>"
    t = type(v).__name__
    if t in ("int", "float", "bool", "str"):
        return v
    if v is None:
        return None
    if t == "PPtr":
        try:
            o = v.read()
            nm = getattr(o, "m_Name", None)
            tn = type(o).__name__
            return {"__ptr__": nm if nm else tn, "type": tn}
        except Exception:
            return {"__ptr__": None, "type": "未解析"}
    if t in ("list", "tuple"):
        return [short(x, depth + 1) for x in v[:12]]
    if t in ("UnknownObject",):
        # [SerializeReference] 之类的托管引用，拿不到值，但类型名往往在 repr 里
        return {"__unknown__": repr(v)[:160]}
    try:
        d = vars(v)
    except Exception:
        return {"__repr__": repr(v)[:120]}
    out = {}
    for k, vv in d.items():
        if k in ("object_reader",) or k.startswith("m_") and k not in ("m_Name",):
            continue
        out[k] = short(vv, depth + 1)
    return out or {"__repr__": repr(v)[:120]}


def main():
    files = sorted(glob.glob(os.path.join(BUNDLE_DIR, "*.bundle")))
    print(f"[1] 加载 {len(files)} 个 bundle …")
    env = UnityPy.load(*files)

    # 建 path_id → (类型名, 名字) 索引，给 GameObject 反查用
    go_by_pid = {}
    for o in env.objects:
        if o.type.name == "GameObject":
            go_by_pid[o.path_id] = o

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
            fields[k] = short(v)
        found[cls] += 1
        by_effect[eff].append({"class": cls, "ambiguous": amb, "fields": fields})

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

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as f:
        json.dump({
            "summary": dict(found),
            "effect_count": len(by_effect),
            "effects": {k: v for k, v in sorted(by_effect.items())},
        }, f, ensure_ascii=False, indent=1)
    print(f"\n[5] 已写出 {OUT}  ({os.path.getsize(OUT)/1024:.0f} KB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
