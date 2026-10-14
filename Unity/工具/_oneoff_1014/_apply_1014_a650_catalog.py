# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · ① A650 结案（原版自己就没有）② §29·d 的「等 allcards」措辞订正（包已到手，缺的是更新的那份）
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_a650_catalog.py
"""
import io

MAIN = r'D:\4\项目任务.md'

A650_NEW = (
    "| ✅✅ ~~`A650`~~ | ✅ **2026-10-14（第三十会话）【结案】—— 按铁律 11 例外①：原版自己就没有这个实例，不是「我们没做」**"
    "（用户当天授权动 `d:/2` ⇒ 两条路都走完了）：\n"
    "🔴 **决定性证据 = 反编译里【零引用】**：全库提到 `EnemyInfoTouch` 的只有 **4 个文件**，**全部是它自己的方法体**"
    "（`EnemyInfoTouch__get_manager.c` / `__Start.c` / `__OnTouchDown.c` / `__Update.c`）⇒ **没有任何一处别的代码引用这个类**"
    "（没有 `AddComponent`/`GetComponent`、没有该类型的字段或参数）⇒ **类在自己的方法体之外零引用 = 【死代码】**。\n"
    "**旁证链四条（都现核）**：① `EnemyInfoTouch` **有 MonoScript 条目**（`assets_full/globalgamemanagers/MonoScript/MonoScript_3040.json`）"
    "⇒ **类在这个 build 里存在**；② 45 个包 MB 解出率 **100%**（`battlearena1` = 1715/1715）、**没有任何「解不出类名的 MB」** ⇒ **没有实例**；"
    "③ `grep -rl \"enemyInfoObj\"` 全 `d:/2` **只命中代码产物**、一个序列化 JSON 都没有；"
    "④ 🔴 **本会话把 `resources.assets` 那 616 个「解不出的对象」【全部解出来了】**"
    "（做法 = 无 type tree ⇒ 读原始字节取 `m_Script pathID`，再查 `globalgamemanagers/MonoScript/MonoScript_<pathID>.json` 的 `m_ClassName`）—— "
    "它们是 **`Image`×174 · `Text`×149 · `Toggle`×66 · `DebugUIHandler*`×约100 · `ContentSizeFitter`×24 · `VerticalLayoutGroup`×23 · `Button`×22 · "
    "`TMP_*` · `LanguageSourceAsset` · `DOTweenSettings` …**，**全是 Unity 自己的运行时调试面板 ＋ 第三方库的 Resources 资产**，"
    "**没有一个游戏自己的 UI 组件**、更没有它。\n"
    "⇒ **结论：那扇「按住显示、松手收起」的敌情浮窗在原版这个 PC build 里【根本不存在】** ⇒ **不做**（铁律 11 例外①）。\n"
    "⚠️ **副产品（本会话新工具，留着复用）**：`d:/4/Unity/工具/_a650_unpack_resources.py` 与 `_a650_probe2.py` —— "
    "**它们能读任何「无 type tree 的 `.assets`」的 MonoBehaviour**（原始字节头 ＋ MonoScript 表反查类名）。"
    "⚠️ `resources.assets` 的 **MB 头在本 build 里 `m_Name` 从 offset 32 开始**（`fileID(4) pathID(8) enabled(1+3pad) script.fileID(4) script.pathID(8) nameLen(4) name`），"
    "别拿固定 32 去套别的文件。⛔ **本条已结案，别再当挂起项读**。 |"
)

A897_ADD = (
    " 🔴 **2026-10-14（第三十会话）就地订正（铁律 5）—— 本行「等 `allcards`」的措辞要改：包【已到手】，缺的是【更新的一份】**：\n"
    "① **`allcards_assets_all` 就在我们抢救回来的那份远端缓存里** = `d:/2/远端缓存_旧profile_1013/Unity缓存_按哈希/54bdb7b1…/__data`"
    "（467 KB，**1,195 个 `RawCardScript`**），**已全字段解出** → `导出/cards_1195_2022时代.json`（38.6 MB）⇒ **不是「包没到手」**。\n"
    "② 🔴 **本会话拿 `catalog_main.json`（Addressables 总清单）做了【全量对账】**：清单里 **84 个 `.bundle` 唯一名**；我们的 `assets_full` 也是 **84 个**，"
    "但**多 7 少 7** ——\n"
    "　· **清单有、我们没有的 7**：`allcards_assets_all` · `localization_assets_all`（**这两只我们都有旧版拷贝，只是不在 `assets_full` 里**）· "
    "`alternateartstyles_assets_all`（同上）· `atlasindividual_assets_0_generaluiatlas` · `Custom_unitybuiltinshaders` · "
    "`scenes_scenes_mainmenuwarpforgeclosedbeta` · `chaosspacemarinesemperirschildrencardanims_assets_all`"
    "（⚠️ **清单里这个拼写是原版自己拼错的**：`emperirs` vs 我们的 `emperorschildren`）；\n"
    "　· **我们有、清单没有的 7**：**`spacemarinesspacewolvescardassets_assets_all` · `spacemarinesspacewolvescardanims_assets_all` · "
    "`scenes_scenes_battlearenaspacewolves`** ＋ 新版主菜单 `scenes_scenes_mainmenuwarpforge` ＋ `Waprforge_monoscripts` / `Warpforge_unitybuiltinassets` ＋ 拼写差异那一个。\n"
    "③ ⇒ 🔴 **结论：我们的客户端比那份 2025-07-30 的老 catalog 【新】（多出 SpaceWolves 整族）** ⇒ 「等 `allcards`」的真实含义 = "
    "**等一份【更新的】`allcards`**（老快照只有 1,195 张、覆盖不到后来新增的卡），⛔ **别写成「包还没到手」**。\n"
    "④ 📋 **总清单文件本身也是一件资产**（本机唯一）：`d:/2/远端缓存_旧profile_1013/Everguild/com.unity.addressables/catalog_main.json`"
    "（1.69 MB · `m_InternalIds` **10,302 条** · 含 **84 个 bundle 名**）—— **将来判「哪个包存在」一律查它**，⛔ 别再靠 `find -iname`（按哈希命名的缓存**搜不到**）。"
)

A1070_ADD = (
    " 🔴 **2026-10-14 同批订正**：本笔与 `A897`/`A970` 是同一笔 —— 「包没到手」这个说法要改成「**到手的是 2025-07-30 的旧快照**」，"
    "详见 §29·d 里 `A897` · `A970` 那一行新加的订正段（含 `catalog_main.json` 的 84 包对账结论）。"
)


def block_end(lines, i):
    j = i
    while j < len(lines) and not lines[j].rstrip().endswith(' |'):
        j += 1
    assert j < len(lines)
    return j


def main():
    b = io.open(MAIN, 'rb').read()
    assert b.count(b'\r\n') == 0
    lines = b.decode('utf-8').split('\n')

    hits = [i for i, l in enumerate(lines) if l.startswith('| `A650` |')]
    assert len(hits) == 1, ('A650 行', len(hits))
    j = block_end(lines, hits[0])
    lines[hits[0]:j + 1] = [A650_NEW]
    print('  ✅ A650 已结案')

    for head, seg in (('| `A897` · `A970` |', A897_ADD), ('| 🆕 **`A1070` 的 ③', A1070_ADD)):
        hits = [i for i, l in enumerate(lines) if l.startswith(head)]
        assert len(hits) == 1, (head, len(hits))
        if '措辞要改' in lines[hits[0]] or '包没到手' in lines[hits[0]]:
            print('  = %s 已订正过' % head); continue
        j = block_end(lines, hits[0])
        lines[j] = lines[j].rstrip()[:-1].rstrip() + seg + ' |'
        print('  +%s' % head)

    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('OK 总行数 %d' % len(lines))


if __name__ == '__main__':
    main()
