# -*- coding: utf-8 -*-
"""B92 波 4 · N5c 账补丁生成器（第九十二会话 2026-10-28 · 调度台第三轮）
用法：python -I -X utf8 _gen_B92_N5c.py            # 只校验，不写盘
      python -I -X utf8 _gen_B92_N5c.py --write    # 真写 账/_parts/B92_N5c.tsv
铁律：键一律用**主表原 `方法` 串**（merge 工具按 `(原版类, 方法)` 定位，0 命中 = 新增 ⇒ 必须为 0）。
"""
import io, os, sys
from collections import Counter

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
LEDGER = os.path.join(ROOT, '账')
OUT = os.path.join(ROOT, '账', '_parts', 'B92_N5c.tsv')
FILE = '其他_基础设施.tsv'


def rows(path):
    b = io.open(path, 'rb').read()
    return [l.split('\t') for l in b.decode('utf-8-sig').replace('\r\n', '\n').split('\n') if l.strip()]


MAIN = rows(os.path.join(LEDGER, FILE))


def pick(cls, meth):
    hits = [x for x in MAIN[1:] if len(x) >= 11 and x[0] == '其他/基础设施' and x[1] == cls and x[2] == meth]
    assert len(hits) == 1, (cls, meth, len(hits))
    return list(hits[0])


def add(r, text):
    r[7] = r[7].rstrip() + '｜' + text
    return r


out = [MAIN[0]]

# ============ 1) CardRaceToString（已 A）：补「优先级偏离」+ 断言式判据 ============
r = pick('GameStaticData', 'CardRaceToString(RawCardScript card)')
add(r, ('🆕 2026-10-28 **B92_N5c**（本代理把 `.c` 逐句读完后的三条补充，状态仍 **A**）：\n'
        '  · **确切的分派式**（`decomp_full/GameStaticData__CardRaceToString.c` 90 行）：'
        '`IsNullOrEmpty(card.cardRaceTextReferenceId /*+0x258*/)` 为真时按 `cardType(+0x94)` 分 —— '
        '`0 ⇒ MinionRaceToString(card, Keyword(+0x98))` · `10 ⇒ GetTranslation("Card_Race/Warlord")` · '
        '`0x14 ⇒ StratagemTypeToString(card)` · **其余 ⇒ `CustomDebug.LogWarning("CardRaceToString undefined for " + 枚举名)` 然后 `return ""`**'
        '（那句文案已由 `资料/普查产出_1017/_b1018_scratch/g2b_scratch/va_map.txt:24112` 解出，**⛔ 别再去读 `0x1842cce48` 的 RVA** —— 本代理上一轮把它误当成「第三个键前缀」）。\n'
        '  · 🔴 **一条如实记录的偏离（优先级）**：原版 **自定义串优先** —— `cardRaceTextReferenceId` 非空就**不看 cardType**；'
        '我们 `CardView.SubtypeLine`（`:5313`，四支在 `:5319-5326`）对 `hero`/`defence` 是**恒用常量** `Warlord`/`Defence`、**忽略 `subtype`**。'
        '按 `.c` 逐字读，那 3 张 `subtype` 写着 Stratagem/Spell/Structure 的防御卡**应该印成那个词**；'
        '但**成品卡面（PnP 印刷品）印的是 `Defence`** ⇒ 两条判据冲突，本行按**卡图**判（用户口径：成品卡图是最终真相）。'
        '⛔ 别改回「subtype 优先」；要彻底定案得读 `RawCardScript` SO 里 `cardRaceTextReferenceId` 的**实值**（本机没做）。\n'
        '  · **断言式判据（灭自证）**：`Editor/CardBaseDemo.AssertSubtypeLine` 那 4 条 —— 其中 ④ 钉住 `RaceKeyBySubtype` 的 6 对映射'
        '（缺一对即红）；「印不印」那一轴由 ①②③ 覆盖 `unit/hero/defence/tactic`。⇒ 本行的 A **不是**「看着像」，是「有断言 + 有印刷品双向核对」。'))
