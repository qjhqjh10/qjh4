#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""fix_booster_prefabs.py —— 修 `Assets/WarpforgeBooster/Prefabs/` 那两份 prefab 里**落不下去的对象引用**。

## 治的是什么（两族，都有编号）

| 族 | 账 | 症状 | 本脚本怎么治 |
|---|---|---|---|
| **`m_Script`** | **`A1117`** | `Booster Info Popup.prefab` 有 **24 个**组件的 `m_Script` 是 `{fileID: <原版 pathID>, guid: 000…0, type: 0}` ⇒ 开出来是 **Missing (Mono Script)**、**这扇窗画不出来**（17 = `UnityEngine.UI.Image` · 7 = `TextMeshProUGUI`） | 按**原版 pathID → 类名 → 工程脚本 guid** 改写成 `{fileID: 11500000, guid: <工程 guid>, type: 3}` |
| **`m_fontAsset`** | **`A1118①`** | 两扇窗共 **134 条**（`Booster Pack Open Window` 92+35 · `Booster Info Popup` 6+1）`m_fontAsset` 是 guid 0 —— 那两份 TMP 字体资产工程里**没有**（`A1118①` 已把它们导进来：`工具/gen_tmp_font_assets.py`） | 按**原版 pathID → 字体名 → 工程字体 guid** 改写 |

## 判据（**全部实读，⛔ 一处也不是猜的**）

  · **`Image` / `TextMeshProUGUI` 的 guid**：现读工程 `Library/PackageCache/com.unity.ugui@*/Runtime/` 下的 `.meta`
    （`UGUI/UI/Core/Image.cs.meta` · `TMP/TextMeshProUGUI.cs.meta`）。本脚本运行时**再核一遍**
    「那个 guid 的 `.meta` 文件名确实是 `Image.cs` / `TextMeshProUGUI.cs`」——名字对不上就**停手**。
    旁证：同一批导出的 `Booster Pack Open Window.prefab` 里，这两种组件**就是这样解析的**
    （`fe87c0e1cc204ed48ad3b37840f39efc` ×71 · `f4688fdb7df04437aeb418b961361dc5` ×122）。
  · **哪份原版 pathID 是哪份字体**：三路独立互证，本脚本把其中两路做成断言 ——
    ① 原版 `MonoBehaviour/<字体名>.json` 的 `m_CreationSettings.referencedFontAssetGUID`
       就是该包容器的**资源键**；② 那个键在 `bundle_fonts_assets_all/AssetBundle/AssetBundle_1.json`
       的 `m_Container` 里 preload 的那三条 pathID = **字体资产 + 它的材质 + 它的图集**。
       实测：`Pragati` = `1df326…6c02` → `3485036404935369831` / `-5050103597772954521` / `8692869601067476071`；
             `Asar`    = `61505a…4f18` → `-8244042478085975641` / `1185407168034066855` / `-2190411507003141721`。
    ③ 旁证：导出日志 `_tmp_view/booster_export_收口.log` 报「`Pragati-Regular SDF`×92 · `Asar-Regular SDF`×35」，
       而 prefab 里正是 `3485036404935369831`×92 · `-8244042478085975641`×35 —— **两条独立计数吻合**。

## 为什么需要这个脚本（而不是直接改导出器）

🔴 **`BoosterPackExporter.cs` 不在本会话的白名单里** ⇒ 不能给它加一段收尾。
而两份 prefab 是它 **`PrefabUtility.SaveAsPrefabAsset` 每次重导都整个重写**的产物
⇒ **手改一次会被下一次重导抹掉**。所以做成**独立、幂等、可重跑**的收尾件：

```bash
python -I d:/4/Unity/工具/fix_booster_prefabs.py          # 修（已修过的条目会报 0）
python -I d:/4/Unity/工具/fix_booster_prefabs.py --check   # 只报不改（对账用）
```

⚠️ **跑 `BoosterPackExporter.Run` 之后**必须跟一次本脚本（否则 24 条 `m_Script` 又会变回 guid 0）。
`m_fontAsset` 那 134 条**不用**跟 —— 导出器自己会接上（它按**资产名**找工程 `TMP_FontAsset`，
`A1118①` 已把名字备好），跑本脚本只是让**今天盘上这份**先对起来。

