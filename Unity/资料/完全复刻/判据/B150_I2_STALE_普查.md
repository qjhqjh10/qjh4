# B149-I2-STALE 普查（脚本产物，⛔ 别手改，重跑会覆盖）

> 扫了 **4579** 份 `.cs`/`.md`（`MyGame/Assets` + `资料/完全复刻`）。
> 判据：官方 I2 全表 **10,862 键 / 13 语言早在本地**（2026-10-18 并入，`数据/游戏数据/官方本地化_en+zh-CN.json`）。

| 类 | 条数 |
|---|---:|
| **STALE**（仍断言拿不到、附近无订正标记） | **19** |
| FIXED（已带订正标记） | 33 |
| OTHER（提 I2 但语义不是「拿不到」） | 267 |

## STALE 逐条

### `MyGame/Assets\CardPresentation\Battle\BattleDriver.cs`（1）

- `:31008` /// **今天仍然不在 `Loc` 表里**（原版显示串在**远端 I2 表**，本地 0 个 value）

### `MyGame/Assets\CardPresentation\Battle\CampaignScriptRunner.cs`（1）

- `:146` /// 原版那张表是**远端** I2 词条表，缺键行为拿不到判据）。

### `MyGame/Assets\CardPresentation\Deck\DeckRuntime.cs`（1）

- `:5600` /// 英文那一列是我们译的（原版英文在远端 I2 表，本地取不到）。</summary>

### `MyGame/Assets\CardPresentation\Editor\BattleScene.cs`（1）

- `:37470` + "—— 原版走远端 I2 表按 `textReference` 取词条，本地没有那张表");

### `MyGame/Assets\CardPresentation\Shell\AllianceMemberOptionsPopup.cs`（2）

- `:161` /// 本地没有词条表 ⇒ 只用作出声与断言，不换字。</summary>
- `:492` // 🔴 本地没有 I2 词条表 ⇒ **不出声就不用换**：只有真按数据算过、且 role 真是 Admin 时才出声说明。

### `MyGame/Assets\CardPresentation\Shell\InboxData.cs`（1）

- `:113` // 🔶 自己设计（非原版）：本地欢迎信的文案与 300 点奖励 · 原版这两项在服务端 / 词条表，本地没有 · 第七十九会话 2026-10-25 执行代理 N17 新建 `InboxData.cs` 时定为「我们挑的」（出处 `资料/完全复刻/报告/N17_日常活动_第1轮.md:27`；⛔ 非用户拍板；该报告未

### `MyGame/Assets\CardPresentation\Shell\PopUpGameWindow.cs`（1）

- `:414` // 两条如实出声（红线）：① 词条表本地没有 ⇒ 现在印的是**键**；② 音效没做。

### `MyGame/Assets\CardPresentation\Shell\PurchasePremiumWindow.cs`（1）

- `:262` /// `points = Missions.GetPointsPerArmyOnDailyLogin(army)`。**本地没有词条表、也没有那个 points** ⇒ 留 `{0}` 原样。</summary>

### `MyGame/Assets\CardPresentation\Shell\ReferralPopupWindow.cs`（1）

- `:610` // 🔴 `MenuShop/referral/counterMaximum` 那条词条的**正文本地拿不到**（词条表在远端 CCD）

### `MyGame/Assets\CardPresentation\Shell\RewardWindow.cs`（1）

- `:270` /// ⚠️ 原版这一串是 I2 词条（`Localize`），**词条表在远端 CCD、本地没有** ⇒ 我们画的就是 prefab 里这串英文。</summary>

### `MyGame/Assets\RuleEngine\Core\DeckRules.cs`（1）

- `:582` /// 两列文案同样是我们起的（原版那套在**远端 I2 语言表**，本地取不到）。</para>

### `资料/完全复刻\_tmp_backup_调度台日志_107.md`（1）

- `:11419` ① `:247-248` 那条 `🔶 自己设计` 写着「I2 词条表在远端、本地没有 value」**是假的**（`Rewards Menu/Claimed` 与 `MainMenu/General/Claim` 都在官方表里且与常量逐字相同）⇒ 那是「本窗还没接本地化」，不是「必须自己定」；

### `资料/完全复刻\判据\B146_对账_B界面卡组收藏.md`（1）

- `:80` 3. **`Card_Trait/trap` / `secretOrder` 的译文**：13 语言官方表确实没有；它们是否由**远端 I2 Live Source** 下发（关服后永远拿不到）—— 未证。若证实是远端，则按 §四 P3 判 D 并写时间限定；若只是漏收，则自译 + `🔶 自己设计`。

### `资料/完全复刻\判据\自设计清单.md`（1）

- `:192` | 83 | 高 | `CardPresentation/Shell/CampaignRewardWindow.cs:248` | const @245 | 不是原版 | / 词条表在**远端 CCD、本地没有** ⇒ 下表是**我们按 term 末段填的**（不是原版字符串）。</summary> |

### `资料/完全复刻\报告\B106_W39.md`（1）

- `:69` /// 🔶 自己设计（非原版）：按钮文案 `Claimed` / `Claim` 的英文串 · I2 词条表在远端 CCD、本地没有 value · TODO(谁定的)｜原注：词条表在**远端 CCD、本地没有** ⇒ 下表是**我们按 term 末段填的**（不是原版字符串）。</summary>

### `资料/完全复刻\报告\G4_自设计打标_第5轮.md`（2）

- `:66` | 38 | `CardPresentation/Shell/CampaignRewardWindow.cs:248` | 按钮文案 `Claimed` / `Claim` 的英文串 | I2 词条表在远端 CCD、本地没有 value | TODO(谁定的) | 注释行 |
- `:101` | 73 | `CardPresentation/Shell/InboxData.cs:113` | 本地欢迎信的文案与 300 点奖励 | 原版这两项在服务端 / 词条表，本地没有 | TODO(谁定的) | 注释行 |

### `资料/完全复刻\调度台日志.md`（1）

- `:11419` ① `:247-248` 那条 `🔶 自己设计` 写着「I2 词条表在远端、本地没有 value」**是假的**（`Rewards Menu/Claimed` 与 `MainMenu/General/Claim` 都在官方表里且与常量逐字相同）⇒ 那是「本窗还没接本地化」，不是「必须自己定」；

