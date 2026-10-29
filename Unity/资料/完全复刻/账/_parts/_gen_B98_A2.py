# -*- coding: utf-8 -*-
"""生成账补丁 `账/_parts/B98_A2.tsv`（账 `日常_奖励.tsv` 第 155 行 = `EndBattleRewardHolder.SetReward`）。

做法（⛔ 不手抄大段原文，避免转写错）：读主表第 155 行 → 在指定几列**追加**本轮的句子 → 写 11 列补丁。
只写 `_parts/`，⛔ 不动主表（合并由调度台做）。
"""
import io

MAIN = r"d:/4/Unity/资料/完全复刻/账/日常_奖励.tsv"
OUT = r"d:/4/Unity/资料/完全复刻/账/_parts/B98_A2.tsv"
LINE = 155          # 文件行号（表头 = 1）

HEADER = ["域", "原版类", "方法", "判据出处", "判据要点", "状态", "我们的落点", "差异", "等级", "轮次", "备注"]

APPEND = {
    3: "[B98_A2 2026-10-29 复核] `MB_3928` 四格（`m_enableAutoSizing 1` / `m_fontSizeMin 18` / "
       "`m_fontSizeMax 40` / `m_fontSizeBase 36`）与 `RT_3139` 的框 70×45 已逐字段现读重核；"
       "换算判据 = `Label.cs:1047-1085 FontSizeToPxNow` + `:1442-1447 NominalPx` + `:1293 SetAutoFitBox`"
       "（内部 `fontSizeX = cur × xPx / NominalPx()` = `xPx ÷ FontSizeToPxNow(1, face)`，`cur` 在式子里约掉）",
    6: "[B98_A2 2026-10-29] 复核确认 `EndPanel.cs:1062-1068` 就是现行代码"
       "（`SetAutoFitBox(70, 45, 18·qk, 40·qk, 36·qk)`，`qk = FontSizeToPxNow(1, ActualFace)`）"
       "+ 类尾 `:1147-1151` 三常量；`FillRewardRow` 仍在 `:1029`（零漂移），本件**无生产代码改动**。"
       "⚠️ `SetRewards` 全仓**零生产调用点**（`grep -rn \"SetRewards(\" Assets` 只剩三个自检文件）",
    7: "【B98_A2 2026-10-29 复核】`Quantity` 三档**已由 B97_W41 落地**（提交 `e7335ca2`），"
       "本代理独立复核：读回 `FontSizeMin/Max/Base = 18 / 40 / 36` + `AutoSizing = true`。"
       "**本件新增断言** → `CardPresentation/Editor/B98A2_Assert.cs`："
       "A 三档独立复核 · B **换算承重（灭自证）**（直接传原版 18 ⇒ 读回 18÷qk ≠ 18，再走一次生产口回 18）· "
       "C 生产态（`SetRewards` 零调用点 ⇒ 整行藏；喂 1 条才露，两态互为灭自证）· "
       "D 一位数 `1` 与五位数 `12345` 的**渲染宽高**都 ≤ 70×45。状态**维持 B** 的唯一原因仍是 ①（`+0x1f8` 虚槽未实现）",
    9: "·B98_A2 2026-10-29",
    10: "B98_A2 2026-10-29：施工单 §一 155 指的残差（`Quantity` 自适应三档）**在代码里已由 W41 关闭**"
        "（`EndPanel.cs:1062-1068`），本代理只做「独立复核 + 三条 W41 没覆盖的断言」，未改生产代码"
        "（该文件行数 1153 不变、`git status` 干净）。⚠️ 断言文件**尚未接线**：调度台在 "
        "`Editor/BattleScene.cs` 的合计行之前追加 `B98A2_Assert.Run(); pass += B98A2_Assert.Pass; fail += B98A2_Assert.Fail;`"
        "（同一物理行追加 ⇒ 行数零变化）。",
}


def main():
    b = io.open(MAIN, "rb").read()
    assert b.count(b"\x00") == 0, "主表里有 NUL"
    lines = b.split(b"\n")
    cols = lines[LINE - 1].decode("utf-8-sig").split("\t")
    assert len(cols) == 11, "主表第 %d 行列数 = %d" % (LINE, len(cols))
    assert cols[1] == "EndBattleRewardHolder", cols[1]
    assert cols[2] == "SetReward(RewardInfo reward)", cols[2]
    # 表头逐字照主表
    hdr = lines[0].decode("utf-8-sig").split("\t")
    assert hdr == HEADER, hdr

    for i, extra in APPEND.items():
        assert "\t" not in extra and "\n" not in extra and "\r" not in extra
        assert "\x00" not in extra
        if i == 9:
            # `轮次` 那一列以「·」分隔各轮 ⇒ 直接接上去（不再加空格，保住 `·B97_W41 ·B97_W8` 那种写法）
            cols[i] = (cols[i].rstrip() + extra).strip()
        else:
            cols[i] = (cols[i].rstrip() + (" " if cols[i].strip() else "") + extra).strip()

    for i, c in enumerate(cols):
        assert "\t" not in c, "第 %d 列里有 TAB" % i
        assert "\n" not in c and "\r" not in c and "\x00" not in c, "第 %d 列里有换行/NUL" % i

    body = "\t".join(HEADER) + "\n" + "\t".join(cols) + "\n"
    io.open(OUT, "wb").write(body.encode("utf-8"))
    print("wrote", OUT, len(body.encode("utf-8")), "bytes")
    # 回读自检：8 列往返全等（裸 split）
    rb = io.open(OUT, "rb").read().decode("utf-8")
    r = [x.split("\t") for x in rb.split("\n") if x.strip()]
    assert len(r) == 2 and all(len(x) == 11 for x in r), [len(x) for x in r]
    assert r[0] == HEADER
    print("回读 OK:", len(r), "行 ×", len(r[0]), "列; 状态 =", r[1][5], "等级 =", r[1][8])


main()
