#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""把原版的 **`MaterialFlickerEffect`**（材质闪烁脚本）生成成 C# 表。

## 为什么有这个东西

13 个战场里有 **38 个**对象挂着一个叫 `MaterialFlickerEffect` 的脚本 —— **我们的生成器从来没抽过它**
（只抽 Mesh + ParticleSystem + 耀斑）⇒ 那些「火把 / 地面 / 塔楼 / 木桶 / 发电机自发光」**永远是死的**，
而我们复刻里那块「烘焙光斑」也**永远不闪**。

**这不是猜的**：2026-09-22 进原版跑实况量过 —— 这两个对象的 `material.color.a`
在 **0.20 ~ 2.03** 之间动（22 次采样，均值 **1.035 / 1.118**），
与按反编译算出的算式（`noise+1`，均值 1.000）吻合。判定正本 = `资料/战场13场_逐场对账_0920.md` §一 ①-a。

## 数据在哪

`d:/2/解包整理/07_场景/<场>/MonoBehaviour/*.json`（**字段集合是唯一的判据**：
`m_amplitude · m_frequency · m_playOnAwake · m_fadeInTime · m_fadeOutTime · m_randomStart` 六个都有；
`LightFlickerEffect` 第 6 个是 `m_affectRange`、`SpriteFlickerEffect` 只有前 5 个 —— 13 场里那两类**各 0 个**）。
对象的**名字**靠「哪个 GameObject 的 `m_Component[]` 里引用了这条组件的 PathID」反查
（组件文件名就是它的 PathID：`MonoBehaviour_4973.json`）。

## 算式（`d:/2/tools/decomp_full/MaterialFlickerEffect__Update.c` + `.rdata` 常量）

    t     = time + desync
    th    = t * frequency
    x     = th + 0.25 * sin(0.35 * th)
    noise = amplitude * ( 0.35*sin(1.67*x + 0.15) + 0.59*sin(x + 0.52)
                        + 0.28*sin(3.24*x - 0.11) + 0.17*sin(7.02*x - 0.42) )
    alpha = (noise + 1.0) * 原alpha * (fade - 0.25) / 0.75        # playOnAwake=1 ⇒ fade 恒 1

## 产物

