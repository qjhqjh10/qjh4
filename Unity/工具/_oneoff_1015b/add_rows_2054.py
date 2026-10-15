# -*- coding: utf-8 -*-
"""补几笔「修红时顺手查出、仍开着」的新账。"""
import io

TASK = r"d:/4/Unity/资料/任务_卡组卡牌战斗.md"
ROWS_ENG = [   # 插到 §一（引擎）末：`A1822` 行之后
"| **`A2054`** | ⚠️ **同一个「黑暗契约记号族」的判据现在【三处各写一份】**：`CreatePool.MentionsDarkPact` 的中文兜底（`CreatePool.cs:1105-1112`，**仍只认字面 `黑暗契约`**）· `RuleEngineTest` 内新写的本地判据 · `A2045`（`IconSetup.ZhGaps`）。⇒ 后两处已跟着 `descZh` 换代改了，**第一处没动**（铁律 6：两处写同一条规则 = 迟早不一致）。出处 → `F1_RuleEngineTest修红.md` 顺手发现 |",
"| **`A2055`** | ⚠️ **`CreatePool.cs` 两处行内自述已过期**：`:267-269` 还拿 `Sergeant Taaman`↔`Sergeant Naaman` 当例子（卡名已改）· `:836-848` 还写着 `128/67 → 125/64`（**现读 127/66**）。⛔ 本会话没动它（不在写手白名单）。出处 → `F1_RuleEngineTest修红.md` 顺手发现 |",
]
ROWS_UI = [   # 插到 §四（界面）末：`A2045` 行之后
"| **`A2056`** | ⚠️ **`prebuilt_decks.json` 有 4 副 `complete:false` 但 `missing`/`problems` 都是空数组**（`SW_SK_1` · `SW_SK_2` · `SpaceWolvesDeck1` · `SpaceWolvesDeck3`；其余 12 副都有理由）⇒ **判据缺失**，疑似生成器漏填或 `complete` 在别处写死。出处 → `F3_MainMenu修红.md` 顺手发现 #1 |",
"| **`A2057`** | ⚠️ **`PrebuiltDecks.Tab` 现读 87 副**（= 103 − 拼不齐 16），与「池 103 副 / 页上列 70 副（经典 32 + 遭遇 38）」那组数**对不上** ⇒ 要么数是旧的、要么 `complete` 那列被改过。出处 → `F3_MainMenu修红.md` 顺手发现 #2 |",
"| **`A2058`** | ⚠️ **`Shell/PointerLayer.cs:1031` 那句注释写「只有顶栏那 4 颗钮登记」应改成 3 颗**：头像那颗（`TopBar.cs:257`）传 `Vector4.zero` ⇒ `Wire` 里早退，`:966` 自己也写着「只登记非零 pad」。⛔ 未动。出处 → `F3_MainMenu修红.md` 顺手发现 #4 |",
"| **`A2059`** | ⚠️ **两个卡组夹具会「静默换牌」**：`DeckWithTactic` / `MakeDeck` 凑牌**既不筛「能不能进卡组」也不看 id 形状**，而 `ResolveDeck` 事后会**静默降级**（摘牌 → `TooFewCards` → 换成自动凑的一副）⇒ 任何用它们的用例都可能踩同一个坑，**症状永远是「拿不到 / 数字对不上」而不是「牌被换了」**。本会话只给 `DriveTacticAndCheck` 补了前置，**别的夹具没查**。出处 → `F2_BattleDeck修红.md` 顺手发现 #3 |",
"| **`A2060`** | ⚠️ **仓内 `.cs`/文档里的 `:行号` 引用没有任何守卫** —— 本会话两个写手各自往中间插行，就分别把 `DeckScene.cs:2939` 推成 `:2962`、把别人的引用点错位。⇒ 建议改成引**函数名 / 断言原文**（本条只记录，没做）。出处 → `F2_BattleDeck修红.md` 顺手发现 #1 · `R1_B2正本锚点.md` 顺手发现 |",
]

s = io.open(TASK, encoding="utf-8", newline="").read()
lines = s.split("\n")
hit = [i for i, L in enumerate(lines) if L.startswith("| 🆕 **`A1822`** |")]
assert len(hit) == 1, hit
for r in ROWS_ENG:
    assert r.count("|") == 3, r.count("|")
lines[hit[0] + 1:hit[0] + 1] = ROWS_ENG
s = "\n".join(lines)

lines = s.split("\n")
hit = [i for i, L in enumerate(lines) if L.startswith("| **`A2045`** |")]
assert len(hit) == 1, hit
for r in ROWS_UI:
    assert r.count("|") == 3, r.count("|")
lines[hit[0] + 1:hit[0] + 1] = ROWS_UI
s = "\n".join(lines)

io.open(TASK, "wb").write(s.encode("utf-8"))
b = io.open(TASK, "rb").read()
print("CRLF", b.count(b"\r\n"), "LF", b.count(b"\n"))
