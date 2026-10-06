# -*- coding: utf-8 -*-
"""R2 · 第七步：渲染最终报告 md。"""
import io, json
from collections import OrderedDict, Counter

OUT = r"d:/4/Unity/资料/普查产出_1015"
rows = json.load(io.open(OUT + '/r2_rows_final.json', encoding='utf-8'))
by = OrderedDict()
for r in rows:
    by.setdefault(r['file'], []).append(r)

def esc(s):
    return (s or '').replace('|', '\\|').replace('\n', ' ').strip()

# 每文件的 VC 节点清单（供表头一行摘要）
vc = {}
for v in json.load(io.open(OUT + '/r2_vc_nodes.json', encoding='utf-8')):
    if v['file'].endswith('ViewportClip.cs'):
        continue
    vc.setdefault(v['file'], []).append('%s@%d' % (v['lhs'] or v['parent'] or '?', v['line']))
# AddComponent 型
for r in rows:
    pass

L = []
A = L.append
A('# R2 · A799 全量表（199 个 `MenuDraw.Text` / `TextBox` 调用点逐条解父链）')
A('')
A('> **只读普查**。跑这一趟的是只读代理 R2；读数时刻 = **2026-10-06**，工程根 `d:/4/Unity/MyGame/Assets/CardPresentation`。')
A('> 🔴 **行号会漂** —— 锚点按【文件 + 第 1 实参 + 句子】，行号只做参考（本轮实测：`PurchasePremiumWindow` 从 `:698/:711` 漂到 `:707/:720`，`AllianceMemberTab` 从 `:785` 漂到 `:797`）。')
A('> 判据与口径 = `资料/普查产出_1014/RO_文字半边与压暗层.md` §二 的三步法 + 第三层筛子；本文件只做它 §二·③「剩下怎么批量判」。')
A('> 机械解链脚本（**只读**，全部落在 `资料/普查产出_1015/`，未写工程源码树）：`r2_extract.py`（抽 199 个调用点）· `r2_vc.py`（抽 VC 建点）· `r2_resolve2.py` / `r2_final.py`（解父链，含调用方跳转）· `r2_callers.py`（通用找调用方）· `r2_table.py` / `r2_md.py`（合成裁定 + 出表）；中间产物 `r2_*.json`。')
A('')
A('---')
A('')
A('## 一 · 结论摘要')
A('')
A('**口径**：全量 = `grep -rn "MenuDraw\\.\\(Text\\|TextBox\\)(" --include=*.cs .` ⇒ 206 行命中；剔注释/字符串后 = **199 个代码调用点**（`Text` 146 / `TextBox` 53）· **45 个文件**（生产 41 · `Editor/` 4）。✅ **与 `RO_文字半边与压暗层.md` §二·② 的 199 逐位吻合**。')
A('')
A('**逐条解链结果（199/199 全部定案，判不了 = 0）**：')
A('')
A('| 判定 | 生产 | Editor 夹具 | 合计 |')
A('|---|---|---|---|')
n_prod = sum(1 for r in rows if r['verdict'] == '会' and not r['file'].startswith('./Editor/'))
n_ed = sum(1 for r in rows if r['verdict'] == '会' and r['file'].startswith('./Editor/'))
n_no = sum(1 for r in rows if r['verdict'] == '不会')
n_un = sum(1 for r in rows if r['verdict'] == '判不了')
A('| **会（A781 首次上裁）** | **%d** | %d | **%d** |' % (n_prod, n_ed, n_prod + n_ed))
A('| 不会（链不落 VC 之下，或已同框幂等） | %d | %d | **%d** |' % (
    sum(1 for r in rows if r['verdict'] == '不会' and not r['file'].startswith('./Editor/')),
    sum(1 for r in rows if r['verdict'] == '不会' and r['file'].startswith('./Editor/')), n_no))
