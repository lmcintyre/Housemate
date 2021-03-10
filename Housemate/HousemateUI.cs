using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Plugin;
using ImGuiNET;

namespace Housemate
{
    internal class HousemateUI : IDisposable
    {
        private static readonly Vector4 Gray = ImGui.ColorConvertU32ToFloat4(0xBBBBBBFF);
        private static readonly Vector4 White = ImGui.ColorConvertU32ToFloat4(0xFFFFFFFF);

        private ImGuiListClipperPtr _clipper;
        private readonly Configuration _configuration;
        private readonly DalamudPluginInterface _pi;
        
        private bool _visible;
        public bool Visible
        {
            get => _visible;
            set => _visible = value;
        }
        
        private bool _outLast;
        private bool _objectsOpen;

        private static HousingData Data => HousingData.Instance;
        private static HousingMemory Mem => HousingMemory.Instance;

        public HousemateUI(Configuration configuration, DalamudPluginInterface pi)
        {
            _configuration = configuration;
            _pi = pi;

            var clipperNative = Marshal.AllocHGlobal(24);
            var clipper = new ImGuiListClipper();
            Marshal.StructureToPtr(clipper, clipperNative, false);
            _clipper = new ImGuiListClipperPtr(clipperNative);
        }
        
        public unsafe void Dispose()
        {
            Marshal.FreeHGlobal(new IntPtr(_clipper.NativePtr));
        }

        public void Draw()
        {
            if (_configuration.Render)
            {
                Render(_configuration.RenderDistance);
                PlacardRender(_configuration.RenderDistance);
            }

            DrawMainWindow();
        }

        private unsafe void PlacardRender(float renderDistance)
        {
            var placardIdOffset = 120;

            if (!Data.TryGetLandSetDict(Mem.GetTerritoryTypeId(), out var landSets)) return;

            var actorTable = _pi.ClientState.Actors;
            if (actorTable == null) return;

            foreach (var actor in actorTable)
            {
                if (actor == null) continue;
                if (_pi.ClientState.LocalPlayer == null) continue;
                var placardId = *(uint*) ((byte*) actor.Address.ToPointer() + placardIdOffset);
                if (!landSets.TryGetValue(placardId, out var landSet)) continue;
                if (Utils.Distance(_pi.ClientState.LocalPlayer.Position, actor.Position) > renderDistance) continue;

                DrawPlotPlate(actor, placardId, landSet);
            }
        }

        private unsafe void Render(float renderDistance = 50f)
        {
            var mgr = Mem.CurrentManager;
            if (mgr == null) return;

            for (var i = 0; i < 400; i++)
            {
                var hObject = (HousingGameObject*) mgr->objects[i];
                if (hObject == null) continue;

                var objectName = "unknown";
                if (Mem.IsOutdoors())
                {
                    if (Data.TryGetYardObject(hObject->housingRowId, out var yardObject))
                        objectName = yardObject.Item.Value.Name.ToString();
                }
                else
                {
                    if (Data.TryGetFurniture(hObject->housingRowId, out var furnitureObject))
                        objectName = furnitureObject.Item.Value.Name.ToString();
                }

                var nPos = _pi.ClientState?.LocalPlayer?.Position;

                if (!nPos.HasValue || !_pi.Framework.Gui.WorldToScreen(new SharpDX.Vector3 {X = hObject->X, Y = hObject->Y, Z = hObject->Z}, out var screenCoords)) continue;

                if (Utils.DistanceFromPlayer(*hObject, nPos.Value) > renderDistance)
                    continue;

                ImGui.PushID("HousingObjects" + i);
                ImGui.SetNextWindowPos(new Vector2(screenCoords.X, screenCoords.Y));
                ImGui.SetNextWindowBgAlpha(0.5f);

                ImGui.Begin($"hou{i}",
                    ImGuiWindowFlags.NoDecoration |
                    ImGuiWindowFlags.AlwaysAutoResize |
                    ImGuiWindowFlags.NoSavedSettings |
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoMouseInputs |
                    ImGuiWindowFlags.NoFocusOnAppearing |
                    ImGuiWindowFlags.NoNav);
                
                if (string.IsNullOrEmpty(objectName))
                {
                    ImGui.End();
                    continue;
                }

                ImGui.Text($"{objectName}");
                if (hObject->color != 0 && Data.TryGetStain(hObject->color, out var color))
                {
                    ImGui.SameLine();
                    Utils.StainButton(objectName, color);
                }

                ImGui.End();

                ImGui.PopID();
            }
        }

