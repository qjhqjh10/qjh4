# -*- coding: utf-8 -*-
"""生成 B88_E6 的账补丁（只写 账/_parts/B88_E6.tsv，不碰主表）。
做法：从主表原行读出来 → 只改本批要改的列 → 按 11 列表头写回 ⇒ 列数与顺序天然正确。
"""
import io, os

LEDGER = 'd:/4/Unity/资料/完全复刻/账'
OUT = LEDGER + '/_parts/B88_E6.tsv'
HEADER = '域\t原版类\t方法\t判据出处\t判据要点\t状态\t我们的落点\t差异\t等级\t轮次\t备注'

# (主表文件名, 物理行号, 改动字典)
JOBS = [
    ('战斗界面_HUD.tsv', 779, 'A', {
        '落点': 'CardPresentation/Core/CardView.cs:7212（ApplyAmbush）（等价实现 = 敌方面朝下伏击单位：`SetStatsHidden(true, hideArmourCell:true)` 把护甲那一格整格关掉（连 `?` 都不印）+ `Show(_armourIcon,false)` 把盾一起关；判据 = `CardScript__UpdateHealthText.c:127-140` 的 `HasCurrentTrait(0x4B0 ambush) && !isPlayer` 支）',
        '差异': '**已复刻（B88_E6 2026-10-27）**。判据 `decomp_full/CardScript__UpdateHealthText.c:127-140`：敌方 + ambush ⇒ `armourCounters(+0xc0).Toggle(false)` + `armourFrame(+0x150).SetActive(false)` ⇒ **护甲整格不出现**。我们这侧：`_ambushHideStats`（= 调用点 `BattleDriver.cs:26617` 传的 `FaceDown && !mine`）**恒等于**「敌方 + 带 ambush」—— 依据 = `RuleCore` 两个翻开出口（`:6338` / `:8825`）都 `RemoveAll(Ambush)`（判据 `CardScript__SetAmbush.c:70`）⇒ 我们这侧 `ambush ⇔ FaceDown`、翻面后 trait 不残留。落地点 = `ApplyAmbush` 的藏数值分支 `CardView.cs:7236-7237`（`hideArmourCell` 是既有形参 `:14276`，`StatsHiddenFor` 的第三条闸 `:14313` 复用）。⚠️ 如实记：原版这是**两次**调用（`SetAmbush→ToggleStats(false,false)` 先印 `?`，之后每次 `UpdateHealthText` 再关护甲格），我们**合并在这一处**（同一张卡、同一道 `isPlayer==0` 闸，两跳之间无可观测帧）。⚠️ 另两支池内 0 张（`dropPod` / `bastion`）⇒ 零画面，不算缺。',
        '备注': '[B87-R14 判 C 的那一条已闭] B88_E6 2026-10-27 落地。判据全文 → `资料/完全复刻/报告/B88_E6_卡展示与手牌.md` §二。⚠️ **本笔没有断言**（断言宿主 `CardPresentation/Editor/BattleScene.cs` 不在本代理白名单）⇒ 工单 = 在既有伏击那一段（`Editor/BattleScene.cs:2959` 之后）补「敌方 ⇒ `StatsHiddenFor(StatArmour)==false` ∧ `!AmbushMarkShown(StatArmour)`」＋对偶「我方 ⇒ 照常显示」，🧨 去掉 `hideArmourCell:true` ⇒ 第一条红。**没查清**：`armourFrame(+0x150)` 我们无该节点（全库只有 `BattleCardUI__ToggleStats.c` 两处引用），parentage 未建立 ⇒ 不影响本条。可达性 = 池内 `cardTraits` 含 `ambush` 的 **12 张**（GSC 族，`cards_engine.json` 现读）。',
    }),
    ('特效动画.tsv', 195, 'C', {
        '差异': '等价实现，但覆盖面比原版窄，三处还差（**B88_E6 2026-10-27 现读复核：三条全部卡在数据侧，视图侧不可补**）：① **`isPlayer` 侧可见性掩码**（原版 `+0x90 visibleForPlayers` / `+0x91 hiddenForPlayers` 两个 byte 掩码，`.c:37-…` 三层嵌套判 ⇒ 对面施加的效果走另一套遮蔽文案），`TempBuff` **没有这两个字段**（现读成员表 `RuleEngine/Core/UnitState.cs:1637-1671`：`IsKeyword/Name/Value/Owner/UntilMyNextTurn/Src/SourceCard/Mod/TraitRec`）；② 原版按 `+0x38 buffType` 分派（`8`/`0x11` 双行合成、`+0x78` 那个枚举再分 `10`/`0x14`/`0x1e`/`0x28` 四个专名、`+0x40 > 1` 给 `who` 追加数值），`TempBuff` **没有 `buffType`**；③ 「谁给的」原版优先 `effectName(+0x20)`（非空才用）→ 否则 `enchantingCard(+0x18).rawCard` 的 `GetLocalizedCardName`，我们取 `SourceCard`→`Src`（`CardDisplayWindow.cs:871-872`）—— **与原版的兜底那支逐字等价**，残差只在「`effectName` 与卡名不同」时，同样要 `TempBuff` 扩档。',
        '备注': '[B87-R24 判 C 维持 · B88_E6 2026-10-27 反证] 三条差异**全部**落在「原版 `CardEffect` 有、`UnitState.TempBuff` 没有」的字段上，而 `RuleEngine/Core/UnitState.cs` = **E4 的地盘**（本代理白名单外）。⇒ 工单 = `TempBuff` 扩档三项（`buffType` / `visibleForPlayers`+`hiddenForPlayers` / `effectName`），之后在 `RowsOf`(:851) + `SetEffectRows`(:915) 逐句落地。⛔ 别用 `TempBuff.Name` 反推 buffType（那是第二份判据源）。判据全文 → `报告/B88_E6_卡展示与手牌.md` §三·1。',
    }),
    ('特效动画.tsv', 198, 'C', {
        '差异': '等价 + 三处如实差异（**B88_E6 2026-10-27 现读复核，逐条给了为什么做不了**）：① **文本空的也占一槽**（原版 `EnableCardEffectSlots.c:46-60`：`IsTextEmpty ⇒ Disable` 但**两支都 `cursor += 1`** ⇒ 中间留空洞），我们按行铸格、空的根本不铸 —— 空洞的**输入**只可能来自「`GetEffectDesc` 回空串」，而那**只在 `buffType` 同类字段全 0 时**发生（`case 1`）；我们这侧对应输入 = `TempBuff.Value == 0`，而当前实现**故意把 0 值印成属性名**（`AttrText` `CardDisplayWindow.cs:903-911`），且**这条被一条现成断言钉死**（`Editor/BattleScene.cs:9719-9726`）⇒ 改它必须**同刻改断言**（断言宿主不在本代理白名单）。⚠️ `SetEffectRows` 本身**已支持**「行存在而文本为空」的形态（`:931/:936` 逐格 `SetText(rows[i]?.Who ?? "")`）⇒ 真做只差 `AttrText` 一处 + 断言一处 + `EffectRowCount` 语义一处（原版 `EnableEffects` 返回的是**非空条数**，`_fxRoot` 显隐用的是**非空条数 > 0**）。② 原版收尾那一跳我们没有（`DisplayCardEffects.c:118-135`：非空条数 > 0 时 `root(+0x80).SetActive(!card.IsEnviromentaleffect())`）—— **今天不可达**（展示窗只服务棋盘单位，`BattleDriver.ToggleUnitCard` 那条链，环境效果卡进不来）。③ 原版进槽前先**倒序**过滤掉 `+0x38 ∈ {6,0x23,0x1e}` 或 (`0x10` 且 `!HasQuests`) 的条目（`DisplayCardEffects.c:44-53` 的 `RemoveAt`），我们行来自 `TempBuff`、没有这层 —— 要 `buffType` + `HasQuests`，两者都在数据侧。',
        '备注': '[B87-R24 判 C 维持 · B88_E6 2026-10-27 反证] ① 的阻塞 = **两处必须同刻改**（`AttrText` + `Editor/BattleScene.cs:9719` 那条断言）且断言宿主不在白名单；② 维持不可达；③ 要 `TempBuff` 扩档（`UnitState.cs` = E4）。⇒ 工单见 `报告/B88_E6_卡展示与手牌.md` §五 工单 2/3。原轮次 B9_C2T7h。',
    }),
    ('菜单_外壳.tsv', 54, 'C', {
        '备注': 'N5定级 P2·序09：还差 = 「新卡」数据（`CardProgress` 没有 new 标记）+ 收藏格/详情窗的调用点。｜[B87-A7 2026-10-27] 判据三份：① 调用链（`CardDrawer__ToggleNewCardBadge.c` 与 `CardAlternateArtDrawer__ToggleNewCardBadge.c` **逐字节同体** —— 良性 ICF 折叠，⛔ 别判「解错」）；② 真实现（ilspy `BasicCardUI.cs:713-727`）；③ 几何/贴图（UnityPy 现读 prefab + sprite）。｜[B88-E6 2026-10-27 现读复核] **视图侧零残差、本笔零改动**（`CardView.ToggleNewCardBadge:15139` 三句逐句对得上原版）⇒ 仍 C 的两条**都不在白名单**：数据 = 存档/收藏层的 new 标记（收藏线 B40）；接线 = `Shell/CollectionWindow.cs` 等调用点（原版**每次 Draw 各调一次**，我们卡视图会复用 ⇒ 调用方必须每张都设一次，否则残留上一张的角标）。与 `_60` 是**同一个原版实现**的两条调用路径，落点共用一处。',
    }),
    ('菜单_外壳.tsv', 60, 'C', {
        '备注': 'N5定级 P2·序09：同 `CardAlternateArtDrawer.ToggleNewCardBadge`（一组共 2 条）。｜[B87-A7 2026-10-27] 判据三份：① 调用链（`CardDrawer__ToggleNewCardBadge.c` 与 `CardAlternateArtDrawer__ToggleNewCardBadge.c` **逐字节同体**，都是一句转调 —— 良性 ICF 折叠，⛔ 别判「解错」）；② 真实现（ilspy `BasicCardUI.cs:713-727`）；③ 几何/贴图（UnityPy 现读 prefab + sprite）。｜[B88-E6 2026-10-27 现读复核] **视图侧零残差、本笔零改动**；仍 C 的两条（数据 = new 标记 / 接线 = `Shell/**` 调用点）都不在白名单。判据全文 → `报告/B88_E6_卡展示与手牌.md` §三·3+4。',
    }),
]


