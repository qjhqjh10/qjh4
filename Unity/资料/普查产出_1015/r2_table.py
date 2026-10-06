# -*- coding: utf-8 -*-
"""R2 · 第六步：出表。合成自动解链结果 + 逐条核读的人工裁定。"""
import io, json, re
from collections import defaultdict, OrderedDict

OUT = r"d:/4/Unity/资料/普查产出_1015"
rows = json.load(io.open(OUT + '/r2_final.json', encoding='utf-8'))

# 人工裁定表：(file, line) -> (verdict, chain_short, has_cliptext, conf, note)
M = {}
def m(f, lines, verdict, chain, has, conf, note):
    for l in lines:
        M[(f, l)] = (verdict, chain, has, conf, note)

# ---- 自动解出的「会」里，四条是【幂等】的（包装器自己已 ClipText） ----
m('./Shell/AllianceMemberTab.cs', [797], '不会', '`TextBox(parent)` ← `TextSoft`', 'Y', '高',
  '包装器 :799 无条件 `MenuDraw.ClipText(lb, null, …)` ⇒ 与 A781 同框幂等')
m('./Shell/ChatPanel.cs', [684], '不会', '`TextBoxBox(p)` ← 静态 `Text(p, …, clip)`', 'Y', '高',
  '包装器 :694 无条件 `MenuDraw.ClipText(lb, clip, …)`（生产传 null）⇒ 幂等')
m('./Shell/SettingsWindow.cs', [1765], '不会', '`Text(p)` ← 本窗 `Text`', 'Y', '高',
  '包装器 :1766 `MenuDraw.ClipText(lb, clip, …)` ⇒ 幂等')
m('./Shell/SettingsWindow.cs', [2105], '不会', '`f._root = Node(parent,…)` ← `MenuInputField.Create` ← `:1506/:1508` 的 `blk`', 'N', '中',
  '`blk = Node(page, "Host/Client Block", …)`（:1495）是 `page` 的**直接子件**；本窗的 VC 是 `page/Scroll View/Viewport`（@665）⇒ 与 `blk` 子树**不相交**')
m('./Shell/ItemDrawer.cs', [999], '不会', '`pd := Node(parent,…)` ← `TextCentered(parent)`', 'Y', '高',
  '本函数 :1021 无条件 `MenuDraw.ClipText(lb, st.Clip, st.ClipSoftness)`（生产 `st.Clip` 恒 null ⇒ 走父链）⇒ 与 A781 同框幂等')
m('./Shell/ItemDrawer.cs', [1262], '不会', '`node := Node(…)` ← `ClippedText(node)`', 'Y', '高',
  '本函数 :1268 无条件 `MenuDraw.ClipText(lb, st.Clip, st.ClipSoftness)` ⇒ 幂等')
m('./Shell/ChatPanel.cs', [143], '不会', '`TextBox(p)` ← 11 参 `Text` ← `InputField (TMP)`', 'N', '中',
  '调用点 :251 的 `input = Node(_enterText,…) ← Node(chat,…) ← Node(_holder,…) ← transform`；`Viewport`(VC@523) 是 `_root` 的**兄弟**子树')

# ---- 自动判不了的：逐条核读后的人工裁定 ----
m('./Shell/MenuWindowBase.cs', [340], '不会', '`TextBox(parent)` ← 子类调用点（各窗根之下）', 'Y', '高',
  '本口自己就调 `MenuDraw.ClipText(lb, RenderClip, ClipSoftness)`（:343）⇒ 幂等')
m('./Shell/PlayerProfileWindow.cs', [626], '不会', '`Text(parent)` ← 四页 ≈50 处', 'Y', '高',
  '本口 :650 无条件 `MenuDraw.ClipText(lb, null, …)` ⇒ 幂等')
m('./Shell/WindowsManager.cs', [378], '不会', '`Text(parent)` ← 各窗', 'Y', '高',
  '本口 :385 `if (_st.RenderClip.HasValue) MenuDraw.ClipText(lb, RenderClip, …)` ⇒ 与 A781 判据同一条 ⇒ 幂等')
