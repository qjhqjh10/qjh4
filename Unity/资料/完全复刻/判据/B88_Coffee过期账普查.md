# 「缺 Coffee UIEffect / 缺 shader」这类**过期账**普查（B88-E7b · 2026-10-27）

## 结论（一句话）

那个变体**早就移植好了** —— `Resources/Shaders/WildcardShine.shader` 是原版
`Hidden/UI/Default (UIEffect)` 的 **`TRANSITION_SHINY`** 分支的忠实移植，配套运行时层是
`CardPresentation/Effects/UIEffectTogglerCoffeeEffects.cs` 里的 **`WildcardShineLayer`**
（`Params` 四个预设：`Icon` / `DoneButton` / `WildcardDisplay` / 自定义）。
⇒ **凡是把「缺 Coffee `UIEffect` 着色器 / 缺 transition 贴图」当阻塞理由的账行，判词都已过期**。

⚠️ 还有一条**容易漏的**：那张 `_TransitionTex`（`Transition-Horizontal`）**我们根本不需要** ——
`WildcardShine.shader` 走的是**解析式等价**（`_UseTransitionTex = 0`，见 `WildcardShineLayer.Attach`
里那一句与 `.shader` 文件头 ③）⇒ 「缺贴图」这个理由**两头都不成立**。

普查范围：`资料/完全复刻/账/*.tsv` 全 24 张表，搜键 = `Coffee` / `UIEffect` / `Shiny` / `shinyEffect` /
`deckInfoShineEffect` / `transition 贴图` / `quad 近似` / `K-13`（43 处原始命中，逐条看过）。
**只有 4 行**的判词属于本族过期；其余命中是 `UIEffectToggler*` 那些**方法本体**的行（与 shader 无关，in-scope 且已 A/D）。

## 过期账表（交调度台合并，⛔ 本代理没改这些主表行）

| 账文件 | 行号 | 原判词（摘要） | 实际 | 结论 / 归属 |
|---|---|---|---|---|
| `轮抽.tsv` | 131 | 「未落地：`deckInfoShineEffect.Play(true)` —— 缺 Coffee `UIEffect` 着色器 + 那张 transition 贴图（已裁 P2）」 | shader + 层都有；本代理已按判据挂到 `Generic Window Red Background Small`（`Shell/DraftModeWindow.cs:1884` + `PlayDeckInfoShine`），参数 120/0.25/0.5/白/filter 6/**1.0 s**/Once | **过期**，已由 `B88_E7.tsv`（行 131 → **A**）覆盖，主表按那份合并即可 |
| `战役.tsv` | 19 | 「② **未落地**：`shinyEffect.Play(resetTime:true)` 的流光 —— 缺 Coffee `UIEffect` 着色器 + transition 贴图（已裁 P2，⛔ 不做 quad 近似）」 | shader/贴图都不是阻塞（贴图不需要，解析式等价）⇒ **阻塞只剩「接线 + 选对实例参数」**；文件 = `Shell/CampaignTab.cs` ⇒ **归 J4（`Shell/Campaign*` 本波的黑名单）** | **过期**（判词），状态仍 C（接线没做，但**不是判据缺口**）｜见下面 §实例参数 |
| `卡组编辑.tsv` | 298 | 「…选/预览监听在建卡时一次挂好；**UIEffectToggler 特效层本仓未建**」 | 同一张表的 **303 行**（同 `PrebuiltPackUI`）自己写着「七步全落（含 ④⑤ 扫光）」⇒ 298 的后半句**被同表推翻** | **过期**（半句）｜状态 A 不变，只该删掉那半句（⛔ 不建议改状态） |
| `锻造.tsv` | 40 | 「…缩放也没有 0.3 s 补间（**`k` 那个乘数原版未解出**，我们取 1.1）」 | `read_literal.py` 实读：`_DAT_1834b31f8 = **1.1**`、`_DAT_1834b2dc8 = **0.3**` ⇒ **k 已解出、与代码逐位一致**；补间本代理已落（`BoosterPackOpenWindow.ScaleCardLayers`） | **过期**（「k 未解出」那半）｜状态 B（卡背 SDF `_Outline` 那半仍缺），已由 `B88_E7.tsv` 覆盖，`B88_E7b.tsv` 再补一句 |

### 已自纠、不用管的（记在这里免得下一个人再查一遍）

| 账文件 | 行号 | 状态 |
|---|---|---|
| `卡组编辑.tsv` | 303（`PrebuiltPackUI.SelectAnimDummy`） | 已就地订正（「等 K-13」作废，④⑤ 扫光已落） |
| `特效动画.tsv` | 455（`UIEffectTogglerCoffeeEffects.Toggle`） | 已改 A（载体换成 `WildcardShineLayer`） |
| `社交_聊天.tsv` | 424（`MessageContainer.Update`） | 已就地订正（「本工程没有那一层」作废，改真扫光） |
| `收藏.tsv` | 348 / 350 / 352 | 已 A（`WildcardDisplay` 扫光 / 计数重播 / 信号两订阅者） |
| `卡组编辑.tsv` | 109（`DeckEditingWindow.ToggleDisplay`） | 已 A（Deck 编辑窗那颗扫光已接，`DeckRuntime.WildcardShineAfterTab`） |

## §实例参数：`CampaignArmyUIDecorator.shinyEffect` 那 4 个实例（本轮新取的硬数据）

`shinyEffect` 是 **`UIEffectTweener`**（不是 `UIEffect`）。4 个 `CampaignArmyUIDecorator` 实例
（`bundle_menus_assets_all/MonoBehaviour/*.json` 里带 `shinyEffect` 字段的那 4 份）指向的 tweener：

| tweener pid | `m_Duration` | `m_WrapMode` | 同 GO 上的 `UIEffect`（`Transition*`） |
|---|---|---|---|
| `-3271889046122976335` | **1.0** | 0 (Once) | 120 / 0.25 / 0.5 / 白 / filter 6 / keepAspect 1 |
| `669576846626199242` | **1.0** | 0 | 120 / 0.25 / 0.5 / 白 / filter 6 |
| `2688218479952654116` | **0.75** | 0 | 120 / 0.25 / 0.5 / 白 / filter 6 |
| `1791554316153643733` | **0.75** | 0 | **139 / 0.213 / 0.533 / (2,2,2,1)** / filter 6 ← 即 `WildcardShineLayer` 文件头记的「139 那套」 |

⇒ 接线时**必须按 GO → 父链**定「哪一颗是锻造/战役进度条那一颗」，⛔ 别拿 120 那套一把套四个。
`WildcardShineLayer.Params` 现在**只有** `Icon`(120/0.25/0.5/白) / `DoneButton` / `WildcardDisplay` 三个预设；
139 那套要就地 `Params p = Params.Icon; p.rotation=139; p.width=0.213f; p.softness=0.533f; p.color=new Color(2,2,2,1);`
（字段全是 `public`，不必改那个文件）。

## 复跑方式（幂等）

```bash
python -I -X utf8 - <<'PY'
import io,glob
for p in sorted(glob.glob('资料/完全复刻/账/*.tsv')):
    b=io.open(p,'rb').read().decode('utf-8').rstrip().split('\n')
    for i,r in enumerate(b,1):
        if i==1: continue
        if any(k in r for k in ('Coffee','shinyEffect','transition 贴图','quad 近似')):
            c=r.split('\t'); print(p.split('/')[-1], i, c[5], c[1][:24], c[2][:30])
PY
```