        private void DrawPlotPlate(Actor placard, uint placardId, CommonLandSet land)
        {
            if (!Mem.GetHousingController(out var controller)) return;
            var customize = controller.Houses(land.PlotIndex);

            if (_pi.Framework.Gui.WorldToScreen(new SharpDX.Vector3 {X = placard.Position.X, Y = placard.Position.Z + 4, Z = placard.Position.Y}, out var screenCoords))
            {
                ImGui.PushID($"Placard{placardId}");
                ImGui.SetNextWindowPos(new Vector2(screenCoords.X, screenCoords.Y));
                ImGui.SetNextWindowBgAlpha(0.5f);

                ImGui.Begin($"Plot {land.PlotIndex + 1}",
                    ImGuiWindowFlags.NoCollapse |
                    ImGuiWindowFlags.AlwaysAutoResize |
                    ImGuiWindowFlags.NoNavFocus |
                    ImGuiWindowFlags.NoBringToFrontOnFocus |
                    ImGuiWindowFlags.NoSavedSettings |
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoMouseInputs |
                    ImGuiWindowFlags.NoFocusOnAppearing |
                    ImGuiWindowFlags.NoNav
                );

                // Let's check if this is a unified exterior
                var roof = customize.GetPart(ExteriorPartsType.Roof);
                if (roof.FixtureKey != 0 && Data.IsUnitedExteriorPart(roof.FixtureKey, out var roofItem))
                {
                    ImGui.Text($"Exterior: {roofItem.Name}");

                    if (roof.Color != 0 && Data.TryGetStain(roof.Color, out var color))
                    {
                        ImGui.SameLine();
                        Utils.StainButton(roofItem.Name, color);
                    }
                }
                else
                {
                    for (var i = 0; i < HouseCustomize.PartsMax; i++)
                    {
                        var type = (ExteriorPartsType) i;
                        var part = customize.GetPart(type);
                        if (!Data.TryGetItem(part.FixtureKey, out var item)) continue;
                        ImGui.Text($"{Utils.GetExteriorPartDescriptor(type)}: {item.Name}");

                        if (part.Color != 0 && Data.TryGetStain(part.Color, out var color))
                        {
                            ImGui.SameLine();
                            Utils.StainButton(item.Name, color);
                        }
                    }
                }

                ImGui.End();
                ImGui.PopID();
            }
        }

        private void DrawMainWindow()
        {
            if (!_visible) return;

            ImGui.SetNextWindowSize(new Vector2(375, 600), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Housemate", ref _visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.BeginTabBar("MainTabs");

                bool isSwapping = false;
                
                // DevTab();
                if (Mem.IsOutdoors())
                {
                    if (!_outLast)
                        isSwapping = true;
                    OutdoorTab();
                    _outLast = true;
                }

                if (Mem.IsIndoors())
                {
                    if (_outLast)
                        isSwapping = true;
                    IndoorTab();
                    _outLast = false;
                }
                
                if (!isSwapping)
                    SettingsTab();

                ImGui.EndTabBar();
            }

            ImGui.End();
        }

        // private unsafe void DevTab()
        // {
        // if (!ImGui.BeginTabItem("Dev")) return;
        //
        // CopyableText("HousingModule @ ", $"{_plugin.HousingModulePtr.ToInt64():X}", "");
        // CopyableText("LayoutWorld @ ", $"{_plugin.LayoutWorldPtr.ToInt64():X}", "");
        // // ImGui.Text($"type {_plugin.LayoutWorld->ActiveLayout->HousingController?.AreaType}");
        //
        // ImGui.EndTabItem();
        // }

        private unsafe void OutdoorTab()
        {
            if (!ImGui.BeginTabItem("Outdoors")) return;

            if (Mem.CurrentManager == null || Mem.IsIndoors())
            {
                ImGui.Text("You aren't in an outdoor housing zone!");
                ImGui.EndTabItem();
                return;
            }

            if (!ImGui.CollapsingHeader("Homes", ImGuiTreeNodeFlags.DefaultOpen))
            {
                RenderHousingObjectList(true);
                ImGui.EndTabItem();
                return;
            }

            // Column header outside of the child
            ImGui.BeginChild("##COLUMNAPIISDUMBIHATEYOU1", new Vector2(200, ImGui.GetFontSize()), false);
            ImGui.Columns(2);
            ImGui.Text("Plot");
            ImGui.SetColumnWidth(0, 38f);
            ImGui.NextColumn();
            ImGui.Text("Parts");
            ImGui.NextColumn();
            ImGui.SetColumnWidth(1, 300f);
            ImGui.EndChild();
            
            float size = _objectsOpen ? ImGui.GetFontSize() * 15f : ImGui.GetWindowHeight() - 140f * Utils.GlobalFontScale();
            
            ImGui.BeginChild("##homes", new Vector2(-1, size), false);

            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 38f);
            ImGui.SetColumnWidth(1, 300f);

