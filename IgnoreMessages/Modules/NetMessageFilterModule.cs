using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using IgnoreMessages.Configuration;
using Sharp.Shared.Enums;
using Sharp.Shared.HookParams;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

namespace IgnoreMessages.Modules;

/// <summary>
/// Core filter module.
///
/// Hooks <c>UM_TextMsg</c> and <c>CS_UM_HintText</c> net messages before they are sent to
/// clients and suppresses any message whose localization key (first param / message field) is
/// present in the <c>ignoremessages_keys</c> ConVar.
///
/// Key detection mirrors the original Swiftly/IgnoreMessages plugin by itsAudioo:
///   - TextMsg:  check all "param" entries for a string that starts with '#'.
///   - HintText: check the "message" field for a string that starts with '#'.
///
/// The hook action <c>EHookAction.SkipCallReturnOverride</c> prevents the net message from
/// being delivered to <i>any</i> receiver, matching the original plugin's behaviour of
/// server-side suppression before network send.
/// </summary>
internal sealed class NetMessageFilterModule : IModule
{
    private readonly InterfaceBridge        _bridge;
    private readonly ILogger                _logger;
    private readonly IIgnoreMessagesConfig  _config;

    // Hook delegate kept alive to avoid GC collection while installed
    private Func<IPostEventAbstractHookParams, HookReturnValue<NetworkReceiver>, HookReturnValue<NetworkReceiver>>? _netMsgHook;

    // Cached ignored-key set, rebuilt whenever the ConVar string changes
    private string           _lastRawKeys = "";
    private HashSet<string>  _ignoredKeys = new(StringComparer.OrdinalIgnoreCase);

    public NetMessageFilterModule(
        InterfaceBridge        bridge,
        ILogger<NetMessageFilterModule> logger,
        IIgnoreMessagesConfig  config)
    {
        _bridge = bridge;
        _logger = logger;
        _config = config;
    }

    // ===== IModule =====

    public bool Init()
    {
        // Register TextMsg and HintText for hooking — ModSharp only fires PostEventAbstract
        // for message types that have been explicitly registered with HookNetMessage.
        _bridge.ModSharp.HookNetMessage(ProtobufNetMessageType.UM_TextMsg);
        _bridge.ModSharp.HookNetMessage(ProtobufNetMessageType.CS_UM_HintText);

        _netMsgHook = OnNetMessagePre;
        _bridge.HookManager.PostEventAbstract.InstallHookPre(_netMsgHook);

        _logger.LogInformation("[IgnoreMessages] Net message filter installed (UM_TextMsg + CS_UM_HintText)");
        return true;
    }

    public void Shutdown()
    {
        if (_netMsgHook is not null)
        {
            _bridge.HookManager.PostEventAbstract.RemoveHookPre(_netMsgHook);
            _netMsgHook = null;
        }
    }

    // ===== Hook handler =====

    private HookReturnValue<NetworkReceiver> OnNetMessagePre(
        IPostEventAbstractHookParams param,
        HookReturnValue<NetworkReceiver> returnValue)
    {
        var msgId = param.MsgId;

        if (msgId == ProtobufNetMessageType.UM_TextMsg)
            return HandleTextMsg(param, returnValue);

        if (msgId == ProtobufNetMessageType.CS_UM_HintText)
            return HandleHintText(param, returnValue);

        return new HookReturnValue<NetworkReceiver>(EHookAction.Ignored);
    }

    /// <summary>
    /// TextMsg carries a repeated "param" field. Each entry may be a localization key.
    /// We check every entry and suppress the message if any key is on the ignore list.
    /// </summary>
    private HookReturnValue<NetworkReceiver> HandleTextMsg(
        IPostEventAbstractHookParams param,
        HookReturnValue<NetworkReceiver> returnValue)
    {
        try
        {
            var data  = param.Data;
            var count = data.GetRepeatedFieldCount("param");

            for (var i = 0; i < count; i++)
            {
                var value = data.ReadString("param", i);
                if (string.IsNullOrEmpty(value))
                    continue;

                var result = EvaluateKey(value);

                if (result == FilterResult.Block)
                    return new HookReturnValue<NetworkReceiver>(EHookAction.SkipCallReturnOverride);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[IgnoreMessages] Error reading UM_TextMsg params");
        }

        return new HookReturnValue<NetworkReceiver>(EHookAction.Ignored);
    }

    /// <summary>
    /// HintText carries a single "message" field that is a localization key.
    /// </summary>
    private HookReturnValue<NetworkReceiver> HandleHintText(
        IPostEventAbstractHookParams param,
        HookReturnValue<NetworkReceiver> returnValue)
    {
        try
        {
            var message = param.Data.ReadString("message");

            if (!string.IsNullOrEmpty(message))
            {
                var result = EvaluateKey(message);

                if (result == FilterResult.Block)
                    return new HookReturnValue<NetworkReceiver>(EHookAction.SkipCallReturnOverride);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[IgnoreMessages] Error reading CS_UM_HintText message");
        }

        return new HookReturnValue<NetworkReceiver>(EHookAction.Ignored);
    }

    // ===== Key evaluation =====

    private enum FilterResult { Allow, Block }

    private FilterResult EvaluateKey(string key)
    {
        // Only localization tokens start with '#'
        if (!key.StartsWith('#'))
            return FilterResult.Allow;

        // Rebuild cached set if ConVar changed
        RebuildCacheIfNeeded();

        if (_ignoredKeys.Contains(key))
        {
            _logger.LogDebug("[IgnoreMessages] Blocked key: {Key}", key);
            return FilterResult.Block;
        }

        if (_config.PrintKeyNames)
        {
            // Strip newlines so the console line stays readable
            var display = key.Replace("\n", "").Replace("\r", "");
            _logger.LogInformation("[IgnoreMessages] Observed key: {Key}", display);
        }

        return FilterResult.Allow;
    }

    private void RebuildCacheIfNeeded()
    {
        var raw = _config.IgnoredKeys;
        if (string.Equals(raw, _lastRawKeys, StringComparison.Ordinal))
            return;

        _lastRawKeys = raw;
        _ignoredKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(raw))
            return;

        foreach (var part in raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrEmpty(part))
                _ignoredKeys.Add(part);
        }

        _logger.LogInformation("[IgnoreMessages] Loaded {Count} ignored key(s): {Keys}",
            _ignoredKeys.Count,
            string.Join(", ", _ignoredKeys));
    }
}
