using System.Runtime.InteropServices;
using Lumina.Data;
using Lumina.Excel;

namespace Housemate
{
    [Sheet("HousingLandSet")]
    public class HousingLandSet : IExcelRow
    {
        public LandSet[] LandSets;

        public uint UnknownRange { get; private set; }
        public uint UnknownRange2 { get; private set; }

        public uint RowId { get; set; }
        public uint SubRowId { get; set; }

        public void PopulateData(RowParser parser, Lumina.Lumina lumina, Language language)
        {
            LandSets = parser.ReadStructuresAsArray<LandSet>(0, 60);
            UnknownRange = parser.ReadColumn<uint>(300);
            UnknownRange2 = parser.ReadColumn<uint>(301);
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct LandSet
        {
            public uint LandRange;
            public uint PlacardId;

            public uint UnknownRange1;

            // public uint ExitPopRange;
            public uint InitialPrice;
            public byte Size;
            private fixed byte padding[3];
        }
    }
}