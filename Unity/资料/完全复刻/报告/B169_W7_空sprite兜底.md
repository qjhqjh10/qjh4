# B169_W7 — 「sprite 取不到」兜底隐藏（mod 写手 · 2026-11-07）

判据来源：`d:/4/Unity/资料/完全复刻/报告/B169_J88_白块诊断.md`（⛔ 未重查）。
对象 = 离线版客户端 mod `E:/warpforge/05_工具/mod/WfOfflineProbe`。

## §一 挂了哪 5 个方法（均非泛型、签名不含 `Nullable<T>`）

| # | 目标（`Assembly-CSharp`） | 出处 | 钩子 |
|---|---|---|---|
| 1 | `ProfileItemDisplay.Initialize(CosmeticItemTitle)` | `out_cpp2il_dev_ilspy/ProfileItemDisplay.cs:50-82`（`image.sprite=…` / `image.enabled=(item!=null)`） | postfix |
| 2 | `WarlordMasteryContainer.OnInitialize(PlayerInfo)` | `WarlordMasteryContainer.cs:73`（`components.armyIcon.sprite = fullHeroSprite`） | postfix + finalizer |
| 3 | `ForgePlayerContainer.OnInitialize(PlayerInfo)` | `ForgePlayerContainer.cs:9-70`（`:44` `armyIcon.enabled=flag` / `:59` 赋图） | postfix + finalizer |
| 4 | `CampaignProfileContainer.OnInitialize(PlayerInfo)` | `CampaignProfileContainer.cs:11-85`（`:47` / `:63`） | postfix + finalizer |
| 5 | `ProfileEventSection.Initialize(PlayerInfo)` | `ProfileEventSection.cs:11-16`（三个容器顺序调用、**无 try/catch**） | postfix + finalizer |

钩子写入 `phase0/hooks_installed.tsv`（`imgfb:*`，OK/SKIP/FAIL）。

## §二 兜底条件（唯一的写入口 `Apply`）

- `sprite == null` ⇒ 该 `Image.enabled = false`（`path=… reason=null-sprite[|null-sprite-after-exc]`）。
- `sprite != null` 且**本探针曾隐藏过它**（`HashSet<int> _hidden` 按 instanceID）⇒ 恢复 `enabled = true`
  —— 只覆盖「同一块被复用于有图的情况」（`TitleTab` 选不同称号复用 `mainDisplay`）。
- `sprite != null` 且没隐藏过 ⇒ **一个字都不写**（正常界面零影响）。
- `sprite`/`armyIcon` **成员读不到**（不是值为 null，是代理里没这个名字）⇒ 记 `NO-SPRITE-MEMBER` 并**不动手**
  —— 否则字段名一变会把**所有**正常图藏掉。
- `ProfileItemDisplay` 在 `item == null` 时原版自己已 `enabled=false`，直接 return（不出日志）。
- ⛔ 不动 `ProfileContainerComponents` 的 `background`/`armyName`/`level` —— 只处理报告里那三块 `Badge`。

为什么必须挂 `ProfileEventSection`：三个容器里**第一个抛异常时后两个一次都不跑**（NRE 在 warlord 的 `FirstOrDefault` 之后）⇒ postfix 不执行，必须用 finalizer 兜。

## §三 日志

- `phase0/imagefb.tsv`：`ts / kind(HIDE|RESTORE|SETENABLED-*|*-EXC) / path(最多 5 级节点名) / reason`
- `phase0/probe.log` 一行：`IMGFB path=Title Tab/Selected Item Panel/Image reason=null-sprite`
- `imagefb.tsv` 每进程启动 `Reset` 一次（截断重来）。

## §四 还差什么

- **未实机验证**（本轮禁起游戏）：只保证编译 0 错误、目标方法与字段名逐条对着反编译产物核过。
- **`ProfileEventSection.Initialize` 是否每次开窗都跑**、以及 `components` 是否已在序列化阶段就位（`ProfileContainer.Awake` 只做 null 兜底）—— 需实机看 `imagefb.tsv` 有没有 HIDE 行确认。
- **服务端 `VictoryCounters` 修好后**（诊断报告 §三 ①）：本探针会自动走 `RESTORE`（sprite 非空），无需回滚。
- 若 `fullHeroSprite` 本身也是 null（报告 §四 未查清），440 块将保持隐藏而不是白 —— 符合要求。

## §五 改动清单

- 新建 `Probes/ImageFallbackProbe.cs`（295 行）。
- `ModMain.cs` **只加一行**（第 91 行）`try { ImageFallbackProbe.Install(); } catch { }`（+161 字节、LF 未变）。
- `bash build.sh`：**0 错误**（仅既有 `CurrencyFixProbe._enabled` CS0414 警告）；
  产物 `bin/WfOfflineProbe.dll` = 331,776 B · 2026-11-07 19:44。
