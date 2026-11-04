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
下标 = 本 transform 在**父的 `m_Children` 列表里的位置**；⛔ **别读 `m_RootOrder`** ——
Unity 2023+/6 已删该字段（序列化里恒无 ⇒ 恒 0），2026-10-26 因为段下标恒 0，
`Pray_Idle/Pray VFX mesh` 少了 `#1`，`AnimFXCheck` 报「模块引用路径 1 处解析失败」。

用法
----
    python -I -X utf8 d:/4/Unity/工具/gen_effect_extras.py <--dry|--check|--write>

      --dry    只扫描并打印统计，⛔ 不写盘
      --check  把本次生成结果与盘上 `effect_extras.json` 比对（一致 rc=0 / 不一致 rc=1），⛔ 不写盘
      --write  写盘 `Resources/WarpforgeVFX/effect_extras.json`

    🔴 **不带任何动作参数 ⇒ 打印本用法并 rc=2，零写盘**（项目纪律：写盘脚本必须带显式动作，
       见 CLAUDE.md「会写盘的脚本：不带显式动作参数时必须什么都不做」）。
       ⚠️ 2026-11-04（`B141-W2`）之前 `main()` 只看 `--dry` / `--check`，两者都没有就**直接写盘** —— 已修。

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

USAGE = """用法：
  python -I -X utf8 d:/4/Unity/工具/gen_effect_extras.py <--dry|--check|--write>

  --dry    只扫描并打印统计，⛔ 不写盘
  --check  把本次生成结果与盘上 effect_extras.json 比对（一致 rc=0 / 不一致 rc=1），⛔ 不写盘
  --write  写盘 Resources/WarpforgeVFX/effect_extras.json

🔴 不带任何动作参数 ⇒ 打印本用法并 rc=2，**零写盘**（写盘脚本必须带显式动作）。
"""

# 🆕 2026-11-04（`B139_G` · 条目 13-03）：`particlesSystems` 里**解析不出**的引用（既不是空槽也不是本文件里的 PS）
# —— 跑完打出来，⛔ 不静默。
UNRESOLVED_PS = set()


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


def yaml_name(raw):
    """`m_Name:` 那一行的值 → **真名**。

    ⚠️ YAML 会把**带首尾空格**的名字用单引号包起来（导出集里 107 处，例如
    `m_Name: 'Impact '`，真名是 `Impact `）。不脱引号的话路径段会多一对 `'`，
    而 `ResolvePath` 是**逐字比**（不 Trim 子件名）⇒ 必然解析失败。2026-10-26 顺手修。
    """
    s = raw.strip()
    if len(s) >= 2 and s[0] == s[-1] and s[0] in "'\"":
        inner = s[1:-1]
        return inner.replace("''", "'") if s[0] == "'" else inner.replace('\\"', '"')
    return s


def fnum(body, key):
    m = re.search(r"(?:^|%s)  %s:\s*([-\d.eE]+)" % (NL, re.escape(key)), body)
    return float(m.group(1)) if m else None


def field_in(blk, key):
    m = re.search(r"(?:^|%s)%s:\s*([-\d.eE]+)" % (NL, re.escape(key)), blk)
    return float(m.group(1)) if m else 0.0


def sub_emitter_indexes(body):
    """`SubEmitterManualTrigger.subEmitterIndexes` → int 列表（🆕 2026-11-04 · `B139_G` 条目 13-04）。

    ⚠️ 原版这份 int[] 在 AssetRipper 的 YAML 里**有两种写法**，都要认：
      · 空数组 ⇒ `  subEmitterIndexes:` 后面什么都没有（现读 12 个挂载里 11 个是这种）；
      · 非空 ⇒ 导出成**十六进制字节串**（小端 int32 序列），例如 `01000000` = `[1]`
        （`Sau_ParticleWhip/Beam#6/Necron Gauss ` 那颗；**不是**「四个元素 1/0/0/0」）。
      另外兼容正常的 YAML 列表写法（`  - 1`）。
    """
    lines = body.split(NL)
    for i, ln in enumerate(lines):
        m = re.match(r"^  subEmitterIndexes:(.*)$", ln)
        if not m:
            continue
        rest = m.group(1).strip()
        if rest:
            if re.fullmatch(r"[0-9a-fA-F]+", rest) and len(rest) % 8 == 0:
                return [int.from_bytes(bytes.fromhex(rest[k:k + 8]), "little", signed=True)
                        for k in range(0, len(rest), 8)]
            return []
        out = []
        j = i + 1
        while j < len(lines):
            mm = re.match(r"^  - (-?\d+)\s*$", lines[j])
            if not mm:
                break
            out.append(int(mm.group(1)))
            j += 1
        return out
    return []


