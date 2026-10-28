# -*- coding: utf-8 -*-
"""B92 波 4 · N5b 账补丁生成器（第九十二会话 2026-10-28 · 调度台追加三件）
用法：python -I -X utf8 _gen_B92_N5b.py            # 只校验，不写盘
      python -I -X utf8 _gen_B92_N5b.py --write    # 真写 账/_parts/B92_N5b.tsv
内容：① 修被本轮弄过期的 TimerDisplay.Awake / OnDestroy 两行 + LocalizationChange 的判据出处
      ② 判据出处 `TimerDisplay.cs:NNN` 补全成反编译件全路径（不是我们的落点，是判据来源）
      ③ 23 行「机制不同」按调度台裁定③(a)**保持 B** 并写具体缺口
      ④ GameStaticData 两行按裁定③(b) 分支级升 A
      ⑤ GameModes 两行补记「两处反射已改强类型」
纪律：裸 split("\t")；UTF-8-BOM + CRLF；方法列原样照抄主表。
"""
import io, os, sys
from collections import Counter

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
LEDGER = os.path.join(ROOT, '账')
OUT = os.path.join(ROOT, '账', '_parts', 'B92_N5b.tsv')


def rows(path):
    b = io.open(path, 'rb').read()
    return [l.split('\t') for l in b.decode('utf-8-sig').replace('\r\n', '\n').split('\n') if l.strip()]


CACHE = {}


def table(name):
    if name not in CACHE:
        CACHE[name] = rows(os.path.join(LEDGER, name))
    return CACHE[name]


def pick(fname, dom, cls, meth):
    fn = FILE_OVERRIDE.get((dom, cls, meth), fname)
    hits = [x for x in table(fn)[1:] if len(x) >= 11 and x[0] == dom and x[1] == cls and x[2] == meth]
    assert len(hits) == 1, (fn, dom, cls, meth, len(hits))
    return list(hits[0])


# 同一原版方法在两张账里各有一行时（铁律：两行都要更新）——这里点名「真身在哪一份」
FILE_OVERRIDE = {
    ('未分类/其他', 'AlternateArtStyleItem<T>', 'TryGetItem(AlternateArtSyle alternateArtSyle, out T returnItem)'):
        '泛型与启动链_补读.tsv',
}