            var basePlot = Mem.GetExteriorCommonFixtures(0);
            var subPlot = Mem.GetExteriorCommonFixtures(30);

            int start = 0, end = 0;

            if (basePlot.Length != 0 && subPlot.Length == 0)
            {
                start = 0;
                end = 30;
            }
            else if (basePlot.Length == 0 && subPlot.Length != 0)
            {
                start = 30;
                end = 60;
            }

            for (var plotId = start; plotId < end; plotId++)
            {
                var exterior = Mem.GetExteriorCommonFixtures(plotId);
                if (exterior.Length == 0 || exterior[0].FixtureType == -1) continue;

                ImGui.Text($"{plotId + 1}");
                ImGui.NextColumn();

                if (Data.IsUnitedExteriorPart((uint) exterior[0].FixtureKey, out var unitedItem))
                {
                    ImGui.Text($"Walls: {unitedItem.Name}");
                    if (exterior[0].Stain != null && exterior[0].Stain.RowId != 0)
                    {
                        ImGui.SameLine();
                        Utils.StainButton($"{exterior[0].Stain.Name}##{unitedItem.Name}", exterior[0].Stain);
                    }

                    ImGui.NextColumn();
                    continue;
                }

                for (var ext = 0; ext < exterior.Length; ext++)
                {
                    if (exterior[ext].FixtureKey == 0) continue;

                    var desc = Utils.GetExteriorPartDescriptor((ExteriorPartsType) ext);

                    ImGui.Text($"{desc}: {exterior[ext].Item.Name}");
                    if (exterior[ext].Stain != null && exterior[ext].Stain.RowId != 0)
                    {
                        ImGui.SameLine();
                        Utils.StainButton($"##{exterior[ext].Item.Name}", exterior[ext].Stain);
                    }
                }

                ImGui.NextColumn();
            }

            ImGui.EndChild();
            RenderHousingObjectList(true);
            ImGui.EndTabItem();
        }

        private void RenderHousingObjectList(bool collapsible)
        {
            if (collapsible)
            {
                if (!ImGui.CollapsingHeader("Nearby housing objects"))
                {
                    _objectsOpen = false;
                    return;
                }
            }
            else
            {
                ImGui.Text("Nearby housing objects");
            }
            _objectsOpen = true;

            ImGui.BeginChild("##COLUMNAPIISDUMBIHATEYOU2", new Vector2(200, ImGui.GetFontSize()), false);
            ImGui.Columns(2);
            ImGui.Text("Distance");
            ImGui.SetColumnWidth(0, 61f);
            ImGui.NextColumn();
            ImGui.Text("Item");
            ImGui.NextColumn();
            ImGui.SetColumnWidth(1, 300f);
            ImGui.EndChild();

            ImGui.BeginChild("##housingObjects", new Vector2(-1, -1), false);

            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 61f);
            ImGui.SetColumnWidth(1, 300f);

            var nPos = _pi?.ClientState?.LocalPlayer?.Position;
            if (!nPos.HasValue)
            {
                ImGui.EndChild();
                return;
            }

            List<HousingGameObject> dObjects;
            bool dObjectsLoaded;
            if (_configuration.SortObjectLists)
            {
                if (_configuration.SortType == SortType.Distance)
                    dObjectsLoaded = Mem.TryGetSortedHousingGameObjectList(out dObjects, nPos.Value);
                else
                    dObjectsLoaded = Mem.TryGetNameSortedHousingGameObjectList(out dObjects);
            }
            else
            {
                dObjectsLoaded = Mem.TryGetUnsortedHousingGameObjectList(out dObjects);
            }

            if (!dObjectsLoaded)
            {
                ImGui.EndChild();
                return;
            }

            _clipper.Begin(dObjects.Count);

