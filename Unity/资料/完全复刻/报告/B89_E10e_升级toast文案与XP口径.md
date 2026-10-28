# B89_E10e · 升级 toast 文案（推翻「既有 A」的自证）+ `GrantMatchXp` 常量复核

> 执行代理 `B89-E10e` · 2026-10-28 · 判据 = `decomp_full/PlayerDataManager__{ShowNewLevelToast,AddCollectorXp}.c`
> ＋ `cpp2il/out_cpp2il_dev_ilspy/ToastNotification.cs`（基类 `Initialize`）＋ 解包 prefab 的 TMP 序列化值。
> 账补丁 = `账/_parts/B89_E10e.tsv`（1 行 · 11 列 · LF · 无冲突）。

---

## §1 结论（第一句）

**原版升级飘窗的正文 = 等级数字本身**（`String.Format(GetTranslation("MenuCollection/Toast/CollectorLevel"), level)`，
而该词条 13 种语言**全是 `{0}`**）；**我们原来是「收藏者升级！」**（词条 `MainMenu/ToastNotifications/PlayerCollectorLevelUp`），
而且**多写了一个原版从不写的格子**（`Player Level Text` 印本地等级）。**已按原版改掉，账 510 的 A 判词同时订正。**

---

## §2 判据链（每一跳都能打开核）

| # | 事实 | 出处 |
|---:|---|---|
| ① | `ShowNewLevelToast(int level)`：`uVar1 = GetTranslation(DAT_1842ce9f0)` → `String.Format(uVar1, boxed level)` → `new ToastNotificationContext(text, …, type = 2)` → `ShowNotification` | `decomp_full/PlayerDataManager__ShowNewLevelToast.c` 全文（`CONCAT44(uVar3,2)` 就是 type=2 = `CollectionLevelUp`） |
| ② | `AddCollectorXp` 的升级那一支**同形**（同一个 `MenuCollection/Toast/CollectorLevel` 词条、同样 `String.Format` 等级） | `cpp2il/out_cpp2il_dev_ilspy/PlayerDataManager.cs:15345-15367` |
| ③ | 该词条官方值 = **`{0}`**（en/zh/es/ru/…13 种全一样） | `CardPresentation/Resources/official_loc_13lang.json`，现读 |
| ④ | 基类 `ToastNotification.Initialize(obj)` **无条件** `text.text = context.Text` ⇒ 正文槽（`Text`/RT 352）被写成 `"12"`，**覆盖** prefab baked 的 `Collector Level Up!` | `cpp2il/out_cpp2il_dev_ilspy/ToastNotification.cs:75-80`；`ToastNotificationMission` 是**唯一**子类（它只给任务飘窗用） |
| ⑤ | `Player Level Text`（RT 369）的 baked 值 = **`-`** | `d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_505.json` 的 `m_text = '-'` |
| ⑥ | **全树没有写手**写升级飘窗的 `Player Level Text`：唯一调 `SetPlayerLevel` 的是 `PlayerProfileUIController`（它写的是**档案窗自己的** `playerLevelText` 字段） | `cpp2il/…/PlayerProfileUIController.cs:80/108` → `:178-186`（`playerLevelText.text = playerLevel.ToString()`） |

⇒ 原版这条飘窗的观感 = **正文一个等级数字** + 圆形徽标里恒 `-`。
（看着像原版的疏漏，但 `.c` + 序列化值两边一致 ⇒ **照抄**，⛔ 别「修好」。）

---

## §3 改了什么（`Shell/ToastNotifications.cs`，**总行数严格不变 = 1500**）

| 处 | 改前 | 改后 |
|---|---|---|
| `:828 ShowCollectionLevelUp` | `new Context("MainMenu/ToastNotifications/PlayerCollectorLevelUp", …)` + `c.Localize = true` | `new Context(string.Format(Loc.T("MenuCollection/Toast/CollectorLevel"), level > 0 ? level : have), …)`，并注明「原版这条**不本地化**（先 GetTranslation 再 Format）」 |
| `:1002`（`Fill` 的 CollectionLevelUp 支） | `FindLabel(root, "Player Level (1)/Player Level Text").SetText(CardTierConfig.PlayerLevelText)` | **删掉那 5 行**，换成等行数的注释 + 一个**刻意留空的 `if`**（留锚点，让账行 `:1002` 仍指在这一支上） |
| `:601`（模板 `Player Level Text` 初值） | `CardTierConfig.PlayerLevelText` | `"-"`（= 原版 `m_text`） |
| `:831-833`（那句告警文案） | 「这一格按 `CardTierConfig.PlayerLevelText` 印」 | 改成「仍按**实参**印（原版印的就是 signal 载荷）」 |

**行数核验**（该文件被 16 条账行按行号引用，必须零漂移）：
`:812 ShowCollectionGainXP` · `:828 ShowCollectionLevelUp` · `:922 Fill` · **`:1002`（= 那条 `if`，正是账 162 写的 `Fill 的 CollectionLevelUp 支`）** ·
`:1063 ToastRootHeight` —— **逐行 `sed -n` 现读复核，全部原位**；文件总行数 `1500`（改前也是 1500）。
⚠️ 中途有一次改成了 1501、一次 1499，都是**先量再补/删**回来的（`wc -l` 每次核过）。