# ============================ ③(a) 具体缺口（每行一句，⛔ 不许只写「机制不同」）============================
GAP = {
    ('ContentSizeFitterMinMax', 'ClampSize()'):
        "缺**组件本体**（`ContentSizeFitter` 派生：重写 `SetLayoutHorizontal/Vertical` 各一句调 `ClampSize`；"
        "`ClampSize` = 读 `rectTransform.sizeDelta`、在 `useX/useY` 为真时按 `m_minSize/m_maxSize` 夹一次再 `set_sizeDelta`，原版 38 行）"
        "＋ **一个真正的消费者**（哪个重建的 uGUI 面板要「随内容伸缩但夹上下限」）。今天**零调用点**。",
    ('ContentSizeFitterMinMax', 'SetLayoutHorizontal()'):
        "缺**组件本体**（见 `ClampSize` 那一行：本方法 = 调 `ContentSizeFitter` 基类 + `ClampSize`，原版 2 句）＋ 消费者。",
    ('ContentSizeFitterMinMax', 'SetLayoutVertical()'):
        "缺**组件本体**（同 `SetLayoutHorizontal` 的纵向那一支）＋ 消费者。",
    ('RectSizeLimiter', 'SetLayoutHorizontal()'):
        "缺**组件本体**（`ILayoutController` 实现：按 `m_minSize/m_maxSize` 调 `SetSizeWithCurrentAnchors`，原版 35 行）＋ 消费者。",
    ('RectSizeLimiter', 'SetLayoutVertical()'):
        "缺**组件本体**（同上，纵向那一格）＋ 消费者。",
    ('UIPreferedSizeFixer', 'ApplyConfig()'):
        "缺**组件本体**（`preferedWidthFollowHeight` 时跑 HLG 四步布局后把 `layoutElement.preferredWidth = rectTransform.rect.height`）"
        "＋ 消费者（原版唯一实例 = `Currency Drawer.prefab`）。",
    ('UIPreferedSizeFixer', 'CalculateLayoutInputHorizontal()'):
        "缺**组件本体**（同 `ApplyConfig`：这一格是 `ILayoutElement` 的回调，原版与 `ApplyConfig` 同机器码）＋ 消费者。",
    ('UIExtension', 'GetCorners(RectTransform rectTransform)'):
        "缺**方法本体**（`GetWorldCorners(4)` → 四角数组，原版 19 行）＋ **任何需要世界坐标换算的调用点**"
        "（我们全在同一 px 设计空间，`PxRect` 的四角就是四个数）。",
    ('UIExtension', 'MaxX(RectTransform rectTransform)'):
        "缺**方法本体**（四角里取最大 x；等价物本可写成 `PxRect.x2`，但原版入参是 `RectTransform` ⇒ 没有同形入口）＋ 调用点。",
    ('UIExtension', 'MaxY(RectTransform rectTransform)'):
        "缺**方法本体**（四角里取最大 y ⇒ `PxRect.y2` 那一跳）＋ 调用点。",
    ('UIExtension', 'MinX(RectTransform rectTransform)'):
        "缺**方法本体**（四角里取最小 x ⇒ `PxRect.x1` 那一跳）＋ 调用点。",
    ('UIExtension', 'MinY(RectTransform rectTransform)'):
        "缺**方法本体**（四角里取最小 y ⇒ `PxRect.y1` 那一跳）＋ 调用点。",
    ('RectExtensions', 'GetWorldRect(RectTransform rect)'):
        "缺 **`四角 → 世界矩形`** 那一跳本体（原版 31 行）＋ 调用点（原版调用方 = PolyAndCode 回收列表 + `UIExtension` 同族）。",
    ('EmptyGraphic', 'OnPopulateMesh(VertexHelper vh)'):
        "缺**一个 `MaskableGraphic` 派生类**（`OnPopulateMesh` 里 `vh.Clear()` = 只吃射线不画像素，原版 12 行）＋ 调用点"
        "（我们的命中区走世界空间 quad `MenuDraw.Hit`，没有 uGUI Graphic 这一层）。",
    ('UICornersGradient', 'ModifyMesh(VertexHelper vh)'):
        "缺**一个 `BaseMeshEffect` 派生类**（`ModifyMesh` 按四角颜色逐顶点插值，原版 131 行）＋ 调用点"
        "（我们的角渐变在生成贴图/顶点时烘进 `Core/CardArt.cs`，没有运行时回调）。",
    ('GameObjectUniqueIdentifier', 'FindComponent(string id)'):
        "缺**「id → 组件」注册表本体**（`Dictionary<string, Component>` + `Register(id, comp)` 写侧）＋ 注册点"
        "（我们现在全靠直接引用 / `transform.Find`，没有 id 这一层）。",
    ('GenericArmyItem', 'GetID()'):
        "缺**「阵营条目对象」实体**（持 `item` + `entries<CardArmy, item>` 表）；我们只有 `Shell/ItemDrawer.cs:427` 的**类型映射**，没有条目对象可取 id。",
    ('GenericArmyItem', 'GetItem()'):
        "缺**「阵营条目对象」实体**（无参取法 = `CampaignHandler.GetItem(SelectedArmy)`，取不到退 `Neutral`）；我们没有按当前选中阵营取条目的入口。",
    ('GenericArmyItem', 'GetItem(CardArmy army)'):
        "缺**「阵营条目对象」实体**（`entries.TryGetValue(army)` → 否则 `values.FirstOrDefault(x.army == army)`）；我们没有「按 army 反查条目」的入口。",
    ('BoardCriteria', 'DeepCopy()'):
        "缺 ①「全链路不可变」的**审计证明**（`CardDef` / `AiScoringData.*Dto` 的写口普查；现读 0 命中 `Clone/Copy`）"
        "或 ② 原版「克隆后就地改」的**对应消费点**。两者都没有 ⇒ 既不升 A 也不判 D。",
    ('HealCriteria', 'DeepCopy()'):
        "缺 ①「全链路不可变」的**审计证明**（同 `BoardCriteria.DeepCopy`）或 ② 原版「克隆评分条件后就地改」的消费点。",
    ('MessageList', 'Refresh()'):
        "缺 ①「消息集合变了 ⇒ 重铺」的**触发点**；② 把 `Shell/InboxWindow.cs:716-745` 的铺行抽成方法级 `Refresh()`、"
        "并让 `Initialize` 改成「存数据 + 调 Refresh」。",
    ('AlternateArtStyleItem<T>', 'TryGetItem(AlternateArtSyle alternateArtSyle, out T returnItem)'):
        "缺 ①「按风格取异画」的**方法级查询**（现在是数据表 + 取图口 `Core/CardArt.cs`）；② 一个**真实调用点**。",
}

