# cs2-ignore-messages

CS2 ModSharp port of [IgnoreMessages](https://github.com/itsAudioo/IgnoreMessages) by **itsAudioo**.

Suppresses HUD text messages (TextMsg / HintText net messages) whose localization keys match a server-configured list. Useful for removing default CS2 round-status notices, pause notices, or any other engine-generated HUD string you don't want players to see.

## How it works

CS2 sends HUD text to clients via two net message types:

- **UM_TextMsg** — general text messages, may carry multiple localization-key params.
- **CS_UM_HintText** — hint-bar messages (yellow text above crosshair).

This plugin hooks both message types before they are sent and drops any message whose localization key (strings starting with `#`) appears in the `ignoremessages_keys` ConVar list.

## ConVars

| ConVar | Default | Description |
|---|---|---|
| `ignoremessages_keys` | `""` | Semicolon-separated list of localization keys to block. Example: `#SFUI_Notice_Match_Will_Pause;#SFUI_Notice_Match_Paused` |
| `ignoremessages_print_keys` | `false` | Print every observed localization key to the server console. Enable this to discover keys to add to `ignoremessages_keys`. |

## Usage

1. Load the plugin on your ModSharp server.
2. Enable key logging to discover what keys pass through:
   ```
   ignoremessages_print_keys true
   ```
3. Trigger the messages you want to block, note the keys logged to console.
4. Add those keys to the block list:
   ```
   ignoremessages_keys "#SFUI_Notice_Match_Will_Pause;#SFUI_Notice_Match_Paused"
   ```
5. Disable key logging when done:
   ```
   ignoremessages_print_keys false
   ```

The ConVar is read on every message, so you can change the list live without reloading.

## Building

```bash
unset version && dotnet build IgnoreMessages.slnx -c Release
```

Output: `.build/modules/IgnoreMessages/IgnoreMessages.dll`

## Credits

Original plugin by [itsAudioo](https://github.com/itsAudioo/IgnoreMessages) — written for the Swiftly CS2 plugin framework.  
ModSharp port by [yappershq](https://github.com/yappershq).
