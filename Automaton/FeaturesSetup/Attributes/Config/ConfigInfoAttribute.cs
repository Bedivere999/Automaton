using Automaton.Utils;
using Dalamud.Interface;
using System;

namespace Automaton.FeaturesSetup.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class ConfigInfoAttribute : Attribute
{
    public ConfigInfoAttribute(string translationkey)
    {
        Translationkey = translationkey;
        Icon = FontAwesomeIcon.InfoCircle;
        Color = Colors.Grey;
    }

    public string Translationkey { get; init; }
    public FontAwesomeIcon Icon { get; init; }
    public HaselColor Color { get; init; }
}