MECH = [
    ('未分类/其他', 'BoardCriteria', 'DeepCopy()'),
    ('未分类/其他', 'ContentSizeFitterMinMax', 'ClampSize()'),
    ('未分类/其他', 'ContentSizeFitterMinMax', 'SetLayoutHorizontal()'),
    ('未分类/其他', 'ContentSizeFitterMinMax', 'SetLayoutVertical()'),
    ('未分类/其他', 'RectSizeLimiter', 'SetLayoutHorizontal()'),
    ('未分类/其他', 'RectSizeLimiter', 'SetLayoutVertical()'),
    ('未分类/其他', 'UIPreferedSizeFixer', 'ApplyConfig()'),
    ('未分类/其他', 'UIPreferedSizeFixer', 'CalculateLayoutInputHorizontal()'),
    ('未分类/其他', 'UIExtension', 'GetCorners(RectTransform rectTransform)'),
    ('未分类/其他', 'UIExtension', 'MaxX(RectTransform rectTransform)'),
    ('未分类/其他', 'UIExtension', 'MaxY(RectTransform rectTransform)'),
    ('未分类/其他', 'UIExtension', 'MinX(RectTransform rectTransform)'),
    ('未分类/其他', 'UIExtension', 'MinY(RectTransform rectTransform)'),
    ('未分类/其他', 'RectExtensions', 'GetWorldRect(RectTransform rect)'),
    ('未分类/其他', 'EmptyGraphic', 'OnPopulateMesh(VertexHelper vh)'),
    ('未分类/其他', 'UICornersGradient', 'ModifyMesh(VertexHelper vh)'),
    ('未分类/其他', 'GameObjectUniqueIdentifier', 'FindComponent(string id)'),
    ('未分类/其他', 'GenericArmyItem', 'GetID()'),
    ('未分类/其他', 'GenericArmyItem', 'GetItem()'),
    ('未分类/其他', 'GenericArmyItem', 'GetItem(CardArmy army)'),
    ('未分类/其他', 'HealCriteria', 'DeepCopy()'),
    ('未分类/其他', 'MessageList', 'Refresh()'),
    ('未分类/其他', 'AlternateArtStyleItem<T>', 'TryGetItem(AlternateArtSyle alternateArtSyle, out T returnItem)'),
]

out = []
out.append(table('日常_奖励.tsv')[0])

# ---------- ① TimerDisplay.Awake ----------
r = pick('日常_奖励.tsv', '日常/奖励', 'TimerDisplay', 'Awake()')
r[3] = 'cpp2il/out_cpp2il_dev_ilspy/TimerDisplay.cs:128'
r[5] = 'A'
r[6] = 'Shell/TimerDisplay.cs（Awake）'
r[7] = ('原版 `TimerDisplay__Awake.c` **全文只做一件事**：把实例方法 `LocalizationChange` 包成 '
        '`LocalizationManager.OnLocalizeCallback` 再 `add_OnLocalizeEvent`（`cpp2il/out_cpp2il_dev_ilspy/TimerDisplay.cs:128-131`）。\n'
        '⇒ 我们同一跳 = `void Awake() { Loc.LanguageChanged += LocalizationChange; }`（`Shell/TimerDisplay.cs`，类尾那一段），'
        '注销在 `OnDestroy`（原版 `TimerDisplay__OnDestroy.c` = `remove_OnLocalizeEvent`，见下一行）。\n'
        '🔴 **2026-10-28 B92_N5 就地订正（铁律 5）**：本行原写「（等价）不用订阅式刷新 / 不订阅全局本地化事件」—— '
        '**已不成立**（那时 `LocalizationChange` 还没有真身、没有可订的回调；本轮 `B92_N5` 把它实现出来并接上了订阅）。'
        '今天**是订阅式**，与原版同形。')
r[10] = ('N17 第1轮 B 核实：方法级双侧核过 ｜ 🆕 2026-10-28 B92_N5：`LocalizationChange` 实现后本行改为「订阅式」，'
         '落点由 `:116（Update）` 改为 `Awake`（类尾新成员，零行号漂移）')
out.append(r)

# ---------- TimerDisplay.LocalizationChange（补 判据出处 全路径 + 清过期备注）----------
r = pick('日常_奖励.tsv', '日常/奖励', 'TimerDisplay', 'LocalizationChange()')
r[3] = 'cpp2il/out_cpp2il_dev_ilspy/TimerDisplay.cs:329'
r[10] = ('N17 第1轮分堆：P2 ｜ B91_Q1 2026-10-28：接线未做 ⇒ 状态保持 ｜ '
         '🆕 2026-10-28 B92_N5 **已实现并接线**（`Shell/TimerDisplay.cs` 类尾 + `Shell/DailyRewardPopup.cs:708` 传 `prefixKey`）⇒ 本备注作废')
