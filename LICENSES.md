# BanMod Licensing Overview

This file explains which license applies to each category of material. It is a practical scope map and does not replace the complete license texts or legal advice.

## 1. GPL-licensed core

Unless a file contains a different notice, source code and build scripts contained directly in this repository are intended to be licensed under the **GNU General Public License version 3.0 (GPLv3)**. See [LICENSE](LICENSE).

When conveying GPL-covered source or binaries, redistributors must comply with GPLv3. Important obligations include preserving notices, marking modified versions, licensing covered derivative code under GPLv3, and providing complete corresponding source with distributed binaries.

Service terms, community rules, or branding restrictions must not be presented as restrictions on the rights granted by GPLv3 over GPL-covered code.

## 2. Third-party code

Some files include or derive from third-party projects. Their original copyright and license notices must remain intact.

Permissively licensed portions, including MIT-licensed components where identified, retain their original notices. When included in the BanMod program, the combined distribution must satisfy both those notices and the GPLv3 requirements applicable to the combined GPL-covered work.

Consult source-file headers and `Resources/Credits and License.txt` before copying or changing a component.

## 3. Optional features and remote modules

Optional features and standalone remote components are **not included in this repository**. They may be delivered dynamically by official BanMod servers only after eligibility, compatibility, and security requirements are satisfied.

They are intended to be separately licensed proprietary/private components and are not required for the GPL-licensed core to operate. They are currently provided without payment; “optional” does not mean paid.

No permission is granted through this repository to copy, publish, redistribute, sublicense, extract, mirror, reverse-package, or include these remote components in a fork or third-party service. Access to an official service or temporary delivery of a component does not transfer ownership or grant redistribution rights.

Whether two software components are legally separate can depend on their actual technical design, integration, and communication. The copyright holder should obtain qualified legal advice before changing how remote components are linked, loaded, distributed, or embedded.

## 4. Visual assets, skins, media, and branding

Unless a file explicitly grants another license, the following are **not licensed under GPLv3** and remain all rights reserved:

- BanMod name, logo, visual identity, and branding;
- custom skins, hats, artwork, illustrations, icons, and animations;
- screenshots, promotional images, videos, audio, and website media;
- assets under `Resources/`, `docs/images/`, and related visual/media directories when marked or described as proprietary.

**Copyright © 2026 GianniBart. All rights reserved.**

No permission is granted to modify, sell, sublicense, or redistribute these assets outside the limited permissions required to view the public repository and its documentation. Written permission is required for reuse in another mod, fork, website, package, video branding, or commercial project.

Because GitHub's public-repository functionality permits users to view and create platform-native forks, proprietary assets placed in a public repository can be reproduced within GitHub through that functionality. This does not grant a broader off-platform asset license. To avoid ambiguity, public forks and redistributed builds should remove or replace proprietary BanMod assets unless explicit written permission has been obtained.

If proprietary assets are embedded into an official binary, the copyright holder can distribute that binary because the holder controls the relevant asset rights. A third-party redistributor should remove or replace those assets unless separately authorized.

## 5. Among Us and Innersloth material

Among Us, its names, logos, characters, game assets, and related intellectual property belong to **Innersloth LLC and/or its licensors**. BanMod does not grant rights in Innersloth material.

BanMod is unofficial and is not affiliated with, endorsed by, sponsored by, or approved by Innersloth LLC. Mod releases must comply with the current [Among Us Mod Policy](https://www.innersloth.com/among-us-mod-policy/), retain the required in-game mod identification stamp, and must not distribute the Among Us base game.

## 6. Official services and GPL rights

GPLv3 permits users to run, study, modify, and redistribute GPL-covered code subject to its conditions. It does not require a private operator to provide credentials, branding rights, proprietary remote modules, or unrestricted access to independently operated servers.

BanMod may protect its services against malformed traffic, excessive requests, impersonation, credential theft, abuse, exploits, or incompatible protocols. Any denial of service access should be described as an operational decision affecting private infrastructure—not as cancellation of a user's GPL rights in code they already received.

Forks should remove official credentials and use their own infrastructure or disable server-dependent functions.

## 7. Contributions

Unless a separate written agreement applies, contributions accepted into GPL-covered code are expected to be distributable under GPLv3. Contributors must have the right to submit their work and must preserve compatible third-party notices.

Do not contribute:

- proprietary optional modules or private server code;
- leaked or decompiled game code that cannot lawfully be redistributed;
- copyrighted assets without permission;
- secrets, tokens, activation codes, private endpoints, personal data, or report databases;
- code copied from a project with an incompatible license.

## 8. Recommended repository layout

For clarity, keep:

- `LICENSE` — unmodified full GPLv3 text;
- `LICENSES.md` — this mixed-licensing scope explanation;
- `Resources/Credits and License.txt` — third-party notices and credits;
- file-level headers — for exceptions, third-party code, or proprietary assets;
- proprietary remote modules — outside the public GPL repository;
- secrets and server data — outside version control.

When there is any conflict, the applicable full license text and file-specific notices control over summaries in documentation.