out.append(r)

# ============ 2) IsTutorialIndexCompleted → A ============
r = pick('PlayerDataManager', 'IsTutorialIndexCompleted(int tutorialIndex)')
r[5] = 'A'
r[6] = 'CardPresentation/Shell/MenuTutorialFlow.cs（IsCompleted）'
add(r, ('✅ 2026-10-28 **B92_N5c 升 A**（三条都满足）：\n'
        '  · **原版体逐句**（`decomp_full/PlayerDataManager__IsTutorialIndexCompleted.c` 49 行现读）：'
        '取活跃活动数据的教程字典 → `key = index.ToString()` → `if (!dict.ContainsKey(key)) return false;` → '
        '`return dict[key];` —— **就是一个「按 int 下标的完成位查询」**。\n'
        '  · **我们的方法级等价**：`MenuTutorialFlow.IsCompleted(int i)`（`:72`，本代理现读）= '
        '取第 `i` 个阶段 → `PlayerPrefs.GetInt("WfMenuTutorial.Done.<stageId>", 0) != 0`。'
        '**同为「按 int 下标取完成位、缺位即 false」**（`PlayerPrefs.GetInt` 的默认值 0 正好等于 `ContainsKey` 为假那一支）。\n'
        '  · ⚠️ **如实记的一处差**：下标的**语义空间**是我们的菜单 FTUE 阶段表（`MenuTutorialStages`），'
        '原版是 liveops 活动数据里那张 `TutorialStage` 表。**查询形状相同、表不同** ⇒ '
        '这与我上一轮把 `AllTutorialsCompleted` 留 B 的理由是**同一条**，请调度台注意口径一致性（见该行）。'))
out.append(r)

# ============ 3) 其余 9 行：保持 B/C，写死具体缺口 ============
r = pick('PlayerDataManager', 'AllTutorialsCompleted()')
add(r, ('🆕 2026-10-28 **B92_N5c**：本代理复算 —— `MenuTutorialFlow.AllTutorialsCompleted`（`:1105`）的 '
        '`done == MenuTutorialStages.Count` 与原版 `dict.Count == 总数` **是同一种形状**，'
        '⇒ 残差**只剩一条**：「数的是哪张表」。⛔ 本代理不自行升档（与 `IsTutorialIndexCompleted` 那条同源，'
        '但那条我升了 —— **口径请调度台一次拍板**：若接受「阶段表 = 我们那份」，本行与 `FirstTwoTutorialCompleted` 可同批升 A）。'))
out.append(r)

r = pick('PlayerDataManager', 'FirstTwoTutorialCompleted()')
r[6] = 'CardPresentation/Shell/MenuTutorialFlow.cs（**无对应方法**；原写 `:72-118` 是文件区间不是方法 —— 见差异）'
add(r, ('🆕 2026-10-28 **B92_N5c** 就地订正两处（铁律 5）：\n'
        '  · **落点原写 `:72-118` 是「文件区间」不是方法**：现读 `:72` = `IsCompleted(int)` 的签名行、`:102` = `MarkStageDone(...)` 的签名行 ⇒ 那一段里**没有** `FirstTwoTutorialCompleted` 的等价物。\n'
        '  · **原版体逐句**（`decomp_full/PlayerDataManager__FirstTwoTutorialCompleted.c` **14 行全文**现读）：'
        '`if (!IsTutorialIndexCompleted(0)) return false; return IsTutorialIndexCompleted(1);` —— 就是那两个下标的一行「与」。\n'
        '  · **还差什么（= 一个消费者，不是判据）**：这一行在本仓**写得出**（`IsCompleted(0) && IsCompleted(1)`），'
        '但会成**零调用者死代码**（原版用它决定「要不要加载主菜单」；我们的主菜单加载不走这条路，'
        '`MainMenuRuntime`（N4）里没有这个闸）⇒ ⛔ 不建（红线：⛔ 零使用者死代码）。'
        '**回来条件**：主菜单加载链接上这个闸时，一行落地即 A。'))
