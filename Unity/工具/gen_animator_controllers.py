#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_animator_controllers.py —— 把原版 `AnimatorController` 摊成一份**能照着建**的数据表。

## 为什么需要它（2026-10-01 踩出来的）

原版那两个阵亡爆散体 prefab（`Card 3D Death Explosion` / `Necrons death explosion`）
挂的 `Animator.m_Controller` 指向一份 **bundle 资产**。`PrefabUtility.SaveAsPrefabAsset`
落不下盘 ⇒ 导出产物里是 `guid: 00000000000000000000000000000000` 的伪引用 ⇒ **运行时 null、
动画一帧不播**。

而 Unity **不肯把 bundle 里那份控制器交给我们**：

  · `AssetDatabase.LoadAssetAtPath<AnimatorController>` / `Object.Instantiate` + `CreateAsset`
    落出来的 `.controller`，读回来是 **`0 层 0 状态`**（空壳）—— 实测两次；
  · 原因：包里那份是**运行时格式**（`m_Controller` / `m_TOS` / `m_StateMachineArray`），
    工程 `.controller` 要的是**编辑器格式**（`m_AnimatorLayers` / `m_AnimatorParameters`），
    **两套字段不是一回事**，没有公开 API 能跨过去。

⇒ 唯一的路 = **把运行时格式解出来，再用编辑器 API 照着建一份**。这份 json 就是那个「解出来的结果」。

## 范围（实测）

`d:/2/新解包资源/assets_full/` 全库**只有 6 份** `AnimatorController`：

| 名字 | 在哪个包 | 谁在用 |
|---|---|---|
| `Card 3D WH40K Explosion` | battleprefabs_vfxandmisc | **两个阵亡爆散体**（我们要的那份） |
| `Battle Arena Leviathan` | battlesharedresources | 战场「利维坦」的触手待机 |
| `Cannon 1` | scenes_battlearena1 | 战场炮台待机 |
| `Pump 1` · `Dynamic Skulls` · `Battle Arena 2` | scenes_battlearena2 | 战场道具待机 |

六份**结构完全同构**：1 层 `Base Layer` · 1 个状态 · 状态的动作 = **单节点 1D 混合树** ·
0 参数 · 0 过渡。脚本按这个形状解；**碰到解不动的形状会打出来、不静默**（`_unhandled`）。

## 输出

`d:/4/Unity/数据/游戏数据/animator_controllers.json`

字段（逐条都有出处，全部实读自 `assets_full/<包>/AnimatorController/<pid>.json`）：

```
controllers[]: name · source(包目录名) · pathId
  clips[]          该控制器的 `m_AnimationClips`（PathID → 名字，跨包查表）
  parameters[]     `m_Controller.m_Values.m_ValueArray`
  layers[]: name(`m_TOS[m_Binding]`) · defaultWeight · blendingMode · ikPass · syncedLayerIndex
    states[]: name(`m_TOS[m_NameID]`) · speed · cycleOffset · mirror · writeDefaultValues
              · ikOnFeet · loop(`m_Loop`) · blendType · blendDuration(`m_NodeArray[0].m_Duration`)
              · clip(节点的 `m_ClipID` → `clips[]` 里的名字) · transitions[]（逐字段）
    defaultState   状态下标（`m_DefaultState`）
    anyStateTransitions[]
```

用法：
    "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_animator_controllers.py