`Assets/WarpforgeArena1/Editor/FlickerData.gen.cs` —— **自动生成，不要手改**。
"""
import io, json, os, re, sys

SCENES = "d:/2/解包整理/07_场景"
OUT = "d:/4/Unity/MyGame/Assets/WarpforgeArena1/Editor/FlickerData.gen.cs"
FIELDS = ["m_amplitude", "m_frequency", "m_playOnAwake", "m_fadeInTime", "m_fadeOutTime", "m_randomStart"]


def load(p):
    try:
        return json.load(io.open(p, encoding="utf-8"))
    except Exception:
        return None


def go_name_map(arena_dir):
    """组件 PathID -> 挂着它的 GameObject 名字（靠 GameObject 的 m_Component 引用反查）。"""
    out = {}
    gdir = os.path.join(arena_dir, "GameObject")
    if not os.path.isdir(gdir):
        return out
    for f in os.listdir(gdir):
        if not f.endswith(".json"):
            continue
        d = load(os.path.join(gdir, f))
        if not d:
            continue
        nm = d.get("m_Name") or os.path.splitext(f)[0]
        for c in d.get("m_Component") or []:
            pid = (c.get("component") or {}).get("m_PathID")
            if pid is not None:
                out[pid] = nm
    return out


def main():
    specs = []
    seen = set()          # 🔴 `解包整理` 每个资产存两份（`X.json` 与 `X_NNN_NNN.json`，PathID 相同）
                          #    ⇒ 必须按 (场, PathID) 去重，否则 38 条会变成 76 条（踩过）
    for arena in sorted(os.listdir(SCENES)):
        adir = os.path.join(SCENES, arena)
        mdir = os.path.join(adir, "MonoBehaviour")
        if not os.path.isdir(mdir):
            continue
        names = go_name_map(adir)
        for f in sorted(os.listdir(mdir)):
            m = re.match(r"MonoBehaviour_(\d+)(?:_\d+)?\.json$", f)
            if not m:
                continue
            pid = int(m.group(1))
            if (arena, pid) in seen:
                continue
            d = load(os.path.join(mdir, f))
            if not d or not all(k in d for k in FIELDS):
                continue
            seen.add((arena, pid))
            go = names.get(pid) or "?"
            specs.append({
                "arena": arena,
                "go": go,
                "pid": pid,
                "amp": float(d["m_amplitude"]),
                "freq": float(d["m_frequency"]),
                "play": 1 if d["m_playOnAwake"] else 0,
                "fin": float(d["m_fadeInTime"]),
                "fout": float(d["m_fadeOutTime"]),
                "rnd": 1 if d["m_randomStart"] else 0,
            })

    # desync：原版是 `Random.Range(0,100)`（**不可复现**）；我们按 (场, 名字) 定死一个 [0,100) 的值，
    # 这样同一份构建每次渲出来一模一样（与工程「对局可复现」那条口径一致）。
    # ⚠️ **这是「我们挑的」**：值域照原版，具体取值是我们定的。
    lines = []
    lines.append("// ─────────────────────────────────────────────────────────────────────────────")
    lines.append("// 自动生成 —— **不要手改**！生成器：`工具/gen_flicker_cs.py`")
    lines.append("//")
    lines.append("// 原版 13 个战场里有 %d 个对象挂着 `MaterialFlickerEffect`（材质闪烁脚本），我们从来没抽过。" % len(specs))
    lines.append("// 算式与常量来自反编译（`MaterialFlickerEffect__Update.c` + `.rdata`），")
    lines.append("// 并已用**原版实况**验过：alpha 在 0.20~2.03 之间动、均值 ≈1.0（见资料/战场13场_逐场对账_0920.md §一 ①-a）。")
    lines.append("// ⚠️ 只有 `desync` 是「我们挑的」（原版用 UnityEngine.Random，改成按名字定死 ⇒ 可复现）。")
    lines.append("// ─────────────────────────────────────────────────────────────────────────────")
    lines.append("")
    lines.append("public static class FlickerData")
    lines.append("{")
    lines.append("    public struct Spec")
    lines.append("    {")
    lines.append("        public string arena;      // 场名（= arenas/<arena>/）")
    lines.append("        public string go;         // 对象名")
    lines.append("        public float amplitude;   // m_amplitude")
    lines.append("        public float frequency;   // m_frequency")
    lines.append("        public float fadeInTime;  // m_fadeInTime")
    lines.append("        public float fadeOutTime; // m_fadeOutTime")
    lines.append("        public bool  randomStart; // m_randomStart")
    lines.append("        public float desync;      // ⚠️ 我们定的（原版 Random.Range(0,100)）")
    lines.append("    }")
    lines.append("")
    lines.append("    /// <summary>%d 条，逐字来自原版场景（出处见生成器头）。</summary>" % len(specs))
    lines.append("    public static readonly Spec[] Specs = new Spec[]")
    lines.append("    {")
    for s in specs:
        h = 0
        for ch in (s["arena"] + "/" + s["go"]):
            h = (h * 131 + ord(ch)) & 0x7FFFFFFF
        desync = 0.0 if not s["rnd"] else round((h % 10000) / 100.0, 2)
        lines.append('        new Spec { arena = "%s", go = "%s", amplitude = %sf, frequency = %sf, fadeInTime = %sf, fadeOutTime = %sf, randomStart = %s, desync = %sf },'
                     % (s["arena"], s["go"].replace('"', "'"), repr(s["amp"]), repr(s["freq"]),
                        repr(s["fin"]), repr(s["fout"]), "true" if s["rnd"] else "false", repr(desync)))
    lines.append("    };")
    lines.append("}")
    io.open(OUT, "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")
    ok = sum(1 for s in specs if s["go"] != "?")
    print("生成 %s：%d 条（对象名解析成功 %d 条）" % (OUT, len(specs), ok))
    for s in specs:
        print("   %-24s %-28s amp=%.3f freq=%.2f fIn=%.2f/%.2f rnd=%d desync=%.2f"
              % (s["arena"], s["go"], s["amp"], s["freq"], s["fin"], s["fout"], s["rnd"],
                 0.0 if not s["rnd"] else 0.0))


if __name__ == "__main__":
    main()