m('./Shell/SocialWindow.cs', [367], '会', '`TextBox(parent)` ← `SocialPage.Text`/`SocialView.Text` ← `v.Text(…)`', 'N', '中',
  '🔴 **本表新查出**：本口不裁（全文件零 ClipText）。`AllianceMemberTab` 的 `v.Text(row, …)`（:1378/:1397/:1399/:1455）父链经 `AllianceMemberRow.BuildAll(v, mcontent, sc)`，而 `mcontent = Node(vp, "Content", …)`（:1137）就在 VC `vp`（`ViewportClip.Hang(sv,…)` @1125）之下 ⇒ **首次上裁**')
m('./Shell/OfferContainer.cs', [1356], '不会', '`LabelFit(parent)` ← `OfferContainer.Build` ← `Editor/ShopScene` 夹具', 'N', '中',
  '`OfferContainer.Build` 全仓只有 `Editor/ShopScene.cs:2632/3106/3748` 三个调用点，其根是 `new GameObject(...)` 独立根 ⇒ 不在任何 VC 之下')
m('./Shell/CostCurveDrawer.cs', [86, 109], '不会', '`Text(node)` ← `Build(parent)` ← `DeckInfoPopup` / `PracticeModePopup`', 'N', '中',
  '调用方 `DeckInfoPopup` 零 VC；`PracticeModePopup` 的 `_general = New(_info,…)`、`_info = New(root,…)` 在窗根下，两处视口（VC@850/@1013）在别的子树里')
m('./Shell/DailyRewardPopup.cs', [265, 281, 316, 336], '不会', '`BuildEntry(parent)`/`BuildDrawer(parent)` ← `content` ← 窗根', 'N', '高',
  '全文件零 `ViewportClip`；`BuildEntry(content, r, i)` 的 `content` 由窗根派下')
m('./Shell/DailyStreakPopup.cs', [347], '不会', '`curLbl.transform` / `p`（`p = Node(root,…)`）', 'N', '中',
  '三元两支都在 `p`（`Streak Successful`）之下，而 VC `Viewport`(@363) 是 `p` 的子件 ⇒ 不相交')
m('./Shell/MissionRerollPopup.cs', [371, 413], '不会', '`BuildButton(parent)` / `BuildPriceCell(parent)` ← 窗根', 'N', '高', '全文件零 `ViewportClip`')
m('./Shell/ReferralPopupWindow.cs', [466, 472], '不会', '`Node(_input,…) ← _inputView ← _content ← _window ← root(=transform)`', 'N', '高', '全文件零 `ViewportClip`')
m('./Shell/TrophyInfoPopup.cs', [343, 408], '不会', '`bar/box` ← `_progressHolder`/`select` ← `controls` ← `right` ← `win` ← `root(=transform)`', 'N', '高', '全文件零 `ViewportClip`')
m('./Shell/BaseOfferPopup.cs', [922, 936, 948, 950], '不会', '`gb/box/disc` ← `pd/row` ← `TextNode` ← `WindowNode` ← `transform`', 'N', '高', '全文件零 `ViewportClip`')
m('./Shell/PlayerProfileWindow.cs', [741, 745], '不会', '`Root` 属性 = `transform`（:491）', 'N', '高', '「看别人的档案」页的根 = 该页组件自己的 GameObject')
m('./Shell/SearchingOpponentWindow.cs', [111], '不会', '`_foeFound = f`（Build 里 `f = Node(side,…)`、`side = Node(root,…)`）', 'N', '中',
  '`_foeFound` 由 Build 期的局部量经 `isFoe` 分支写入（:257）；字段写入点在调用点之后 ⇒ 静态链靠这条核读补')