out.append(r)

# ---------- ① TimerDisplay.OnDestroy ----------
r = pick('日常_奖励.tsv', '日常/奖励', 'TimerDisplay', 'OnDestroy()')
r[3] = 'cpp2il/out_cpp2il_dev_ilspy/TimerDisplay.cs:342'
r[5] = 'A'
r[6] = 'Shell/TimerDisplay.cs（OnDestroy）'
r[7] = ('原版 `TimerDisplay__OnDestroy.c` **全文只做一件事**：把同一个 `LocalizationChange` 委托 '
        '`remove_OnLocalizeEvent`（`cpp2il/out_cpp2il_dev_ilspy/TimerDisplay.cs:342-345`）。\n'
        '⇒ 我们同一跳 = `void OnDestroy() { Loc.LanguageChanged -= LocalizationChange; }`（`Shell/TimerDisplay.cs` 类尾）。\n'
        '🔴 **2026-10-28 B92_N5 就地订正（铁律 5）**：本行原写「（等价）[非缺口] 无订阅可摘」—— **已不成立**：'
        '本轮加了订阅 ⇒ **有订阅可摘**，而且摘的正是原版那一个委托。')
r[10] = ('N17 第1轮 B 核实：方法级双侧核过 ｜ 🆕 2026-10-28 B92_N5：订阅加上之后本行从「无订阅可摘」变成**逐句等价**，'
         '落点由 `:92（Attach）` 改为 `OnDestroy`（类尾新成员，零行号漂移）')
out.append(r)

# ---------- ③(a) 23 行「机制不同」：保持 B + 具体缺口 ----------
for dom, cls, meth in MECH:
    r = pick('未分类_其他.tsv', dom, cls, meth)
    key = (cls, meth)
    gap = GAP[key]
    r[7] = (r[7].rstrip() + '｜🔧 2026-10-28 **B92_N5b · 调度台裁定③(a)：保持 B**'
            '（uGUI 在本仓是依赖、原版行为可观测可复现 ⇒ 不属 §18·4 (d)「架构性不适用」；'
            '也⛔ 不拿「机制不同」四个字当结论）⇒ **具体缺口 = ' + gap + '**')
    out.append(r)

# ---------- ④ GameStaticData 两行 → 分支级 A ----------
r = pick('其他_基础设施.tsv', '其他/基础设施', 'GameStaticData', 'MinionRaceToString(RawCardScript card, Keyword race)')
r[5] = 'A'
r[6] = 'CardPresentation/Core/CardView.cs（RaceTerm）'
r[7] = ('🔴 **一个方法覆盖原版 3 个方法：本行对应第 2 支**（第 1 支 = `CardRaceToString`，第 2 支 = `MinionRaceToString`，'
        '第 3 支 = `StratagemTypeToString`）。\n'
        '✅ 2026-10-28 B92_N5b 升 **A**（调度台裁定③(b) 三条全满足）：\n'
        '  · **逐句对过 `.c`**（`decomp_full/GameStaticData__MinionRaceToString.c` 45 行全文现读）：'
        '`card != null && !IsNullOrEmpty(card+0x258 /*cardRaceTextReferenceId*/) ⇒ s = 那个自定义种族串；'
        '否则 s = ((Keyword)race).ToString()`；最后 `GetTranslation(String.Concat("Card_Race/", s))`（前缀 = `DAT_1842cdf40` 已按 RVA 读出 = `"Card_Race/"`）。\n'
        '  · **我们的分支**（`CardView.RaceTerm`）：① 入参 `race` = 卡表 `subtype`（就是原版那个「自定义种族串」）⇒ 走 '
        '`RaceKeyBySubtype` **未命中**时**原样**当键后缀（`:5405` 的三元式右支）；② 前缀 `RaceTermPrefix = "Card_Race/"`（`:5335`）逐字相同；'
        '③ 取词 = `Loc.BulkOr / Loc.T`（= 原版 `GetTranslation`）。\n'
        '  · 与第 3 支的分界就在这里：**本支没有** `spellType == Normal ⇒ ""` 那一格（原版 `MinionRaceToString` 也没有）。\n'
        '  · 残留如实标：我们的 `RaceTerm` 多了「③ 老键回落 + 查不到出声 + 印英文原文」三跳（原版 `GetTranslation` 缺词时返回键本身）—— '
        '那是数据面适配，不影响本支的「前缀 + 种族串」形状。')
