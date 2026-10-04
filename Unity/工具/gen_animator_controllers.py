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

🆕 **2026-10-07（A200）：产物里那一节手写的 `clipsByGuid` —— 本生成器「不认识、但不许冲掉」**

这张表里另有一节 **手写**的 `clipsByGuid`（2 条：`LightAnimationOrbit` ·
`Dark Angels Void Combat animations`）。它记的是「**没有任何控制器引用、只被环境 SO 按
assetGUID 引用**」的动画片段（消费方 `ScenarioAnimationBlend`；打包方
`extract_missing_shaders.py --prefabs` 按它的 `name` 把 clip 收进包、按 `guid` 登记容器别名）。
⚠️ **冲掉它的后果是静默的**：打包时收不到那两条 clip ⇒ 重打包后 GUID 别名消失 ⇒
只在**运行时**才 `LogError`（没人会想到是生成器干的）—— 所以它现在**必须活过每一次重跑**。

本生成器**不产生**这一节（要重建得回到原版 SO 那一跳，而且逐条的 `_ref` / `_note`
是人工结论、生成不出来）⇒ 做法 = **读旧产物、把这一节逐字搬过去**，并逐条核对：

  · `source` 那个包目录在 `assets_full` 里存在
  · `<源包>/AnimationClip/AnimationClip_<pathId>.json` 在，且 `m_Name` 与 `name` 相符
  · `guid` 是 32 位十六进制

核对不过 ⇒ **点名 + 落进 `_unhandled` + 退出码 1**（不静默）。
旧产物**不存在**或**里面没有这一节** ⇒ 同样点名（写出去的那节会是空的）。
旧产物**存在但 JSON 解析不了** ⇒ **拒绝写文件**（否则会把刚手改的编辑一起冲掉），
先修 json 再跑。

⇒ **要新增一条 clip：直接改产物里那节 `clipsByGuid`，再重跑本生成器即可**（原样保住）。

用法：
    "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_animator_controllers.py