out.append(r)

r = pick('PlayerDataManager', 'SetTutorialState()')
add(r, ('🆕 2026-10-28 **B92_N5c** 现核：`decomp_full/PlayerDataManager__SetTutorialState.c` **42 行只是 async trampoline**'
        '（`AsyncTaskMethodBuilder.Start<d__467>`）⇒ **真体在 `decomp_full/PlayerDataManager._SetTutorialState_d__467__MoveNext.c`**'
        '（本代理已确认该文件存在）。原版那一段 = 取 `GameModes` 活跃活动 → 拿 `demoData` → 写教程状态 → 随后 `UploadOnlineDemoData()`。\n'
        '**还差什么**：① 我们写的是 `MenuTutorialFlow` 的 `PlayerPrefs` 进度位（`MarkStageDone`），**没有 `DemoData` 那层实体**；'
        '② 上报 = **P3**。⇒ 要 A 得先有「教程状态挂在 DemoData 上」那一层（同 `TestResetTutorial` 的缺口）。'))
out.append(r)

r = pick('PlayerDataManager', 'TestResetTutorial()')
add(r, ('🆕 2026-10-28 **B92_N5c** 现核（两个 `.c` 都全文读了）：\n'
        '  · `PlayerDataManager__TestResetTutorial.c` **13 行**：`if (demoData(+0x250) != null) { DemoData.ResetTutorial(demoData); UploadOnlineDemoData(); }` 否则 NRE。\n'
        '  · `DemoData__ResetTutorial.c` **19 行**：`*(this+0x10) = new Dictionary<string,bool>()`（**换新表**）+ `*(this+0x18) = 0`（**`gameAfterTutorialPlayed = false`**）。\n'
        '  · ⇒ 我们 `MenuTutorialFlow.ResetProgress`（`:384`）清的「逐 stage `PlayerPrefs.DeleteKey` + v1 旧档 + skip 旗标」**对应的是「换新表」那一半**（形状等价），'
        '**缺的只有 `+0x18` 那一格**（`gameAfterTutorialPlayed`，全仓 0 命中）+ 上报（P3）。\n'
        '  **还差什么**：那一格**没有任何读者**（原版用它决定「教程后是否已玩过一局」）⇒ 现在补上就是**只写不读的死状态** ⇒ ⛔ 不补；'
        '等「教程后首局」那条流程落地时，与读者一起补。'))
out.append(r)

r = pick('PlayerDataManager', 'LoadPlayerData()')
add(r, ('🆕 2026-10-28 **B92_N5c**：`decomp_full/PlayerDataManager__LoadPlayerData.c` **243 行**现读 —— '
        '`ReadDataFromFile()` 拿不到 ⇒ `LoadStarterData()`；拿到 ⇒ 把 ~25 格**逐格灌进 `GameStaticData.playerData`**。\n'
        '**还差什么（具体到可施工）**：需要一张 **「设置总档 ↔ 各消费点」映射表** —— '
        '每一格（音乐/音效/语音音量、画质档、触摸、提示开关、语言、帧率…）现在分别由谁读：'
        '音量 = `WarpforgeAudio.Music/SoundFx/VoiceOver`（吃 `PlayerPrefs["MusicVolume"]` 等）、'
        '语言 = `Loc.Current`（吃 `PlayerPrefs["Language"]`）、画质 = `QualitySettings`、帧率 = `Application.targetFrameRate`。'
        '**这张表没有 ⇒ 灌不进去**（灌了也不知道往哪格写）。'))
out.append(r)

r = pick('PlayerDataManager', 'ReadDataFromFile()')
add(r, ('🆕 2026-10-28 **B92_N5c**：`decomp_full/PlayerDataManager__ReadDataFromFile.c` **82 行**现读 = '
        '`BinaryFormatter.Deserialize(persistentDataPath/<playerId>.dat)` + 版本比较（`Version` 构造），读不到返回 null。\n'
        '**还差什么**：**我们先不产出那份 `.dat`**（`SaveCenter` 是每 key 一份 JSON、`LegacySaveReader` 读的是另一族旧档）'
        '⇒ 「读那一份 .dat」在本仓**没有读的对象**。顺序上依赖上一行（先有总档，才有读它的这一跳）。'))
