using System;
using Dalamud.Configuration;

namespace Housemate
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public bool Render { get; set; }
        public float RenderDistance { get; set; } = 10f;
        public bool SortObjectLists { get; set; }
        public SortType SortType { get; set; } = SortType.Distance;
        public int Version { get; set; } = 1;
        
        public void Save()
        {
            DalamudApi.PluginInterface.SavePluginConfig(this);
        }
    }
}