加 `--check` = 只体检、不写文件。
"""

import json
import os
import sys

ASSETS_FULL = r"d:/2/新解包资源/assets_full"
OUT = r"d:/4/Unity/数据/游戏数据/animator_controllers.json"

BLEND_TYPES = {0: "Simple1D", 1: "SimpleDirectional2D", 2: "FreeformDirectional2D",
               3: "FreeformCartesian2D", 4: "Direct"}


def load(p):
    with open(p, encoding="utf-8") as f:
        return json.load(f)


def clip_names_in(bundle_dir):
    """`<包>/AnimationClip/*.json` → {PathID: 名字}（含同包内其它 CAB 的）。"""
    out = {}
    d = os.path.join(bundle_dir, "AnimationClip")
    if not os.path.isdir(d):
        return out
    for fn in os.listdir(d):
        if not fn.startswith("AnimationClip_") or not fn.endswith(".json"):
            continue
        try:
            pid = int(fn[len("AnimationClip_"):-len(".json")])
        except ValueError:
            continue
        try:
            o = load(os.path.join(d, fn))
        except Exception:
            continue
        n = o.get("m_Name")
        if n is not None:
            out[pid] = n
    return out


def ptr_id(v):
    if isinstance(v, dict):
        return v.get("m_PathID")
    return None


def build(bundle_dir, ctrl_path, clip_index):
    """把一份控制器摊成数据。返回 (dict, unhandled 列表)。"""
    d = load(ctrl_path)
    bad = []
    name = d.get("m_Name") or ""
    tos = {int(a): b for a, b in (d.get("m_TOS") or [])}

    def nm(i):
        return tos.get(int(i), "")

    clips_pid = [ptr_id(x) for x in (d.get("m_AnimationClips") or [])]
    clips = []
    for pid in clips_pid:
        n = clip_index.get(pid)
        if n is None:
            bad.append(f"{name}: `m_AnimationClips` 里的 PathID {pid} 在解包树里查不到名字")
            clips.append("")
        else:
            clips.append(n)

    c = d.get("m_Controller") or {}
    sms = c.get("m_StateMachineArray") or []

    # ---- 参数 ----
    params = []
    vals = ((c.get("m_Values") or {}).get("data") or {}).get("m_ValueArray") or []
    for v in vals:
        vd = v.get("data", v)
        params.append({"nameID": vd.get("m_NameID"), "type": vd.get("m_Type"),
                       "defaultFloat": vd.get("m_DefaultFloat"), "defaultInt": vd.get("m_DefaultInt"),
                       "defaultBool": vd.get("m_DefaultBool")})
    if params:
        bad.append(f"{name}: 有 {len(params)} 个参数 —— **参数的名字要另一张表**（`m_Values` 里只有 id），"
                   "当前没人用到，先如实留着")

    layers = []
    for L in (c.get("m_LayerArray") or []):
        ld = L.get("data", L)
        smi = ld.get("m_StateMachineIndex", 0)
        sm = (sms[smi].get("data", {}) if smi < len(sms) else {})
        states = []
        for st in (sm.get("m_StateConstantArray") or []):
            s = st.get("data", st)
            tree = s.get("m_BlendTreeConstantIndexArray") or []
            clip, btype, bdur = "", None, None
            if len(tree) == 1:
                arr = s.get("m_BlendTreeConstantArray") or []
                if tree[0] < len(arr):
                    nodes = (arr[tree[0]].get("data", {}) or {}).get("m_NodeArray") or []
                    if len(nodes) == 1:
                        nd = nodes[0].get("data", {})
                        cid = nd.get("m_ClipID", -1)
                        clip = clips[cid] if 0 <= cid < len(clips) else ""
                        btype = nd.get("m_BlendType")
                        bdur = nd.get("m_Duration")
                        if nd.get("m_ChildIndices"):
                            bad.append(f"{name}/{nm(s.get('m_NameID'))}: 混合树有子节点索引，不认")
                    else:
                        bad.append(f"{name}/{nm(s.get('m_NameID'))}: 混合树有 {len(nodes)} 个节点"
                                   "（不是单节点），只取第一个前的形状没实现")
            elif tree:
                bad.append(f"{name}/{nm(s.get('m_NameID'))}: 有 {len(tree)} 个混合树")
            trans = []
            for t in (s.get("m_TransitionConstantArray") or []):
                td = t.get("data", t)
                conds = []
                for cd in ((td.get("m_ConditionConstantArray") or [])):
                    cdd = cd.get("data", cd)
                    conds.append({"mode": cdd.get("m_ConditionMode"),
                                  "eventID": cdd.get("m_EventID"),
                                  "threshold": cdd.get("m_EventThreshold")})
                trans.append({"destState": td.get("m_DestinationState"),
                              "fullPathID": td.get("m_FullPathID"),
                              "duration": td.get("m_TransitionDuration"),
                              "exitTime": td.get("m_TransitionExitTime"),
                              "offset": td.get("m_TransitionOffset"),
                              "hasExitTime": td.get("m_HasExitTime"),
                              "hasFixedDuration": td.get("m_HasFixedDuration"),
                              "interruptionSource": td.get("m_InterruptionSource"),
                              "orderedInterruption": td.get("m_OrderedInterruption"),
                              "canTransitionToSelf": td.get("m_CanTransitionToSelf"),
                              "conditions": conds})
                if conds:
                    bad.append(f"{name}/{nm(s.get('m_NameID'))}: 过渡上带条件 —— "
                               "条件用的是 **eventID**（要另一张表才认得出参数名）")
            if trans:
                bad.append(f"{name}/{nm(s.get('m_NameID'))}: 有 {len(trans)} 条过渡（本表已逐字段收，"
                           "但 C# 侧这一版**只建状态与默认状态**，别当它搬过去了）")
            states.append({"name": nm(s.get("m_NameID")),
                           "path": nm(s.get("m_PathID", 0)),
                           "speed": s.get("m_Speed"),
                           "cycleOffset": s.get("m_CycleOffset"),
                           "mirror": bool(s.get("m_Mirror")),
                           "writeDefaultValues": bool(s.get("m_WriteDefaultValues")),
                           "loop": bool(s.get("m_Loop")),
                           "ikOnFeet": bool(s.get("m_IKOnFeet")),
                           "tagID": s.get("m_TagID"),
                           "blendType": btype,
                           "blendTypeName": BLEND_TYPES.get(btype, str(btype)),
                           "blendDuration": bdur,
                           "clip": clip,
                           "transitions": trans})
        anyt = sm.get("m_AnyStateTransitionConstantArray") or []
        if anyt:
            bad.append(f"{name}: 有 {len(anyt)} 条 AnyState 过渡（同上，没收进 C#）")
        if sm.get("m_SynchronizedLayerCount", 0) > 1:
            bad.append(f"{name}: `m_SynchronizedLayerCount` = {sm.get('m_SynchronizedLayerCount')}")
        layers.append({"name": nm(ld.get("m_Binding")),
                       "defaultWeight": ld.get("m_DefaultWeight"),
                       "blendingMode": ld.get("(int&)m_LayerBlendingMode"),
                       "ikPass": bool(ld.get("m_IKPass")),
                       "syncedLayerIndex": ld.get("m_StateMachineSynchronizedLayerIndex"),
                       "defaultState": sm.get("m_DefaultState"),
                       "states": states,
                       "stateMachineName": ""})

    if (d.get("m_StateMachineBehaviours") or []):
        bad.append(f"{name}: 有 {len(d['m_StateMachineBehaviours'])} 个状态机行为（没搬）")

    return ({"name": name,
             "source": os.path.basename(bundle_dir),
             "pathId": None,
             "clips": clips,
             "parameters": params,
             "layers": layers}, bad)


def main():
    check = "--check" in sys.argv
    roots = sorted(os.path.join(ASSETS_FULL, d) for d in os.listdir(ASSETS_FULL)
                   if os.path.isdir(os.path.join(ASSETS_FULL, d)))
    # 先建「整棵树」的 clip 名字索引（控制器可能引用别的包里的 clip）
    clip_index = {}
    for r in roots:
        clip_index.update(clip_names_in(r))

    out, bad = [], []
    for r in roots:
        cd = os.path.join(r, "AnimatorController")
        if not os.path.isdir(cd):
            continue
        for fn in sorted(os.listdir(cd)):
            if not fn.endswith(".json"):
                continue
            c, b = build(r, os.path.join(cd, fn), clip_index)
            try:
                c["pathId"] = int(fn[:-5].split("_")[-1])
            except ValueError:
                pass
            out.append(c)
            bad += b

    print(f"[AC] 扫了 {len(roots)} 个包目录 ⇒ 控制器 **{len(out)}** 份")
    for c in out:
        per = " · ".join(f"{l['name']}:{len(l['states'])}状态" for l in c["layers"])
        clipn = c["layers"][0]["states"][0]["clip"] if c["layers"] and c["layers"][0]["states"] else "?"
        print(f"     {c['name']!r:40s} [{c['source']}] {per} · clip={clipn!r}")
    missing = [c["name"] for c in out
               if not c["layers"] or not c["layers"][0]["states"] or not c["layers"][0]["states"][0]["clip"]]
    print(f"[AC] clip 没解出来的控制器：{missing if missing else '无'}")
    if bad:
        print(f"[AC] ⚠️ 解不动的 {len(bad)} 条（**不静默**）：")
        for b in bad:
            print("     " + b)
    else:
        print("[AC] 解不动的：无")

    doc = {"_note": "由 工具/gen_animator_controllers.py 生成 —— 原版 AnimatorController 的"
                    "运行时格式摊平（工程 .controller 是编辑器格式，Unity 不肯跨界，只能照着建）。"
                    "逐字段出处 = assets_full/<包>/AnimatorController/<pid>.json",
           "_unhandled": bad,
           "count": len(out),
           "controllers": out}
    if check:
        print("[AC] --check：只体检，未写文件")
        return 0 if not missing else 1
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        json.dump(doc, f, ensure_ascii=False, indent=1)
    print(f"[AC] 写出 {OUT}（{os.path.getsize(OUT)} 字节）")
    return 0 if not missing else 1


if __name__ == "__main__":
    sys.exit(main())