out.append(r)

r = pick('PlayerDataManager', 'SavePlayerData()')
add(r, ('🆕 2026-10-28 **B92_N5c** 现核（`decomp_full/PlayerDataManager__SavePlayerData.c` **153 行**）：'
        '`BinaryFormatter.Serialize` 整份 `LocalPlayerData` → 写 `<playerId>.dat`，**写完立刻 `SetMusicAndFxVolumes()` + `SetTargetFramerate()`**。\n'
        '  · 🔴 **「写完立刻重新应用」这一跳在我们这边【没有对应时刻】，而且不需要**：我们的设置 setter 是**应用 + 落盘一体** —— '
        '`WarpforgeAudio.SetMusic/SetSoundFx/SetVoiceOver`（`Core/WarpforgeAudio.cs:178/187/195`）直接写 mixer **并**写 `PlayerPrefs`；'
        '`Application.targetFrameRate` 同理（改了就生效）。⇒ 原版那个「先攒在对象里、落盘后再统一应用」的时序在我们这里**不存在**。\n'
        '  · **还差什么（只剩一半）**：那份「**一份总档**」（`LocalPlayerData` 对象）+ 它的序列化/版本号。'
        '落盘动作本身已有（`SaveCenter.Set/SetRaw`，tmp→rename 原子写）。'))
out.append(r)

r = pick('PlayerDataManager', 'UnpackInventory()')
add(r, ('🆕 2026-10-28 **B92_N5c**：`decomp_full/PlayerDataManager__UnpackInventory.c` **29 行**现读 = **两跳**：'
        '① `InventoryManager.DeserializeInventory(userData+0x50, +0x68)` 把服务器背包解进本地；'
        '② `GetOnlineData(0x32)` 取待发奖励 ⇒ `RewardService.Collect(pending)`。\n'
        '**还差什么**：① 的输入（服务器 `userData`）属 **P3**；② 的 `RewardService.Collect` 是**编排层**且本仓没建'
        '（同族 `CollectItemReward` / `CollectSingleReward` 两行的缺口）。⇒ 本行随 reward-event 家族一起走。'))
out.append(r)

# ============ 4) 两行「只写报告」 ============
r = pick('GameModes', 'GetActiveEvent(Type gameModeType)')
add(r, ('🆕 2026-10-28 **B92_N5c**（调度台指定：**只写报告，不动代码**）：本行残差仍是 `IRankedEvent` 未建；'
        '而**唯一该实现它的类是 `RankedEventBase`，在 `Shell/RankedEventBase.cs`（N2 白名单）** ⇒ 本代理建接口只能得到零实现者的空接口。'
        '**解锁顺序**：N2 让 `RankedEventBase` 实现 `IRankedEvent` ⇒ 本代理把 `RankedEventInterfaceType` 换成 `typeof(IRankedEvent)` 并删注入点（`:433`）⇒ 本行回 A。'))
out.append(r)

r = pick('GameModes', 'get_CurrentSeasonNumber()')
add(r, ('🆕 2026-10-28 **B92_N5c**（调度台指定：**只写报告**）：双重阻塞不变 —— ① `IRankedEvent` 接口未建'
        '（实现类 `RankedEventBase` 在 **N2** 的 `Shell/RankedEventBase.cs`）；② 即便建了，`config == null` 那一支原版是 NRE。'
        '⇒ **本行在 N2 交付前不可能动**，本代理不改代码。'))
out.append(r)

# ============ 写盘 ============
if '--write' not in sys.argv:
    print('DRY-RUN: %d 行' % (len(out) - 1))
    print('状态分布：', dict(Counter(r[5] for r in out[1:])))
    for r in out[1:]:
        assert len(r) == 11, (r[2], len(r))
        assert '(' in r[2], r[2]
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
