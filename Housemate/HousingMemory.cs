using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Logging;
using Dalamud.Plugin;

namespace Housemate
{
    public class HousingMemory
    {
        private HousingMemory(SigScanner scanner)
        {
            try
            {
                HousingModulePtr =
                    scanner.GetStaticAddressFromSig(
                        "40 53 48 83 EC 20 33 DB 48 39 1D ?? ?? ?? ?? 75 2C 45 33 C0 33 D2 B9 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 85 C0 74 11 48 8B C8 E8 ?? ?? ?? ?? 48 89 05 ?? ?? ?? ?? EB 07", 0xA);

                LayoutWorldPtr =
                    scanner.GetStaticAddressFromSig(
                        "48 8B 05 ?? ?? ?? ?? 48 8B 48 20 48 85 C9 74 31 83 B9 ?? ?? ?? ?? ?? 74 28 80 B9 ?? ?? ?? ?? ?? 75 1F 80 B9 ?? ?? ?? ?? ?? 74 03 B0 01 C3", 2);

                PluginLog.Log($"Pre-HousingModule at {HousingModulePtr.ToInt64():X}");
                PluginLog.Log($"Pre-LayoutWorld at {LayoutWorldPtr.ToInt64():X}");

                HousingModulePtr = Marshal.ReadIntPtr(HousingModulePtr);
                LayoutWorldPtr = Marshal.ReadIntPtr(LayoutWorldPtr);

                PluginLog.Log($"HousingModule at {HousingModulePtr.ToInt64():X}");
                PluginLog.Log($"LayoutWorld at {LayoutWorldPtr.ToInt64():X}");
            }
            catch (Exception e)
            {
                PluginLog.Log(e, "Could not load Housemate!!");
            }
        }

        public static HousingMemory Instance { get; private set; }

        private IntPtr HousingModulePtr { get; }
        private IntPtr LayoutWorldPtr { get; }

        private unsafe HousingModule* HousingModule => (HousingModule*) HousingModulePtr;
        private unsafe LayoutWorld* LayoutWorld => (LayoutWorld*) LayoutWorldPtr;
        public unsafe HousingObjectManager* CurrentManager => HousingModule->currentTerritory;

        public static void Init(SigScanner scanner)
        {
            Instance = new HousingMemory(scanner);
        }

        public unsafe InteriorFloor CurrentFloor()
        {
            if (HousingModule->currentTerritory == null || HousingModule->IsOutdoors()) return InteriorFloor.None;
            return HousingModule->CurrentFloor();
        }

        public uint GetTerritoryTypeId()
        {
            if (!GetActiveLayout(out var manager)) return 0;
            return manager.TerritoryTypeId;
        }

        public float GetInteriorLightLevel()
        {
            if (IsOutdoors()) return 0f;
            if (!GetActiveLayout(out var manager)) return 0f;
            if (!manager.IndoorAreaData.HasValue) return 0f;
            return manager.IndoorAreaData.Value.LightLevel;
        }

        public CommonFixture[] GetInteriorCommonFixtures(int floorId)
        {
            if (IsOutdoors()) return new CommonFixture[0];
            if (!GetActiveLayout(out var manager)) return new CommonFixture[0];
            if (!manager.IndoorAreaData.HasValue) return new CommonFixture[0];
            var floor = manager.IndoorAreaData.Value.GetFloor(floorId);

            var ret = new CommonFixture[IndoorFloorData.PartsMax];
            for (var i = 0; i < IndoorFloorData.PartsMax; i++)
            {
                var key = floor.GetPart(i);
                if (!HousingData.Instance.TryGetItem(unchecked((uint) key), out var item))
                    HousingData.Instance.IsUnitedExteriorPart(unchecked((uint) key), out item);

                ret[i] = new CommonFixture(
                    false,
                    i,
                    key,
                    null,
                    item);
            }

            return ret;
        }

        public CommonFixture[] GetExteriorCommonFixtures(int plotId)
        {
            if (IsIndoors()) return new CommonFixture[0];
            if (!GetHousingController(out var controller)) return new CommonFixture[0];
            var home = controller.Houses(plotId);

            if (home.Size == -1) return new CommonFixture[0];
            if (home.GetPart(0).Category == -1) return new CommonFixture[0];

            var ret = new CommonFixture[HouseCustomize.PartsMax];
            for (var i = 0; i < HouseCustomize.PartsMax; i++)
            {
                var colorId = home.GetPart(i).Color;
                HousingData.Instance.TryGetStain(colorId, out var stain);
                HousingData.Instance.TryGetItem(home.GetPart(i).FixtureKey, out var item);

                ret[i] = new CommonFixture(
                    true,
                    home.GetPart(i).Category,
                    home.GetPart(i).FixtureKey,
                    stain,
                    item);
            }

            return ret;
        }

