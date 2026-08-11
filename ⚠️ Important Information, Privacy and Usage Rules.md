# ⚠️ Important Information, Privacy and Usage Rules

> **Before using BanMod, its online services, opening a report, or making a complaint, please read this page and the official documentation linked below.**

BanMod is an open-source project for **Among Us** with optional online services managed through the official BanMod infrastructure.

This document is only a **summary** of the most important points.

For complete and up-to-date information, always refer to the official pages:

- **Rules:** https://banmod.online/rules
- **Privacy Policy:** https://banmod.online/policy/privacy
- **Cookie Policy:** https://banmod.online/policy/cookies
- **Official website:** https://banmod.online
- **Official repository:** https://github.com/GiannBart/BanMod

If there are any differences between this summary and the official pages, the latest versions published on **banmod.online** and the licenses applicable to the repository shall prevail.

---

## 1. BanMod is optional

Nobody is required to use BanMod.

Nobody is required to use:

- the official build;
- BanMod servers;
- the official APIs;
- online services;
- verification systems;
- optional features.

If you do not agree with the rules or with how the official services operate, you can simply **choose not to use those services**.

The public source code covered by the **GNU GPL v3.0** remains available in the repository and may be studied, modified, compiled, and redistributed in accordance with the applicable license.

You may create your own fork and use:

- your own backend;
- your own APIs;
- or no backend at all.

You are not required to use the official BanMod infrastructure.

---

## 2. Open-source code and official services are separate

It is important to distinguish between two different things.

### Open-source core

The public BanMod core is distributed under the **GNU GPL v3.0**, except for any files or components that explicitly state a different license.

The rights granted by the GPL over GPL-covered code are not removed by the rules governing BanMod services.

### Official services

Servers, APIs, activation and verification systems, tokens, reports, lobby services, server messages, anti-abuse systems, and optional components distributed separately are services operated through the official BanMod infrastructure.

Having the right to modify GPL-licensed code **does not automatically give anyone the right to use BanMod's private servers with any modified client**.

---

## 3. Forks and modified builds

Modifying and creating forks of GPL-covered code is allowed in accordance with the license.

A modified build must be clearly identified as unofficial.

A modified or self-compiled DLL must also **disable or replace integrations with the official BanMod services**, unless specific authorization has been granted.

A developer may therefore:

- remove BanMod API calls;
- replace them with their own APIs;
- use their own server;
- completely remove server-dependent functionality.

A modified build must not continue sending requests to the official endpoints while pretending to be an authorized BanMod build.

Restrictions applied by the official servers concern only **access to BanMod's private infrastructure** and do not prevent anyone from modifying GPL-covered code.

---

## 4. Protection of official services

Official BanMod services use technical security, verification, compatibility, and abuse-prevention systems.

The official infrastructure must not be used for:

- cheat menus;
- malicious clients;
- exploits;
- bypasses;
- manipulated requests;
- API abuse;
- spam;
- false reports;
- unauthorized automated or excessive requests;
- attempts to impersonate an official build;
- activities intended to damage the project, the server, or other users.

Requests coming from unauthorized or modified clients may be refused or blocked.

Depending on the situation, security measures may involve technical identifiers such as:

- friend code;
- IP address;
- Mod ID;
- tokens;
- client identifiers;
- player ID;
- other technical identifiers associated with the request.

A server-side block may remain active even after reinstalling the official build when necessary to protect the service.

For the complete rules:

https://banmod.online/rules

---

## 5. Cheat menus and other mods

The official BanMod build is not designed to be used together with cheat menus intended to gain unfair advantages, use exploits, or bypass security systems.

This does not mean that every other mod is automatically forbidden.

Legitimate mods may be used when compatible, but some configurations may not satisfy the requirements needed to access certain official services.

These checks primarily exist to protect:

- APIs;
- online services;
- activation systems;
- reports;
- optional features;
- BanMod infrastructure;
- other users.

---

## 6. What technical information may be processed

BanMod uses online services for some of its functionality.

Depending on the features being used, technical information may include:

- friend code;
- player name;
- Mod ID;
- API token;
- BanMod version;
- language and region;
- player ID;
- platform;
- client status information;
- public IP address;
- lobby information;
- data submitted through reports;
- technical logs when the report system is used;
- technical information relating to installed plugins;
- information required to verify activation and the technical identity of an installation.

This list is only a summary.

To know **exactly which categories of data are processed, why they are processed, and how long they may be retained**, please read the complete Privacy Policy:

https://banmod.online/policy/privacy

---

## 7. IP address

When BanMod communicates with the official APIs, the server receives the IP address of the connection.

The IP address may be used for:

- communication with the server;
- activation;
- access control;
- security;
- abuse prevention;
- enforcement of server-side restrictions.

It is not used for advertising or commercial profiling.

---

## 8. BepInEx plugins

During certain verification procedures, BanMod may inspect technical information relating to DLL files located in:

`BepInEx/plugins`

This information may include:

- plugin/mod name;
- file name;
- assembly information;
- version;
- SHA-256 hash.

### The full contents of the DLL files are not uploaded to the server.

This information is used for technical checks, compatibility, security, and prevention of use of the official services together with cheats, exploits, or unauthorized configurations.

For more information:

https://banmod.online/policy/privacy

---

## 9. Cryptographic installation identity

