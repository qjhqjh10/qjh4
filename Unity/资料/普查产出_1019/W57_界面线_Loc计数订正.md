# W57 界面线 UI-8：`Core/Loc.cs` 计数注释订正

- 日期：2026-10-19
- 白名单：**只改** `Unity/MyGame/Assets/CardPresentation/Core/Loc.cs`（**只改注释**，逻辑/数值/断言期望零改动；`git diff` 全部落在 `//` 行）。
- 起因：`资料/普查产出_1019/W57_界面线_日签Tab与Term兜底.md` §8「顺手发现」——bulk 族计数全过期。
- 结论：主目标 6 族 + 总数全部过期（2,758 → **3,156**；`Event_Description` 111 → **144**）；另扫出 **16 处**同类过期计数，一并订正（均保留更正痕迹）。

---

## 一、`Resources/official_names.json` 逐族对账（主目标）

复算口径：`entries[]` 按 `k.split('/')[0]` 分组（现读 `count` 字段 = 3156、`len(entries)` = 3156、键唯一）。

| 族 | `Loc.cs` 原文（2026-10-17 快照） | 现读（2026-10-19） | 差 |
| --- | ---: | ---: | ---: |
| `Card_Name/*` | （原文未列） | **1198** | — |
| `Avatars/*` | 419 | **498** | +79 |
| `Titles/*` | 368 | **459** | +91 |
| `Modular_Description/*` | （原文未列） | **342** | — |
| `Achievements/*` | 198 | **212** | +14 |
| `Cardbacks/*` | 157 | **181** | +24 |
| `PrebuiltDecks/*` | 74（更早曾写 97） | **95** | +21 |
| `Event_Description/*` | 111 | **144** | +33 |
| `Card_Trait/*` | （原文未列） | **27** | — |
| **合计** | **2,758** | **3,156** | **+398** |

> 原文只列了 6 族（`Titles/Avatars/Achievements/Cardbacks/PrebuiltDecks/Event_Description`），但它的「2,758」是**全表**总数——差额其余部分来自 `Card_Name` / `Modular_Description` / `Card_Trait`（这三族是 `A1763` 之后加进 bulk 的）。
> 现读 9 族 = 原文 6 族 + `Card_Name` + `Modular_Description` + `Card_Trait`。

---

## 二、`Loc.cs` 其它「族计数 / 条数」注释逐条对账

| 位置（现读行号） | 原文 | 现读 | 处置 |
| --- | --- | --- | --- |
| `Loc.cs:42-49`（文件头·现状） | 本表 775 条 · 官方有键 440 · ZH 不同 45 · EN 不同 6 | **780** · **446** · **46** · **7** | ✅ 已改 |
| `Loc.cs:89` | 「现值见文件头 → 现读 440 条」 | **446** | ✅ 已改 |
| `Loc.cs:935` | 官方 `Card_Race/*` 21 条 · 「本族两列与官方逐字相同」 | **23 条**；重叠键里 **3 条 ZH 不同**（Battlesuit/Secret/Codicil，均为保留旧译） | ✅ 已改（补 caveat） |
| `Loc.cs:941` | 「1126 张卡」 | 全池 **1,127** 张；有非空 `subtype` 的 **1,116** 张 | ✅ 已改（原数字所指不明，两个现读数都写清） |
| `Loc.cs:1337` | 官方 `Tips/*` 22 条 | **23 条** | ✅ 已改 |
| `Loc.cs:1636` | 官方 `MenuDeck/Error/*` 9 条具名键 | **15 条** = 13 具名 + 2 `/Short` | ✅ 已改 |
| `Loc.cs:2241`（⑬·A 段头） | `Settings/Online/St/*` 45 条 | 块内 **46 条** | ✅ 已改 |
| `Loc.cs:2328`（⑬·B 段头） | `Settings/Online/Lobby/*` 17 条 | 块内 **19 条**（与块内「这 19 处」一致） | ✅ 已改 |
| `Loc.cs:2502` | 官方 `Settings/Graphics/*` 14 条 | **17 条** | ✅ 已改 |
| `Loc.cs:2622` | 官方 `Card_Trait/*` 77 条 | **83 条** | ✅ 已改 |
| `Loc.cs:2623` | `Card_Trait` 「暂缓换回」25 条 | **28 条**（逐条 = 换官方前的旧值，见下节复算） | ✅ 已改 |
| `Loc.cs:2860` | bulk = 「那 6 族」 | **9 族 / 3,156 条** | ✅ 已改 |
| `Loc.cs:3068-3079` | bulk 6 族 + 合计 2,758 | **9 族 + 3,156**（全表已列） | ✅ 已改（主目标） |
| `Loc.cs:3230-3232` | `Warlord_Lore` 1042 条；覆盖 999/1127（523/586 · 390/446 · 50/56 · 36/39）；缺 128 | **1,157 条**；覆盖 **1,111/1,127**（**586/586 · 430/446 · 56/56 · 39/39**）；缺 **16**（全是 tactic） | ✅ 已改 |
| `Loc.cs:3234` | `Cosmetic_Lore` 154 条；233 张卡背命中 138 | **179 条**；命中 **160 / 233** | ✅ 已改 |
| `Loc.cs:3414` | 中文列空 21 条（13 列各 8–21 条空列） | 21 条 ✓；空列范围实为 **8–23**（en 8 · es 23） | ✅ 已改（只改括号范围） |
| `Loc.cs:3432` | 13 列各 9,438–9,441/9,447 非空 | **10,839–10,854 / 10,862** | ✅ 已改 |