            while (_clipper.Step())
                for (var i = _clipper.DisplayStart; i < _clipper.DisplayEnd; i++)
                {
                    var gameObject = dObjects[i];

                    var itemName = "";
                    if (Data.TryGetYardObject(gameObject.housingRowId, out var yardObject))
                        itemName = yardObject.Item.Value.Name.ToString();
                    if (Data.TryGetFurniture(gameObject.housingRowId, out var furnitureObject))
                        itemName = furnitureObject.Item.Value.Name.ToString();

                    var distance = Utils.DistanceFromPlayer(gameObject, nPos.Value);

                    ImGui.Text($"{distance:F2}");
                    ImGui.NextColumn();
                    ImGui.Text($"{itemName}");
                    if (gameObject.color != 0 && Data.TryGetStain(gameObject.color, out var stain))
                    {
                        ImGui.SameLine();
                        Utils.StainButton($"##{distance}{itemName}", stain);
                    }

                    ImGui.NextColumn();
                }


            ImGui.EndChild();
        }

        private unsafe void IndoorTab()
        {
            if (!ImGui.BeginTabItem("Indoors")) return;

            if (Mem.CurrentManager == null || Mem.IsOutdoors())
            {
                ImGui.Text("You aren't in an indoor housing zone!");
                ImGui.EndTabItem();
                return;
            }

            if (!ImGui.CollapsingHeader("Fixtures", ImGuiTreeNodeFlags.DefaultOpen))
            {
                RenderHousingObjectList(false);
                ImGui.EndTabItem();
                return;
            }

            // Column header outside of the child
            var fixtureColumnWidth = 135f;

            ImGui.BeginChild("##COLUMNAPIISDUMBIHATEYOU1", new Vector2(200, ImGui.GetFontSize()), false);
            ImGui.Columns(2);
            ImGui.Text("Type");
            ImGui.SetColumnWidth(0, fixtureColumnWidth);
            ImGui.NextColumn();
            ImGui.Text("Part");
            ImGui.NextColumn();
            ImGui.SetColumnWidth(1, 300f);
            ImGui.EndChild();
            ImGui.Separator();

            ImGui.BeginChild("##fixtures", new Vector2(-1, ImGui.GetFontSize() * 17f), false);

            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, fixtureColumnWidth);
            ImGui.SetColumnWidth(1, 300f);

            try
            {
                for (var i = 0; i < IndoorAreaData.FloorMax; i++)
                {
                    var fixtures = Mem.GetInteriorCommonFixtures(i);
                    if (fixtures.Length == 0) continue;
                    var isCurrentFloor = Mem.CurrentFloor() == (InteriorFloor) i;

                    for (var j = 0; j < IndoorFloorData.PartsMax; j++)
                    {
                        if (fixtures[j].FixtureKey == -1 || fixtures[j].FixtureKey == 0) continue;
                        var fixtureName = Utils.GetInteriorPartDescriptor((InteriorPartsType) j);

                        var color = isCurrentFloor ? White : Gray;

                        ImGui.TextColored(color, $"{Utils.GetFloorDescriptor((InteriorFloor) i)} {fixtureName}");
                        ImGui.NextColumn();
                        ImGui.TextColored(color, $"{fixtures[j].Item.Name}");
                        ImGui.NextColumn();
                    }

                    ImGui.Columns(1);
                    ImGui.Separator();
                    ImGui.Columns(2);
                }

                ImGui.Columns(1);
                ImGui.Text($"Light level: {Mem.GetInteriorLightLevel()}");
            }
            catch (Exception e)
            {
                PluginLog.Log(e, "hey");
            }

            ImGui.EndChild();
            ImGui.Separator();

            RenderHousingObjectList(false);

            ImGui.EndTabItem();
        }

        private void SettingsTab()
        {
            if (!ImGui.BeginTabItem("Settings")) return;

            var render = _configuration.Render;
            var renderDistance = _configuration.RenderDistance;
            var sortedObjects = _configuration.SortObjectLists;
            var sortType = _configuration.SortType;

            if (ImGui.Checkbox("Display housing object overlay", ref render))
                _configuration.Render = render;

            if (render && ImGui.SliderFloat("View distance", ref renderDistance, 0f, 100f))
                _configuration.RenderDistance = renderDistance;

            if (ImGui.Checkbox("Sort housing object lists", ref sortedObjects))
                _configuration.SortObjectLists = sortedObjects;

            if (sortedObjects)
            {
                ImGui.Text("Sort objects by:");
                ImGui.SameLine();
                if (ImGui.BeginCombo("##sortCombo", sortType.ToString()))
                {
                    if (ImGui.Selectable(SortType.Distance.ToString()))
                        _configuration.SortType = SortType.Distance;
                    if (ImGui.Selectable(SortType.Name.ToString()))
                        _configuration.SortType = SortType.Name;
                    ImGui.EndCombo();
                }
            }

            if (ImGui.Button("Save"))
                _configuration.Save();

            ImGui.EndTabItem();
        }
    }
}