namespace Minecraft.Server.FourKit.Block;

/// <summary>
/// Represents a block. This is a live object, and only one Block may exist for
/// any given location in a world.
/// </summary>
public class Block
{
    private readonly World _world;
    private readonly int _x;
    private readonly int _y;
    private readonly int _z;

    internal Block(World world, int x, int y, int z)
    {
        _world = world;
        _x = x;
        _y = y;
        _z = z;
    }

    /// <summary>
    /// Gets the Location of the block.
    /// </summary>
    /// <returns>Location of the block.</returns>
    public Location getLocation()
    {
        return new Location(_world, _x, _y, _z, 0f, 0f);
    }

    /// <summary>
    /// Gets the type of this block.
    /// </summary>
    /// <returns>Block type.</returns>
    public Material getType()
    {
        int id = getTypeId();
        return Enum.IsDefined(typeof(Material), id) ? (Material)id : Material.AIR;
    }

    /// <summary>
    /// Gets the type ID of this block.
    /// </summary>
    /// <returns>Block type ID.</returns>
    public int getTypeId()
    {
        if (NativeBridge.GetTileId != null)
            return NativeBridge.GetTileId(_world.getDimensionId(), _x, _y, _z);
        return 0;
    }

    /// <summary>
    /// Gets the world which contains this Block.
    /// </summary>
    /// <returns>World containing this block.</returns>
    public World getWorld() => _world;

    /// <summary>
    /// Gets the x-coordinate of this block.
    /// </summary>
    /// <returns>X-coordinate.</returns>
    public int getX() => _x;

    /// <summary>
    /// Gets the y-coordinate of this block.
    /// </summary>
    /// <returns>Y-coordinate.</returns>
    public int getY() => _y;

    /// <summary>
    /// Gets the z-coordinate of this block.
    /// </summary>
    /// <returns>Z-coordinate.</returns>
    public int getZ() => _z;

    /// <summary>
    /// Sets the type of this block.
    /// </summary>
    /// <param name="type">Material to change this block to.</param>
    public void setType(Material type)
    {
        setTypeId((int)type);
    }

    /// <summary>
    /// Sets the type ID of this block.
    /// </summary>
    /// <param name="type">Type ID to change this block to.</param>
    /// <returns>Whether the change was successful.</returns>
    public bool setTypeId(int type)
    {
        NativeBridge.SetTile?.Invoke(_world.getDimensionId(), _x, _y, _z, type, 0);
        return true;
    }

    /// <summary>
    /// Gets the metadata value for this block.
    /// </summary>
    /// <returns>Block specific metadata.</returns>
    public byte getData()
    {
        return (byte)NativeBridge.GetTileData(_world.getDimensionId(), _x, _y, _z);
    }

    /// <summary>
    /// Sets the metadata value for this block.
    /// </summary>
    /// <param name="data">New block specific metadata.</param>
    public void setData(byte data)
    {
        NativeBridge.SetTileData?.Invoke(_world.getDimensionId(), _x, _y, _z, data);
    }

    /// <summary>
    /// Breaks the block and spawns items as if a player had digged it.
    /// </summary>
    /// <returns>true if the block was destroyed.</returns>
    public bool breakNaturally()
    {
        if (NativeBridge.BreakBlock != null)
            return NativeBridge.BreakBlock(_world.getDimensionId(), _x, _y, _z) != 0;
        return false;
    }

    /// <summary>
    /// Gets the block at the given offsets
    /// </summary>
    /// <param name="modX">X offset</param>
    /// <param name="modY">Y offset</param>
    /// <param name="modZ">Z offset</param>
    /// <returns>Block at the given offsets</returns>
    public Block getRelative(int modX, int modY, int modZ)
    {
        return getWorld().getBlockAt(getX() + modX, getY() + modY, getZ() + modZ);
    }

    /// <summary>
    /// Gets the block at the given face
    /// <para>This method is equal to getRelative(face, 1)</para>
    /// </summary>
    /// <param name="face">BlockFace to get relative to</param>
    /// <returns>Block at the given face</returns>
    public Block getRelative(BlockFace face)
    {
        return getRelative(face, 1);
    }

    /// <summary>
    /// Gets the block at the given distance of the given face
    /// <para>For example, the following method places water at 100,102,100; two
    /// blocks above 100,100,100.</para>
    /// <code>
    /// Block block = world.getBlockAt(100, 100, 100);
    /// Block shower = block.getRelative(BlockFace.UP, 2);
    /// shower.setType(Material.WATER);
    /// </code>
    /// </summary>
    /// <param name="face">BlockFace to get relative to</param>
    /// <param name="distance">Distance to get relative to</param>
    /// <returns>Block at the given distance of the given face</returns>
    public Block getRelative(BlockFace face, int distance)
    {
        return getRelative(face.getModX() * distance, face.getModY() * distance, face.getModZ() * distance);
    }
    
}