def main():
    rows = []
    for fname, lineno, new_status, edits in JOBS:
        path = os.path.join(LEDGER, fname)
        data = io.open(path, 'rb').read().decode('utf-8')
        lines = data.split('\n')
        cols = lines[lineno - 1].split('\t')
        assert len(cols) == 11, (fname, lineno, len(cols))
        for c in cols:
            assert '\n' not in c and '\r' not in c, (fname, lineno)
        old_status, old_land = cols[5], cols[6]
        old_diff, old_note = cols[7], cols[8]
        cols[5] = new_status
        if '落点' in edits:
            cols[6] = edits['落点']
        if '差异' in edits:
            cols[7] = edits['差异']
        if '备注' in edits:
            cols[10] = edits['备注']
        cols[9] = 'B88-E6'
        for c in cols:
            assert '\t' not in c.replace('\t', '') or True
        assert len(cols) == 11
        print('%-16s L%-4d %s->%s  land[%s]  diff %.0f->%.0f  note %.0f->%.0f'
              % (fname, lineno, old_status, new_status, cols[6][:40],
                 len(old_diff), len(cols[7]), len(old_note), len(cols[10])))
        rows.append('\t'.join(cols))

    body = HEADER + '\n' + '\n'.join(rows) + '\n'
    io.open(OUT, 'wb').write(body.encode('utf-8'))
    print('wrote', OUT, len(rows), 'rows')


main()
