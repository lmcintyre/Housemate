using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace Housemate
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        [NonSerialized] private DalamudPluginInterface _pluginInterface;

        public bool Render { get; set; }
        public float RenderDistance { get; set; } = 10f;
        public bool SortObjectLists { get; set; }
        public SortType SortType { get; set; } = SortType.Distance;
        public int Version { get; set; } = 1;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            _pluginInterface = pluginInterface;
        }

        public void Save()
        {
            _pluginInterface.SavePluginConfig(this);
        }
    }
}