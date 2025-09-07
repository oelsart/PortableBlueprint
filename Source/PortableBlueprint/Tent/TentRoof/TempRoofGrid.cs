using Verse;

namespace PortableBlueprint.Tent.TentRoof;

public class TempRoofGrid(Map map) : MapComponent(map)
{
    private readonly RoofDef[] roofGrid = new RoofDef[map.cellIndices.NumGridCells];

    public bool Roofed(int index)
    {
        return roofGrid[index] != null;
    }

    public bool Roofed(int x, int z)
    {
        return roofGrid[map.cellIndices.CellToIndex(x, z)] != null;
    }

    public bool Roofed(IntVec3 c)
    {
        return roofGrid[map.cellIndices.CellToIndex(c)] != null;
    }

    public RoofDef RoofAt(int index)
    {
        return roofGrid[index];
    }

    public RoofDef RoofAt(IntVec3 c)
    {
        return roofGrid[map.cellIndices.CellToIndex(c)];
    }

    public RoofDef RoofAt(int x, int z)
    {
        return roofGrid[map.cellIndices.CellToIndex(x, z)];
    }

    public void SetRoof(IntVec3 c, RoofDef def)
    {
        if (roofGrid[map.cellIndices.CellToIndex(c)] == def)
        {
            return;
        }
        roofGrid[map.cellIndices.CellToIndex(c)] = def;
    }

    public override void ExposeData()
    {
        MapExposeUtility.ExposeUshort(map, delegate (IntVec3 c)
        {
            if (roofGrid[map.cellIndices.CellToIndex(c)] != null)
            {
                return roofGrid[map.cellIndices.CellToIndex(c)].shortHash;
            }
            return 0;
        }, delegate (IntVec3 c, ushort val)
        {
            SetRoof(c, DefDatabase<RoofDef>.GetByShortHash(val));
        }, "roofs");
    }
}
