# -*- coding: utf-8 -*-
"""生成 `账/_parts/B9_E2UDW.tsv`（账补丁：只改 差异 / 备注 / 轮次 三列）。

用法（**不带 `--write` 只做 dry-run，一个字节都不写**）：
    python -I -X utf8 d:/4/Unity/资料/完全复刻/账/_parts/_gen_B9_E2UDW.py
    python -I -X utf8 d:/4/Unity/资料/完全复刻/账/_parts/_gen_B9_E2UDW.py --write

规矩（调度台 2026-10-27 的派活）：
  · 表头 11 列**逐字**从主表第一行复制；
  · `状态`（第 6 列）**保持原值** —— 只有调度台能裁；
  · 只改 `差异`(7) / `备注`(10) / `轮次`(9)；
  · `方法` 列自带完整签名 `(...)`（本脚本按方法名定位，⛔ 不按行号）。
"""
import io
import os
import sys

BASE = os.path.dirname(os.path.abspath(__file__))
MAIN = os.path.join(BASE, '..', '战斗逻辑驱动.tsv')
OUT = os.path.join(BASE, 'B9_E2UDW.tsv')
ROUND = 'B9_E2UDW'

# ---- 每个方法的追加文案（键 = 方法列原文）-------------------------------
DIFF_ADD = {
    'DissolveMinionCard(bool dissolveY)':
        '｜🆕 **2026-10-27（`B9_E2UDW`）**：④ 原账那条「`+0x2e0` 查不到是哪颗」**已结案** = '
        '`BattleCardUI.RemnantBody3D`（一个 `RemnantBody` 组件；`il2cpp_out/dump.cs:26237` 字段偏移 `0x2E0` 实读'
        '——原账说的「`dump.cs` 字段表到 `0x298` 就断了」是错的，表一直排到 `0x2E8`）。'
        '原版末句 = `RemnantBody3D.Toggle(false, withAnimation: true, null)`（🥇 ILSpy 逐句 '
        '`cpp2il/out_cpp2il_dev_ilspy/CardScript.cs:11643-11646`；`.c` 侧那三个实参 `(0,1,0)` 与之逐一吻合）'
        '⇒ **撤残骸体、带消失动画**。已在 `CardPresentation/Core/CardFeel.cs:3726` 接上：'
        '`if (card.RemnantBodyRoot != null) card.SetRemnantBody(false, card.RemnantBodyPrefabName);`'
        '（等价物内部按 `RemnantBodyPrefabName` 选 `From Remnant` 片段 / 灵族立即撤，判据在 `CardView.SetRemnantBody`）。'
        '⑤ 同支的 2D 淡入缓动 `Linear → OutQuad`（原版 `FadeCard` 没写 `SetEase`；`Assets/Resources/DOTweenSettings.asset:31` '
        '`defaultEaseType = 6`）。`typecheck` 0/0。',
    'UnDissolveWholeCard()':
        '｜🆕 **2026-10-27（`B9_E2UDW`）**：① 原账「需接线」那半**已不成立** —— 现核调用点 = '
        '`CardPresentation/Battle/BattleDriver.cs:8176`（`ResolvePlayTargetByUnit` 的取消支：'
        '`CardFeel.MoveBackToHand(card, CardView.QHandMoving, targeted)` → 本方法；`targeted` 取自 `CardDef.Targeted == 1`）。'
        '② 本轮修掉两处与原版不符（判据 = `cpp2il/out_cpp2il_dev_ilspy/BasicCardUI.cs:761-770` 的 `FadeCard` 全方法体）：'
        '**淡的量** `card.SetAlpha`（整卡，连 3D 卡体一起淡）→ **`SetAlpha2D`**（= 原版 `fadeGroup` / `2DCard` 那个 CanvasGroup；'
        '同族纪律见 `CardPresentation/Core/CardView.cs:6516`「⛔ 不能拿整卡 `SetAlpha` 顶替」）；'
        '**缓动** `Ease.Linear → Ease.OutQuad`（原版没有 `SetEase` ⇒ DOTween 默认；`DOTweenSettings.asset:31` = 6），'
        '并补 `OnKill`（原版那 200 ms 是 `Task.Delay`、不受补间打断影响，且 `if (plVar2 != null)` 才动）'
        '⇒ `CardPresentation/Core/CardFeel.cs:3653-3667`。'
        '③ 🔴 **仍欠的是「出对半」**（已另立新账）：原版打出 targeted 战术/附魔卡时会走 `CardScript.MoveIntoFieldFromHand` '
        '→ `DissolveWholeCard(timeToDissolveCard)`（`decomp_full/CardScript__MoveIntoFieldFromHand.c:60-75`：'
        '关碰撞 + `DOScale(×globalVars+0x40)` + `FadeCardItems(0, t)`），取消时才由本方法淡回来；'
        '我们的战术卡是「打出去就销毁视图」（`BattleDriver.cs:13708-13710`）⇒ 本方法的 α 起点本来就是 1 ⇒ '
        '**观测量在当前链路上不可观察**（⛔ 别据此判「实现无效」）。',
    'UndissolveToDeck()':
        '｜🆕 **2026-10-27（`B9_E2UDW`）**：① 两个调用点补上（原账没写）：'
        '`BattleManager._ResolveDrawCard_d__432__MoveNext.c:96-103`（牌库空 ⇒ 从**墓地**逐个往回捞：'
        '`ResetToValuesInHand(1)` → `UndissolveToDeck()` → 重挂 `DeckManager` 的 transform → `RemoveFromCemeteryList` '
        '→ `ShowBoardObjects(false)` / `SetObjectVisibility(false)`）与 '
        '`BattleManager._ResolveRecallToDeck_d__543__MoveNext.c:182` / `:319`。'
        '② 五步逐句（ILSpy `cpp2il/out_cpp2il_dev_ilspy/CardScript.cs:11683-11692`）：`SetCardImage()` → '
        '`FadeCardItems(1f, **0f**)` → `StopSummonSicknessAnim()` → `gameObject.SetActive(true)` → '
        '`cardCollider2D.Toggle(true)`。⚠️ **淡入时长实参是 `0`（瞬时）**，不是 `timeToDissolveCard` 的 0.2 —— ⛔ 别照 0.2 写。'
        '③ 我们这侧「墓地 → 牌库」是**数据级**（`RuleEngine/Core/RuleCore.cs:3073` `ReshuffleCemeteryIntoDeck`，只搬卡 id），'
        '没有可复用的「停在半溶」视图；新建视图的初值（α 1 / `HitEnabled` true / 无召唤病动画 / 卡图真身）**恰等于**这五步的终态 '
        '⇒ 维持 C：它是「状态等价」，不是逐句对齐（⛔ 仍不许为它新建「把视图停成半溶再复原」的路径）。',
}