---

## §4 `GrantMatchXp` 常量复核（你点名要确认的那件）

**结论：`🔶` 只有一条是对的，⛔ 不要拆。**

| 项 | 是不是我们定的 |
|---|---|
| 分支表（哪些 `MatchType` 给 100/30、哪些给 0、哪些走 default） | **原版** —— `PlayerDataManager__GetXPFromMatch.c` 逐分支 |
| 常量 100 / 30 / **130** / 0 | **原版**（130 那个是原版 bug，`return 0x82`，注释写死「⛔ 别修」） |
| 赢的判据 `matchWon == 1.0f` **精确相等** | **原版**（`DAT_1834b2bb8` 实读 = `1.0f`） |
| `default` 分支的告警串 `"GetXPFromMatch undefined for playMode "` | **原版** |
| `xp <= 0` ⇒ 两条 toast 都不弹 | **原版** —— `RewardService.Collect` 自己就有 `if (grantedXp <= 0)` 跳过 `AddCollectorXp` / `ShowGrantedXpToast`（`RewardService.cs:390 / :438`） |
| **「这个值由谁给」** | 🔶 **我们的**：原版由**服务器回包**（`RewardInfo.grantedXp`）给，离线没有 ⇒ 用上面那个原版公式充 |

⇒ 自设计范围 = **只有「值从哪来」**；公式本身 **0 自设计**。
已把这句写进 `PlayerProfileState.cs:306 GrantMatchXp` 的 doc（并写明「拆成两条 `🔶` 会误导下一个人去改分支表」）。

---

## §5 验证

- **秒级类型检查**：`TMPDIR=/tmp/wf_B89E10e bash d:/4/Unity/工具/typecheck.sh` → **运行时 0 / 编辑器 0**。
  ⚠️ 中途被 `RuleEngine/**` 的 8 条（`CS1061 FindEnchantmentTemplate` ×2 · `CS0117 EnchantmentTemplatesProvider` ·
  `CS1503 MatchTypes.BattleContext` 等）顶红 —— **R1d/E6c 的文件**，等它编干净后复跑即 **0/0**。
- ⛔ 本波**没有跑 Unity**（红线）⇒ 新断言**没执行过**。
- 新增 1 条断言（`PlayerDataMathCheck.CheckCollectorXp` 开头）：
  `Loc.T("MenuCollection/Toast/CollectorLevel") == "{0}"` —— 钉住**文案口径**（屏幕上印的一定是等级数字），
  这也是防「再拿我们自己的实现当标准」的那颗钉子。

---

## §6 顺手发现（⛔ 没改）

1. 🔴 **`个人资料_成就_排行.tsv:162`（`OnCollectorLevelUpdate`）的落点 `ToastNotifications.cs:1002-1008（Fill 的 CollectionLevelUp 支）` 仍然成立**
   —— 我特意把那个 `if` 的锚点保持在 `:1002`（改前 `:1002` 是 `if`，现在也是）。但那一行说的是
   「`OnCollectorLevelUpdate` = `TopBar` + `ProfileTab` + `Fill` 的 CollectionLevelUp 支」——
   **第三段（`Fill` 支）现在什么都不做**（原版就什么都不做，见 §2 ⑥）⇒ 那一格的判词建议由调度台顺手订正
   （「`Fill` 支 = 原版无此写手，故留空」，不再是「等价实现」之一）。⛔ 我没动别人的账。
2. **`PlayerProfileUIController.SetPlayerLevel` 写的是档案窗的 `playerLevelText`** —— 我们这边对位的是
   `Shell/ProfileTab.cs` / `Shell/TopBar.cs` 的等级刷新（账 162 那一行），**与飘窗无关**。
   这次误判（把两个同名字段当同一个）就是这么来的，已在本文件 :601 那一段写成「两个 prefab 同名不同物」。
3. `ToastNotificationMission` 是 `ToastNotification` 的**唯一**子类（`cpp2il/…/ToastNotificationMission.cs`）——
   以后要判「某类飘窗有没有自己的 `Initialize`」时，只有这一种例外，⛔ 别再假设「升级飘窗也有子类」。

---

## §7 交接

- **账补丁** `账/_parts/B89_E10e.tsv`：1 行 —— **510 `ShowNewLevelToast` 保持 `A`，但判词按 §2 订正**
  （原版 = 等级数字 / 我们原来 = 「收藏者升级！」+ 多写的 `Player Level Text` / 2026-10-28 已改 / 误判成因）。
- **自检入口不用改**（断言仍在 `PlayerDataMathCheck.Run`）。
- **本波碰过的文件**：`Shell/ToastNotifications.cs`（行数不变）· `Shell/PlayerProfileState.cs`（只改 doc）·
  `Editor/PlayerDataMathCheck.cs`（+1 条断言）。⛔ 其余一个字没动。
- **仍然开着的**：XP 的**值来源**是本地模拟（🔶，公式本身是原版的）；其余无。