**核对无误、未改的**（现存数字仍成立）：

- 官方表 `terms` 总数 = **10,854**（`Loc.cs:33/47/53`）✓
- 官方 13 语言全表 = **10,862** 键；每种 **10,839–10,854** 非空（`Loc.cs:2886`）✓
- `Card_Race/*` 本表 43 条 = 卡池全部非空 `subtype`（43 个）✓；其中印到卡面的 **19 个 / 722 张** ✓（按 `CardView.SubtypeLine` + `TacticSubtypeShown` 复算）
- `Card_Name` 本表 26 条 ✓；`CustomErrors/*` 41 ✓；`Battle/BattleEnd/*` 官方 11 ✓；`Armies/*` 那 14 条（不含 `_short`）✓
- 二进制字面量：`Battle/` 93 条 · `Cemetery/` 19 · `Effect/` 25（`stringliteral.json` 里多出的 `Battle/Effect/` 裸前缀不算键）· `Tips/` 24 · `HUD/` 8 ✓；`stringliteral.json` 共 **26,507** 条 ✓
- `Settings/Online/HowToConnect/*` 12 条 ✓；`Cancel/*` 3 · `Echo/*` 2 · `Wire/*` 8 ✓

---

## 三、改动清单（全部在 `Core/Loc.cs`，只动 `//` 注释）

1. `:42-49` 文件头现状：775/440/45/6 → **780/446/46/7**；复算说明补「`grep -c` 会把 2 行注释里的字样算进去（现读 782 − 2 = 780）」；补复算口径（官方值为纯字符串的 14 条按 en=zh 比对）。
2. `:89` 「现读 440 条」→ **446**。
3. `:934-938` `Card_Race/*` 21 → **23**；补「3 条 ZH 保留旧译」的 caveat。
4. `:940-943` 「1126 张卡」→ 全池 **1,127** / 有非空 subtype **1,116**。
5. `:1337` 官方 `Tips/*` 22 → **23**。
6. `:1636-1638` 官方 `MenuDeck/Error/*` 9 → **15**（13 + 2 `/Short`）。
7. `:2241-2242` ⑬·A `St/*` 45 → **46**。
8. `:2328-2329` ⑬·B `Lobby/*` 17 → **19**。
9. `:2502` 官方 `Settings/Graphics/*` 14 → **17**。
10. `:2620-2624` 官方 `Card_Trait/*` 77 → **83**；「暂缓换回」25 → **28**。
11. `:2860-2861` bulk「6 族」→ **9 族 / 3,156 条**。
12. `:3068-3079` bulk 各族计数与合计：368/419/198/157/74/111、2,758 → 9 族逐族现读 + **3,156**（保留 2026-10-17 旧数作为历史快照）。
13. `:3230-3232` `Warlord_Lore` 1042→**1,157**、覆盖 999→**1,111**/1127、缺 128→**16**（并说明 16 张全是 tactic、官方表无键）。
14. `:3234` `Cosmetic_Lore` 154→**179**、命中 138→**160**/233。
15. `:3414-3415` 13 列空列范围 8–21 → **8–23**。
16. `:3432-3433` 13 列非空 9,438–9,441/9,447 → **10,839–10,854/10,862**（并注明原数是 2025-07-30 快照）。