Supported BanMod versions may use a per-installation cryptographic identity to protect the activation system and make it more difficult to clone a BanMod identity onto another computer.

The private key is created and stored locally through Windows cryptographic services.

### The private key is never sent to the BanMod server.

The server may receive the public information required to cryptographically verify the installation, such as:

- public key;
- KeyId;
- cryptographic provider information;
- activation signature;
- technical information associated with activation.

This system is not a traditional hardware scan and is not used to collect CPU serial numbers, motherboard serial numbers, disk serial numbers, MAC addresses, or similar hardware identifiers.

Its purpose is to verify the technical identity of the installation and protect the official services.

For complete details regarding data processing:

https://banmod.online/policy/privacy

---

## 10. Reports and logs

When the mod's report system is used, technical BepInEx logs may be sent automatically when needed to analyze:

- bugs;
- crashes;
- incompatibilities;
- errors;
- technical issues.

Logs may contain technical information generated by the game, BanMod, BepInEx, or other installed mods.

Do not include the following in reports:

- passwords;
- private tokens;
- sensitive data;
- unnecessary personal information.

Further information is available in the Privacy Policy.

---

## 11. Use of data

Data processed by BanMod **is not sold** and is not used for advertising or commercial profiling.

Technical data is used for purposes necessary for operation, security, moderation, activation, and abuse prevention.

Technical providers required for infrastructure operation or administrative channels may be involved in the circumstances described in the Privacy Policy.

For this reason, the official Privacy Policy remains the complete reference regarding:

- processing;
- retention;
- recipients;
- security;
- external services;
- user rights.

https://banmod.online/policy/privacy

---

## 12. Optional / "Premium" features

Some additional features have historically been referred to as **Premium**, but they are currently provided free of charge.

They are:

- optional;
- separate from the GPL core;
- not required to use the main DLL;
- distributed separately through the official services;
- subject to compatibility, integrity, and security checks.

Users can choose which optional features they want to enable.

When enabled and when the applicable requirements are met, the related components may be downloaded automatically through the official system.

These features are also intended as an incentive to use the official build in compatible configurations and without cheat menus that violate the rules governing BanMod services.

### You do not need these features in order to use the open-source core.

Their availability does not restrict the right to modify or redistribute GPL-covered code.

---

## 13. If you do not accept these conditions

The solution is simple.

**You are not required to use the official BanMod services.**

You can download the source code:

https://github.com/GiannBart/BanMod

and create a GPL-compliant fork using:

- your own backend;
- your own APIs;
- your own activation system;
- or no online services at all.

This possibility is intentional.

BanMod protects its own backend and services, but does not prevent developers from exercising the rights granted by the GPL over the open-source code.

---

## 14. Before making a complaint

Questions and complaints have already been raised in the past regarding, among other things:

- IP addresses;
- BepInEx plugins;
- DLL checks;
- activation systems;
- access to official servers;
- modified builds;
- cheat menus;
- optional features;
- the relationship between the GPL and private services.

Questions, criticism, and reports are completely legitimate.

However, before claiming that BanMod hides information, prevents modification of the code, or collects data without informing users, **please read the official documentation first**.

At minimum, review:

1. https://banmod.online/rules
2. https://banmod.online/policy/privacy
3. https://banmod.online/policy/cookies
4. https://github.com/GiannBart/BanMod
5. `LICENSE`
6. `LICENSES.md`

If something is still unclear after reading the documentation, you are welcome to ask for clarification or request additional documentation.

Precise reports based on the actual behavior of the project help improve both BanMod and its documentation.

---

## 15. No obligation and no automatic right to use the servers

In summary:

- **BanMod is optional.**
- **The public core is open source under GPLv3.**
- **You may modify GPL-covered code.**
- **You may create a fork.**
- **You may use your own backend.**
- **You may remove server-side functionality entirely.**
- **You are not required to use BanMod APIs.**
- **You are not required to use optional features.**
- **BanMod servers remain private infrastructure and may enforce access-control and security measures.**
- **Modified builds must not use the official endpoints without authorization.**
- **Data is not sold or used for advertising or commercial profiling.**
- **The private key associated with the cryptographic installation identity is never sent to the server.**
- **The full contents of detected BepInEx DLL files are not uploaded to the server.**

If you do not accept the conditions governing the official services, simply use the GPL source code with your own backend or without a backend.

---

## 16. Among Us and Innersloth

BanMod is an unofficial community-created modification.

BanMod is not affiliated with, sponsored by, endorsed by, or developed by Innersloth.

Users must use BanMod responsibly and comply with:

- the Among Us Terms of Use;
- the Among Us Mod Policy;
- lobby and community rules;
- applicable licenses;
- applicable laws;
- other players.

BanMod must not be used to ruin other players' experience, gain unfair advantages, or disrupt public lobbies.

---

# Official documentation

For any questions, always refer to the latest official documentation.

**Rules**  
https://banmod.online/rules

**Privacy Policy**  
https://banmod.online/policy/privacy

**Cookie Policy**  
https://banmod.online/policy/cookies

**Official website**  
https://banmod.online

**GitHub**  
https://github.com/GiannBart/BanMod

---

> **This file is an informational summary. The Rules, Privacy Policy, Cookie Policy, applicable licenses, and the latest official documentation remain the complete references for the use of BanMod and its services.**