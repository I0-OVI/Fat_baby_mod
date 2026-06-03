# 杀戮尖塔 2 人物 Mod 聊天承接

这个文件用于换到新聊天后快速承接当前项目。新聊天开始后，先读这个文件，再读 `workflow.md` 和 `implementation_log.md`，然后可以直接开始执行任务。

## 新聊天开场

可以直接把下面这段发给新聊天：

```text
请先阅读 /Users/carambola/Desktop/cursor+codex/mod/chat_handoff.md，承接之前的杀戮尖塔 2 人物 Mod 项目。
之后按 /Users/carambola/Desktop/cursor+codex/mod/workflow.md 的规则执行：开始任务前先写小任务清单，每完成一项就勾掉，完成后更新 implementation_log.md。
不要重新从零分析项目，先基于现有工程继续做。
```

如果已经有明确任务，可以这样说：

```text
请先阅读 chat_handoff.md 和 workflow.md，然后立刻开始这个任务：……
```

## 当前工作区

根目录：

```text
/Users/carambola/Desktop/cursor+codex/mod
```

Mod 工程：

```text
/Users/carambola/Desktop/cursor+codex/mod/workspace/mod
```

参考 Mod：

```text
/Users/carambola/Desktop/cursor+codex/mod/reference_mods/extracted_EW
```

实际游戏读取的 mod 安装目录：

```text
/Users/carambola/Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app/Contents/MacOS/mods/mod
```

重要文档：

- `workflow.md`: 每次任务的清单和进度。
- `implementation_log.md`: 已实现、未实现、测试方式。
- `cards.md`: 卡牌设计和机制说明。
- `sprite_pipeline.md`: 局内 sprite 管线。
- `chat_handoff.md`: 本承接文档。

## 执行规则

每次开始新任务前：

1. 在 `workflow.md` 的“当前任务”下面新增一个任务块。
2. 把任务拆成多个小项。
3. 每做完一项，就把 `[ ]` 改成 `[x]`。
4. 实现完成后运行可行验证。
5. 更新 `implementation_log.md`。

代码或资源改动优先使用现有工程结构，不要另起新项目。

## 当前角色

人物名：胖宝宝。

代码类名仍是：

```text
workspace/mod/ModCode/Character/ScarletAcolyte.cs
```

当前人物壳：

- 72 初始生命。
- 使用 `ScarletCardPool`。
- 使用 `ScarletRelicPool`。
- 使用 `ScarletPotionPool`。
- 战斗视觉当前使用 `fat_baby_combat_visual.tscn` 无脚本场景 + `fat_baby_combat_idle.png` 静态立绘；`ScarletAcolyte.CustomVisualPath` 指向该场景，由 BaseLib 自动转换为 `NCreatureVisuals`。
- 选人界面和 UI 小图标已经接入自定义资源。

## 已实现机制

猩红腐败：

- `ScarletCorruptionPower`
- 上限 3 层。
- 你的回合开始时失去等同层数的生命值。
- 不自然衰减。
- 失血无视格挡。

失衡：

- `ImbalancePower`
- 普通怪 10。
- 群怪精英 15。
- 单体精英 20。
- Boss 25。
- 削韧降低失衡值。
- 失衡到 0 时调用 `CreatureCmd.Stun(...)`，眩晕一回合。
- 眩晕后失衡值重置。
- 当前由起始遗物 `SCARLET_EMBER_RELIC` 在玩家回合开始前给敌人补失衡。

## 已实现卡牌

已实现代码的卡牌只有三张：

- `SCARLET_COST` / 猩红代价
- `FLAME_PURIFY` / 火焰啊，净化
- `PRAYER_STRIKE` / 祈祷一击，已接入削韧 2

其余 `cards.md` 里的牌目前是设计稿，未实现。

## 当前资源

选人背景大图：

```text
workspace/mod/mod/images/character_select/fat_baby_cover.png
workspace/mod/mod/scenes/screens/char_select/fat_baby_select_bg.tscn
```

选人按钮小图：

```text
workspace/mod/mod/images/character_select/fat_baby_select_icon.png
```

角色 UI 小图标：

```text
workspace/mod/mod/images/character_ui/fat_baby_onion_icon.svg
```

当前 UI 小图标状态：

- 白色正面洋葱头盔。
- 透明背景。
- 顶部已经压低，轮廓偏扁圆。

