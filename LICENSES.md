# BanMod Licensing Overview

This file explains which license applies to each category of BanMod material. It is a practical scope summary and does not replace the full license texts, file-specific notices, or professional legal advice.

## 1. GPL-licensed core

Unless a file contains a different compatible notice, the source code, documentation, and build scripts included directly in this public repository are distributed under the **GNU General Public License version 3.0 (GPLv3)**. See [LICENSE](LICENSE).

When distributing GPL-covered source code or binaries, redistributors must comply with GPLv3. This includes preserving applicable notices, identifying modified versions, licensing covered derivative code under GPLv3, and providing the complete corresponding source when distributing binaries.

Service rules, access requirements, private-component terms, and anti-abuse measures do not remove or reduce the rights granted by GPLv3 over GPL-covered material already received.

## 2. Third-party code

Some files include, adapt, or derive from third-party open-source projects. Their original copyright notices, attribution requirements, and license terms remain applicable.

Permissively licensed portions, including MIT-licensed components where identified, retain their original notices. A combined BanMod distribution must satisfy both those notices and the GPLv3 requirements applicable to the GPL-covered work.

Review source-file headers and `Resources/Credits and License.txt` before copying, modifying, or redistributing a component.

## 3. Optional features and remote components

Optional features and remote components are **not included in this repository or its source code**. They may be downloaded at runtime from official BanMod servers only after the server confirms that all applicable eligibility, compatibility, integrity, and security requirements have been satisfied.

These optional components are distributed separately under a **private/proprietary license**. They are not required for the GPL-licensed core to operate, and their separate delivery does not place them under the GPL grant covering this repository.

No permission is granted through this repository to copy, republish, mirror, redistribute, sublicense, extract, reverse-package, embed, or include these optional components in a fork, third-party build, or separate service. Access to an official server or temporary delivery of a component does not transfer ownership or grant redistribution rights.

Optional features may currently be provided without payment, but their availability is not guaranteed. Access may be changed, suspended, or revoked according to the applicable service rules. A modified or self-compiled client is not an official BanMod release and may be ineligible for these components.

Nothing in this section limits the rights granted by GPLv3 over the GPL-covered core code.

## 4. BanMod custom skins

Only **BanMod custom skins** are treated as separate BanMod-owned copyrighted assets under this licensing overview.

**Copyright © 2026 GianniBart. All rights reserved.**

The custom skins are not included in the public repository or its source code. They may be supplied separately through official BanMod services when the applicable requirements are satisfied.

No permission is granted to copy, extract, modify, publish, sell, sublicense, redistribute, mirror, bundle, or include the custom skins in a fork, mod pack, website, third-party build, or other project without separate written authorization from the copyright holder.

The BanMod logo, repository banner, documentation screenshots, documentation text, and other project files are not classified as proprietary BanMod assets by this section. Their applicable license is determined by the repository license or by any specific notice contained in the relevant file.

## 5. Among Us and Innersloth material

Among Us, its names, logos, characters, game assets, and related intellectual property belong to **Innersloth LLC and/or its licensors**. BanMod does not grant rights in Innersloth material.

BanMod is unofficial and is not affiliated with, endorsed by, sponsored by, or approved by Innersloth LLC. Mod releases must comply with the current [Among Us Mod Policy](https://www.innersloth.com/among-us-mod-policy/), retain the required in-game mod identification stamp, and must not distribute the Among Us base game or unauthorized game files.

## 6. Official services and GPL rights

GPLv3 permits users to run, study, modify, and redistribute GPL-covered code subject to its conditions. It does not require a private service operator to provide credentials, tokens, access to private infrastructure, proprietary optional components, or unrestricted server access.

BanMod may protect its official services against malformed or excessive traffic, impersonation, credential theft, spam, exploits, protocol abuse, incompatible clients, or other harmful activity. Any service restriction is an operational decision concerning private infrastructure; it is not a cancellation of GPL rights over code already received under GPLv3.

Forks should remove official credentials and private endpoint details, then use their own infrastructure or disable server-dependent functions.

## 7. Forks and redistributed builds

A fork or redistributed build of the GPL-covered core should:

- preserve the GPLv3 license, applicable copyright notices, credits, and warranty disclaimers;
- clearly identify modifications and the date of those modifications;
- provide complete corresponding source for distributed binaries;
- license GPL-covered derivative code under GPLv3 without additional restrictions;
- identify itself as unofficial and avoid suggesting endorsement by GianniBart, BanMod, Among Us, or Innersloth;
- not include, extract, or redistribute proprietary BanMod custom skins without separate written authorization;
- not include private server modules, optional remote components, credentials, tokens, personal data, reports, or game files;
- replace or disable official BanMod service integrations unless use of the official infrastructure has been separately authorized.

A fork can remain GPL-compliant by using its own backend or no backend. Eligibility for official BanMod services is a separate operational matter.

## 8. Contributions

Unless a separate written agreement applies, contributions accepted into GPL-covered code are expected to be distributable under GPLv3. Contributors must have the right to submit their work and must preserve compatible third-party notices.

Do not contribute:

- proprietary optional components or private server code;
- proprietary BanMod custom skins;
- leaked, unlawfully obtained, or non-redistributable game code;
- secrets, tokens, activation codes, private endpoint details, personal data, or report databases;
- code or assets copied from a project without a compatible license and the required attribution.

## 9. Recommended repository layout

For clarity, keep:

- `LICENSE` — the unmodified full GPLv3 license text;
- `LICENSES.md` — this mixed-licensing scope explanation;
- `README.md` — the primary English documentation;
- `README_IT.md` — the Italian documentation;
- `Resources/Credits and License.txt` — third-party notices and credits;
- file-level headers — for exceptions and third-party material;
- optional proprietary components and custom skins — outside the public GPL repository;
- secrets, credentials, server data, and personal data — outside version control.

If this summary conflicts with the full GPLv3 text, a valid third-party license, or a file-specific notice, the applicable full license text or specific notice controls.