def prefab_paths():
    """prefab 基名 → 导出集里的**完整路径**（整棵导出树，不是只看根）。

    🔴 2026-11-04（`B141-W2` · 判据 `判据/B141_W130_对账_资源.md` **16-09**）：
    原来按 `os.path.join(EXPORT, nm + ".prefab")` **只在根目录**找 ⇒ 导出到 `GameObject/` 子目录的
    prefab 一律被当成「导出集缺件」静默跳过。实测 978 个特效里 **13 个**落在 `GameObject/`，
    其中就包括 **`Card 3D Death Explosion`**（它挂着 `DestroyByTime lifeTime: 2.2` —— 16-09 漏收的那条）。
    改成按整棵树建索引；⚠️ 同名多份时取**第一份**（本轮实测这 978 个名字里**重名 0 组**）。
    """
    by = {}
    for dp, _dirs, fs in os.walk(EXPORT):
        for fn in fs:
            if fn.endswith(".prefab"):
                by.setdefault(fn[:-len(".prefab")], os.path.join(dp, fn))
    return by


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
            goname[pid] = yaml_name(m.group(1)) if m else ""

    tr = {}
    for pid, (ty, body) in docs.items():
        if ty in ("4", "224"):
            g = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", body)
            f = re.search(r"m_Father: \{fileID: (-?\d+)\}", body)
            # 🔴 Unity 2023+/6 已删 `m_RootOrder`（序列化里根本没有这个字段，硬读恒 0
            #    ⇒ 每一段的下标都成 0 ⇒ 非首个子件的路径静默解析失败）。
            #    兄弟下标只认父的 `m_Children` 序：本 transform 在父列表里的位置（下面统一算 `sib_of`）。
            kids = re.search(r"%s  m_Children:\s*%s((?:  - \{fileID: -?\d+\}%s)+)" % (NL, NL, NL), body)
            tr[pid] = (g.group(1) if g else None,
                       f.group(1) if f else "0",
                       re.findall(r"\{fileID: (-?\d+)\}", kids.group(1)) if kids else [])

    sib_of = {}
    for _pid, (_g, _f, kids) in tr.items():
        for i, k in enumerate(kids):
            sib_of.setdefault(k, i)
    tr = {pid: (g, f, sib_of.get(pid, 0)) for pid, (g, f, _kids) in tr.items()}

    comp_go = {}
    for pid, (ty, body) in docs.items():
        # ⚠️ 原来只收 `114`（MonoBehaviour）。2026-11-04（`B139_G` · 条目 13-03）起也要 `198`（ParticleSystem）——
        #    `SyncParticleSeeds.particlesSystems` 引用的是**PS 组件**，要靠这一跳换成 GameObject 路径。
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
        # ⚠️ 2026-11-04（`B139_G` · 条目 13-02 的**数据半边**）：原版 `m_Enabled`。9 个挂载是 0
        #    （3 个 DestroyByTime / 1 个 SubEmitterManualTrigger / 5 个嵌套 AnimFXController），
        #    运行时按 `disabled > 0` **不挂**（原版那个组件根本不跑）。
        #    ⚠️ 用「disabled」而**不是**「enabled」：缺键时 JsonUtility 给的是字段默认值 0 ⇒ 旧表行为不变。
        if re.search(r"^  m_Enabled: 0\s*$", body, re.M):
            rec["disabled"] = 1
        if cls == "DestroyByTime":
            rec["f0"] = fnum(body, "lifeTime")
        elif cls == "SyncParticleSeeds":
            # 🆕 2026-11-04（`B139_G` · 条目 13-03）：原版 `particlesSystems` 是**手挑的一小串**（40 挂载共 90 颗），
            #   不是「整棵子树」。这里逐个 PPtr → PS 组件的 GameObject → 节点路径（与 `ResolvePath` 同一套约定）。
            #   ⚠️ 列表为空 ⇒ 照样写 `ps: []`（= 原版数组确实为空，运行时**一颗都不同步**）；
            #      **键缺了**才由运行时报「旧表」并退回超集行为。
            psl = []
            for blk in yaml_block(body, "particlesSystems"):
                mm = re.search(r"fileID: (-?\d+)", blk)
                if not mm:
                    continue
                ref = mm.group(1)
                if ref == "0":
                    # 🔴 原版数组里的**空槽**（`m_PathID 0`）—— `SyncParticleSeeds__Awake.c` 的循环
                    # 遇到 null 元素是 **`break`**（不是 continue）⇒ 后面的一律不再处理。
                    # 这里写空串占位，运行时见了**停在原地**（保持数组长度与语义）。
                    psl.append("")
                    continue
                pp = path_of(comp_go.get(ref, ""))
                if pp:
                    psl.append(pp)
                else:
                    # 解析不出来的引用（跨文件 / 嵌套 prefab）—— 也按空槽处理，并留痕给调度台查。
                    psl.append("")
                    UNRESOLVED_PS.add(ref)
            rec["ps"] = psl
        elif cls == "SubEmitterManualTrigger":
            rec["f0"] = fnum(body, "triggerOnSecond")
            rec["se"] = sub_emitter_indexes(body)      # 🆕 条目 13-04：原版下标数组（空 = 一次都不触发）
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
    # 🔴 显式动作参数：`--dry` / `--check` / `--write` 三选一。
    #    不带 / 带了未知参数 / 一次带多个 ⇒ 打印用法 + rc=2，**零写盘**（见 USAGE 与模块头）。
    acts = [a for a in sys.argv[1:] if a.startswith("--")]
    stray = [a for a in sys.argv[1:] if not a.startswith("--")]
    if stray or len(acts) != 1 or acts[0] not in ("--dry", "--check", "--write"):
        print(USAGE)
        return 2
    action = acts[0]

    g2c = class_by_guid()
    G2A = asset_names()
    pmap = prefab_paths()
    names = [os.path.splitext(os.path.basename(e["prefab"]))[0]
             for e in json.load(io.open(EFFECT_INDEX, encoding="utf-8"))["effects"]]

    effects, missing, stats = [], [], collections.Counter()
    for nm in names:
        p = pmap.get(nm)
        if not p:
            missing.append(nm)
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
          % (len(names), len(missing), len(effects), sum(stats.values())))
    for k, v in stats.most_common():
        print("  %-34s %d" % (k, v))
    if missing:
        print("  导出集缺件（prefab 名）：" + " · ".join(missing))

    if action == "--dry":
        return 0
    if action == "--check":
        old = io.open(OUT, encoding="utf-8").read() if os.path.exists(OUT) else ""
        same = old == text
        print("--check：盘上文件与本次生成%s" % ("一致 ✅" if same else "**不一致** ❌（重跑 --write 即可）"))
        return 0 if same else 1

    io.open(OUT, "w", encoding="utf-8", newline=NL).write(text)
    # ⚠️ 原来写的是 `len(text)`，那是**字符数**、却被印成「字节」（中文一个字 3 字节 ⇒ 少报 44）。
    print("已写 " + OUT + "（%d 字节）" % len(text.encode("utf-8")))
    return 0


if __name__ == "__main__":
    sys.exit(main())