加 `--check` = 只体检、不写文件（体检项同样含上面那节 `clipsByGuid`）。
退出码 1 = 有控制器的 clip 没解出来，**或** `clipsByGuid` 那一节核对不过 / 读不到。
"""

import json
import os
import sys

# 🔴 **UTF-8 stdout 兜底**（2026-10-07 · A200）：默认编码是 GBK（cp936）时，打印 `🔴` / `⚠️`
#    会 `UnicodeEncodeError` **整个脚本崩掉**，中文还会全变乱码（终端、`> log.txt` 一样）。
#    （同族先例：`extract_missing_shaders.py` · `_verify_prefab_bundle.py` 顶上那两段。）
try:
    sys.stdout.reconfigure(encoding="utf-8")
except Exception:
    pass

ASSETS_FULL = r"d:/2/新解包资源/assets_full"
OUT = r"d:/4/Unity/数据/游戏数据/animator_controllers.json"

BLEND_TYPES = {0: "Simple1D", 1: "SimpleDirectional2D", 2: "FreeformDirectional2D",
               3: "FreeformCartesian2D", 4: "Direct"}

# ---- 手写的那一节 `clipsByGuid`：本生成器不产生它，但**重跑不许冲掉**（2026-10-07 · A200）----
# 这一段文案由**本生成器**写：它描述的正是生成器自己的行为，留在 json 里手改就一定会过期。
CLIPS_BY_GUID_NOTE = (
    "🆕 2026-10-07（A192 加 · A200 起由生成器**原样保留**，本段文案由生成器写）：这一节记的是"
    "「**没有任何控制器引用、只被环境 SO 按 assetGUID 引用**」的动画片段 —— 消费方 "
    "`ScenarioAnimationBlend`（`CardPresentation/Battle/ScenarioBlendables.cs`）。"
    "🔴 **它是手写的**：`工具/gen_animator_controllers.py` **不产生**它，但**重跑生成器不会把它冲掉** —— "
    "生成器读旧产物的这一节、**逐字搬过来**，并逐条核对「`source` 那个包目录在 · "
    "`AnimationClip_<pathId>.json` 在 · `m_Name` 与 `name` 相符 · `guid` 是 32 位十六进制」，"
    "核对不过会点名并落进 `_unhandled`。⇒ **要新增一条 clip，改这一节、再重跑生成器即可**。"
    "`工具/extract_missing_shaders.py --prefabs` 按这里的 `name` 把 clip 收进 "
    "`wf_prefabs_extra.bundle`、并按这里的 `guid` **登记一条容器别名**（原版源包的容器键**就是 GUID**，"
    "实测 `assets_full/bundle_battleprefabs_vfxandmisc_assets_all/AssetBundle/AssetBundle_1.json` "
    "的 988 条键全是 32 位十六进制）⇒ 运行时 `LoadAsset<AnimationClip>(guid)` 就是原版那条取法，"
    "**GUID→名字的映射全仓只有这一处**。")

_HEX = set("0123456789abcdef")


def read_prev_clips_by_guid(out_path):
    """读旧产物里**手写**的 `clipsByGuid`。返回 `(entries, how, fatal)`。

    `fatal=True` **只在「文件在、但 JSON 解析不了」时**给 —— 那时什么都读不出来，
    再写就等于把用户刚手改的那一节一起冲掉 ⇒ 调用方**必须拒绝写**。
    「文件不存在」/「没有这一节」⇒ `(None, 说明, False)`（可以写，但要点名）。
    """
    if not os.path.isfile(out_path):
        return None, f"旧产物不存在（{out_path}）", False
    try:
        with open(out_path, encoding="utf-8") as f:
            doc = json.load(f)
    except Exception as e:
        return None, f"旧产物 JSON 解析不了（{e}）", True
    v = doc.get("clipsByGuid")
    if v is None:
        return None, "旧产物里没有 `clipsByGuid` 这一节", False
    if not isinstance(v, list):
        return None, f"旧产物的 `clipsByGuid` 不是数组（{type(v).__name__}）", False
    return v, f"读自旧产物（{len(v)} 条）", False


def check_clips_by_guid(entries):
    """逐条核对 `clipsByGuid`（源包目录 · clip 文件 · `m_Name` · GUID 形态）。

    返回 `(通过的条数, 问题文案列表)`。核对的是**原版解包树**，不是我们自己的常量。
    """
    ok, prob = 0, []
    for i, c in enumerate(entries):
        if not isinstance(c, dict):
            prob.append(f"clipsByGuid[{i}]: 不是对象（{type(c).__name__}）")
            continue
        name, guid, src, pid = c.get("name"), c.get("guid"), c.get("source"), c.get("pathId")
        tag, errs = f"clipsByGuid[{i}] {name!r}", []
        if not name:
            errs.append("没写 `name`（打包方靠它收 clip）")
        if not (isinstance(guid, str) and len(guid) == 32
                and all(ch in _HEX for ch in guid.lower())):
            errs.append(f"`guid` 不像 assetGUID（{guid!r}）—— 容器别名要靠它登记")
        if not src:
            errs.append("没写 `source`（打包方靠它认包）")
        elif not os.path.isdir(os.path.join(ASSETS_FULL, src)):
            errs.append(f"`source` 那个包目录不在 assets_full 里（{src}）")
        elif not isinstance(pid, int):
            errs.append(f"`pathId` 不是整数（{pid!r}）⇒ 核对不了「源包里有这条 clip」")
        else:
            f = os.path.join(ASSETS_FULL, src, "AnimationClip", f"AnimationClip_{pid}.json")
            if not os.path.isfile(f):
                errs.append(f"源包里没有 `AnimationClip/AnimationClip_{pid}.json`")
            else:
                try:
                    real = load(f).get("m_Name")
                except Exception as e:
                    errs.append(f"{os.path.basename(f)} 读不动（{e}）")
                else:
                    if real != name:
                        errs.append(f"源包里 PathID {pid} 的 `m_Name` = {real!r}，与 `name` 不符")
        if errs:
            prob += [f"{tag}: {e}" for e in errs]
        else:
            ok += 1
    return ok, prob


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
    # ---- 手写的那一节 `clipsByGuid`：原样搬过去 + 逐条核对（2026-10-07 · A200）----
    # （放在下面「解不动的」汇总之前 —— 核对出的问题也要进那份汇总）
    prev_clips, prev_how, fatal = read_prev_clips_by_guid(OUT)
    guid_bad = False
    if fatal:
        print(f"[AC] 🔴 `clipsByGuid` 读不到：{prev_how}")
        print("[AC] 🔴 **拒绝写文件** —— 现在写就会把那一节（手写的）一起冲掉。先修好 json、再重跑本脚本。")
        print("[AC]    （若那一节已经没了 / 修不动：删掉产物文件再重跑 ⇒ 会重建 `controllers`，"
              "但 `clipsByGuid` 会**空着**、要按原格式重新手加。）")
        return 1
    if prev_clips is None:
        guid_bad = True
        msg = (f"`clipsByGuid` 这一节**读不到**（{prev_how}）⇒ 写出去的产物里这一节会是**空的**："
               "`extract_missing_shaders.py --prefabs` 会因此**收不到**那两条环境 clip，"
               "重打包后 GUID 容器别名消失（现象只在**运行时**才报，见 `CLIPS_BY_GUID_NOTE`）。"
               "修法：把那一节按原格式恢复进旧产物，再重跑本生成器。")
        print(f"[AC] 🔴 `clipsByGuid` 这一节读不到（{prev_how}）—— 产物里会空着，"
              "后果与修法写在下面「解不动的」那一条里")
        bad.append("[clipsByGuid] " + msg)
    else:
        okn, probs = check_clips_by_guid(prev_clips)
        print(f"[AC] clipsByGuid（**手写**的那一节）：原样保留 **{len(prev_clips)}** 条 —— {prev_how}；"
              f"逐条核对通过 {okn}/{len(prev_clips)}")
        for c in prev_clips:
            if isinstance(c, dict):
                print(f"     · {c.get('guid')} → {c.get('name')!r}"
                      f"（{c.get('source')} · PathID {c.get('pathId')}）")
        for p in probs:
            print(f"[AC] 🔴 {p}")
            bad.append("[clipsByGuid] " + p)
        guid_bad = bool(probs)

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
           "controllers": out,
           "_clipsByGuid_note": CLIPS_BY_GUID_NOTE,
           "clipsByGuid": prev_clips if prev_clips is not None else []}
    if check:
        print("[AC] --check：只体检，未写文件")
        return 0 if (not missing and not guid_bad) else 1
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    # 🔴 先写临时文件再 `os.replace`（**原子**）：否则跑到一半被打断就会留下半截 json，
    #    而上面那条「解析不了 ⇒ 拒绝写」的规则会让下一次重跑也写不动（2026-10-07 · A200）。
    tmp = OUT + ".tmp"
    with open(tmp, "w", encoding="utf-8", newline="\n") as f:
        json.dump(doc, f, ensure_ascii=False, indent=1)
    os.replace(tmp, OUT)
    print(f"[AC] 写出 {OUT}（{os.path.getsize(OUT)} 字节）")
    return 0 if (not missing and not guid_bad) else 1


if __name__ == "__main__":
    sys.exit(main())
