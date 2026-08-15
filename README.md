> [!WARNING]
> ## Project status update — August 11, 2026
>
> BanMod is currently undergoing a technical and policy review in relation to the [Among Us Mod Policy](https://www.innersloth.com/among-us-mod-policy/). Please read this notice before downloading or using the mod.

### What happened

I was recently contacted by a person who stated that they knew of an exploit affecting BanMod. During the conversation, information about the exploit was accompanied by repeated requests for the source code of FFA, a separate private project that is not included in this repository.

I explained that I would not provide FFA's source code in exchange for information about a vulnerability. Additional concerns were subsequently raised regarding BanMod's licensing, optional components, and general compliance with Innersloth's Mod Policy.

The sequence of these events gave me the personal impression that security information and legal concerns might have been used as leverage to obtain private source code.

I want to make it completely clear that this is only my perception of the conversation. It may be partially or entirely incorrect, and I am not claiming to know or establish the intentions of the person involved.

This notice is not intended as a personal accusation. Its purpose is to explain the circumstances that prompted the current review and to keep BanMod users informed.

The concerns that were raised will not be ignored. Regardless of how the requests were presented, they may identify genuine issues. I am therefore reviewing BanMod's code, licensing information, documentation, and compliance in good faith.

### GPL code and separately distributed optional components

To avoid further misunderstandings, I would like to clarify the following.

The public BanMod core remains available under GPLv3. Additional features—historically called “Premium” even though they are currently provided free of charge—are optional components distributed separately from this repository:

- they are not included in this repository;
- they are not required for the public core to operate or compile;
- they are delivered as separate DLLs through the official services;
- they are subject to the terms described in [LICENSE](LICENSE) and [LICENSES.md](LICENSES.md).

The project's position is that these separately distributed components are independent works and are not part of the GPL-covered code published in this repository.

This position and the boundaries between the different licenses are described in the repository's two licensing documents: [LICENSE](LICENSE), containing GPLv3 for the public core, and [LICENSES.md](LICENSES.md), describing the licenses applicable to the individual components.

In particular, the final paragraph of [section 5 of GPLv3](https://www.gnu.org/licenses/gpl-3.0.html#section5) addresses an “aggregate”: a compilation in which a covered work is placed alongside separate and independent works that are not, by their nature, extensions of the covered work and are not combined with it into a larger program.

Section 5 states that including such independent works in an aggregate does not, by itself, cause GPLv3 to apply to the other parts of the aggregate.

On this basis, the project considers the separately distributed optional DLLs to be outside the GPL license applicable to the public BanMod core.

Nothing in this notice is intended to restrict the rights granted by GPLv3 over code that is actually covered by that license.

The Premium features are independent components under a private license and are not required for the operation or compilation of the mod published in this repository.

### Official servers and modded-lobby registration

After reviewing Innersloth's updated policy and its [official technical documentation](https://github.com/Innersloth-LLC/AmongUsModdingInformation), it appears that mods may use the official Among Us backend.

However, a mod that changes gameplay, custom-role behavior, another player's experience, or other game functionality must identify and register itself when the lobby is created.

BanMod will be updated so that the appropriate mode can be selected during lobby creation:

- features that modify gameplay or affect the experience of other participants will require the lobby to be registered as modded;
- if only visual modifications and anti-cheat features that do not change gameplay or another player's experience are used, it appears possible to continue using the official servers without registering the lobby as modded.

However, Innersloth states that not every edge case involving anti-cheat functionality can be classified in advance and recommends registering the mod when there is uncertainty.

BanMod will provide the tools needed to make the appropriate selection. Users will be responsible for selecting the correct lobby mode and for not using features that are not permitted on the official servers.

### Features under review

The review will include removing or limiting features that could be misused to gain an unfair advantage.

In particular, the following will be removed or restricted:

- automatic task animations;
- teleportation features;
- actions that may improperly alter another player's experience;
- testing or debugging tools that could be used outside a controlled environment.

Host, moderation, and anti-cheat tools will also be reviewed to determine which may be used on official servers and which require the lobby to be registered as modded.

Until the review is complete, BanMod should be used only in private, consensual, or clearly identified modded lobbies.

This is a precaution adopted by the BanMod project and is not presented as a general policy requirement applying to every Among Us mod.

### Reporting problems

Anyone who believes that BanMod violates a specific provision of the Mod Policy is encouraged to provide:

1. the exact provision believed to have been violated;
2. the affected feature or behavior;
3. the technical steps needed to verify or reproduce the issue;
4. any relevant technical information.

Concrete, complete, and responsible reports will be reviewed seriously.

I will not provide the source code of FFA or other private projects as a condition for receiving information about a vulnerability.

At the same time, I will not ignore a technical report simply because I disagree with the way it was presented. If the issue can be verified, I will try to correct it.

### Future of the project

If reasonable compliance cannot be achieved or substantial problems remain, the repository and related services may be suspended or closed.

Thank you to everyone who reports problems, vulnerabilities, or possible violations responsibly, completely, and constructively.

---

<div align="center">

<img src="docs/images/image.png" alt="BanMod banner" width="100%">

# BanMod

**Lobby moderation, anti-abuse tools, host controls, custom roles, and configurable game modes for Among Us.**

[![Code license: GPL-3.0](https://img.shields.io/badge/code-GPL--3.0-blue.svg)](LICENSE)
[![Platform: Windows](https://img.shields.io/badge/platform-Windows-0078D4.svg)](#requirements)
[![Official website](https://img.shields.io/badge/website-banmod.online-7A5CFA.svg)](https://banmod.online)

**English** · [Italiano](README_IT.md)

[Website](https://banmod.online) · [Instructions](https://banmod.online/instructions)

</div>

> [!IMPORTANT]
> ## Please read before using BanMod
>
> Before downloading, using the official BanMod build, connecting to BanMod online services, or opening an issue, please read the following:
>
> - [Important Information & Rules](IMPORTANT_INFO_AND_RULES.md)
> - [Official Rules](https://banmod.online/rules)
> - [Privacy Policy](https://banmod.online/policy/privacy)
> - [Cookie Policy](https://banmod.online/policy/cookies)
>
> BanMod is optional. Use of the official servers, APIs, verification systems, and optional features is not required.
>
> If you do not agree with the rules or privacy conditions governing the official services, you may fork the GPL-licensed source code and use your own backend, replace the official APIs, or use no backend at all.
>
> Questions and criticism are welcome, but please read the documentation above before making claims or opening complaints about data processing, DLL/plugin checks, activation, modified builds, or access to the official infrastructure.

> [!IMPORTANT]
> BanMod is an unofficial community-made modification. Use it only in fair, consensual, or clearly modded lobbies. Host and testing tools must not be used to deceive other players, disrupt public games, or gain unfair advantages.

## Description

BanMod is a Windows mod for **Among Us** based on **BepInEx IL2CPP**. The GPL-licensed core focuses on persistent moderation, anti-abuse protections, host administration, configurable gameplay, custom roles, and supporting interfaces.

The public repository contains only the **core mod**. Some additional features—historically called “Premium” even though they are currently free—are optional components delivered separately by the official BanMod servers after eligibility, compatibility, and security checks. They are not required for the core to operate and are not included in this repository.

## Main features

- **Lobby moderation:** persistent ban and block tools, cheater/suspicious-player lists, prohibited name and word checks, spam protection, AFK management, and player administration menus.
- **Host controls:** autostart, meeting and voting rules, tasks and sabotages, doors, maps, lobby messages, summaries, and configurable player actions.
- **Roles and modes:** custom or modified roles, role configuration, Hide and Seek improvements, testing modes, and additional presets.
- **Client and visual tools:** configurable keys, zoom in permitted states, lobby decorations, dark theme, custom interfaces, outfit/skin menus, and local options.
- **Private-lobby testing tools:** some host/debug features are intended only for controlled testing or games where every participant knows and accepts the rules.
- **Connected services:** optional online verification, reports, server messages, lobby services, anti-abuse systems, and separately distributed optional features.

Features may change between releases. The official website and release notes are the reference for the currently supported build.

## Images

<p align="center">
  <img src="docs/images/main-menu.png" alt="BanMod main menu" width="48%">
  <img src="docs/images/options-menu.png" alt="BanMod options menu" width="48%">
</p>

<p align="center">
  <img src="docs/images/game-settings.png" alt="BanMod game settings" width="82%">
</p>

> **BanMod custom skins** are separate proprietary content. They are not included in the source code or the public repository and are not distributed under the GPL. Documentation images present in the repository follow the license stated in their respective files. See the [Licensing overview](LICENSES.md).

## Requirements

- A legitimate copy of **Among Us** for Windows PC.
- The game version supported by the current BanMod release.
- The correct package for **Steam** or **Epic Games**.
- Permission to extract files into the folder containing `Among Us.exe`.

Game updates may break mod compatibility. Always check the latest available release before installing BanMod or reporting an issue.

## Installation

1. Download the current package from the [official download page](https://banmod.online/downloads).
2. Select the Steam or Epic Games version.
3. Open the main game folder, which is the folder containing `Among Us.exe`.
4. Extract all files from the BanMod ZIP into that folder.
5. Make sure that `Among Us.exe` and the `BepInEx` folder are at the same level.
6. Start Among Us. After BepInEx finishes loading, BanMod should appear in the main menu.

### Finding the game folder

**Steam:** Library → right-click **Among Us** → **Manage** → **Browse local files**.

**Epic Games:** Library → three-dot menu next to **Among Us** → **Manage** → folder icon.

<p align="center">
  <img src="docs/images/install-folder.png" alt="Example Among Us installation folder" width="360">
</p>

### Updating

Back up the `BAN_DATA` folder when instructed by the release notes. Remove obsolete or duplicate BanMod DLLs from `BepInEx/plugins`, then install the new official package. Do not mix files from different releases.

### Uninstalling

Use your platform's file verification feature:

- **Steam:** Properties → Installed Files → Verify integrity of game files.
- **Epic Games:** Manage → Verify.

Before verification, save any BanMod presets or personal configuration files you want to keep.

## Default controls

- `Delete`: opens the main BanMod menu.
- `F10`: opens the keybind configuration menu.

Available keys and menus may change depending on the release or host permissions. Check the in-game help and the [official instructions](https://banmod.online/instructions).

## Official services and optional features

The GPL core and the online services operated by BanMod are separate layers:

- The core source code may be studied, modified, and redistributed under GPLv3.
- APIs, verification, tokens, reports, lobbies, server messages, and other official services are operated by the BanMod project and are subject to their applicable service and privacy rules.
- Remote optional features are **not present in the repository**. They may be downloaded at runtime only after the official server verifies compliance with all applicable requirements.
- Remote optional features are proprietary components distributed under a separate private license. This repository does not grant permission to copy, republish, redistribute, sublicense, extract them into another project, or include them in a fork.
- Optional features are currently provided without payment, but availability is not guaranteed and access may be changed, suspended, or revoked.
- A modified or self-compiled client is not an official BanMod release and is generally not eligible for official optional features.

Service rules do not remove the rights granted by GPLv3 over GPL-covered code.

## Fair-use and service rules

When using an official build or BanMod services:

1. Do not use cheat menus, malicious clients, exploits, unlockers, request manipulation, API abuse, spam, false reports, bypass tools, or systems intended to harm the game, the project, or other users.
2. Other legitimate mods are not automatically prohibited, but compatibility or security checks may restrict connected features. Contact the administrator before development testing or unusual multi-mod configurations.
3. Do not send automated, malformed, excessive, or unauthorized requests to official BanMod endpoints.
4. Do not reuse tokens, credentials, build identifiers, or private official-endpoint details in forks.
5. Technical restrictions may be applied to clients, tokens, mod IDs, friend codes, player IDs, accounts, IP addresses, or other identifiers linked to abuse or incompatibility.
6. User reports are items to be verified and must not be treated as evidence without reasonable checks.
7. Follow the Among Us Terms of Use and Mod Policy, community rules, applicable law, and the consent of other players.

Before enabling connected features, read the updated **Important Rules** and **Privacy Policy** on the [official policies page](https://banmod.online/policies).

## Forks and modified builds

You may fork and modify GPL-covered code. A compliant public fork should:

- preserve GPLv3, copyright, attribution, and warranty-disclaimer notices;
- clearly identify that the project has been modified and state the modification date;
- provide complete corresponding source for every distributed binary;
- distribute GPL-covered derivative code under GPLv3 without adding further restrictions;
- identify itself as unofficial and not imply endorsement by GianniBart, BanMod, Among Us, or Innersloth;
- not include, extract, or redistribute proprietary BanMod custom skins, which are not part of the public repository, unless separately authorized in writing;
- disable or replace integrations with official BanMod APIs and not use official infrastructure without authorization;
- never include private server modules, remote optional components, keys, tokens, personal data, reports, or game files;
- support the fork independently and not redirect fork-specific issues to official BanMod support channels.

A fork can remain fully GPL-compliant by using its own backend or no backend. Eligibility for official services is a separate operational decision and does not restrict the right to modify the code.

## Building from source

The project uses **.NET 6** and BepInEx IL2CPP packages.

```bash
git clone https://github.com/GiannBart/BanMod.git
cd BanMod
dotnet restore
dotnet build -c Release
```

Before building, review `BanMod.csproj`:

- replace or remove developer-specific absolute Windows paths;
- update IL2CPP assembly and metadata paths using your legitimate game installation;
- change or remove the local post-build copy target;
- do not publish `BanMod.BuildCode.txt`, generated secret sources, API credentials, or local configuration files;
- do not publish or redistribute Among Us binaries, `Among Us_Data`, `GameAssembly.dll`, or other game files.

The DLL is normally produced in `bin/Release/net6.0/`. Self-compiled builds are unofficial and may not connect to official BanMod services.

## Releases and changes

- Official release notes and compatibility notices are published on [banmod.online](https://banmod.online).
- Version numbers in the repository or project file may represent ongoing development and may be newer than the latest public release. Only a package explicitly published as official should be treated as a release.

## Contributing

Issues and pull requests for the GPL core are welcome when they respect the law, people, licenses, and the project's goals.

By submitting code, you confirm that you have the right to contribute it and that the contribution may be distributed under GPLv3 unless a different written agreement applies. Do not submit proprietary optional modules, proprietary BanMod custom skins, unlawfully obtained game code, secret endpoints, credentials, personal data, or code copied without a compatible license and attribution.

## Credits

BanMod contains original work and portions inspired by or derived from open-source community projects. Preserve all notices contained in source files and in `Resources/Credits and License.txt`.

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

## Licensing overview

- **Core source code:** GNU General Public License v3.0, except for files carrying a different compatible notice.
- **Third-party code:** remains subject to its original notices and license obligations.
- **Optional server-delivered components:** separate proprietary/private license; not included in the repository and not covered by the repository's GPL grant.
- **BanMod custom skins:** © 2026 GianniBart. All rights reserved. They are separate content and are not included in the source code or public repository.
- **Among Us intellectual property:** belongs to Innersloth LLC and/or its licensors.

See [LICENSE](LICENSE) for GPLv3 and [LICENSES.md](LICENSES.md) for component-level details. This summary is informational and does not replace the applicable license texts.

## Non-affiliation and legal notice

BanMod is an unofficial fan-made modification for Among Us. It is not affiliated with, endorsed by, sponsored by, or otherwise authorized by Innersloth LLC. Among Us, related names, logos, characters, and materials belong to Innersloth LLC or their respective owners.

The mod must retain the in-game identification stamp required by the [Among Us Mod Policy](https://www.innersloth.com/among-us-mod-policy/). BanMod releases must not contain the Among Us base game or unauthorized copies of game files.

The software is provided **as is**, without warranties. To the maximum extent permitted by law, the authors are not responsible for bans, account restrictions, data loss, incompatibility, crashes, service interruptions, or damages caused by misuse, unsupported configurations, modified builds, or violations of third-party rules.

## Support

- Website: [banmod.online](https://banmod.online)
- Email: `banmod.giannibart@gmail.com`
- Bugs in the public GPL core: [GitHub Issues](https://github.com/GiannBart/BanMod/issues)

When reporting an issue, include the BanMod version, Among Us version, platform, reproduction steps, and sanitized logs. Do not publish tokens, friend codes, player identifiers, email addresses, private messages, or other personal data.
