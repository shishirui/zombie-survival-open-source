# 开发与集成指南

## 公开了什么

本次发布以完整游戏 0.13.0 / Build 64 的自有源码为基线，包含运行时代码、编辑器接入/生成工具、三关波次配置、基础战斗参数、iOS 启动参数桥接，以及 Unity 包清单。

不含成品 `.unity` 场景、`PackVisuals` 配置及其派生预制件、商业/第三方素材、生成的导航网格、签名工程、IPA、用户存档和原私有仓库历史。部分场景/资源依赖必须自行重新接入，不能把本仓库描述为开箱即用的完整工程。

## 环境

- Unity 6000.3.25f1（Unity 6.3）
- URP 17.3.0、AI Navigation 2.0.4、Cinemachine 3.1.2；完整清单见 `Packages/manifest.json`
- TopDown Engine 4.2，须自行合法获取。唯一直接引用的第三方 C# 命名空间为 `MoreMountains.TopDownEngine`
- iOS：安装 Unity iOS Build Support、Xcode，并使用自己的 Bundle ID、团队与签名

## 推荐接入顺序

1. 在独立目录克隆本仓库，使用对应 Unity 版本打开。由于尚未导入 TopDown，此时出现 `Health` / `TypedDamage` 缺失是预期依赖错误。
2. 导入自行授权的 TopDown Engine 4.2，处理其官方包依赖。不要将导入的源码提交到公开 Git 历史。
3. 完整游戏表现依赖 Apocalypse、Epic Toon FX、音频、字体、图标和感染犬。可以自行取得许可后接入，也可以用自有资源替代；替换需适配 `LoftActor`、`PackVisuals`、`SurvivalAudio` 等资源合同。
4. 按下表准备资源和三个场景。生成导航网格，设置 Infected 层为 24、障碍物层为 8，并检查射线、碰撞与路径。
5. 场景中配置 `SurvivalGame`、`SurvivalHud`、主相机、光照和基础配置。URP 管线与材质需在本地创建/接入。
6. 先在编辑器确认三个关卡启动和资源加载，再进行桌面构建与真机测试。

| 必要接入点 | 代码中的合同 |
| --- | --- |
| `Resources/DeadDistrict/PackVisuals.asset` | `PackVisuals.cs`：角色、武器、粒子、道具引用 |
| 角色预制件 | `LoftActor.cs`：动画、骨骼/挂点、持枪与击发 |
| 重装及感染犬 | `Resources/DeadDistrict/Brutes/Brute-0`、`Brute-1`、`InfectedHound` |
| 声音 | `SurvivalAudio.cs` 中的 `Load` 调用；缺失声音会抛异常 |
| UI 字体/图标 | `SurvivalHud`、`ActionGlyph` 与相关 UI 脚本的 `Resources.Load` |
| 场景路径 | `ChapterCatalog.ScenePaths` 定义的 DeadDistrict、QuarantineCamp、FreightDepot |
| 环境、拾取和强化表现 | `DistrictAtmosphere`、`RewardFeedback`、`PackBurst` 中的资源加载 |

可用 `rg 'Resources.Load|AssetDatabase.LoadAssetAtPath' Assets/ZombieSurvival` 查看完整引用。这些路径是接入合同，仓库不会下载商业包，也不提供绕过购买/授权的机制。

## 编辑器工具的定位

`PrototypeSetup`、`VisualUpgrade`、`StreetSampleUpgrade`、`CampChapterSetup`、`FreightChapterSetup`、`FeedbackAssets` 等保存了迭代中的生成与转换逻辑。它们依赖具体的包版本、路径和已有项目状态，且会写入场景/资源。

这些工具**不是**从空目录完整重建最新游戏的已验证流水线。请先检查输入路径、在副本里分步运行并检查输出；不要盲目按文件名依次执行，也不要对已有作品直接点击 Rebuild。

## 构建

完成资源与场景集成后，`PrototypeSetup.Build` 可构建 macOS 开发版本；`MobileBuild.ExportIOS` 导出 iOS 开发工程。`DistributionBuild.ExportIOS` 是非开发版导出入口，公开版使用环境变量设置签名身份：

```sh
export DEAD_DISTRICT_APPLE_TEAM="YOUR_TEAM_ID"
export DEAD_DISTRICT_BUNDLE_ID="com.yourcompany.yourgame"
export DEAD_DISTRICT_RELEASE_BUILD="1"
export DEAD_DISTRICT_RELEASE_OUTPUT="/absolute/path/outside/project/ios"
```

导出后的 Xcode 签名、Archive 和分发由你自己的 Apple 开发者账户配置。本仓库不含作者的证书、描述文件或发布服务凭据。

## 验证范围

代码里有按命令行显式启用的运行时验证，例如 `-chapter-smoke`、`-camp-smoke`、`-freight-smoke`、`-progression-feedback-smoke`、`-density-smoke`。它们需要完成资源接入后的构建，部分用例使用无敌/固定敌人等测试条件，不能当作真实玩家试玩。

```sh
/path/to/Game.app/Contents/MacOS/Game \
  -chapter-smoke -validation-output /absolute/path/results
```

发布前记录：自有源码来自已验证的完整私有工程；公开目录做文件边界、敏感信息和链接检查。**公开目录没有在缺失商业依赖和场景的状态下完成可玩性验证**，CI 也不声称 Unity 编译或真机验收通过。

## 不要丢失的玩法约束

- 经验满只授予强化点，打开强化由玩家主动触发。
- 当前波必须清空，休息 5 秒才开始下一波。
- 枪械需同时满足关卡许可和局内获取条件。
- 自动射击需满足视野与掩体检测；不要为了射击连续性越过这些检查。
- 尊重闪光减弱设置；高射速、高密度场景需要单独做手机性能测试。
