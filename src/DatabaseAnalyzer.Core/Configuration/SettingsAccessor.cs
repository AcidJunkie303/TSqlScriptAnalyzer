using DatabaseAnalyzer.Common.Contracts.Settings;

namespace DatabaseAnalyzer.Core.Configuration;

internal static class SettingsAccessor
{
    public static object GetSettings(object rawSettings, Type rawSettingsType, Type finalSettingsType)
    {
        var accessorType = typeof(SettingsAccessorInternal<,>).MakeGenericType(rawSettingsType, finalSettingsType);
        var accessor = (ISettingsAccessor) Activator.CreateInstance(accessorType, (object[]) [rawSettings])!;
        return accessor.GetSettingsInternal();
    }

    private sealed class SettingsAccessorInternal<TRaw, TFinal> : ISettingsAccessor
        where TRaw : class, IRawSettings<TFinal>
        where TFinal : class, ISettings<TFinal>
    {
        private readonly TRaw _rawSettings;
#pragma warning disable S1144 // Unused private types or members should be removed -> ise used above through Activator.CreateInstance
        public SettingsAccessorInternal(TRaw rawSettings)
        {
            _rawSettings = rawSettings;
        }
#pragma warning restore S1144
        public object GetSettingsInternal() => _rawSettings.ToSettings();
    }

    private interface ISettingsAccessor
    {
        object GetSettingsInternal();
    }
}
