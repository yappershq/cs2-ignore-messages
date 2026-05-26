using System.IO;
using Microsoft.Extensions.Logging;
using Sharp.Modules.LocalizerManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace IgnoreMessages;

internal sealed class InterfaceBridge
{
    internal static InterfaceBridge Instance { get; private set; } = null!;

    // === Paths ===
    internal string SharpPath { get; }
    internal string DllPath   { get; }

    // === Managers ===
    internal IConVarManager      ConVarManager      { get; }
    internal IClientManager      ClientManager      { get; }
    internal IHookManager        HookManager        { get; }
    internal IModSharp           ModSharp           { get; }
    internal ILoggerFactory      LoggerFactory      { get; }
    internal ISharpModuleManager SharpModuleManager { get; }

    // === Optional modules (resolved in OnAllModulesLoaded) ===
    internal ILocalizerManager? LocalizerManager { get; private set; }

    public InterfaceBridge(
        string         dllPath,
        string         sharpPath,
        ISharedSystem  sharedSystem,
        ILoggerFactory loggerFactory)
    {
        Instance = this;

        SharpPath = sharpPath;
        DllPath   = dllPath;

        ConVarManager      = sharedSystem.GetConVarManager();
        ClientManager      = sharedSystem.GetClientManager();
        HookManager        = sharedSystem.GetHookManager();
        ModSharp           = sharedSystem.GetModSharp();
        LoggerFactory      = loggerFactory;
        SharpModuleManager = sharedSystem.GetSharpModuleManager();
    }

    internal void InitLocalizer()
    {
        var iface = SharpModuleManager
            .GetOptionalSharpModuleInterface<ILocalizerManager>(ILocalizerManager.Identity);
        if (iface?.Instance is not { } lm)
            return;

        LocalizerManager = lm;

        try
        {
            lm.LoadLocaleFile("ignoremessages", suppressDuplicationWarnings: true);
        }
        catch (FileNotFoundException)
        {
            // Locale file missing — plugin still works, just no translated strings
        }
    }

    /// <summary>
    /// Localize a string for a specific client. Falls back to key if localizer unavailable.
    /// </summary>
    internal string LocalizeFor(IGameClient client, string key, params object?[] args)
    {
        var lm = LocalizerManager;
        if (lm is null)
            return key;

        try
        {
            return lm.For(client).Text(key, args);
        }
        catch
        {
            return key;
        }
    }
}
