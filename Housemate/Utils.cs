using System;
using System.Numerics;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace Housemate
{
    public static class Utils
    {
        public static string GetExteriorPartDescriptor(ExteriorPartsType partsType)
        {
            return partsType switch
            {
                ExteriorPartsType.Roof => "Roof",
                ExteriorPartsType.Walls => "Walls",
                ExteriorPartsType.Windows => "Windows",
                ExteriorPartsType.Door => "Door",
                ExteriorPartsType.RoofOpt => "Roof (opt)",
                ExteriorPartsType.WallOpt => "Wall (opt)",
                ExteriorPartsType.SignOpt => "Signboard (opt)",
                ExteriorPartsType.Fence => "Fence",
                _ => "Unknown"
            };
        }

        public static string GetInteriorPartDescriptor(InteriorPartsType partsType)
        {
            return partsType switch
            {
                InteriorPartsType.Walls => "Walls",
                InteriorPartsType.Windows => "Windows",
                InteriorPartsType.Door => "Door",
                InteriorPartsType.Floor => "Floor",
                InteriorPartsType.Light => "Light",
                _ => "Unknown"
            };
        }

        public static string GetFloorDescriptor(InteriorFloor floor)
        {
            return floor switch
            {
                InteriorFloor.Ground => "Ground Floor",
                InteriorFloor.Basement => "Basement Floor",
                InteriorFloor.Upstairs => "2nd Floor",
                InteriorFloor.External => "Main",
                _ => "Unknown"
            };
        }

        public static float DistanceFromPlayer(HousingGameObject obj, Vector3 playerPos)
        {
            return Distance(new Vector3(playerPos.X, playerPos.Y, playerPos.Z), new Vector3(obj.X, obj.Y, obj.Z));
        }

        public static float Distance(Vector3 v1, Vector3 v2)
        {
            var x1 = Math.Pow(v2.X - v1.X, 2);
            var y1 = Math.Pow(v2.Y - v1.Y, 2);
            var z1 = Math.Pow(v2.Z - v1.Z, 2);

            return (float) Math.Sqrt(x1 + y1 + z1);
        }

        // Too expensive
        // private static bool IsOccluded(Vector3 pos, out Vector2 result)
        // {
        //     var dxPos = new SharpDX.Vector3(pos.X, pos.Y, pos.Z);
        //
        //     if (!pi.Framework.Gui.WorldToScreen(dxPos, out var screenCoords))
        //     {
        //         result = new Vector2(-1, -1);
        //         return false;
        //     }
        //
        //     result = new Vector2(screenCoords.X, screenCoords.Y);
        //
        //     if (!pi.Framework.Gui.ScreenToWorld(screenCoords, out SharpDX.Vector3 dxCheckPos, 1000000000f))
        //         return false;
        //
        //     return Distance(dxPos, dxCheckPos) > 1f;
        // }

        public static void StainButton(string id, Stain color)
        {
            var size = new Vector2(ImGui.GetFontSize(), ImGui.GetFontSize());
            StainButton(id, color, size);
        }

        private static void StainButton(string id, Stain color, Vector2 size)
        {
            var mousePos = ImGui.GetIO().MousePos;
            var floatColor = StainToVector4(color.Color);
            var topLeft = ImGui.GetCursorScreenPos();
            ImGui.ColorButton($"##{id}", floatColor, ImGuiColorEditFlags.NoTooltip, size);
            bool buttonVisible = ImGui.IsItemVisible();
            var bottom = ImGui.GetCursorScreenPos().Y;
            ImGui.SameLine();
            var right = ImGui.GetCursorScreenPos().X;
            ImGui.Text("");

            if (buttonVisible && Collides(topLeft, new Vector2(right, bottom), mousePos))
                ColorTooltip(color.Name, floatColor);
        }
        
        private static bool Collides(Vector2 origin, Vector2 bounds, Vector2 pos)
        {
            return pos.X > origin.X && pos.X < bounds.X &&
                   pos.Y > origin.Y && pos.Y < bounds.Y;
        }

        private static void ColorTooltip(string text, Vector4 color)
        {
            ImGui.BeginTooltip();
            var size = new Vector2(ImGui.GetFontSize() * 4 + ImGui.GetStyle().FramePadding.Y * 2,
                ImGui.GetFontSize() * 4 + ImGui.GetStyle().FramePadding.Y * 2);
            var cr = (int) (color.X * 255);
            var cg = (int) (color.Y * 255);
            var cb = (int) (color.Z * 255);
            var ca = (int) (color.W * 255);
            ImGui.ColorButton("##preview", color, ImGuiColorEditFlags.None, size);
            ImGui.SameLine();
            ImGui.Text(
                $"{text}\n" +
                $"#{cr:X2}{cb:X2}{cg:X2}{ca:X2}\n" +
                $"R: {cr}, G: {cg}, B: {cg}, A: {ca}\n" +
                $"({color.X:F3}, {color.Y:F3}, {color.Z:F3}, {color.W:F3})");
            ImGui.EndTooltip();
        }

        private static Vector4 StainToVector4(uint stainColor)
        {
            var s = 1.0f / 255.0f;

            return new Vector4()
            {
                X = ((stainColor >> 16) & 0xFF) * s,
                Y = ((stainColor >> 8) & 0xFF) * s,
                Z = ((stainColor >> 0) & 0xFF) * s,
                W = ((stainColor >> 24) & 0xFF) * s
            };
        }

        public static float GlobalFontScale()
        {
            return ImGui.GetIO().FontGlobalScale;
        }
    }
}