        public unsafe bool TryGetHousingGameObject(int index, out HousingGameObject? gameObject)
        {
            gameObject = null;
            if (HousingModule == null ||
                HousingModule->GetCurrentManager() == null ||
                HousingModule->GetCurrentManager()->objects == null)
                return false;

            if (HousingModule->GetCurrentManager()->objects[index] == 0)
                return false;

            gameObject = *(HousingGameObject*) HousingModule->GetCurrentManager()->objects[index];

            return true;
        }

        public unsafe bool TryGetSortedHousingGameObjectList(out List<HousingGameObject> objects, Vector3 playerPos)
        {
            objects = null;
            if (HousingModule == null ||
                HousingModule->GetCurrentManager() == null ||
                HousingModule->GetCurrentManager()->objects == null)
                return false;

            var tmpObjects = new List<(HousingGameObject gObject, float distance)>();
            objects = new List<HousingGameObject>();
            for (var i = 0; i < 400; i++)
            {
                var oPtr = HousingModule->GetCurrentManager()->objects[i];
                if (oPtr == 0)
                    continue;
                var o = *(HousingGameObject*) oPtr;
                tmpObjects.Add((o, Utils.DistanceFromPlayer(o, playerPos)));
            }

            tmpObjects.Sort((obj1, obj2) => obj1.distance.CompareTo(obj2.distance));
            objects = tmpObjects.Select(obj => obj.gObject).ToList();
            return true;
        }

        public unsafe bool TryGetNameSortedHousingGameObjectList(out List<HousingGameObject> objects)
        {
            objects = null;
            if (HousingModule == null ||
                HousingModule->GetCurrentManager() == null ||
                HousingModule->GetCurrentManager()->objects == null)
                return false;

            objects = new List<HousingGameObject>();
            for (var i = 0; i < 400; i++)
            {
                var oPtr = HousingModule->GetCurrentManager()->objects[i];
                if (oPtr == 0)
                    continue;
                var o = *(HousingGameObject*) oPtr;
                objects.Add(o);
            }

            objects.Sort(
                (obj1, obj2) =>
                {
                    string name1 = "", name2 = "";
                    if (HousingData.Instance.TryGetFurniture(obj1.housingRowId, out var furniture1))
                        name1 = furniture1.Item.Value.Name.ToString();
                    else if (HousingData.Instance.TryGetYardObject(obj1.housingRowId, out var yardObject1))
                        name1 = yardObject1.Item.Value.Name.ToString();
                    if (HousingData.Instance.TryGetFurniture(obj2.housingRowId, out var furniture2))
                        name2 = furniture2.Item.Value.Name.ToString();
                    else if (HousingData.Instance.TryGetYardObject(obj2.housingRowId, out var yardObject2))
                        name2 = yardObject2.Item.Value.Name.ToString();

                    return string.Compare(name1, name2, StringComparison.Ordinal);
                });
            return true;
        }

        public unsafe bool TryGetUnsortedHousingGameObjectList(out List<HousingGameObject> objects)
        {
            objects = null;
            if (HousingModule == null ||
                HousingModule->GetCurrentManager() == null ||
                HousingModule->GetCurrentManager()->objects == null)
                return false;

            objects = new List<HousingGameObject>();
            for (var i = 0; i < 400; i++)
            {
                var oPtr = HousingModule->GetCurrentManager()->objects[i];
                if (oPtr == 0)
                    continue;
                var o = *(HousingGameObject*) oPtr;
                objects.Add(o);
            }

            return true;
        }

        public unsafe bool GetActiveLayout(out LayoutManager manager)
        {
            manager = new LayoutManager();
            if (LayoutWorld == null ||
                LayoutWorld->ActiveLayout == null)
                return false;
            manager = *LayoutWorld->ActiveLayout;
            return true;
        }

        public bool GetHousingController(out HousingController controller)
        {
            controller = new HousingController();
            if (!GetActiveLayout(out var manager) ||
                !manager.HousingController.HasValue)
                return false;

            controller = manager.HousingController.Value;
            return true;
        }

        public unsafe bool IsOutdoors()
        {
            if (HousingModule == null) return false;
            return HousingModule->IsOutdoors();
        }

        public unsafe bool IsIndoors()
        {
            if (HousingModule == null) return false;
            return HousingModule->IsIndoors();
        }
    }
}