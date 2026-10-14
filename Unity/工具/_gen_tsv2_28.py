# -*- coding: utf-8 -*-
"""_gen_tsv2_28.py -- 生成第二十八会话「第二批并账」的插入指令 TSV。

锚点**从第一批 TSV 里读**（那 5 条已由上一轮代理验证过「全文唯一」），只换插入内容。
用法: python -I -X utf8 d:/4/Unity/工具/_gen_tsv2_28.py
"""
import io

TSV1 = "d:/4/Unity/资料/普查产出_第二十八会话/并账_插入指令.tsv"
TSV2 = "d:/4/Unity/资料/普查产出_第二十八会话/并账_插入指令_第二批.tsv"

# 锚点：从 TSV1 里取「每个目标文件最后一条插入指令的锚点」（它们就是那 5 条唯一整行）
raw = io.open(TSV1, "rb").read().decode("utf-8")
anchors = {}
for ln in raw.split("\r\n")[1:]:
    if not ln:
        continue
    c = ln.split("\t")
    if len(c) != 4:
        continue
    anchors[c[1].strip()] = c[2]         # 目标文件 -> 锚点（取最后一条，5 个目标各一条）
assert len(anchors) == 2, anchors

A = "`资料/任务_卡组卡牌战斗.md`"          # 2 列表
B = "`项目任务.md` §29·b"                  # 4 列表

PIN = "`资料/普查产出_第二十八会话/`"
W1R = PIN + "W1_逐卡音效与animateFeel.md"
W2R = PIN + "W2_卡名换官方.md"
R1R = PIN + "A1814_A1813_诊断.md"
R2R = PIN + "战斗事件层_现核.md"

