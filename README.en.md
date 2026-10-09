# Dead District

**Find a way through the city. Survive the next wave.**

A mobile-first, landscape, 3D top-down zombie survival shooter built with Unity and URP. Automatic targeting and firing leave room for movement, weapon choice, grenades and well-timed dodges.

[中文](README.md) · [Watch the trailer](https://github.com/shishirui/zombie-survival-open-source/releases/download/v0.13.0-source/dead-district-trailer.mp4) · [Development guide](docs/DEVELOPMENT.md) · [Asset licenses](THIRD_PARTY.md)

![Dead District gameplay](docs/media/hero.jpg)

> **Original-source release, based on v0.13.0 / Build 64.** This repository contains our gameplay code, UI, chapter data, editor tools and validation scripts. Commercial engine source, purchased assets, derived art assets and finished scenes are excluded. A fresh clone is **not a ready-to-play Unity project**.

## Three chapters. More ways to survive.

| Chapter | Weapons available during the run | Boss encounter |
| --- | --- | --- |
| Abandoned Commercial Street | Rifle | Area slam |
| Abandoned Quarantine Camp | Rifle, shotgun | Charging brute; chained charges below half health |
| Abandoned Freight Depot | Rifle, shotgun, grenade launcher | Multi-zone acid volleys |

New weapons must still be collected. Each chapter has eight waves. Clear the entire wave, take a five-second breather, then face the next one. Later chapters introduce denser attacks from multiple directions.

- **Continuous combat:** automatic targeting respects screen visibility and cover.
- **Distinct weapons:** sustained rifle fire, close-range shotgun pellets, straight-flying explosive launcher rounds with smoke trails.
- **Choose when to upgrade:** earn points during combat; open a three-choice upgrade panel when you are ready.
- **Readable enemies:** ordinary infected, fast infected dogs, bulky infected and telegraphed boss attacks.
- **Mobile controls:** movement stick, drag-to-aim grenades, dodge, reload and weapon switching.
- **World feedback:** destructible props, doors, pickups, cover fading, corpses, environmental ambience and layered effects.
- **Comfort settings:** music/SFX levels, camera shake, reduced flashes, joystick mode and button size.

| Street combat | Camp combat |
| --- | --- |
| ![Street](docs/media/street.jpg) | ![Camp](docs/media/camp.jpg) |
| ![Launcher](docs/media/freight.jpg) | ![Acid boss](docs/media/boss.jpg) |

[![Watch the trailer](docs/media/trailer-poster.jpg)](https://github.com/shishirui/zombie-survival-open-source/releases/download/v0.13.0-source/dead-district-trailer.mp4)

Media was captured in a desktop presentation build using shared game systems and art. Capture uses scripted inputs, staged weapons/bosses and player protection. It is not an unassisted playthrough or an iPhone performance benchmark. Third-party assets shown in media are not licensed under MIT.

## Explore the source

- `Assets/ZombieSurvival/Runtime`: gameplay, enemy AI, UI, growth, VFX orchestration and opt-in runtime validations.
- `Assets/ZombieSurvival/Editor`: level construction, art integration and build tools.
- `Assets/ZombieSurvival/Resources`: original chapter/wave data.
- `Assets/ZombieSurvival/Settings`: combat tuning.
- `Assets/ZombieSurvival/Plugins/iOS`: launch-argument bridge.

Start with `SurvivalGame`, `SurvivalWeapons`, `ChapterDirector`, `RunGrowth`, `EliteCharge` and `EliteAcid`.

## Development

Baseline: **Unity 6000.3.25f1**, **URP 17.3.0**, **TopDown Engine 4.2**. `SurvivalHealth` extends the commercial engine's `Health` class. iOS exports require Xcode and your own signing credentials.

You can study the code without buying assets. Compiling it requires the engine dependency; recreating the complete game also requires legally obtained or replacement art/audio, configured resource prefabs, scenes and navigation meshes. Importing the commercial packages alone does not recreate the complete project. There is currently no one-click restoration tool. See [the integration guide](docs/DEVELOPMENT.md).

Desktop controls: WASD/arrows to move, G grenade, Shift dodge, R reload, Q switch weapon, Esc pause. Click the upgrade icon, then choose with 1–3.

## Contributing and license

Issues and focused pull requests are welcome. Include the chapter, device, version and reproduction steps. See [CONTRIBUTING.md](CONTRIBUTING.md).

Original code/configuration: **MIT, © 2026 Rexshi**. Commercial dependencies, third-party assets and promotional media are outside that license; see [THIRD_PARTY.md](THIRD_PARTY.md). Please do not upload purchased assets or credentials.