m('./Shell/RankedDivisionInfo.cs', [88], '不会', '`Text(content)` ← `Build(parent)` ← `RankedEventWindow:110` 的 `col`', 'N', '高', '`col = Node(root,…)`，`root = transform`')
m('./Shell/RankedEventWindow.cs', [86, 100, 120, 124], '不会', '`col/lbn/tg` ← `Node(root,…)`；`root` 由 `LiveOpsEventWindow.Build()` 传 `transform`', 'N', '高', '`BuildLeftColumn(root)` 实参 = `transform`（LiveOpsEventWindow.cs:366/375）')
m('./Shell/SkirmishEventWindow.cs', [96, 98, 106, 115, 205], '不会', '`col/vic/skull` ← `Node(root,…)`；`b` ← `Node(root,…)`', 'N', '高', '同上一行：`root = transform`；`BuildBanned` 由 `BuildExtras(root)` 调（:180）')
m('./Shell/EnergySinglePlayerOnlyEventWindow.cs', [342, 344, 378, 455, 474, 479, 490, 521, 533, 539, 558, 573], '不会',
  '`rp/tm/ins/asp/hdr ← Node(root,…)`；`bar ← Node(rp,…)`；`row ← Node(levels,…)`；`pv ← Node(rp,…)`', 'N', '高',
  '两处 VC（`Image Mask` 挂 `root` @326、`Viewport` 挂 `sel` @564）都是窗根的**子件**，与这些站点的父链**不相交**（`asp` 与 `Image Mask` 是兄弟）')
m('./Shell/AllianceEventScoreInfo.cs', [137], '不会', '`row ← Node(levels,…) ← Node(_root,…)`', 'N', '中',
  '`_root` 由 `AllianceEventScorePanel.Create` 的 `c._root = go.transform` 写入（跨对象成员赋值，静态解链解不到）；全仓唯一调用方 = `Editor/MainMenuScene.cs:8334` 夹具（`host.transform`，零 VC）')
m('./Shell/AllianceEventScorePanel.cs', [114, 125, 138, 144, 150], '不会', '`_inAlliance/_noAlliance ← Node(_root,…)`；`_viewLb/_joinBtn/_noLb` 再下一层', 'N', '中', '同上')

# ---- 自动解不出的「链」文本：逐条核读后手写（结论不变，只换可读的解链叙述） ----
CHAIN_OVERRIDE = {
    ('./Shell/MatchLogRow.cs', 153):
        '`p`（本口形参）← **两个调用点都在 VC 之下**：① `BattleLogPopup.cs:238` 传 `_content`（`= Node(vp, …)`，VC `Viewport` `Hang(matches,…)` @163）② `BattleLogTab.cs:215` 传 `_content`（`= Node(vpNode, …)`，VC `Viewport` `Hang(mt,…)` @89）',
}

# ---- 应用人工裁定 ----
for r in rows:
    k = (r['file'], r['line'])
    if k in M:
        v, ch, has, conf, note = M[k]
        r['verdict'], r['chain_short'], r['has_cliptext'], r['conf'], r['final_note'] = v, ch, has, conf, note
    else:
        # 自动结果
        ch = ' > '.join([p for p in r['path'] if '⇒' in p or ':=' in p or '★★' in p or '【' in p or '↪' in p])[:220]
        r['chain_short'] = ch or r['arg0']
        r['has_cliptext'] = 'N'
        r['conf'] = r['conf'] if r['conf'] in ('高', '中', '低') else '中'
        r['final_note'] = r['note']
    if k in CHAIN_OVERRIDE:
        r['chain_short'] = CHAIN_OVERRIDE[k]

io.open(OUT + '/r2_rows_final.json', 'w', encoding='utf-8', newline='\n').write(
    json.dumps(rows, ensure_ascii=False, indent=1))

from collections import Counter
print(Counter(r['verdict'] for r in rows))
hui = [r for r in rows if r['verdict'] == '会']
print('会 =', len(hui), '（生产', sum(1 for r in hui if not r['file'].startswith('./Editor/')), '· Editor 夹具',
      sum(1 for r in hui if r['file'].startswith('./Editor/')), '）')
for r in sorted(hui, key=lambda z: (z['file'], z['line'])):
    print('  %s:%d  %s  %s' % (r['file'], r['line'], r['api'], r['arg0'][:40]))
