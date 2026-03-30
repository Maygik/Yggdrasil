using System;

namespace Yggdrasil.Domain.Rigging
{
    public interface IExportBoneNamingConvention
    {
        string Name { get; }
        string GetBoneName(RigSlot slot);
    }

    public static class ExportBoneNamingConventions
    {
        public static IExportBoneNamingConvention ValveBiped { get; } =
            new DelegateExportBoneNamingConvention("ValveBiped", slot => slot.LogicalBone);

        private sealed class DelegateExportBoneNamingConvention : IExportBoneNamingConvention
        {
            private readonly Func<RigSlot, string> _resolver;

            public DelegateExportBoneNamingConvention(string name, Func<RigSlot, string> resolver)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(name);
                ArgumentNullException.ThrowIfNull(resolver);

                Name = name;
                _resolver = resolver;
            }

            public string Name { get; }

            public string GetBoneName(RigSlot slot)
            {
                ArgumentNullException.ThrowIfNull(slot);
                return _resolver(slot);
            }
        }
    }
}