out.append(r)

r = pick('其他_基础设施.tsv', '其他/基础设施', 'GameStaticData', 'StratagemTypeToString(RawCardScript card)')
r[5] = 'A'
r[6] = 'CardPresentation/Core/CardView.cs（RaceTerm）'
r[7] = ('🔴 **一个方法覆盖原版 3 个方法：本行对应第 3 支**（第 1 支 = `CardRaceToString`，第 2 支 = `MinionRaceToString`，'
        '第 3 支 = `StratagemTypeToString`）。\n'
        '✅ 2026-10-28 B92_N5b 升 **A**（调度台裁定③(b) 三条全满足）：\n'
        '  · **逐句对过 `.c`**（`decomp_full/GameStaticData__StratagemTypeToString.c` 51 行全文现读 + '
        '`cpp2il/out_cpp2il_dev_ilspy/GameStaticData.cs:1333-1370` 交叉）：三格 = '
        '① `cardRaceTextReferenceId(+0x258)` 非空 ⇒ 用它；② `spellType(+0x90) != Normal(0)` ⇒ `((SpellType)spellType).ToString()`；'
        '③ `spellType == Normal` ⇒ **直接 `return ""`**（ILSpy 明写，`DAT_1842b80e0` 就是 `string.Empty`）；三格之后统一 '
        '`GetTranslation("Card_Race/" + s)`。\n'
        '  · **我们的分支**：① 同上（`race` 原样当后缀）；② `RaceKeyBySubtype`（`:5352`）把 SpellType 的 **6 个展示名**'
        '（Psychic Power / Combat Elixir / Genomic Enhancement / Overlord Power / Dark Pact / Defence）映射成原版枚举名；'
        '③ Normal 那一格 = `RaceTerm("")` **原样返回空**（`:5403` 的 `IsNullOrEmpty ⇒ return race`，与 `return ""` 同义）。\n'
        '  · 印不印由 `SubtypeLine`（`:5313`）判，与「印哪个词」分开 —— 原版也是两跳（`CardRaceToString` 的 `0x14` 支转调本方法）。')
out.append(r)

# ---------- ⑤ GameModes 两行：补记反射→强类型 ----------
for meth, what in (
        ('CombatFinished(RankedMatchResult result, Action<OnlineMatchResult> callback)',
         '`GameModesHandler.CombatFinished` → `TryFinishMatch`'),
        ('GetActiveEvent(PlayModes playMode)',
         '`GameModesHandler.GetActiveEvent(PlayModes)` → `TryGetPlayMode`')):
    r = pick('其他_基础设施.tsv', '其他/基础设施', 'GameModes', meth)
    r[7] = (r[7].rstrip() + '｜🆕 2026-10-28 **B92_N5b**：' + what + ' 的**反射已删、改强类型** —— '
            'N1 本轮建好 `IPlayEvent`（`Shell/PlayEvent.cs:878`，`PlayEvent<,>` 在 `:903` 声明实现）⇒ '
            '改成 `e as IPlayEvent` + 直接读 `EventPlayMode` / 直接调 `MatchFinished(...)`（原版没有反射这条路，18·0）；'
            '失败格仍出声（`Awake` 那一格每类型一次、结算那一格每次）。⚠️ 落点行号不变（改造**逐行等长**，见 `_gen_B92_N5b` 报告 §3）。')
    out.append(r)

# ============================ 写盘 ============================
if '--write' not in sys.argv:
    print('DRY-RUN: %d 行' % (len(out) - 1))
    print('状态分布：', dict(Counter(r[5] for r in out[1:])))
    for r in out[1:]:
        assert len(r) == 11, (r[2], len(r))
    sys.exit(0)

buf = '\r\n'.join('\t'.join(c.replace('\t', ' ').replace('\r', '').replace('\n', '\\n') for c in r) for r in out) + '\r\n'
io.open(OUT, 'wb').write(b'\xef\xbb\xbf' + buf.encode('utf-8'))
b = io.open(OUT, 'rb').read()
assert b[:3] == b'\xef\xbb\xbf'
back = b.decode('utf-8-sig').split('\r\n')
assert len(back) == len(out) + 1, (len(back), len(out) + 1)
for k in range(len(out)):
    cells = back[k].split('\t')
    assert len(cells) == 11, (k, cells[2], len(cells))
    if k: assert '(' in cells[2], (k, cells[2])
print('WROTE %s rows=%d crlf=%d bom=yes' % (OUT, len(out) - 1, b.count(b'\r\n')))
