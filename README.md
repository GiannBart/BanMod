# BanMod
An Among Us mod that prevents disruptive players from rejoining and adds several features, including a discreet anti-cheat system, new roles, and new game modes.

📢 BanMod: Project Update & Requirements
We are re-introducing BanMod! The repository and its primary codebase will soon be updated with significant structural changes.

Please read the following guidelines and requirements carefully regarding how the mod operates, its interaction with server APIs, and anti-cheat policies.

🔒 Requirements & Operational Rules
BanMod relies on server-side verification to ensure fair play and system stability. To use BanMod, the following conditions must be met:

1. Fair Play & Allowed Mods
No Cheat Mods: BanMod will not function if cheat mods or unauthorized modifications are detected on your client.

Whitelisting Safe Mods: If you are using legitimate/honest mods alongside BanMod, contact the administrator. After a quick manual verification, your setup can be enabled via our server APIs.

2. Official Unmodified Release Required
The mod requires the latest official and unmodified .dll build to function fully.

Modified .dll Files: While modified binaries may technically bypass local checks due to code modifications, all optional / extended features will be automatically disabled.

3. API Integrity & Permanent IP Blacklisting
⚠️ IMPORTANT WARNING FOR DEVELOPERS & FORKERS:
If a modified version of the mod continues to make API calls to our official servers without authorization, our security systems will automatically trigger a permanent IP Block.

Once blacklisted, your IP address will be permanently banned from accessing server APIs, even if you later switch back to an official, clean .dll.

💡 Project Philosophy & Optional ("Premium") Features
100% Free Forever: This project is not built for fame, popularity, or profit. BanMod is and will always remain completely free.

"Premium" / Optional Features: For simplicity, additional independent features are referred to as Premium, but they do NOT cost money. They are strictly optional features served dynamically via API once all eligibility requirements are met.
