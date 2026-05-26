using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace IgnoreMessages.Configuration;

internal interface IIgnoreMessagesConfig
{
    /// <summary>When true, logs every localization key that passes through the hook to server console.</summary>
    bool PrintKeyNames { get; }

    /// <summary>Semicolon-separated list of localization keys to block (e.g. #SFUI_Notice_Match_Will_Pause).</summary>
    string IgnoredKeys { get; }
}

internal sealed class IgnoreMessagesConfig : IIgnoreMessagesConfig
{
    private readonly IConVar? _cvPrintKeyNames;
    private readonly IConVar? _cvIgnoredKeys;

    public IgnoreMessagesConfig(InterfaceBridge bridge)
    {
        var cv = bridge.ConVarManager;

        _cvPrintKeyNames = cv.CreateConVar(
            "ignoremessages_print_keys",
            false,
            "Print every localization key that passes through the TextMsg/HintText hook to server console. Useful for finding keys to block.");

        _cvIgnoredKeys = cv.CreateConVar(
            "ignoremessages_keys",
            "",
            "Semicolon-separated list of localization keys (#...) to suppress from TextMsg and HintText net messages. Example: #SFUI_Notice_Match_Will_Pause;#SFUI_Notice_Match_Paused");
    }

    public bool   PrintKeyNames => _cvPrintKeyNames?.GetBool()   ?? false;
    public string IgnoredKeys   => _cvIgnoredKeys?.GetString()   ?? "";
}
