# W58 · thick3d 三条断言插入 `Editor/BattleScene.cs`

> 执行代理 `W58-THICKASSERT`（2026-10-20）。唯一改动文件：
> `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`。
> ⛔ 未跑 Unity、未跑 `*.Run`、未动 git、未改正本。

---

## 结论

三条断言 **已原样插入** `⑤·b` 块（`StatProbe` / `sv` 所在作用域），插在
`Check(sv.StatShown(…)…)` **之后**、`Check(sv.StatFlash(CardView.StatHealth) == Color.white, "刚建出来没有闪现色")` **之前**。
所需测试口全部存在、签名匹配，**没有缺的口**。秒级类型检查 **0/0**。

| 条 | 断言 | 灭自证含义 |
|---|---|---|
| 1 | `PragatiDigits.OutlineWidth(Thick3D) == 0.12f`（±0.0005） | 生成器 `OUTLINES` 把 thick3d 改回 0.097 再重跑 ⇒ 红 |
| 2 | `CardView.BoardStatVariantTag(s) == Tag(Thick3D)`，`s = StatMelee..StatHealth`（1..4，四人格全覆盖） | `CardView.StatVariant` 的 board 分支改回 `Thick` ⇒ 红 |
| 3 | `HasVariant(Thick3D) && InkPixels('0',Thick3D) > InkPixels('0',Thick)` | 常量与像素两处一起造假 ⇒ 红（像素是烘出来的） |

---

## 插入位置与行号（插入后）

- 锚点（内容定位，非行号）：`+ $"（isUnit={sd.isUnit} · Pragati={PragatiDigits.Available} · face={sv.Face}）");`
- 插入块：**1671–1700**（注释 1671–1680，三条断言 1681–1700）
- 下一条原有断言 `Check(sv.StatFlash(CardView.StatHealth) == Color.white, "刚建出来没有闪现色");` 现位于 **1701**
- 文件总行数 33406 → **33437**（+31 行）

上下文核对：`sv` = `CardView`（`:1652` 创建）、`Check` 为宿主局部方法、`Mathf` 已用（全文 888 处）。
作用域内 `CardView.StatMelee=1 / StatHealth=4` 为连续常量（`Core/CardView.cs:6108`）⇒ 循环正好覆盖近战/远程/护甲/生命四格。

---

## 测试口核对（均已存在，只读确认）

| 口 | 位置 | 签名 |
|---|---|---|
| `PragatiDigits.Variant` | `Core/PragatiDigits.cs:35` | `enum Variant { Thin, Thick, Thick3D }` |
| `PragatiDigits.Tag(Variant)` | `:38` | `public static string` |
| `PragatiDigits.OutlineWidth(Variant)` | `:49` | `public static float`（转发 `OutlineWidth(string)`） |
| `PragatiDigits.HasVariant(Variant)` | `:53` | `public static bool` |
| `PragatiDigits.InkPixels(char, Variant)` | `:64` | `public static int` |
| `CardView.BoardStatVariantTag(int)` | `Core/CardView.cs:7927` | `public static string` |
| `CardView.StatVariant(int,bool)` | `Core/CardView.cs:7919` | `static PragatiDigits.Variant` |

---

## grep 证据

```
1672: Check(Mathf.Abs(PragatiDigits.OutlineWidth(PragatiDigits.Variant.Thick3D) - 0.12f) < 0.0005f,
1674:     + $" —— 实测 {PragatiDigits.OutlineWidth(PragatiDigits.Variant.Thick3D):F4}（2D 的 thin/thick = "
1677: string statTagBad = "";
1679:     if (CardView.BoardStatVariantTag(si) != PragatiDigits.Tag(PragatiDigits.Variant.Thick3D))
1680:         statTagBad += $" [stat{si}:{CardView.BoardStatVariantTag(si)}]";
1681: Check(statTagBad.Length == 0,
1683:     + $" —— 不在 0.12 档上的格：{(statTagBad.Length == 0 ? "无" : statTagBad)}");
1685:     && PragatiDigits.InkPixels('0', PragatiDigits.Variant.Thick3D)
1690:     + $"thick3d={PragatiDigits.InkPixels('0', PragatiDigits.Variant.Thick3D)}");
```

---

## 缺的口

无。

---

## typecheck

```
TMPDIR=/tmp/wf_thicka bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

## 行尾与 diff

- `b.count(b'\r\n')=33437` / `b.count(b'\n')=33437` ⇒ **纯 CRLF，未翻**（bare-LF = 0）。
- `git diff --numstat` 该文件 = **1427/52** —— ⚠️ 这是**本批多个在飞代理的既有改动合计**，本次增量只有 **+31 行**（行数 33406→33437 可对账）。未动其它文件。
