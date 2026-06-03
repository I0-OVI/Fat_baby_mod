# Fat Baby Mod（STS2）

《杀戮尖塔 2》角色 Mod 源码仓库。请从 **[Releases](https://github.com/I0-OVI/Fat_baby_mod/releases)** 下载 zip，不需要克隆本仓库。

目前mod只有63张卡牌，但体系基本完全，可以正常游玩。后续作者会补充完整86张卡牌，如果游玩过程中觉得缺了什么组件/玩着有点难受/bug 欢迎投稿作者邮箱: [zhangxin_sh@qq.com](mailto:zhangxin_sh@qq.com)

## 下载安装

1. 安装 Steam 版 **Slay the Spire 2**。
2. 打开本仓库 **Releases**，下载最新 `fat-baby-mod-x.y.z.zip`。
3. 解压后将 **`fat_baby`** 和 **`【001】必装前置`** 两个文件夹复制到游戏 `mods/` 目录，覆盖旧文件。
4. 完全退出游戏后重新启动，在 Mod 列表中勾选本模组。

详细路径说明见 zip 内的 `INSTALL.md` 或 [docs/INSTALL.md](docs/INSTALL.md)。

## 持续更新

### 本地开发（macOS）

```bash
# 可选：把游戏 DLL 拷到 deps/game/，见 deps/README.md
./scripts/sts2_mod_dev.sh all    # 构建并安装到本机 Steam 目录
./scripts/sts2_mod_dev.sh test   # 卡牌静态检查 + 编译
```

### 打开发布 zip

```bash
./scripts/package_release.sh          # 版本号默认读 mod.json
./scripts/package_release.sh 0.0.2  # 指定版本
# 输出: dist/fat-baby-mod-<version>.zip
```

zip 内容：`fat_baby/`、`【001】必装前置/`、`INSTALL.md`。

### 发布到 GitHub Releases

1. 更新 `workspace/mod/mod.json` 里的 `version`。
2. 本地生成 zip：

```bash
./scripts/package_release.sh
```

3. 在 GitHub 仓库页面打开 **Releases → Draft a new release**，上传 `dist/fat-baby-mod-<version>.zip`。

如果之后启用了 GitHub Actions，也可以提交并打标签自动发版（标签需与版本一致，带 `v` 前缀）：

```bash
git add -A
git commit -m "Release v0.0.2"
git tag v0.0.2
git push origin main --tags
```

当前自动发版 workflow 暂未提交到仓库；本机 GitHub token 需要额外的 `workflow` 权限才能推送 `.github/workflows/`。

**CI 配置（首次一次）**：在仓库 **Settings → Secrets and variables → Actions → Variables** 添加：


| 变量名             | 示例值                                                                              |
| --------------- | -------------------------------------------------------------------------------- |
| `STS2_GAME_DIR` | `/Users/you/Library/Application Support/Steam/steamapps/common/Slay the Spire 2` |


或在 `deps/game/` 放入 `sts2.dll`、`BaseLib.dll`、`0Harmony.dll`（不提交到 Git，仅 CI 自托管 runner 可用）。

### 首次推送到 GitHub

```bash
cd "/Users/carambola/Desktop/cursor+codex/mod"
git remote add origin https://github.com/I0-OVI/Fat_baby_mod.git
git push -u origin main
```

如果要一起发布第一个 Release，再打版本标签：

```bash
git tag v0.0.1
git push origin v0.0.1
```

## 仓库结构


| 路径                           | 说明              |
| ---------------------------- | --------------- |
| `workspace/mod/ModCode/`     | C# 模组逻辑         |
| `workspace/mod/mod/`         | 资源、本地化、立绘       |
| `scripts/sts2_mod_dev.sh`    | 本地构建安装          |
| `scripts/package_release.sh` | 生成 Releases zip |
| `cards.md`                   | 卡牌设计表           |
| `tests/`                     | 卡牌静态测试基线        |


不包含：`extract/`（反编译参考）、根目录 `assets/`（原始视频/参考素材），体积过大。实际随 Mod 发布的图片资源在 `workspace/mod/mod/images/`。

## 平台说明

- **游玩**：Windows / macOS 均可（使用 Releases 中的 zip）。
- **开发脚本**：当前以 macOS + Steam 安装路径为主；Windows 可参考 [deps/README.md](deps/README.md) 配置 `STS2_GAME_DIR` 与 `dotnet build`。

