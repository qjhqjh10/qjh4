#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_effect_extras.py — 生成 `Resources/WarpforgeVFX/effect_extras.json`（特效 prefab 上的**非 AnimFX** 组件表）。

为什么要这张表（判据）
----------------------
`WarpforgeEffectPlayer.BuildModules` **只携带 AnimFX 模块**（`animfx_modules.json`）；
原版特效 prefab 上还挂着别的 MonoBehaviour，那些**一个都不会被携带** ⇒ 静默丢失。
本脚本把「哪件特效挂了什么、挂在哪个节点、参数多少」抽成数据，运行时由
`WarpforgeVFX/Runtime/WFEffectExtras.cs` 按表补挂（未实现的 kind 会**出声 + 计数**，⛔ 不静默）。

数据源（**只读**）
------------------
`D:/2/tools/assetripper_out/ExportedProject` —— 它把 prefab 里的 `m_Script` 解析成了 guid，
再用 `Assets/Scripts/**/*.cs.meta` 的 guid → 类名反查。
⚠️ 不能用 `d:/2/新解包资源/assets_full` 的 `bundle_*/MonoBehaviour/*.json` 直接判：
   那里的 `m_Script.m_PathID` 落在**外部文件**（`m_FileID: 1`）里，和 `bundle_Waprforge_monoscripts`
   的文件名（pathID）**对不上**（本机实测 400/400 全 miss）——这是踩过的坑。

节点路径
--------
按 `Transform.m_Father` 逐级拼回，段格式 `名字#下标`（下标 0 省略），与
`WFEffectModule.ResolvePath` 的语法**逐字一致**（同一份约定，⛔ 别在别处另定）。

用法
----
    python -I -X utf8 d:/4/Unity/工具/gen_effect_extras.py [--dry] [--check]

产物
----
    d:/4/Unity/MyGame/Assets/Resources/WarpforgeVFX/effect_extras.json
    ⚠️ 该目录被 `.gitignore` 忽略（与 `WarpforgeEffectLibrary.asset` 同规格：引用的 prefab 不进仓库）
       ⇒ **换机器/清库之后要重跑本脚本**。
