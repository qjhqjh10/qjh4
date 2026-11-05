# B146-E12 · CompTruths 断言计数订正（2026-11-05）

## 一、现算方法与结果（条目数 / crunched 数）

- **方法**（独立现算，未采信 E11 报告）：
  `io.open(ArtBaker.cs,'rb')` + 按行扫描，定位 `static readonly … Dictionary<string, ArtCompressionTruth> CompTruths =`（现读 `:970`）到配对的 `    };`（`:1242`），
  逐行匹配 `^\s*\{\s*"([^"]+)"\s*,` 取键；`Crunched` 由构造器实参判（`UiDxt1(true)` / `UiDxt5(true)` / `Crunched = true`）。
  脚本落在 `C:/Users/qjh36/AppData/Local/Temp/e12_count.py`（工程外，纯读）。
- **结果：条目 234 条 · `Crunched=true` 92 条。**
- 分档：`UiUncompressed()` 25 + 裸 `Uncompressed/-1` 5 + `UiDxt5(false)` 108 + `UiDxt1(false)` 1 = **142 非 crunched**；
  `UiDxt5(true)` 86 + `UiDxt1(true)` 6 = **92 crunched**。大小写不敏感重名 **0**。
- 与 E11 报告一致：8 条新增（`Debug *` 7 + `40k_rewards_bt_missions_expansion pass`）全是 `UiDxt5(false)` ⇒ 条目 226→234、crunched 不变 92。

## 二、改了哪些期望与注释（文件:行）

`CardPresentation/Editor/B142_E33_Assert.cs`
- `:18` 头部「现读 144 条（70 条 crunched）」→ **234 / 92**；`:28` 分组说明、`:219` C 组 Section 标题同步。
- `:100-101` 常量 `CompTruthsCount 144→234` · `CompTruthsCrunched 70→92`（C1/C2 用它们 ⇒ 文案自动同步）。
- `:286` D1「现读 92 条 crunched=true」· `:288` D2「现读 234 条」· `:299` 注释 144→234。
- `:292-297` D4/D5/D6 正向标记改为「现读 234 条」「92 条 crunched=true」「234 条里 92 条 true」。
- `:303-304` D7 文案「注释写的 234 / 92 == 表内真实 …」。

`CardPresentation/Editor/ArtBaker.cs`（**零行数变化**，3 行就地改数字，`git diff -U0` 为 3 组成对 `-/+`）
- `:531` 「现读 144 条」→「现读 234 条」
- `:635` 「现读 144 条 · 70 条 crunched=true」→「现读 234 条 · 92 条 crunched=true」
- `:648` 「现表内 144 条里 70 条 true」→「现表内 234 条里 92 条 true」
- ⛔ 未动 `CompTruths` 任何条目、未增删行、未改行尾。

## 三、灭自证

新增 `NegativeControls` 的 **E5/E6/E7** + 辅助 `SourceCompTruthKeys(src)`（解析 `ArtBaker.cs` 源文本 `CompTruths` 块键集，**独立于反射的第二读口**）：
- **E5** 前提：源文本键集解得出来（解析失败 ⇒ 硬红，不静默跳过）。
- **E6 双读**：`foreach` 逐键枚举数 == `table.Count` == 常量 234 ⇒ **删一条时三处不可能同时相等**（只断一个 `Count` 挡不住「删条目 + 改常量」这类同源改动中的前一半，双读能挡住「只删不改编」与「枚举/Count 分叉」）。
- **E7 逐键 SetEquals**：运行时键集 vs 源文本键集，差集**点名**（「源有运行无 / 运行有源无」）⇒ 源里声明了却没进字典（条目落到 `};` 之外、键名拼错、重复键初始化被吞）当场红。
- 会红的情形已写进文件头「灭自证」清单（`:36`）。**已知边界**：若有人「删一条 + 同时改 234→233 + 源里也真删」，E7 与常量会一起漂——那需要外部固定清单才能拦，本文件用 C3/C4 的 8 个具名期望 + D7 双向对账做局部兜底，未再引入全表摘要常量。

## 四、typecheck

`TMPDIR=/tmp/wf_e12 bash d:/4/Unity/工具/typecheck.sh` → **运行时错误数 0 / 编辑器错误数 0**（连跑两次，第二次在最终注释改动之后）。
未跑 Unity 批处理、未动 git。

**顺手发现**：全 `Assets` grep `CompTruthsCount|现读 144|144 条|70 条 crunched` —— 除本文件与 `ArtBaker.cs` 三行外，
其余「144 条」命中与压缩档无关（`BattleDriver.cs:32448` 是本文件账落点数；`BaseEventPopup.cs:411/:465` 是本地化键数）；无第二个文件硬编码 `CompTruths` 计数。