rows = [
    # ---- §一 对战引擎 ----
    (A, "A1824", "🔴 **换卡名带出：`RuleEngineTest.cs` 的中文档钉值会变**（`:18941` `3→4` · `:18903 reanimateViaZh==12` · `:18901/18902` 的 132/67）⇒ **先跑一次 `RuleEngineTest.Run` 取真值再钉**（⚠️ 跑 Unity 只有主对话能干）。判据 → " + W2R),
    (A, "A1826", "⚠️ **`ruleprobe` 三指纹**：⑦ 解析输出 md5 `3070ac7c…` **逐字未变**（卡名不进解析输出）；⑤ 卡池 md5 `481c4a51…→c1f10198…`，但**换卡名之前就已经在走「此时不判」**（钉住 `fe3362d9…`）⇒ 要不要重钉待判（本会话已派 `W6`）。判据 → " + W2R),
    (A, "A1828", "⚠️ **`RuleEngineTest.cs:20477` 注释里的路径 `数据/游戏数据/cards_engine.json` 不存在**（真身 = `MyGame/Assets/RuleEngine/Resources/cards_engine.json`）⇒ 直接改注释。判据 → " + W2R),
    # ---- §二 卡组编辑 ----
    (A, "A1825", "🔴 **换卡名带出：`prebuilt_decks.json` 的 `defensiveNameZh` 49 条过期**（8 张督军防御卡；例 `ASH56` 激励传承 → **鼓舞遗产**）⇒ **重跑 `工具/gen_prebuilt_decks.py`**（它自己 `:602` 就这么写着；消费点 `MainMenuScene.cs:3671` · `LiveOpsEventWindow.cs:1233` · `PracticeModePopup.cs:1601/1822`）。本会话已派 `W6`。判据 → " + W2R),
    # ---- §三 卡牌 ----
    (A, "A1827", "🔴 **`descZh` 里仍写着 89 个旧卡名 / 150 处**（例 `ASH17` 效果文字说「部署 1 个**幽卫**」，那张卡现读叫「**幽冥守卫**」）⇒ 卡名与效果文字两套说法。⛔ **⛔ 别派机械替换** ——「复生」在「每当你**复生**一个残骸时」是**动词**不是卡名，短名与常用词同形 ⇒ **须逐条人判**。判据 → " + W2R),
    (A, "A1829", "⚠️ **待裁**：`SOR24` 我们是 `Simulacrum Imperialis`、官方键的英文是 `Simulacrum Celestian`（中文「天界模拟体」）—— 键是 id 故照换了，**这处建议人判**。判据 → " + W2R),
    (A, "A1833", "⚠️ **`Core/VfxMap.cs:227` 的文档数字过期**（写「240 KB · 979 张卡」，现读 **288.9 KB / 989 张**）⇒ 按铁律 5 改注释。判据 → " + W1R),
    (A, "A1836", "⚠️ **`VfxMap.ByCard` / `CardTableCards` / `CardTableEntries` 都是静态且无 `clear`**（`CardView` 那族同病）⇒ 自检跨用例不重置；今天无害，**第二个消费者要当心**。判据 → " + R2R),
    # ---- §四 界面 / 功能复刻 ----
    (A, "A1830", "🔴 **异画「两档风格」是【我们自己按文件名拆的】—— 原版只有 1 档**：`AlternateArtSyle` 只有 `Default=0` / `HammerAndBolter=5`；`GetAlternateArtStyleOnly` = `FindAll(style != 0)`（**轮播只列非 Default**）；7 张卡全是 `style=5` ⇒ **Styles 页应只列 1 档（Hammer and Bolter · logo = CEL SHADED）**。判据 → " + PIN + "A1808_异画风格与logo.md"),
    (A, "A1834", "🔴 **一条探针失效**：`CollectionWindow.cs:947-952` 的 `TimeOneTextRelayout` 排在 `RebuildCardsCells` **之后**（字形已烘、文本未变）⇒ 它量到的 **0.0 ms 是必然结果**；而 `:3831` 的文案正拿它当「字体假设已推翻」的证据 ⇒ **探针要挪到建卡之中/之前**才有鉴别力。判据 → " + R1R),
    (A, "A1835", "🔴 **断言盲区**：`Editor/MainMenuScene.cs:931` 的 `CheckAt(…, \"SettingsBtn 齿轮\")` **只比中心**（定义 `:164-172` 只读 `t.localPosition`）⇒ **命中框缩掉一半它照样绿**（这正是 `A1814` 能长期活着的原因）⇒ 要补「边界两态 + 灭自证」。判据 → " + R1R),
    # ---- §29·b（其余）----
    (B, "A1831", "🔴 **`工具/import_original_audio.py` 漏导 4 条卡 SO 引用的语音**（`EC17`/`GOF5`/`TL21`/`UM57`；那 4 个 `.ogg` 在 `assets_full/bundle_*cardassets_assets_all/AudioClip/` 里**都在**，是它只按 `card_index.json` 的引用导）⇒ 那 4 张卡**仍无声**", "🔴", "见 " + W1R),
    (B, "A1832", "🔴 **`voice_lines.json`（`card_index.json` 那条链）归属有错**：**34 条 clip 挂在别的卡名下**（`BL4→BL46` · `DA34→DA83` · `EC21→EC_Terminator…`），另 **35 张**有精确绑定的卡**整张不在表里** ⇒ 表该不该重导是一笔新账（本轮按「卡 SO 权威」处置）", "⚠️", "见 " + W1R),
]


def row_for(target, num, text, *rest):
    if target == B:
        state, ptr = rest
        return "| 🆕 **`%s`** | %s | %s | %s |" % (num, text, state, ptr)
    return "| 🆕 **`%s`** | %s |" % (num, text)


out = ["编号\t目标文件\t锚点（该文件里已存在的整行，原文照抄）\t插入内容（完整一行，含首尾 |）"]
for r in rows:
    tgt, num = r[0], r[1]
    line = row_for(*r)
    assert line.startswith("|") and line.endswith("|"), line
    out.append("\t".join([num, tgt, anchors[tgt], line]))

data = "\r\n".join(out) + "\r\n"
io.open(TSV2, "wb").write(data.encode("utf-8"))
print("写出 %s（数据行 %d）" % (TSV2, len(rows)))