行尾：改动前后均为**纯 LF**（`CRLF 0 / LF 3658`）；`git diff --numstat` = **61 增 / 19 删**（其中 19 增 / 2 删是**别的代理先前已落在工作区**的注释改动，非本笔）。

---

## 四、没查清 / 判不准、**未改**的部分

1. **`Loc.cs:1614` vs `:1616` 自相矛盾**：同段里一处写「原版具名族只有 **8 条 + 2 条 `Short`**」、另一处写「原版具名族只有 **9 条**」。
   - 现读二进制字面量（`d:/2/tools/il2cpp_out/stringliteral.json`）= **10 条具名**（8 + 2 Short）+ 2 条数字族；现读官方表 = **15 条**（13 + 2 Short）。
   - **为什么不改**：这两句描述的是反编译方法体（`CanAddCard/CanAddHero/CanAddDefensive`）用到的键集，不是官方表全族；`decomp_full` 里没有直接命中 `MenuDeck/Error/` 的文本，无法逐条确认「8 条」是哪 8 条。**判不准 ⇒ 不动**。
2. **⑫ 波段的段头计数**（`:1886` P1 14 条 · `:1925` P2 46 条 · `:1995` P2c 8 条 · `:2045` P3 16 条 · `:2102` P4 33 条）与「段内 `new Entry(` 行数」不一致（13/38/16/44）。这些是**批次施工单的条目数**（一个条目可能跨行、段内可能混入后续补的键），不是干净的前缀族计数 ⇒ **判不准，不动**。
3. **`Loc.cs:2447` ⑬·E `Upnp/*` 9 条**：块内实有 **10** 个键——第 10 个是 2026-10-19（P6d）新加的 `{1}` 碎片，句子里「九条都是 `Result.message`」仍然成立 ⇒ **未改**（仅在此记录）。
4. **`Loc.cs:306`「登录弹窗 mTerm 去重后正好 16 条」**：这是对 prefab 子树做的 mTerm 扫描，未重跑（需 `w4probe.py` + bundle）；**未核**。
5. **`Loc.cs:1010`「查得到 28 条 `Battle/` 键」**（prefab mTerm 扫描）与 **`:2476`「`Settings/` 前缀 47 条」**（`assets_full` 全库 mTerm 扫描）：未重跑大扫描；**未核**。
6. **`Loc.cs:2909` / `:3367`「33 条刻意保留的旧译」**：这是 2026-10-14 那批「暂缓换回」的**历史批次口径**。现按同一判据（现值 == 换官方前备份、且 ≠ 官方）复算 = **38 条**（`Card_Trait` 28 · `Armies` 4 · `Card_Race` 3 · `MenuDeck/HUD/…` 1 · `Settings/Graphics/{FrameLimit,UnlimitedFPS}` 2）。
   - **为什么不改**：「刻意保留」的成员资格需要人工裁定（例如 `Settings/Graphics` 那两条是不是「刻意」），且同段 `:49`/`:90-92` 明确标了历史快照 ⇒ **不动，只报现值**。
7. **`Loc.cs:84`「`Card_Trait` 25 · `Armies` 4 · `Card_Race` 2」**：属于「2026-10-14 当时的历史快照」那一句，按历史保留（现读对应为 28/4/3）。
8. `Loc.cs:276`「页标题 2 条」等**批次内条目数**：按批次记录理解，未逐块核对 ⇒ 未动。

---

## 五、复算命令（可直接复跑）

