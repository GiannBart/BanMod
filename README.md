[🇮🇹 Read this README in Italian](https://github.com/GiannBart/BanMod/blob/main/README_IT.md)

> [!WARNING]
> ## Before using BanMod: choose the correct mode
>
> BanMod provides two modes. The correct mode depends on the features that will be used in the lobby:
>
> - **Modded +25:** for hosting lobbies with gameplay changes, custom roles, host features that change game behavior, or any modification that may affect other players' experience. The lobby must be identified and registered as modded according to the [Among Us Mod Policy](https://www.innersloth.com/among-us-mod-policy/) and the [official technical documentation](https://github.com/Innersloth-LLC/AmongUsModdingInformation).
> - **Vanilla:** for using only anti-cheat and local visual modifications that do not change gameplay or other players' experience. “Vanilla” is the name of the BanMod mode and does not mean that the client itself is unmodified. Innersloth states that not every anti-cheat case can be classified in advance and strongly recommends registering the mod when in doubt.
>
> In **Modded +25** mode, commands remain unchanged. To hide the command text from other players, replace the `/` prefix with `/cmd`: for example, `/bm blu` becomes `/cmd bm blu`.
>
> Always select the mode that matches the active features. Do not use BanMod to disturb other players, alter non-consensual lobbies, deceive participants, or gain unfair advantages.

> [!CAUTION]
> ## BanMod is anti-cheat
>
> If BanMod detects other mods or unrecognized components, it automatically disables itself and blocks Premium features. This check exists to protect the project, its services, and users from cheats, tampering, and incompatible configurations.
>
> If you use another legitimate mod, contact the administrator before using it together with BanMod. It will be reviewed and, if considered legitimate and compatible, may be added to the whitelist. The presence of an unrecognized mod does not grant access to official services or optional features.

---

<div align="center">

<img src="docs/images/image.png" alt="BanMod banner" width="100%">

# BanMod

**Lobby moderation, anti-abuse protection, host controls, custom roles, and configurable game modes for Among Us.**

[![Core license: GPL-3.0](https://img.shields.io/badge/core-GPL--3.0-blue.svg)](LICENSE)
[![Platform: Windows](https://img.shields.io/badge/platform-Windows-0078D4.svg)](#requirements)
[![Official website](https://img.shields.io/badge/website-banmod.online-7A5CFA.svg)](https://banmod.online)

[Website](https://banmod.online) · [Instructions](https://banmod.online/instructions) · [Downloads](https://banmod.online/downloads)

</div>

> [!IMPORTANT]
> BanMod is an unofficial community-made modification. Before downloading it or using official services, read the [Important Information & Rules](IMPORTANT_INFO_AND_RULES.md), the [Official Rules](https://banmod.online/rules), the [Privacy Policy](https://banmod.online/policy/privacy), the [Cookie Policy](https://banmod.online/policy/cookies), and, above all, the [Among Us Mod Policy on Innersloth's official website](https://www.innersloth.com/among-us-mod-policy/). The policy may change, and users are responsible for checking the current version.

## Description

BanMod is a **Windows mod for Among Us**, based on **BepInEx IL2CPP**. The public core, distributed under GPLv3, provides persistent moderation tools, anti-abuse protections, host administration, gameplay options, custom roles, and supporting interfaces.

The public repository contains only the **core**. Some additional features, historically called “Premium”, are separate optional components and are not required to compile or use the core.

## Main features

- **Moderation:** persistent bans and blocks, suspicious-player lists, name and word filters, spam protection, AFK management, and player administration.
- **Host controls:** automatic start, meeting and voting rules, tasks, sabotages, doors, maps, lobby messages, summaries, and configurable actions.
- **Roles and modes:** custom or modified roles, presets, role configuration, Hide and Seek improvements, and testing modes.
- **Client and visual tools:** configurable keys, zoom in permitted states, decorations, dark theme, custom interfaces, outfit/skin menus, and local options.
- **Anti-cheat and connected services:** optional verification, reports, server messages, anti-abuse systems, and lobby services.

Host, debug, or testing tools must only be used in controlled environments or in lobbies where all participants know and accept the rules.

## Images

<p align="center">
  <img src="docs/images/main-menu.png" alt="BanMod main menu" width="48%">
  <img src="docs/images/options-menu.png" alt="BanMod options menu" width="48%">
</p>

<p align="center">
  <img src="docs/images/game-settings.png" alt="BanMod game settings" width="82%">
</p>

BanMod custom skins are separate proprietary content. They are not part of the public source code and are not distributed under the GPL. See [Licenses and components](#licenses-and-components).

## Requirements

- A legitimate copy of **Among Us** for Windows PC.
- A game version supported by the current BanMod release.
- The correct package for **Steam** or **Epic Games**.
- Permission to extract files into the folder containing `Among Us.exe`.

Among Us updates may break compatibility. Always check the latest release before installing BanMod or reporting an issue.

## Installation

1. Download the current package from the [official download page](https://banmod.online/downloads).
2. Select the Steam or Epic Games version.
3. Open the folder containing `Among Us.exe`.
4. Extract all files from the BanMod ZIP into that folder.
5. Make sure `Among Us.exe` and the `BepInEx` folder are at the same level.
6. Start Among Us. After BepInEx finishes loading, BanMod should appear in the main menu.

**Steam:** Library → right-click **Among Us** → **Manage** → **Browse local files**.

**Epic Games:** Library → three-dot menu next to **Among Us** → **Manage** → folder icon.

### Updating and uninstalling

When instructed by the release notes, back up the `BAN_DATA` folder. Remove obsolete or duplicate BanMod DLLs from `BepInEx/plugins` and do not mix files from different releases.

To uninstall, first save any presets or configuration files you want to keep, then use your platform's file verification:

- **Steam:** Properties → Installed Files → Verify integrity of game files.
- **Epic Games:** Manage → Verify.

## Default controls

- `Delete`: opens the main BanMod menu.
- `F10`: opens the keybind configuration menu.

Available keys and menus may change depending on the release or host permissions. Check the in-game guide and the [official instructions](https://banmod.online/instructions).

## Optional Premium features

Premium features are additional features that are not associated with the main operation of the core:

- they are **optional**, **free**, and not required to use or compile the public mod;
- they are not included in this repository and are distributed separately through official services;
- they must be selected in the main login menu to be activated;
- they are offered only to users who comply with the rules, pass the applicable security checks, and use compatible configurations;
- they are subject to a separate private/proprietary license, described in [LICENSES.md](LICENSES.md);
- they are not mandatory, promised, or owed, and their availability may be changed, suspended, or revoked;
- they may not be copied, extracted, republished, redistributed, sublicensed, or included in forks without separate written authorization.

BanMod services—including APIs, verification, tokens, reports, server messages, and lobby services—are separate from the GPL core and subject to their own rules and policies. Service rules do not restrict the rights granted by GPLv3 over code that is actually covered by the GPL.

## Rules of use

When using an official build or BanMod services:

1. Do not use cheats, malicious clients, exploits, unlockers, request manipulation, spam, false reports, bypass tools, or other systems intended to harm the game, the project, or its users.
2. Do not disturb public lobbies, modify the experience of players who are unaware of the mod, or gain unfair advantages.
3. Select **Modded +25** when the active features change gameplay, role behavior, host authority, or another player's experience. When in doubt, register the mod.
4. Use **Vanilla** only with anti-cheat and local visual modifications that do not modify gameplay or another player's experience.
5. Do not send automated, malformed, excessive, or unauthorized requests to BanMod endpoints.
6. Do not reuse official tokens, credentials, build identifiers, or private endpoint details in forks.
7. Follow the Among Us Terms of Use and Mod Policy, community rules, applicable law, and the consent of other players.

User reports must be verified and must not be treated as proof without reasonable checks. Technical restrictions may be applied to clients, tokens, or identifiers associated with abuse, incompatibility, or violations.

## Forks and modified builds

GPLv3-covered core code may be studied, modified, and forked. Anyone distributing a fork must, among other things:

- preserve GPLv3, copyright notices, attributions, and warranty disclaimers;
- clearly identify the modifications and their date;
- provide the complete corresponding source for every distributed binary;
- distribute GPLv3-covered derivative code under GPLv3 without additional restrictions;
- clearly state that the fork is unofficial and must not imply endorsement by GianniBart, BanMod, Among Us, or Innersloth;
- not include BanMod proprietary skins, separate Premium components, private server modules, keys, tokens, personal data, reports, or game files;
- replace or disable integrations with official BanMod APIs unless separately authorized;
- provide independent support and not redirect fork-specific issues to official BanMod support channels.

A fork may use its own backend or no backend. A modified or self-compiled build is not an official release and may not be eligible for official services or optional features; this does not remove the rights granted by GPLv3 over the GPL-covered core.

The project considers separately distributed Premium components to be independent works and not part of the published GPL core. The relevant reference is the final paragraph of [section 5 of GPLv3](https://www.gnu.org/licenses/gpl-3.0.html#section5), concerning aggregates. This reference does not automatically make a component an independent work: its qualification depends on its actual nature and technical integration. Nothing in this README limits GPL rights over code that is actually covered by that license.

## Building from source

The project uses **.NET 6** and BepInEx IL2CPP packages:

```bash
git clone https://github.com/GiannBart/BanMod.git
cd BanMod
dotnet restore
dotnet build -c Release
```

Before building, review `BanMod.csproj`: remove developer-specific Windows paths, configure IL2CPP assemblies and metadata using your legitimate game installation, and remove local post-build targets. Do not publish secrets, credentials, local configurations, Among Us binaries, `Among Us_Data`, `GameAssembly.dll`, or other game files.

The DLL is normally generated in `bin/Release/net6.0/`.

## Contributions and credits

Issues and pull requests for the GPL core are welcome when they respect people, the law, licenses, and the project's goals. By submitting code, you confirm that you have the right to contribute it and agree that the contribution may be distributed under GPLv3 unless a different written agreement applies. Do not submit proprietary components, unlawfully obtained game code, secret endpoints, credentials, or personal data.

BanMod contains original work and portions inspired by or derived from open-source projects. Preserve all notices contained in source files and in `Resources/Credits and License.txt`.

Main credited projects:

- [Town of Host](https://github.com/tukasa0001/TownOfHost)
- [Town of Host Enhanced](https://github.com/EnhancedNetwork/TownofHost-Enhanced)
- [EndlessHostRoles](https://github.com/Gurge44/EndlessHostRoles)
- [AmongUsRevamped](https://github.com/ApeMV/AmongUsRevamped)
- [MalumMenu](https://github.com/scp222thj/MalumMenu)
- [TheOtherRoles](https://github.com/TheOtherRolesAU/TheOtherRoles) / TheOtherHats
- [BetterAmongUs](https://github.com/D1GQ/BetterAmongUs-Public)
- [GameLogger](https://github.com/whichtwix/GameLogger)
- NLayer components and contributors, under the MIT License where indicated

Credits do not imply affiliation or endorsement.

## Licenses and components

| Component | License or ownership | Reference |
| --- | --- | --- |
| BanMod public core and source | GNU General Public License v3.0, except files carrying a different compatible notice | [LICENSE](LICENSE) · [GPLv3 §5](https://www.gnu.org/licenses/gpl-3.0.html#section5) |
| Third-party code and libraries | Original licenses and notices, including MIT where indicated | [LICENSES.md](LICENSES.md) · `Resources/Credits and License.txt` |
| Optional Premium components delivered by the server | Separate private/proprietary license; not included in the repository | [LICENSES.md](LICENSES.md) |
| BanMod custom skins | © 2026 GianniBart. All rights reserved; separate from the core | [LICENSES.md](LICENSES.md) |
| Among Us, names, logos, characters, and related materials | Property of Innersloth LLC and/or its licensors | [Among Us Mod Policy](https://www.innersloth.com/among-us-mod-policy/) |

This table is only a summary and does not replace the applicable license texts. The distinction between the GPL core and separate components also depends on their actual structure and integration. For legal decisions, consult a qualified professional.

## Innersloth notice, non-affiliation, and liability

BanMod must display the mod stamp required by the [Among Us Mod Policy](https://www.innersloth.com/among-us-mod-policy/) during gameplay and must not include the Among Us base game or unauthorized copies of Among Us files.

Official text required by Innersloth, reproduced without modification:

> This mod is not affiliated with Among Us or Innersloth LLC, and the content contained therein is not endorsed or otherwise sponsored by Innersloth LLC. Portions of the materials contained herein are property of Innersloth LLC. © Innersloth LLC.

The software is provided **“as is”**, without express or implied warranties. To the maximum extent permitted by law, the authors and contributors are not responsible for bans, account restrictions, data loss, incompatibility, crashes, service interruptions, or damages resulting from use or misuse of the mod, unsupported configurations, modified builds, or violations of third-party rules.

Use of the mod is at the user's own risk. Neither this README nor the mode selector guarantees that a specific configuration will comply with every future version of Innersloth's rules. Always consult the [current official policy](https://www.innersloth.com/among-us-mod-policy/) and the [official technical documentation](https://github.com/Innersloth-LLC/AmongUsModdingInformation) before use.

## Support

- Website: [banmod.online](https://banmod.online)
- Email: `banmod.giannibart@gmail.com`
- Discord: `GianniBart`
- Telegram: [`@GianniBart`](https://t.me/GianniBart)
- Bugs in the public GPL core: [GitHub Issues](https://github.com/GiannBart/BanMod/issues)

When reporting an issue, include the BanMod version, Among Us version, platform, reproduction steps, and sanitized logs. Do not publish tokens, friend codes, player identifiers, email addresses, private messages, or other personal data.
