<div align="center">
  <h1><strong>IgnoreMessages</strong></h1>
  <p>Suppress unwanted CS2 HUD text — drop engine TextMsg / HintText notices by localization key.</p>
</div>

<p align="center">
  <img src="https://img.shields.io/github/license/yappershq/cs2-ignore-messages" alt="License">
  <img src="https://img.shields.io/github/stars/yappershq/cs2-ignore-messages?style=flat&logo=github" alt="Stars">
</p>

---

A ModSharp port of [itsAudioo/IgnoreMessages](https://github.com/itsAudioo/IgnoreMessages) (originally for Swiftly). It hooks the `UM_TextMsg` and `CS_UM_HintText` net messages server-side and drops any message whose localization key matches a configurable list — handy for hiding default round-status notices, pause messages, or any other engine-generated HUD string you don't want players to see.

## 🚀 Install

Copy the build output into your ModSharp install (`<sharp>` = your `sharp` directory):

| From | To |
|------|----|
| `.build/modules/IgnoreMessages/` | `<sharp>/modules/IgnoreMessages/` |
| `.build/locales/ignoremessages.json` | `<sharp>/locales/ignoremessages.json` |

Restart the server (or change map) to load. Requires the LocalizerManager module (ships with ModSharp).

## ⚙️ Configuration

Configured entirely via ConVars (set in your server config or live in console):

| ConVar | Default | Meaning |
|--------|---------|---------|
| `ignoremessages_keys` | `""` | Semicolon-separated list of localization keys (`#...`) to suppress, e.g. `#SFUI_Notice_Match_Will_Pause;#SFUI_Notice_Match_Paused`. |
| `ignoremessages_print_keys` | `false` | Log every localization key that passes through the hook to the server console — use it to discover which keys to block. |

The key list is re-read whenever it changes, so you can edit it live without reloading the plugin.

## 🔧 How it works

CS2 delivers HUD text through two net messages: `UM_TextMsg` (general messages, carrying one or more `param` localization keys) and `CS_UM_HintText` (the hint bar, a single `message` key). The plugin registers both via `HookNetMessage`, inspects each key (tokens starting with `#`), and returns `SkipCallReturnOverride` to suppress the send when a key is on the ignore list — blocking it for every receiver before it reaches the network. Keys are matched case-insensitively against a cached set that is rebuilt only when the ConVar changes.

To discover keys to block: set `ignoremessages_print_keys true`, trigger the message in-game, copy the logged key into `ignoremessages_keys`, then turn logging back off.

## 📦 Build

```bash
dotnet build IgnoreMessages.slnx -c Release
```

Outputs `.build/modules/IgnoreMessages/IgnoreMessages.dll` and `.build/locales/ignoremessages.json`.

## 🙏 Credits

Port of [itsAudioo/IgnoreMessages](https://github.com/itsAudioo/IgnoreMessages) by **itsAudioo** (written for the Swiftly CS2 framework). ModSharp port by [yappershq](https://github.com/yappershq).

---

<div align="center">
  <p>Made with ❤️ by <a href="https://github.com/yappershq">yappershq</a></p>
  <p>⭐ Star this repo if you find it useful!</p>
</div>