## 没做的事

  · 🔴 **没查清**：**为什么同一源包、同一次导出，`Booster Pack Open Window` 解析了、`Booster Info Popup` 没解析**
    —— `A1119` 那笔账里如实记着「成因没查清」，本脚本只治症（把引用改对），不宣称查明了因。
  · 两扇窗里还有 **52 条 `m_Sprite`** guid 0（`Open Window` 40 · `Info Popup` 12，共 12 个不同 sprite）
    —— 那是 `A1109` 那一支 `EffectExporter.ImportSprite` 导不出图的账，**不在本脚本范围**。
"""
import io
import json
import os
import hashlib
import re
import sys

try:
    sys.stdout.reconfigure(errors="replace")
    sys.stderr.reconfigure(errors="replace")
except Exception:
    pass

ASSETS = r"d:/4/Unity/MyGame/Assets"
BUNDLE = r"d:/2/新解包资源/assets_full/bundle_fonts_assets_all"
PKGCACHE = r"d:/4/Unity/MyGame/Library/PackageCache"
ZERO = "00000000000000000000000000000000"
#: 原版 MonoScript 的 `m_ClassName`（实读 `assets_full/bundle_Waprforge_monoscripts/MonoScript/*.json`）
SCRIPTS = {
    # 原版 pathID        : (类名, 工程脚本文件名, 仓内既有记录)
    "350208831926335389": ("Image", "Image.cs", "资料/已知的坑.md:1900"),
    "7477354737935883349": ("TextMeshProUGUI", "TextMeshProUGUI.cs", "资料/普查产出_1018/REV_W1_词条化.md:252"),
}
#: 原版字体资产 pathID : 字体名
FONTS = {
    "3485036404935369831": "Pragati-Regular SDF",
    "-8244042478085975641": "Asar-Regular SDF",
}
PREFABS = ["Booster Info Popup.prefab", "Booster Pack Open Window.prefab"]


def font_guid(name):
    """与 `gen_tmp_font_assets.py` **同一个派生式**（两处不一致就会指到不存在的资产）。"""
    return hashlib.md5(("warpforge:tmp-font-asset:" + name).encode("utf-8")).hexdigest()


# ─────────────────────────── 前置断言（任一条不过就停手） ───────────────────────────

def _pkg_script_guids(fname):
    """在 `Library/PackageCache` 里按**文件名**找脚本的 `.meta`，返回 `[(guid, 路径)]`。
    🔴 按文件名找、**不是**按 guid 反查 —— 反查要先猜一个 guid，那就成了自证。
    ⚠️ 同名文件可能不止一份（实测 `Image.cs` 有两份：`com.unity.ide.visualstudio` 里也有一个）
    ⇒ 返回全部，由调用方**用「另一扇窗的既有解析结果」**挑 —— 实测命中的只有一份。"""
    hits = []
    for r, d, fs in os.walk(PKGCACHE):
        if fname + ".meta" in fs:
            p = os.path.join(r, fname + ".meta")
            t = io.open(p, encoding="utf-8", errors="replace").read(300)
            m = re.search(r"^guid: ([0-9a-f]{32})", t, re.M)
            if m:
                hits.append((m.group(1), os.path.relpath(p, r"d:/4/Unity/MyGame")))
    if not hits:
        raise SystemExit("[STOP] PackageCache 里找不到 `%s.meta`" % fname)
    return hits


def selftest():
    # ① 两个脚本 guid：按**文件名**取候选，再用「同一次导出的另一扇窗」的既有解析结果**挑**
    cross = io.open(os.path.join(ASSETS, "WarpforgeBooster/Prefabs",
                                 "Booster Pack Open Window.prefab"), encoding="utf-8").read()
    resolved = {}
    for pid, (cls, fname, ev) in SCRIPTS.items():
        cands = _pkg_script_guids(fname)
        used = [(g, rel, cross.count("m_Script: {fileID: 11500000, guid: %s, type: 3}" % g))
                for g, rel in cands]
        hit = [u for u in used if u[2] > 0]
        if len(hit) != 1:
            raise SystemExit("[STOP] `%s` 的候选 guid 里有 %d 份出现在另一扇窗的 `m_Script` 里"
                             "（期望恰好 1）：%r" % (fname, len(hit), used))
        g, rel, n = hit[0]
        resolved[pid] = (cls, g, rel)
        print("[selftest] %-20s %-16s -> %s  (%s · 另一扇窗里出现 %d 次 · 候选 %d 份)"
              % (pid, cls, g, rel, n, len(cands)))

    # ② 字体 pathID → 名字：用原版自己的 `referencedFontAssetGUID` + 包容器两路互证
    cont = json.load(io.open(os.path.join(BUNDLE, "AssetBundle/AssetBundle_1.json"),
                            encoding="utf-8"))["m_Container"]
    for pid, name in FONTS.items():
        src = json.load(io.open(os.path.join(BUNDLE, "MonoBehaviour", name + ".json"),
                               encoding="utf-8"))
        key = src["m_CreationSettings"]["referencedFontAssetGUID"]
        inbundle = [e[1]["asset"]["m_PathID"] for e in cont if e[0] == key]
        if int(pid) not in inbundle:
            raise SystemExit("[STOP] 容器键 %s 里没有 pathID %s（实得 %r）⇒ 字体名与 pathID 对不上。"
                             % (key, pid, inbundle))
        g = font_guid(name)
        mp = os.path.join(ASSETS, "CardPresentation/Resources/Fonts", name + ".asset.meta")
        if not os.path.isfile(mp):
            raise SystemExit("[STOP] 字体资产还没生成：%s（先跑 `gen_tmp_font_assets.py`）" % mp)
        got = re.search(r"^guid: ([0-9a-f]{32})", io.open(mp, encoding="utf-8").read(), re.M).group(1)
        if got != g:
            raise SystemExit("[STOP] %s 的 guid = %s，与派生式给的 %s 不一致" % (name, got, g))
        print("[selftest] %-22s %-22s -> %s  (容器键 %s 里 %d 条 pathID 含它)"
              % (pid, name, g, key, len(inbundle)))
    return resolved


# ─────────────────────────── 改写 ───────────────────────────

def fix(path, resolved, check):
    raw = io.open(path, "rb").read()
    crlf, lf = raw.count(b"\r\n"), raw.count(b"\n")
    if crlf:
        raise SystemExit("[STOP] %s 的行尾不是纯 LF（CRLF=%d / LF=%d）—— 改它会翻行尾，停手。"
                         % (path, crlf, lf))
    text = raw.decode("utf-8")
    report = []
    for pid, (cls, guid, _) in resolved.items():
        old = "m_Script: {fileID: %s, guid: %s, type: 0}" % (pid, ZERO)
        new = "m_Script: {fileID: 11500000, guid: %s, type: 3}" % guid
        n = text.count(old)
        report.append(("m_Script/%s" % cls, n, 0 if check else n))
        text = text.replace(old, new)
    for pid, name in FONTS.items():
        old = "m_fontAsset: {fileID: %s, guid: %s, type: 0}" % (pid, ZERO)
        new = "m_fontAsset: {fileID: 11400000, guid: %s, type: 3}" % font_guid(name)
        n = text.count(old)
        report.append(("m_fontAsset/%s" % name, n, 0 if check else n))
        text = text.replace(old, new)

    # 先把要写的内容整个算好、确认没抛异常，再落盘（CLAUDE.md §二：`wb.write(<表达式>)` 会先截断成 0 字节）
    out = text.encode("utf-8")
    if not check:
        io.open(path, "wb").write(out)
    left = out.decode("utf-8").count("guid: " + ZERO)
    return report, left


def main():
    check = "--check" in sys.argv
    resolved = selftest()
    bad = 0
    for name in PREFABS:
        p = os.path.join(ASSETS, "WarpforgeBooster/Prefabs", name)
        rep, left = fix(p, resolved, check)
        done = sum(r[2] for r in rep)
        print("%s %-32s 改写 %d 条 %s · 该文件剩余 guid0 **%d** 条（含 `m_Sprite` 那族）"
              % ("[check]" if check else "[fix]  ", name, done,
                 " · ".join("%s=%d" % (r[0], r[1]) for r in rep), left))
        if not check and done == 0:
            bad += 1
    if not check:
        print("\n提示：这两份 prefab 是 `BoosterPackExporter` 重导的产物 —— "
              "**每次跑过 `BoosterPackExporter.Run` 之后要再跑一次本脚本**（`m_Script` 那 24 条会被重写回 guid 0）。")
    return 0 if bad == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
