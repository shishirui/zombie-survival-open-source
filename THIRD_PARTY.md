# 素材、展示媒体与许可证边界 / Third-party notices

MIT 适用于本仓库的自有代码、自有关卡参数、工具及文字文档。它不覆盖商业依赖、第三方素材、游戏名称/标识及 `docs/media`、Release 中的截图和视频。媒体仅用于展示本游戏；其中的美术、声音和图标仍受原作者许可约束。请勿从中提取素材再分发。

## 未分发的商业资源

| 资源 | 本项目使用的版本 | 用途 / 获取途径 |
| --- | --- | --- |
| TopDown Engine — More Mountains | 4.2 | `Health` 接口、Loft 示例中的表现资源；[官方站点](https://topdown-engine.moremountains.com/) |
| POLYGON – Apocalypse Pack — Synty Studios | 1.07 | 角色、建筑、车辆、道具和氛围资源；[官方产品](https://syntystore.com/products/polygon-apocalypse-pack) |
| Epic Toon FX — Archanor VFX | 1.81 | 命中、爆炸、酸液、拾取和强化表现；[Asset Store](https://assetstore.unity.com/packages/vfx/particles/epic-toon-fx-57772) |
| Ultimate Sound FX Bundle | 本地授权包 | 部分战斗/反馈声音；具体文件版本与购买许可应由使用者自行核对 |

素材包中包含 C# 文件并不意味着这些文件可以开源。TopDown 等第三方源码及其副本不在此仓库；基于资源包修改、转换或合并后的模型、材质、动画、粒子预制件和音频也未分发。`Editor` 中的自有接入代码通过资源路径引用依赖，不包含依赖内容。

具体权利由购买渠道和实际许可决定。参考 [Unity Asset Store EULA](https://unity.com/legal/as-terms) 与 [Synty 许可说明](https://syntystore.com/pages/licences-overview)。Synty 商店购买与 Unity Asset Store 购买可能适用不同条款，不能用仓库的 MIT 替代。

## 完整游戏及演示媒体中的开放资源

以下资源的原始文件也未包含在本次代码发布中，保留署名便于理解媒体来源和自行接入。

**Game-icons.net — CC BY 3.0**

- [Grenade](https://game-icons.net/1x1/lorc/grenade.html)、[Doorway](https://game-icons.net/1x1/lorc/doorway.html)、[Padlock](https://game-icons.net/1x1/lorc/padlock.html) — Lorc
- [Acrobatic](https://game-icons.net/1x1/darkzaitzev/acrobatic.html) — DarkZaitzev
- [Machine gun magazine](https://game-icons.net/1x1/delapouite/machine-gun-magazine.html)、[MP5](https://game-icons.net/1x1/delapouite/mp5.html)、[Sawed-off shotgun](https://game-icons.net/1x1/delapouite/sawed-off-shotgun.html) — Delapouite

[许可证](https://creativecommons.org/licenses/by/3.0/)。游戏中进行了颜色和大小调整；媒体记录这些显示效果。

**Quaternius — Animal Pack Vol.2 / Dog**

[作者发布页](https://opengameart.org/content/animated-animales-low-poly)，[CC0](https://creativecommons.org/publicdomain/zero/1.0/)。游戏中调整了材质、比例、动画播放与角色接入。

**中文字体**

完整工程中的 `Chinese.ttf` 附 SIL Open Font License，版权声明为 Cyano Hao (2018–2022)，部分来自 Adobe Source 字体 (2014–2021)。原许可另见 [font-license.txt](docs/font-license.txt)，字体本身未分发。

## 贡献时的边界

请只提交有权公开的代码和自行创作的内容。不要上传 `.unitypackage`、供应商源码、FBX/贴图、从商业包派生的预制件、音频、签名证书、Provisioning Profile 或构建日志中的账号资料。
