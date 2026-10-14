# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · ① 改掉交接里那条「55 分钟」 ② 把只读诊断查出的五点立成新账 A1967~A1971。
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_diag.py
"""
import io

HANDOFF = r'D:\4\Unity\资料\交接_第三十会话.md'
TASKS = r'D:\4\项目任务.md'

OLD = (
    "⚠️ **但它跑了 ~55 分钟**（`CLAUDE.md` 记录的基准是 **7m56s**）—— 日志 **17 万行**、一直是 `BattleScene` 自己在刷栈。"
    "**不是卡住**（日志持续增长）。🔴 **新会话值得花 5 分钟查一下**：`grep -c \"BattleScene.cs Line\" _tmp_view/arena2/run8_battle.log` "
    "看是哪一行在刷 —— 若与 `A1949` 无关，那是**既有**的日志量；若相关，说明那处改动多打了什么。"
)
NEW = (
    "🔴 **2026-10-14（第三十会话）已查清 ⇒ 尾巴拆成新账 `A1967`**："
    "① **「~55 分钟」这个前提【站不住】** —— 日志文件自身 `birth 20:15:08 → modify 20:33:23` = **18 分 15 秒**（`stat` 现测）；"
    "55 分钟是**上一件自检结束到这一件结束的墙钟**（`run7_deck.log` mtime 19:36 → `run8` 收尾 20:33），中间 19:36→20:15 **有 39 分钟没跑 Unity**。"
    "「17 万行」也像是**跑动中数的**（收口值 **257,415 行**）。"
    "② **刷屏的不是 `BattleScene`**：`Instantiating material due to calling renderer.material during edit mode…` **10,642 条 × 每条 14–16 行栈 = 135,517 行 = 全文 52.6%**；"
    "源码只有一句 = **`Core/CardView.cs:726` 的 `mr.material.renderQueue = _queue;`**（`StampQueue` 内）。"
    "③ **归因 = 既有、与 `A1949` 无关**（`StampQueue` 由 `ce10113`（2026-10-12）引入；`1c3d27a` 一个字节没碰 `CardView.cs:705-727`，`Editor/BattleScene.cs` 也不在那笔提交里）。"
    "⇒ **正解另立账 `A1967`，⛔ 别顺手改**（同一批渲染器上 `BattleDriver.cs:5377-5378` 也调 `mr.material`）。"
)

A1967 = (
    "| 🆕 **`A1967`** | 🔴 **自检日志被 Unity 的材料实例化警告刷掉 52.6%**（2026-10-14 只读诊断 + 主对话现核）："
    "`instantiating material due to calling renderer.material during edit mode` **10,642 条 × 14–16 行托管栈 = 135,517 / 257,415 行**。"
    "源码只有一句 = `CardPresentation/Core/CardView.cs:726` 的 `mr.material.renderQueue = _queue;`（`StampQueue` 内，守卫在 `:725`）。"
    "**引入者 = `ce10113`（2026-10-12）**，与 `A1949` 无关（`1c3d27a` 没碰 `CardView.cs:705-727`。判据 = `git log -S`）。"
    "🔴 **⛔ 别照「等价改法」顺手改**（`new Material(mr.sharedMaterial)` + 写回 `sharedMaterial`）："
    "**它会破坏「每渲染器只有一份实例材质」这个性质** —— 同一批渲染器上 `BattleDriver.cs:5377-5378` 也调 `mr.material`（出牌揭晓 `DOFade`），"
    "改完那一处会**再克隆第二份** ⇒ 每张揭晓卡多一份材质（本仓被「暗中多拆材质」烧伤过）。"
    "**正解方向**（要做，铁律 11）：把「每渲染器一份克隆」换成**按 `(源材质, 队列)` 共享的静态实例缓存**，用 `sharedMaterial` 赋值 —— "
    "既消掉 10k 次克隆、又恢复合批；⚠️ 同一次要处理 `BattleDriver.cs:5377` 那一处 `.material`，并**验「材质总数」与「draw call 数」**（⛔ 别只看日志变短）。"
    "⚠️ 另一层：警告文案点明「**during edit mode … will leak materials into the scene**」⇒ 自检宿主跑完会 `SaveScene()`，"
    "**这 10k 份实例材质可能被烘进 `Battle.unity`**（与「生成物抖动」同族）—— 改前先 `git diff --numstat -- Scenes/Battle.unity` 对一眼。"
    "判据 = `_tmp_view/arena2/run8_battle.log`（10,642 条 · 现读复核过）。 |"
)

A1968 = (
    "| 🆕 **`A1968`** | ⚠️ **效果库资产带【坏 PPtr】，每次加载报警 39 次**（2026-10-14 只读诊断顺手查出，全库从没记过）："
    "`Broken text PPtr. GUID 00000000000000000000000000000000 fileID -6128945447879795609 is invalid!` —— "
    "7 个不同 fileID（`-6128945447879795609`×27 · `6627610752530350053`×4 · `-1887485317901086766`×2 · `3946243950355606049`×2 · `4592898973196831887`×2 + 2 个 ×1）。"
    "栈落在 `Resources.Load<WarpforgeEffectLibrary>(\"WarpforgeVFX/WarpforgeEffectLibrary\")`（源 = `WarpforgeVFX/Runtime/WarpforgeEffectLibrary.cs:88`）；"
    "**同一批 39 条也出现在生成侧 `run2_effectlib.log`** ⇒ 是 **`Assets/Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset` 资产本身带了悬空引用**"
    "（GUID 全零 = 文件内引用指向已不存在的对象），**不是加载方的问题**。**不致命**（库仍能用，`get_Instance` 78 帧都在跑），"
    "但有若干条目引用着不存在的东西。⇒ **要做**；建议**并到 `A1902`（效果库重写 968 个）那一笔后面**一起收。"
    "⚠️ 还差一步：**坏的是哪个条目的哪个字段**（只从 warning 拿到 fileID，没反解 `.asset` 的 YAML）。 |"
)

A1969 = (
    "| 🆕 **`A1969`** | ⚠️ **一条【回放分叉】的 LogError**（2026-10-14 只读诊断顺手查出）："
    "`[Replay] 分叉点 = 第 0 条（kind=0 … slot=5）：录的时候这条做完是 1580197177，现在做完是 -53266880`，"
    "且「录的局面」P2 手牌 **5** vs「现在的局面」**4**（`run8_battle.log:204145` / `:204169` / `:204181`）。"
    "调用链 = `BattleScene.cs:15566` → `BattleDriver.PlayReplay:723` → `ApplyLoggedAction:305`。"
    "🔴 **没判出它是不是【故意的负例】** —— `BattleScene.cs:15926` 一带确有描述「分叉点落在第一条」那种**人造分叉**的注释，"
    "且整跑自报 2873/0 全绿。⇒ **要做**：先读那一段测试代码确认它是不是「打岔负例」；⛔ **不是负例就是真缺陷**（本地录像回放线）。"
    "判据 = 同上日志（全文 `^\\[Replay\\]` 61 条里唯一的异常那条）。 |"
)

A1970 = (
    "| 🆕 **`A1970`** | ⚠️ **两条 AnimFX `[ERROR]`**（2026-10-14 只读诊断顺手查出）："
    "`Can't find target or acting card in an AnimFXModule of prefab` —— 各 1 次，出现在两个**原版 prefab** "
    "`BulletImpact_1shot_trail`（`run8_battle.log:32439`）与 `BulletImpact_2shot_trail_ork_NEW`。"
    "⚠️ 全文 `^[ERROR]` **只有这 2 条** ⇒ 量小但**像真缺口**（动画/特效的 target 解不出）。⇒ **要做**；判据 = 同一份日志 + 那两个 prefab 的 AnimFX 配置。 |"
)

A1971 = (
    "| 🆕 **`A1971`** | ⚠️ **自检夹具又出现【有洞棋盘】，以 LogError 打了 21 次**（2026-10-14 只读诊断顺手查出）："
    "`[BoardSlots] ⚠️ 棋盘不连续（Insert/RemoveAt）… 请把那个夹具改成合法局面`（Insert 9+3 · RemoveAt 7+2）。"
    "🔴 **记忆里的口径是「夹具 67 处有洞棋盘 2026-10-01 已清零（告警 67→0）」** ⇒ 这 21 条说明**那次清零没覆盖到 `BattleScene.Run` 自己的夹具**"
    "（或后来新加）⇒ **要核**（并按 `warpforge-board-contiguous-model` 那套「连续无洞模型」改成合法局面）。判据 = 同一份日志。 |"
)


def patch(path, pairs, insert_after_marker=None, insert_lines=None):
    b = io.open(path, 'rb').read()
    assert b.count(b'\r\n') == 0, '%s must be LF' % path
    text = b.decode('utf-8')
    for old, new in pairs:
        assert text.count(old) == 1, 'anchor hits=%d in %s :: %s' % (text.count(old), path, old[:60])
        text = text.replace(old, new)
    if insert_after_marker:
        lines = text.split('\n')
        hits = [i for i, l in enumerate(lines) if insert_after_marker in l]
        assert len(hits) == 1, 'marker hits=%d' % len(hits)
        i = hits[0]
        assert lines[i].startswith('| ') and lines[i].rstrip().endswith(' |'), lines[i][:80]
        lines[i + 1:i + 1] = [insert_lines]
        text = '\n'.join(lines)
    assert '\x00' not in text and '\r\n' not in text
    io.open(path, 'wb').write(text.encode('utf-8'))
    print('OK %s  bytes %d -> %d' % (path, len(b), len(text.encode('utf-8'))))


def main():
    patch(HANDOFF, [(OLD, NEW)])
    patch(TASKS, [], insert_after_marker='**`A1811`**', insert_lines='\n'.join([A1967, A1968, A1969, A1970, A1971]))


if __name__ == '__main__':
    main()