NOTE_ADD = {
    'DissolveMinionCard(bool dissolveY)':
        ' ｜ 🆕 2026-10-27 `B9_E2UDW`：本行**不再有「查不到」项**（`+0x2e0` 已结案 = `RemnantBody3D`）。'
        '仍是等价替身的两处：`ActivateCard(true)`（视图生命周期归驱动层）与「`FadeCardItems(1,0.2)` 由 `ArtAlpha` 承担」'
        '（整卡那一档在场上本来就是 1，看得见的是卡面）。',
    'UnDissolveWholeCard()':
        ' ｜ 🆕 2026-10-27 `B9_E2UDW`：⚠️ 本条观测量今天**不可观察**（α 起点恒 1，见差异列 ③）；'
        '要验它得先造出 α≠1 的起点（或等「出对半」落地）。',
    'UndissolveToDeck()':
        ' ｜ 🆕 2026-10-27 `B9_E2UDW`：调用点与五步已补进差异列；本条**只差「同一张卡停在半溶态」这个前提**，'
        '一旦将来出现「复用视图且带溶解态」的设计，落点 = 洗回牌库那一路（`RuleCore.ReshuffleCemeteryIntoDeck`）。',
}


def main():
    write = '--write' in sys.argv[1:]
    raw = io.open(MAIN, 'rb').read()
    assert b'\r\n' not in raw, '主表应为纯 LF —— 先确认再动'
    lines = raw.decode('utf-8').split('\n')
    header = lines[0]
    assert len(header.split('\t')) == 11, ('表头不是 11 列', len(header.split('\t')))
    assert header.split('\t')[5].strip() == '状态', header.split('\t')

    out_rows, seen = [], set()
    for L in lines[1:]:
        if not L.strip():
            continue
        f = L.split('\t')
        assert len(f) == 11, ('数据行不是 11 列', len(f), f[:3])
        method = f[2].strip()
        if method in DIFF_ADD:
            assert method not in seen, ('方法重名，无法唯一定位', method)
            seen.add(method)
            # 只在「差异」列还没被本轮追加过时追加（幂等）
            if ROUND not in f[7]:
                f[7] = f[7].rstrip() + DIFF_ADD[method]
                f[10] = f[10].rstrip() + NOTE_ADD[method]
            f[9] = ROUND
            out_rows.append('\t'.join(f))

    assert len(out_rows) == 3, ('没找齐三行', [r.split('\t')[2] for r in out_rows])
    for r in out_rows:
        assert len(r.split('\t')) == 11, ('输出行不是 11 列', r.split('\t')[2])

    text = '\n'.join([header] + out_rows) + '\n'
    if not write:
        print('=== DRY-RUN（未写盘）=== 目标：', OUT)
        for r in out_rows:
            f = r.split('\t')
            print('---', f[2], '| 状态=', f[5], '| 等级=', f[8], '| 轮次=', f[9])
            print('   差异尾部：', f[7][-220:])
            print('   备注尾部：', f[10][-160:])
        print('（要真写盘：加 --write）')
        return
    io.open(OUT, 'wb').write(text.encode('utf-8'))
    print('WROTE', OUT, len(out_rows), 'rows')


if __name__ == '__main__':
    main()