"""
import collections
import io
import json
import os
import re
import sys

PROJ = r"D:/4/Unity/MyGame/Assets"
EXPORT = r"D:/2/tools/assetripper_out/ExportedProject/Assets"
EFFECT_INDEX = r"D:/4/Unity/数据/游戏数据/effect_index.json"
OUT = PROJ + "/Resources/WarpforgeVFX/effect_extras.json"

NL = chr(10)

# 需要抽的类（= 普查得出的「非 AnimFX」那一批；新增类只改这里 + WFEffectExtras.cs 的 switch）
WANT = {
    "SyncParticleSeeds", "DestroyByTime", "SubEmitterManualTrigger",
    "InitiMaterialWithRandomValue", "ShaderProperySetter", "ParticleFindFloorCollider",
    "ShakeAndSoundObject", "MaterialFlickerEffect", "SpriteFlickerEffect",
    "ParticleEmitOnEnable", "ParticleForceForceFieldController",
}

G2A = {}   # guid -> .asset 文件名（presetSO / AudioCue）


def class_by_guid():
    g2c = {}
    for dp, _dirs, fs in os.walk(os.path.join(EXPORT, "Scripts")):
        for fn in fs:
            if not fn.endswith(".cs.meta"):
                continue
            t = io.open(os.path.join(dp, fn), encoding="utf-8", errors="ignore").read()
            m = re.search(r"^guid: ([0-9a-f]{32})", t, re.M)
            if m:
                g2c.setdefault(m.group(1), fn[:-8])
    return g2c


def asset_names():
    """guid -> `.asset` 文件名（不带扩展）。用于把 `presetSO` / `sound` 的 guid 换成名字。"""
    g2a = {}
    for dp, _dirs, fs in os.walk(EXPORT):
        for fn in fs:
            if not fn.endswith(".asset.meta"):
                continue
            t = io.open(os.path.join(dp, fn), encoding="utf-8", errors="ignore").read()
            m = re.search(r"^guid: ([0-9a-f]{32})", t, re.M)
            if m:
                # ⚠️ `.asset.meta` 要去掉**两段**扩展（`[:-5]` 只去掉 `.meta` ⇒ 名字会带 `.asset`，踩过）
                g2a.setdefault(m.group(1), fn[:-len(".asset.meta")])
    return g2a


def yaml_block(body, key):
    """取 `  key:` 下面的列表块，返回条目文本列表（每条已去掉前导 `- `）。"""
    lines = body.split(NL)
    out, inside = [], False
    for ln in lines:
        if re.match(r"^  %s:" % re.escape(key), ln):
            inside = True
            continue
        if inside:
            if ln.startswith("  - "):
                out.append(ln[4:])
            elif ln.startswith("    ") and out:
                out[-1] += NL + ln[4:]
            elif ln.strip():
                break
    return out


def fnum(body, key):
    m = re.search(r"(?:^|%s)  %s:\s*([-\d.eE]+)" % (NL, re.escape(key)), body)
    return float(m.group(1)) if m else None


def field_in(blk, key):
    m = re.search(r"(?:^|%s)%s:\s*([-\d.eE]+)" % (NL, re.escape(key)), blk)
    return float(m.group(1)) if m else 0.0


def scan_prefab(path, g2c):
    t = io.open(path, encoding="utf-8", errors="ignore").read()
    parts = re.split(r"%s--- !u!(\d+) &(-?\d+)" % NL, t)
    docs = {}
    for i in range(1, len(parts) - 1, 3):
        docs[parts[i + 1]] = (parts[i], parts[i + 2])

    goname = {}
    for pid, (ty, body) in docs.items():
        if ty == "1":
            m = re.search(r"%s  m_Name: (.*)" % NL, body)
            goname[pid] = m.group(1).strip() if m else ""

    tr = {}
    for pid, (ty, body) in docs.items():
        if ty in ("4", "224"):
            g = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", body)
            f = re.search(r"m_Father: \{fileID: (-?\d+)\}", body)
            sib = re.search(r"m_RootOrder: (\d+)", body)
            tr[pid] = (g.group(1) if g else None,
                       f.group(1) if f else "0",
                       int(sib.group(1)) if sib else 0)

    comp_go = {}
    for pid, (ty, body) in docs.items():
        if ty == "114":
            g = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", body)
            if g:
                comp_go[pid] = g.group(1)

    go2tr = {}
    for tpid, (g, _f, _s) in tr.items():
        if g:
            go2tr.setdefault(g, []).append(tpid)

    def path_of(go_pid):
        if go_pid not in go2tr:
            return goname.get(go_pid, "")
        segs, cur, guard = [], go2tr[go_pid][0], 0
        while cur and cur != "0" and cur in tr and guard < 64:
            guard += 1
            g, f, s = tr[cur]
            nm = goname.get(g, "")
            segs.append(nm if s == 0 else "%s#%d" % (nm, s))
            cur = f
        return "/".join(reversed(segs))

    out = []
    for pid, (ty, body) in docs.items():
        if ty != "114":
            continue
        m = re.search(r"m_Script:\s*\{fileID: 11500000, guid: ([0-9a-f]{32})", body)
        if not m:
            continue
        cls = g2c.get(m.group(1))
        if cls not in WANT:
            continue
        rec = {"kind": cls, "node": path_of(comp_go.get(pid, ""))}
        if cls == "DestroyByTime":
            rec["f0"] = fnum(body, "lifeTime")
        elif cls == "SubEmitterManualTrigger":
            rec["f0"] = fnum(body, "triggerOnSecond")
        elif cls == "InitiMaterialWithRandomValue":
            rec["f0"] = fnum(body, "minValue")
            rec["f1"] = fnum(body, "maxValue")
        elif cls in ("MaterialFlickerEffect", "SpriteFlickerEffect"):
            rec["f0"] = fnum(body, "m_amplitude")
            rec["f1"] = fnum(body, "m_frequency")
            rec["f2"] = fnum(body, "m_fadeInTime")
            rec["f3"] = fnum(body, "m_fadeOutTime")
            rec["f4"] = fnum(body, "m_randomStart")
        elif cls == "ShakeAndSoundObject":
            pres = []
            for blk in yaml_block(body, "onEnableShakes"):
                g = re.search(r"guid: ([0-9a-f]{32})", blk)
                if g and g.group(1) in G2A:
                    pres.append(G2A[g.group(1)])
            slots = []
            for blk in yaml_block(body, "sounds"):
                g = re.search(r"sound: \{fileID: 11400000, guid: ([0-9a-f]{32})", blk)
                if not g or g.group(1) not in G2A:
                    continue
                repeat = int(field_in(blk, "repeat"))
                loops = int(field_in(blk, "loops"))
                slots.append({
                    "cue": G2A[g.group(1)],
                    "time": field_in(blk, "time"),
                    "interval": field_in(blk, "timeInterval"),
                    # 原版 `PlaySoundOnTime`：`n = repeat ? loops : 1`（见 `WarpforgeEffectPlayer.BuildSoundList`）
                    "total": (loops if repeat else 1),
                    "is2d": int(field_in(blk, "is2d")),
                })
            if pres:
                rec["presets"] = pres
            if slots:
                rec["sounds"] = slots
        elif cls == "ShaderProperySetter":
            pn = re.search(r"%s  - propertyName: (.*)" % NL, body)
            fv = re.search(r"%s    floatValue: ([-\d.eE]+)" % NL, body)
            rec["s0"] = pn.group(1).strip() if pn else ""
            rec["f0"] = float(fv.group(1)) if fv else 0.0
        out.append({k: v for k, v in rec.items() if v is not None})
    return out


def main():
    global G2A
    dry = "--dry" in sys.argv
    check = "--check" in sys.argv

    g2c = class_by_guid()
    G2A = asset_names()
    names = [os.path.splitext(os.path.basename(e["prefab"]))[0]
             for e in json.load(io.open(EFFECT_INDEX, encoding="utf-8"))["effects"]]

    effects, missing, stats = [], 0, collections.Counter()
    for nm in names:
        p = os.path.join(EXPORT, nm + ".prefab")
        if not os.path.exists(p):
            missing += 1
            continue
        recs = scan_prefab(p, g2c)
        if recs:
            effects.append({"effect": nm, "extras": recs})
            for r in recs:
                stats[r["kind"]] += 1

    effects.sort(key=lambda x: x["effect"])
    doc = {
        "note": "特效 prefab 上非 AnimFX 模块的组件（生成器 = 工具/gen_effect_extras.py；"
                "运行时消费 = WarpforgeVFX/Runtime/WFEffectExtras.cs）",
        "effects": effects,
    }
    text = json.dumps(doc, ensure_ascii=False, indent=1)

    print("特效 %d 条 / 导出集缺 %d 件 / 带额外组件的 %d 件 / 挂载 %d 处"
          % (len(names), missing, len(effects), sum(stats.values())))
    for k, v in stats.most_common():
        print("  %-34s %d" % (k, v))

    if dry:
        return 0
    if check:
        old = io.open(OUT, encoding="utf-8").read() if os.path.exists(OUT) else ""
        same = old == text
        print("--check：盘上文件与本次生成%s" % ("一致 ✅" if same else "**不一致** ❌（重跑一次即可）"))
        return 0 if same else 1

    io.open(OUT, "w", encoding="utf-8", newline=NL).write(text)
    print("已写 " + OUT + "（%d 字节）" % len(text))
    return 0


if __name__ == "__main__":
    sys.exit(main())
