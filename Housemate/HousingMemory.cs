using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Housemate
{
    public class HousingMemory
    {
        private unsafe HousingMemory()
        {
            try
            {
                HousingModuleBasePtr = DalamudApi.SigScanner.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 8B 52");
                LayoutWorldBasePtr = DalamudApi.SigScanner.GetStaticAddressFromSig("48 8B 0D ?? ?? ?? ?? 85 C0 74 15");
                DalamudApi.PluginLog.Info($"HousingModuleBase at {HousingModuleBasePtr.ToInt64():X}");
                DalamudApi.PluginLog.Info($"HousingModule at {(ulong)HousingModule:X}");
                DalamudApi.PluginLog.Info($"LayoutWorldBase at {LayoutWorldBasePtr.ToInt64():X}");
            }
            catch (Exception e)
            {
                DalamudApi.PluginLog.Error(e, "Could not load Housemate!");
            }
        }

        public static HousingMemory Instance { get; private set; }

        private IntPtr HousingModuleBasePtr { get; }
        private IntPtr LayoutWorldBasePtr { get; }

        private unsafe HousingModule* HousingModule => HousingModuleBasePtr != IntPtr.Zero ? (HousingModule*) Marshal.ReadIntPtr(HousingModuleBasePtr) : null;
        private unsafe LayoutWorld* LayoutWorld => LayoutWorldBasePtr != IntPtr.Zero ? (LayoutWorld*) Marshal.ReadIntPtr(LayoutWorldBasePtr) : null;
        public unsafe HousingObjectManager* CurrentManager => HousingModule->currentTerritory;

        public static void Init()
        {
            Instance = new HousingMemory();
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

        public unsafe bool IsWorkshop()
        {
	        if (HousingModule == null) return false;
	        return HousingModule->IsWorkshop();
        }
    }
}