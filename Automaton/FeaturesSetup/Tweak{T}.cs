using Automaton.Configuration;
using Automaton.FeaturesSetup.Attributes;
using Automaton.Utils;
using Dalamud.Game.Command;
using Dalamud.Interface.Utility.Raii;
using ECommons.Configuration;
using ECommons.DalamudServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Dalamud.Game.Command.CommandInfo;

namespace Automaton.FeaturesSetup;

public abstract class Tweak<T> : Tweak
{
    // https://github.com/Haselnussbomber/HaselTweaks
    public Tweak() : base()
    {
        CachedConfigType = typeof(T);
        Config = (T?)(typeof(TweakConfigs)
            .GetProperties()?
            .FirstOrDefault(pi => pi!.PropertyType == typeof(T), null)?
            .GetValue(C.Tweaks))
            ?? throw new InvalidOperationException($"Configuration for {typeof(T).Name} not found.");
    }

    public Type CachedConfigType { get; init; }
    public T Config { get; init; }

    protected IEnumerable<MethodInfo> CommandHandlers
        => CachedType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(mi => mi.GetCustomAttribute<CommandHandlerAttribute>() != null);

    public override void DrawConfig()
    {
        var configFields = CachedConfigType.GetFields()
            .Select(fieldInfo => (FieldInfo: fieldInfo, Attribute: fieldInfo.GetCustomAttribute<BaseConfigAttribute>()))
            .Where((tuple) => tuple.Attribute != null)
            .Cast<(FieldInfo, BaseConfigAttribute)>();

        if (!configFields.Any())
            return;

        ImGuiX.DrawSection("Configuration");

        foreach (var (field, attr) in configFields)
        {
            var hasDependency = !string.IsNullOrEmpty(attr.DependsOn);
            var isDisabled = hasDependency && (bool?)CachedConfigType.GetField(attr.DependsOn)?.GetValue(Config) == false;

            using var id = ImRaii.PushId(field.Name);
            using var indent = ImGuiX.ConfigIndent(hasDependency);
            using var disabled = ImRaii.Disabled(isDisabled);

            attr.Draw(this, Config!, field);
        }
    }

    protected override void EnableCommands()
    {
        foreach (var methodInfo in CommandHandlers)
        {
            var attr = methodInfo.GetCustomAttribute<CommandHandlerAttribute>()!;
            var enabled = string.IsNullOrEmpty(attr.ConfigFieldName);

            if (!string.IsNullOrEmpty(attr.ConfigFieldName))
            {
                enabled |= (typeof(T).GetField(attr.ConfigFieldName)?.GetValue(Config) as bool?)
                    ?? throw new InvalidOperationException($"Configuration field {attr.ConfigFieldName} in {typeof(T).Name} not found.");
            }

            if (enabled)
            {
                EnableCommand(attr.Command, attr.HelpMessage, methodInfo);
            }
        }
    }

    protected override void DisableCommands()
    {
        foreach (var methodInfo in CommandHandlers)
        {
            var attr = methodInfo.GetCustomAttribute<CommandHandlerAttribute>()!;
            var enabled = string.IsNullOrEmpty(attr.ConfigFieldName);

            if (!string.IsNullOrEmpty(attr.ConfigFieldName))
            {
                enabled |= (typeof(T).GetField(attr.ConfigFieldName)?.GetValue(Config) as bool?)
                    ?? throw new InvalidOperationException($"Configuration field {attr.ConfigFieldName} in {typeof(T).Name} not found.");
            }

            if (enabled)
            {
                DisableCommand(attr.Command);
            }
        }
    }

    internal override void OnConfigChangeInternal(string fieldName)
    {
        foreach (var methodInfo in CommandHandlers)
        {
            var attr = methodInfo.GetCustomAttribute<CommandHandlerAttribute>()!;
            if (attr.ConfigFieldName != fieldName)
                continue;

            var enabled = string.IsNullOrEmpty(attr.ConfigFieldName);

            if (!string.IsNullOrEmpty(attr.ConfigFieldName))
            {
                enabled |= (typeof(T).GetField(attr.ConfigFieldName)?.GetValue(Config) as bool?)
                    ?? throw new InvalidOperationException($"Configuration field {attr.ConfigFieldName} in {typeof(T).Name} not found.");
            }

            if (enabled)
            {
                EnableCommand(attr.Command, attr.HelpMessage, methodInfo);
            }
            else
            {
                DisableCommand(attr.Command);
            }
        }

        base.OnConfigChangeInternal(fieldName);
    }

    internal override void OnLanguageChangeInternal()
    {
        DisableCommands();
        base.OnLanguageChangeInternal();
        EnableCommands();
    }

    private void EnableCommand(string command, string helpMessage, MethodInfo methodInfo)
    {
        var handler = methodInfo.CreateDelegate<HandlerDelegate>(this);

        if (Svc.Commands.AddHandler(command, new CommandInfo(handler) { HelpMessage = helpMessage }))
        {
            Log($"Added CommandHandler for {command}");
        }
        else
        {
            Warning($"Could not add CommandHandler for {command}");
        }
    }

    private void DisableCommand(string command)
    {
        if (Svc.Commands.RemoveHandler(command))
        {
            Log($"Removed CommandHandler for {command}");
        }
        else
        {
            Warning($"Could not remove CommandHandler for {command}");
        }
    }
}
