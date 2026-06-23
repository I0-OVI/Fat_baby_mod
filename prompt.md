# 卡面生图 Prompt

**目的：** 杀戮尖塔 2 卡面；画风偏黑魂 3 / 法环。
**原则：** 只改画风，**不改变**参考图的动作、站姿与手持武器；可裁切/zoom，但姿势与视角不变。

## 色调参考（统一）

**所有卡面生成必须对齐 `blood_slash` 的色调。**

- 参考图路径：`workspace/mod/mod/images/card_portraits/blood_slash.png`
- 特征：偏暖灰褐、低饱和石质环境、手绘颗粒感、柔和漫射光、中等亮度（不过暗也不过曝）
- 魔法/特效色：保留录屏中的高饱和点缀（如猩红斩击、月光蓝、星雨紫蓝），环境底色仍跟 `blood_slash`

## 推荐流程

1. **原帧裁切：** `assets/card_portraits_src/picks/<stem>/<frame>.png` → PIL 等比放大后居中裁切到 **1000×760**（不用 `sips -z` 拉伸）
2. **轻修上色：** `GenerateImage` 以裁切图为构图参考，**同时传入 `blood_slash.png` 作色调参考**；prompt 强调不改动作、不改视角
3. **色调对齐：** PIL 后处理，将整体亮度/饱和度/暖色倾向贴近 `blood_slash`（见下方脚本）
4. **接入 mod：** 输出到 `workspace/mod/mod/images/card_portraits/<stem>.png`，对应卡牌类加 `PortraitPath`

```bash
# 裁切 + 色调对齐（第二步之后）
python3 scripts/grade_card_portrait.py <输入.png> workspace/mod/mod/images/card_portraits/<stem>.png
```

## Prompt

```text
Slay the Spire 2 card portrait, horizontal 1000x760, Dark Souls 3 and Elden Ring dark fantasy painted style,
keep exact same pose action camera angle and weapon grip as reference image, do not change stance or swap weapon,
only restyle to game card illustration, onion knight ring-band armor, thick outline, soft blurred simple background,
apply same color tone as blood_slash card portrait: muted warm grey-beige desaturated stone, gritty painterly texture,
soft diffuse overcast lighting, medium brightness, keep spell VFX as saturated accent color, not screenshot
```

## Negative

```text
different pose, new action, weapon change, wrong weapon, dual wield, missing weapon, extra weapons,
reaching different direction, turned body, motion blur, HUD, text, photorealistic, anime, bright colors,
busy background, wide landscape empty scene, tiny character, small figure, far away shot, distant character,
lots of floor and sky, environment larger than character, merged double exposure, too dark, too bright, cold blue tint on stone
```

## img2img

- 构图参考：选帧裁切图（1 张）
- 色调参考：`blood_slash.png`（1 张，必传）
- 尺寸：1000×760
- 变化幅度：尽量小，只上色与去 HUD/黑边，不重绘构图