局内 sprite 概念素材：

```text
assets/character_sprites/onion_knight_ingame_sprite_v1.png
assets/character_sprites/onion_knight_ingame_sprite_v1_chromakey.png
```

当前战斗静态立绘：

```text
workspace/mod/mod/images/character_combat/fat_baby_combat_idle.png
workspace/mod/mod/scenes/character/fat_baby_combat_visual.tscn
workspace/mod/ModCode/Character/ScarletAcolyte.cs
```

管线说明：

```text
sprite_pipeline.md
```

## 构建和导出

快速只改 C#：

```bash
cd /Users/carambola/Desktop/cursor+codex/mod
./scripts/sts2_mod_dev.sh code
```

改了图片、本地化、PCK 资源或场景：

```bash
cd /Users/carambola/Desktop/cursor+codex/mod
./scripts/sts2_mod_dev.sh all
```

看日志：

```bash
./scripts/sts2_mod_dev.sh log
```

注意：

- 游戏实际扫描 app 包内部的 `Contents/MacOS/mods`。
- `./scripts/sts2_mod_dev.sh all` 的 Godot headless 导出过程中可能打印一次 `sts2.dll` 加载提示，但只要脚本最后显示安装完成，一般就是成功的。
- DLL 变更后需要重启游戏。
- PCK 资源变更后也建议重启游戏。

## 游戏内测试

启动游戏后选择 `胖宝宝`。

控制台可用：

```text
card SCARLET_COST
card FLAME_PURIFY
card PRAYER_STRIKE
```

检查点：

- 选人界面是否显示胖宝宝背景。
- 选人按钮是否显示胖宝宝小图。
- 角色 UI 小图标是否显示白色扁圆洋葱头盔。
- 敌人是否出现 `失衡`。
- `PRAYER_STRIKE` 是否让目标失衡值减少 2。
- `SCARLET_COST` 是否抽牌并获得猩红腐败。
- 下个己方回合开始时猩红腐败是否造成失血。
- `FLAME_PURIFY` 是否移除腐败并获得格挡。

## 未实现/待确认

机制：

- 充能。
- 虚无。
- 魔力。
- 多段削韧逐段结算的通用封装。
- 失衡是否受 Artifact 或免疫负面效果影响。
- Boss 是否需要眩晕抗性或特殊重置规则。

人物：

- 战斗内正式贴图。
- 战斗动画。
- 能量图标。
- 起始遗物正式效果。
- 自定义药水。

卡牌：

- `cards.md` 里大多数卡牌仍未实现。
- 建议下一批从基础牌开始：攻击、防御、蓄力攻击，或者从 2-3 张主题牌开始验证流派。

## 推荐下一步

如果做代码：

1. 实现基础攻击/防御，替换当前起始牌组里的临时牌。
2. 做 `充能` 或 `虚无` 的通用机制。
3. 实现一张多段削韧攻击，验证每段都削韧。

如果做美术：

1. 替换 `workspace/mod/mod/images/character_combat/fat_baby_combat_idle.png` 为新的透明 RGBA PNG。
2. 如尺寸变化，微调 `workspace/mod/mod/scenes/character/fat_baby_combat_visual.tscn` 中的 `Sprite2D` 缩放、位置、`Bounds`、`CenterPos` 和 `IntentPos`。
3. 执行 `./scripts/sts2_mod_dev.sh all`，重启游戏进战斗检查落点、血条/意图位置和透明边缘。

如果做设计：

1. 清理 `cards.md` 中重复/错字。
2. 确定 10 张核心普通牌。
3. 确定起始遗物正式效果。

## 关键代码入口

人物：

```text
workspace/mod/ModCode/Character/ScarletAcolyte.cs
workspace/mod/ModCode/Character/ScarletCardPool.cs
```

卡牌：

```text
workspace/mod/ModCode/Cards/
```

机制：

```text
workspace/mod/ModCode/Mechanics/ScarletCorruption.cs
workspace/mod/ModCode/Mechanics/Imbalance.cs
workspace/mod/ModCode/Powers/ScarletCorruptionPower.cs
workspace/mod/ModCode/Powers/ImbalancePower.cs
```

遗物：

```text
workspace/mod/ModCode/Relics/ScarletEmberRelic.cs
```

本地化：

```text
workspace/mod/mod/localization/zhs/
workspace/mod/mod/localization/eng/
```