A('| 判不了 | 0 | 0 | **0** |')
A('')
A('**① 会新裁的完整清单（生产 9 处；`文件:行号` 为读数时刻）**')
A('')
A('| # | 站点 | 锚点（节点名 / 句子） | 父链落到的 VC | 来路 |')
A('|---|---|---|---|---|')
named = [
    ('./Shell/PurchasePremiumWindow.cs', 707, '`Army Name`（`MenuDraw.TextBox(cn, …)`）', '本文件 `vpVc`（`ViewportClip.Hang(sv, "Viewport", …)` @512）', '账上已点名'),
    ('./Shell/PurchasePremiumWindow.cs', 720, '`Premium Text`（`MenuDraw.TextBox(pi, …)`）', '同上', '账上已点名'),
    ('./Shell/RankedRewardEventWindow.cs', 530, '`Army Text`（`MenuDraw.TextBox(tb, …)`）', '本文件 `vpVc`（`Hang(sv, "Viewport", ScrollR, …)` @397）', '账上已点名'),
    ('./Shell/LeaderboardRow.cs', 163, '`Rank`（`MenuDraw.Text(row, …)`）', '经 `LeaderboardWindow._listContent`（`Node(vp, …)`，`vp = vpVc.transform`；VC @586 / @383）', '**账上判错**：账写「Leaderboard 4 处不受影响」——那是 `LeaderboardWindow.cs` 自己的 4 处；这 4 处在另一个文件'),
    ('./Shell/LeaderboardRow.cs', 206, '`Name`', '同上', '同上'),
    ('./Shell/LeaderboardRow.cs', 213, '`Guild Name`', '同上', '同上'),
    ('./Shell/LeaderboardRow.cs', 223, '`Points`', '同上', '同上'),
    ('./Shell/MatchLogRow.cs', 153, '`Text(p, …)`（`Match Log` 行的唯一一份 builder）', '经 `BattleLogPopup._content`（`Node(vp, …)`，VC @163）**与** `BattleLogTab._content`（`Node(vpNode, …)`，VC @89）——**两条调用路都在 VC 之下**', '账里完全没提'),
    ('./Shell/SocialWindow.cs', 367, '`SocialPage.Text` 里的 `MenuDraw.TextBox(parent, …)`', '经 `AllianceMemberTab` 的 `v.Text(row, …)`（:1378/:1397/:1399/:1455）→ `AllianceMemberRow.BuildAll(v, mcontent, sc)`，`mcontent = Node(vp, …)`（@1137）在 VC `vp`（@1125）之下', '🔴 **本表新查出**（账的 16 组包装器里没有它）'),
]
for i, (f, ln, anchor, chainv, src) in enumerate(named, 1):
    A('| %d | `%s:%d` | %s | %s | %s |' % (i, f, ln, anchor, chainv, src))
A('')
A('**② 与抽样结论不符的条目**')
A('')
A('- ✅ **账上 8 处全部复现**（行号有漂移，逐条按锚点核对上了）：账的 `:698/:711` → 今 `:707/:720`；账的 `AllianceMemberTab:785` → 今 `:797`。')
A('- 🔴 **新增 1 处**：`Shell/SocialWindow.cs:367`（`SocialPage.Text` 的 `MenuDraw.TextBox`）。**理由**：这个口**自己不调** `MenuDraw.ClipText`（`grep` 全文件 = 0），而它在 `AllianceMemberTab` 里有一条调用路的父链落在 VC `vp`(@1125) 之下 ⇒ A781 给它**首次上裁**。账的 §二·③ 只把 `SocialWindow.Text` 列进「16 组包装器」，没往下解。')
A('- 🔴 **自动解链一度报了 4 处「会」但复核后是「不会」**（逐条读源码推翻，属**第三层筛子**）：`AllianceMemberTab.cs:797`（`:799` 已无条件 `ClipText(lb, null, …)`）· `ChatPanel.cs:684`（`:694` 已无条件 `ClipText(lb, clip, …)`）· `ChatPanel.cs:143`（该重载的调用点是输入框占位串，不在 `Viewport` 子树里）· `SettingsWindow.cs:1765`（`:1766` 已 `ClipText`）。⇒ **它们与账上 §二·② 的「不会新增（同框幂等，已裁过）」一致**。')
A('- ⚠️ **账上「甲类 181 处链停窗根 / 乙类 16 组包装器」这两个数不可直接相加**（8 + 181 + 16 = 205 ≠ 199 ⇒ 它们本来就不是一个划分）。本表的「不会」**184** 处按**原因**划分：**链停窗根 152** · **`new` 出来的独立根 9** · **包装器已裁（幂等）8** · **人工核读确认的跨文件链 15**（这一档就是账的「乙类」解到底的产物）。')
A('- ✅ **账上「VC 挂点 45 处 / 27 个生产文件」逐位成立**（本表复算：45 = `ViewportClip.Hang` 36 + `AddComponent<ViewportClip>` 9，其中 1 处就是 `Hang` 自己的实现 `Shell/ViewportClip.cs:211`；生产 38 行 / **27 文件**（含 `ViewportClip.cs` 自己）· `Editor/` 7 行 / 3 文件）。')
A('')
A('**③ 判不了的条目数 = 0**（199/199 全部定案）。')
A('')
A('---')
A('')

