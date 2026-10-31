#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""生成 `账/_parts/B109_E35.tsv`（B109_E35 · C14/C15/C17/C18 生产接线）。

为什么用脚本而不是手写：`merge_ledger_parts.py` 的更新语义是**整行替换**（按 (原版类, 方法) 命中），
所以补丁行必须把主表那一行的 11 格**原样带全**，只在本脚本点名的两格末尾追加。
手抄那两行（尤其 `备注`）必然抄错 ⇒ 用「读主表 → 追加 → 写补丁」这条路，逐列原样。

用法：
    python -I -X utf8 d:/4/Unity/资料/完全复刻/账/_parts/_gen_B109_E35.py
    （只读主表 + 写 `_parts/B109_E35.tsv`；⛔ 不动主表）
"""
import io
import os

LEDGER = r"d:/4/Unity/资料/完全复刻/账/日常_奖励.tsv"
OUT = r"d:/4/Unity/资料/完全复刻/账/_parts/B109_E35.tsv"
COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]

# (原版类, 方法) -> 追加到「我们的落点」/「备注」/「轮次」的文本
ADD = {
    ("EventImageSelectorController", "Initialize(List<EventAsset> eventAssets)"): dict(
        land="· 🆕 2026-10-31（B109_E35）：**生产调用点已接** —— "
             "CardPresentation/Shell/DraftModeWindow.cs:4800（`BuildTimedEventImages`：两格 "
             "`RankingIcon(6)` → `Frame` · `HeaderImage(3)` → `Badge`，"
             "挂 `Draft Mode Pay State/Alliance Trophy Image controller`，"
             "= 原版 `DraftModeMenuPayStateTimed.OnOpen` 末句）",
        note="｜[B109_E35 2026-10-31] **生产接线已落**（宿主 = `Shell/DraftModeWindow.cs` 的 `BuildTimedEventImages`，"
             "gate = `TimedDraftLocal.Enabled`，与 `BuildTimedFamily` 同闸）；"
             "断言 = `Editor/B109_E35_DraftImage_Assert.cs` G3 组（两格 type = [6,3]；没资产 ⇒ 两格都关）。"
             "⚠️ 事件资产表在服务端（本地 13 份活动 SO 里没有 draft 事件）⇒ 生产口径走原版「没有该 type ⇒ enabled=false」那一支。",
        round="｜B109_E35"),
    ("EventImageSelectorController", "TryDraw(List<EventAsset> eventAssets)"): dict(
        land="· 🆕 2026-10-31（B109_E35）：**生产调用点已接** —— "
             "CardPresentation/Shell/DraftModeWindow.cs:4800（`BuildTimedEventImages` 逐格 `Initialize` ⇒ 逐格 `TryDraw`）",
        note="｜[B109_E35 2026-10-31] **生产接线已落**（同上一行那条 `Initialize`）；"
             "断言 = `Editor/B109_E35_DraftImage_Assert.cs` G4 组：只喂 `RankingIcon` ⇒ "
             "`Frame` 那格记下资产名并画上、`Badge` 那格资产名保持 null 且仍关着（type 闸真的在起作用）。",
        round="｜B109_E35"),
    ("LiveopUIDrawer_EventAssetSprites", "EventAssetImages.Initialize(ILiveOps liveopEvent)"): dict(
        land="· 🆕 2026-10-31（B109_E35）：**生产调用点已接** —— "
             "CardPresentation/Shell/DraftModeWindow.cs:4751（`BuildEventImagesHeader`：一格 `GameModeIcon(12)`，"
             "挂 `Game Mode Header With Back Button/Header Background/Game Mode Icon`）",
        note="｜[B109_E35 2026-10-31] **生产接线已落**（宿主 = `Shell/DraftModeWindow.cs` 的 `BuildEventImagesHeader`，"
             "在 `WireHeaderComponent` 之后调）；断言 = `Editor/B109_E35_DraftImage_Assert.cs` G1/G4 组。"
             "⚠️ **结构差异（如实记）**：原版这一跳在 `WindowHeaderWithBackButton.Initialize` 的 `eventUIDrawers` 循环里"
             "（`LiveopUIDrawer_EventAssetSprites : LiveopEventUIDrawers`），而我们的抽屉机制仍是空壳"
             "（`IWindowHeaderUIDrawer` 全仓 0 实现，账 416 未建）⇒ 这一批**直调**，⛔ 不是原版那条父链。",
        round="｜B109_E35"),
    ("LiveopUIDrawer_EventAssetSprites", "LiveopUIDrawer_EventAssetSprites.Initialize(ILiveOps liveopEvent)"): dict(
        land="· 🆕 2026-10-31（B109_E35）：**生产调用点已接** —— "
             "CardPresentation/Shell/DraftModeWindow.cs:4751（`BuildEventImagesHeader`：外层遍历那一格）",
        note="｜[B109_E35 2026-10-31] **生产接线已落**（同上一行）；原版**两个实例**（Demo 变体 / Timed 变体各一格 "
             "`GameModeIcon(12)`）在我们这里只有**一颗表头** ⇒ 只建一格（Demo/Timed 共用）。"
             "断言 = `Editor/B109_E35_DraftImage_Assert.cs` G1（无资产 ⇒ 关）/ G4（注入 `GameModeIcon` ⇒ 画上）。",
        round="｜B109_E35"),
}


def main():
    b = io.open(LEDGER, "rb").read()
    bare = b.count(b"\r") - b.count(b"\r\n")
    if bare:
        raise SystemExit("主表有裸 CR：%d" % bare)
    eol = "\r\n" if (b.count(b"\r\n") and b.count(b"\r\n") == b.count(b"\n")) else "\n"
    lines = b.decode("utf-8-sig").split(eol)

    hdr = [h.strip() for h in lines[0].split("\t")]
    if hdr != COLS:
        raise SystemExit("主表表头不是那 11 列：%r" % (hdr,))

    out_rows = []
    done = set()
    for ln in lines[1:]:
        if not ln.strip():
            continue
        f = ln.split("\t")
        if len(f) != len(COLS):
            # 主表里若有坏行，直接停下（⛔ 不猜）
            raise SystemExit("主表有列数 != 11 的行：%r" % (f[:3],))
        k = (f[1].strip(), f[2].strip())
        add = ADD.get(k)
        if add is None:
            continue
        f[6] = f[6].rstrip() + add["land"]
        f[9] = f[9].rstrip() + add["round"]
        f[10] = f[10].rstrip() + add["note"]
        out_rows.append(f)
        done.add(k)

    missing = [k for k in ADD if k not in done]
    if missing:
        raise SystemExit("主表里没找到这些 (原版类, 方法)：%r" % (missing,))

    txt = eol.join(["\t".join(COLS)] + ["\t".join(r) for r in out_rows]) + eol
    data = txt.encode("utf-8")
    # 先算进变量、确认无误再写盘（`io.open(p,'wb').write(<表达式>)` 会先截断）
    tmp = OUT + ".tmp"
    io.open(tmp, "wb").write(data)
    os.replace(tmp, OUT)
    print("写出 %s（%d 行数据 + 表头；行尾 %s；%d 字节）"
          % (OUT, len(out_rows), "CRLF" if eol == "\r\n" else "LF", len(data)))


if __name__ == "__main__":
    main()
