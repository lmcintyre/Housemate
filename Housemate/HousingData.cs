using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace Housemate
{
    public class HousingData
    {
        private readonly Dictionary<uint, HousingFurniture> _furnitureDict;
        private readonly Dictionary<uint, Item> _itemDict;
        private readonly Dictionary<uint, Stain> _stainDict;

        private readonly Dictionary<uint, Dictionary<uint, CommonLandSet>> _territoryToLandSetDict;
        private readonly Dictionary<uint, uint> _unitedDict;
        private readonly Dictionary<uint, HousingYardObject> _yardObjectDict;

        private HousingData(DalamudPluginInterface pi)
        {
            var sheet = pi.Data.GetExcelSheet<HousingLandSet>();
            uint[] terriKeys = {339, 340, 341, 641};

            _territoryToLandSetDict = new Dictionary<uint, Dictionary<uint, CommonLandSet>>();

            for (uint i = 0; i < sheet.RowCount; i++)
            {
                if (terriKeys.Length < i) continue;
                var row = sheet.GetRow(i);
                var rowDict = new Dictionary<uint, CommonLandSet>();
                for (var j = 0; j < row.LandSets.Length; j++)
                {
                    var cset = CommonLandSet.FromExd(row.LandSets[j], j);
                    rowDict[cset.PlacardId] = cset;
                }

                _territoryToLandSetDict[terriKeys[i]] = rowDict;
            }

            var unitedExteriorSheet = pi.Data.GetExcelSheet<HousingUnitedExterior>();
            _unitedDict = new Dictionary<uint, uint>();
            foreach (var row in unitedExteriorSheet)
            foreach (var item in row.Item)
                _unitedDict[item.Row] = row.RowId;

            _itemDict = pi.Data.GetExcelSheet<Item>()
                .Where(item => item.AdditionalData != 0 && (item.ItemSearchCategory.Row == 65 || item.ItemSearchCategory.Row == 66))
                .ToDictionary(row => row.AdditionalData, row => row);

            _stainDict = pi.Data.GetExcelSheet<Stain>().ToDictionary(row => row.RowId, row => row);
            _furnitureDict = pi.Data.GetExcelSheet<HousingFurniture>().ToDictionary(row => row.RowId, row => row);
            _yardObjectDict = pi.Data.GetExcelSheet<HousingYardObject>().ToDictionary(row => row.RowId, row => row);

            PluginLog.Log($"Loaded {_territoryToLandSetDict.Keys.Count} landset rows");
            PluginLog.Log($"Loaded {_furnitureDict.Keys.Count} furniture");
            PluginLog.Log($"Loaded {_yardObjectDict.Keys.Count} yard objects");
            PluginLog.Log($"Loaded {_unitedDict.Keys.Count} united parts");
            PluginLog.Log($"Loaded {_stainDict.Keys.Count} stain infos");
            PluginLog.Log($"Loaded {_itemDict.Keys.Count} items with AdditionalData");
        }

        public static HousingData Instance { get; private set; }

        public static void Init(DalamudPluginInterface pi)
        {
            Instance = new HousingData(pi);
        }

        public bool TryGetYardObject(uint id, out HousingYardObject yardObject)
        {
            return _yardObjectDict.TryGetValue(id, out yardObject);
        }

        public bool TryGetFurniture(uint id, out HousingFurniture furniture)
        {
            return _furnitureDict.TryGetValue(id, out furniture);
        }

        public bool IsUnitedExteriorPart(uint id, out Item item)
        {
            item = null;
            if (!_unitedDict.TryGetValue(id, out var unitedId))
                return false;

            if (!_itemDict.TryGetValue(unitedId, out item))
                return false;
            return true;
        }

        public bool TryGetLandSetDict(uint id, out Dictionary<uint, CommonLandSet> dict)
        {
            return _territoryToLandSetDict.TryGetValue(id, out dict);
        }

        public bool TryGetItem(uint id, out Item item)
        {
            return _itemDict.TryGetValue(id, out item);
        }

        public bool TryGetStain(uint id, out Stain stain)
        {
            return _stainDict.TryGetValue(id, out stain);
        }
    }
}