A('## 二 · 全量表（199 行 · 按 `文件:行号` 排序）')
A('')
A('图例：`判定` = **会**（A781 会改行为：首次上裁）/ **不会**（链不落 VC 之下，或已同框幂等）；`已裁?` = 该处（或其包装器）**是否已经**显式 `ClipText` / 已传 `clip`。')
A('')
A('| 文件:行号 | parent 实参 | 解链结果 | 判定 | 已裁? | 置信度 |')
A('|---|---|---|---|---|---|')
for f, rs in by.items():
    for r in rs:
        A('| `%s:%d` | `%s` | %s | **%s** | %s | %s |' % (
            f, r['line'], esc(r['arg0'])[:46], esc(r['chain_short'])[:200], r['verdict'], r['has_cliptext'], r['conf']))
A('')
A('---')
A('')
A('## 三 · 与 `RO_文字半边与压暗层.md` 不符 / 补充之处')
A('')
A('1. **`SocialWindow.cs:367` 是新的一处**（该文件 §二·③ 的 16 组包装器里只写了「`SocialWindow.Text`」，没往下解；解完是**会**）。')
A('2. **「甲类 181 处｜乙类 16 组」这两个数不再是「没解完」**：本表把 199 个点**逐条解到了底**（含 16 组包装器全部往调用方再解一跳，个别链路还跨了 2～3 跳）。')
A('3. **「`MenuWindowBase.Text/TextBox` 是特例：自己就调 `ClipText` ⇒ 幂等」——成立**，且**同一形状的还有 6 个文件 / 7 个口**，账里一个都没列：`WindowsManager.Text`(:385) · `PlayerProfileWindow.Text`(:650) · `SettingsWindow.Text`(:1766) · `ChatPanel.Text`(:694) · `AllianceMemberTab.TextSoft`(:799) · `ItemDrawer.TextCentered`(:1021) / `ItemDrawer.ClippedText`(:1268)。⇒ 因「已幂等」而**不算新裁**的口共 **7 个文件 / 9 处调用点**，不是只有 `MenuWindowBase` 一家。')
A('4. **`MatchLogRow` 与 `LeaderboardRow` 的父链比账上写的更宽**：账只写了 `LeaderboardWindow._listContent` / `BattleLogPopup._content`；实测 `MatchLogRow.Build` 有**两个**调用点（`BattleLogPopup:238` 与 `BattleLogTab:215`），**两条路都**在 VC 之下 ⇒ 判「会」的依据是两条，不是一条。')
A('5. **账上「VC 挂点 45 处 / 27 个生产文件」**：处数复算一致（45），**文件口径**见 §一·② 最后一条。')
A('6. ⚠️ **账上的「抽样 20 处里 `CampaignTab.cs:516` 是一处调用点」不成立**：`Shell/CampaignTab.cs` 里 `MenuDraw.Text/TextBox` 命中 = **0**（`grep -c` 实读）。那一行是 `ClipText` 一类，不是本表的 199 之一。')
A('')
A('---')
A('')
A('## 四 · 顺手发现（⛔ 本代理一处未改）')
A('')
A('### 4·1 🔴 「包装器自己裁 + A781 在 `MenuDraw.Text` 里又裁一刀」= 同一个标签裁两刀')
A('')
A('A781 之后，`MenuDraw.Text` / `TextBox` 在**末尾**会调一次 `ClipText`（`Shell/MenuDraw.cs:1620` / `:1685`）。而上面 §一·② 点名的 **7 个包装器**是「先 `MenuDraw.Text`，再自己 `ClipText`」⇒ **同一颗 `Label` 每次建出来都被裁两刀**。')
A('')
A('- **画面后果：查不到**（同一个框、顶点已在框内 ⇒ `ClipQuad` 第二次不动它；alpha 那一半按 A225-② 的写法是**绝对值**，注释里写明就是为了「同一代 mesh 里裁两刀」幂等）。**但这是「靠两处各自成立才幂等」，不是结构上保证的** —— 判据是 `MenuDraw.ClipTmpMesh` 里 `al[k]` 那一段注释 + `ClipQuad` 的夹取语义。')
A('- **真的副作用 = 诊断计数**：`MenuDraw.TextClipUnavailable` / `TextClipUploadSkipped` 会被**数两遍**（`Text` 内一刀 + 包装器一刀）。而 `MenuDraw.TextCore` 的注释**正是**为了这个才把内层拆出来（原文：「既白做一次，又会让 `TextClipUnavailable` / `TextClipUploadSkipped` 这两个诊断计数**虚高**」）—— 那条纪律**在包装器这一层没有对应物**。')
A('- **谁会被数两遍**（生产）**：`ItemDrawer.TextCentered` / `ClippedText` · `ChatPanel.Text`(两处) · `AllianceMemberTab.TextSoft` · `SettingsWindow.Text` · `PlayerProfileWindow.Text` · `WindowsManager.Text` · `MenuWindowBase.Text/TextBox`。')
A('- ⛔ **判据不足、本代理不动手**：要判「这是不是缺陷」得先定「双击计数算不算红线」（`Editor/RewardsScene.cs:2891` 有一条断 `TextClipUnavailable == 0`）。**建议**：由调度台裁一次「包装器那一句 `ClipText` 该不该删」。')
A('')
A('### 4·2 ⚠️ 一个**没有写进任何文档**的「包装器不裁」口：`SocialPage.Text` / `SocialView.Text`')
A('')
A('`Shell/SocialWindow.cs` 的 `SocialPage.Text`（→ `MenuDraw.TextBox`）与 `SocialPage.Hit` 这一族**全文件零 `ClipText`**，而它的使用者 `AllianceMemberTab` / `AlliancesTab` / `FriendsTab` **三个文件都有 `ViewportClip`**。⇒ A435 那一轮「把文半边接上」时，这一条口**整族漏了**（与 `ItemDrawer`/`ChatPanel`/`PlayerProfileWindow` 那几个口被点名的改法不同形）。A781 会**顺带补上**它 —— 这是**好事**，但账上没人知道，且它没有配断言。')
A('')
A('### 4·3 记账误差（供 `项目任务.md` / 账本订正用）')
A('')
A('- 账 §二·② 的 3 条行号已全部漂移（`PurchasePremiumWindow` 698→**707**、711→**720**；`AllianceMemberTab` 785→**797**）。')
A('- 账 §二·② 写「`LeaderboardRow.cs:163/206/213/223`」——**行号本轮未漂**，逐字命中。')
A('- 账 §二·② 的「`ChatPanel.cs:677` / `SettingsWindow.cs:1765` / `ItemDrawer.cs:999/1262`」——`ItemDrawer` 两处未漂；`ChatPanel` 677→**684**；`SettingsWindow` 未漂。')
A('')
io.open(OUT + '/R2_A799全量表.md', 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n')
print('lines =', len(L))
print('table rows =', sum(len(v) for v in by.values()))