```bash
# ① official_names.json 逐族（主目标）
python -I -X utf8 -c "import json,collections;d=json.load(open(r'd:/4/Unity/MyGame/Assets/CardPresentation/Resources/official_names.json',encoding='utf-8'));c=collections.Counter(x['k'].split('/')[0] for x in d['entries']);print(len(d['entries']),dict(c))"
# → 3156 {'Titles': 459, 'Avatars': 498, 'Achievements': 212, 'Cardbacks': 181, 'Event_Description': 144, 'Modular_Description': 342, 'Card_Name': 1198, 'PrebuiltDecks': 95, 'Card_Trait': 27}

# ② Loc.cs 表条目数（grep 会把 2 行注释算进去）
grep -c "new Entry(" d:/4/Unity/MyGame/Assets/CardPresentation/Core/Loc.cs   # → 782（−2 注释 = 780）

# ③ 官方表族计数（Card_Race / Tips / Settings/Graphics / Card_Trait / MenuDeck/Error/…）
python -I -X utf8 -c "import json;o=json.load(open(r'd:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json',encoding='utf-8'))['terms'];print(len(o));print({p:sum(1 for k in o if k.startswith(p)) for p in ['Card_Race/','Tips/','Settings/Graphics/','Card_Trait/','MenuDeck/Error/','Battle/BattleEnd/','CustomErrors/','Armies/','Battle/Cemetery/','Battle/Tips/','Battle/Effect/','Battle/HUD/']})"
# → 10854；Card_Race 23 · Tips 23 · Graphics 17 · Card_Trait 83 · MenuDeck/Error 15 · BattleEnd 11 · CustomErrors 41 · Armies 20 · Cemetery 20 · Battle/Tips 39 · Battle/Effect 28 · Battle/HUD 16

# ④ 本表条目 vs 官方表（780 / 446 / 334 / ZH 46 / EN 7）
#    逐条解析 `{ "键", new Entry("zh","en") }` 后与 terms 比对；
#    值为纯字符串的官方条目按 en = zh 比对（本表命中 14 条，都是语言名等语言中立值）。

# ⑤ lore 覆盖
python -I -X utf8 -c "import json;l=json.load(open(r'd:/4/Unity/MyGame/Assets/CardPresentation/Resources/lore_zh+en.json',encoding='utf-8'))['entries'];wl={x['k'].split('/',1)[1] for x in l if x['k'].startswith('Warlord_Lore/')};cl={x['k'].split('/',1)[1] for x in l if x['k'].startswith('Cosmetic_Lore/')};c=json.load(open(r'd:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json',encoding='utf-8'))['cards'];import collections;by=collections.Counter();hit=collections.Counter();[ (by.__setitem__(x['type'],by[x['type']]+1), hit.__setitem__(x['type'],hit[x['type']]+(x['id'] in wl))) for x in c];print(len(wl),len(cl),sum(hit.values()),'/',len(c),dict(hit),dict(by))"
# → 1157 179 1111 / 1127 {'unit':586,'tactic':430,'hero':56,'defence':39} {'unit':586,'tactic':446,'hero':56,'defence':39}

# ⑥ 13 语言表
python -I -X utf8 -c "import json;d=json.load(open(r'd:/4/Unity/MyGame/Assets/CardPresentation/Resources/official_loc_13lang.json',encoding='utf-8'));print(len(d['keys']),{L:(sum(1 for x in d[L] if x),sum(1 for x in d[L] if not x)) for L in d['langs']})"
# → 10862；非空 10,839–10,854 / 空 8–23
```

---

## 六、typecheck

```text
TMPDIR=/tmp/wf_ui8 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（本笔只动注释；typecheck 在全部改动之后跑过一遍，0/0。）

---

## 七、顺手发现（不在本笔白名单，未改）

1. 🔴 **`工具/_loc_official/loc_revert_clash.py` 现在跑不起来**：第 11 行 `"""` 已闭合 docstring，第 12 行却还有一句正文
   （`**落盘位置**：…`）⇒ `SyntaxError: invalid character '：' (U+FF1A)`。
   本笔的「暂缓换回 = 28 条」是**照它同一套判据手工复算**的（同一 `PAT` + `descZh/nameZh` 包含判断）。
   ⛔ 修复需要动 `工具/`，不在本笔白名单。
2. `Resources/official_names.json` 的 `count` 字段与 `entries` 长度一致（3,156），本次未发现产物与生成器描述冲突；
   但 `工具/_loc_official/gen_official_names.py` 文件头仍写着旧快照数（`Titles 368` / `Avatars 419` / `Card_Trait 77`），
   那些是**生成器文档里的历史数**，未改（白名单外）。
3. `Settings/Graphics` 两处官方计数在注释里仍以「2026-10-14 快照」出现（`14 条`）——已在下面补了现读 17，但
   同一份历史句仍会被 grep 命中，属**保留更正痕迹**的有意为之。
