using System.Runtime.InteropServices;
using Minecraft.Server.FourKit.Entity;
using Minecraft.Server.FourKit.Inventory;

namespace Minecraft.Server.FourKit;

/// <summary>
/// Represents a world, which may contain entities, chunks and blocks.
/// </summary>
public class World
{
    private readonly int _dimensionId;
    private readonly string _name;

    internal World(int dimensionId, string name)
    {
        _dimensionId = dimensionId;
        _name = name;
    }

    /// <summary>
    /// Gets the dimension ID of this world.
    /// </summary>
    /// <returns>Dimension ID of this world.</returns>
    public int getDimensionId() => _dimensionId;

    /// <summary>
    /// Gets the unique name of this world.
    /// </summary>
    /// <returns>Name of this world.</returns>
    public string getName() => _name;

    /// <summary>
    /// Gets the Block at the given coordinates.
    /// </summary>
    /// <param name="x">X-coordinate of the block.</param>
    /// <param name="y">Y-coordinate of the block.</param>
    /// <param name="z">Z-coordinate of the block.</param>
    /// <returns>Block at the given coordinates.</returns>
    public Block.Block getBlockAt(int x, int y, int z)
    {
        return new Block.Block(this, x, y, z);
    }

    /// <summary>
    /// Gets the Block at the given Location.
    /// </summary>
    /// <param name="location">Location of the block.</param>
    /// <returns>Block at the given location.</returns>
    public Block.Block getBlockAt(Location location)
    {
        return getBlockAt(location.getBlockX(), location.getBlockY(), location.getBlockZ());
    }

    /// <summary>
    /// Gets the block type ID at the given coordinates.
    /// </summary>
    /// <param name="x">X-coordinate of the block.</param>
    /// <param name="y">Y-coordinate of the block.</param>
    /// <param name="z">Z-coordinate of the block.</param>
    /// <returns>Type ID of the block.</returns>
    public int getBlockTypeIdAt(int x, int y, int z)
    {
        if (NativeBridge.GetTileId != null)
            return NativeBridge.GetTileId(_dimensionId, x, y, z);
        return 0;
    }

    /// <summary>
    /// Gets the block type ID at the given Location.
    /// </summary>
    /// <param name="location">Location of the block.</param>
    /// <returns>Type ID of the block.</returns>
    public int getBlockTypeIdAt(Location location)
    {
        return getBlockTypeIdAt(location.getBlockX(), location.getBlockY(), location.getBlockZ());
    }

    /// <summary>
    /// Gets the highest non-air coordinate at the given coordinates.
    /// </summary>
    /// <param name="x">X-coordinate.</param>
    /// <param name="z">Z-coordinate.</param>
    /// <returns>The Y-coordinate of the highest non-air block.</returns>
    public int getHighestBlockYAt(int x, int z)
    {
        if (NativeBridge.GetHighestBlockY != null)
            return NativeBridge.GetHighestBlockY(_dimensionId, x, z);
        return 0;
    }

    /// <summary>
    /// Gets the highest non-air coordinate at the given Location.
    /// </summary>
    /// <param name="location">Location to check.</param>
    /// <returns>The Y-coordinate of the highest non-air block.</returns>
    public int getHighestBlockYAt(Location location)
    {
        return getHighestBlockYAt(location.getBlockX(), location.getBlockZ());
    }

    /// <summary>
    /// Gets the highest non-empty block at the given coordinates.
    /// </summary>
    /// <param name="x">X-coordinate.</param>
    /// <param name="z">Z-coordinate.</param>
    /// <returns>Highest non-empty block.</returns>
    public Block.Block getHighestBlockAt(int x, int z)
    {
        int y = getHighestBlockYAt(x, z);
        return getBlockAt(x, y, z);
    }

    /// <summary>
    /// Gets the highest non-empty block at the given Location.
    /// </summary>
    /// <param name="location">Coordinates to get the highest block at.</param>
    /// <returns>Highest non-empty block.</returns>
    public Block.Block getHighestBlockAt(Location location)
    {
        return getHighestBlockAt(location.getBlockX(), location.getBlockZ());
    }

    private double[] GetWorldInfoSnapshot()
    {
        double[] buf = new double[7];
        if (NativeBridge.GetWorldInfo != null)
        {
            var gh = GCHandle.Alloc(buf, GCHandleType.Pinned);
            try
            {
                NativeBridge.GetWorldInfo(_dimensionId, gh.AddrOfPinnedObject());
            }
            finally
            {
                gh.Free();
            }
        }
        return buf;
    }

    /// <summary>
    /// Gets the default spawn Location of this world.
    /// </summary>
    /// <returns>The spawn location of this world.</returns>
    public Location getSpawnLocation()
    {
        double[] info = GetWorldInfoSnapshot();
        return new Location(this, info[0], info[1], info[2], 0f, 0f);
    }

    /// <summary>
    /// Sets the spawn location of the world.
    /// </summary>
    /// <param name="x">X-coordinate.</param>
    /// <param name="y">Y-coordinate.</param>
    /// <param name="z">Z-coordinate.</param>
    /// <returns>True if the spawn was set successfully.</returns>
    public bool setSpawnLocation(int x, int y, int z)
    {
        if (NativeBridge.SetSpawnLocation != null)
            return NativeBridge.SetSpawnLocation(_dimensionId, x, y, z) != 0;
        return false;
    }

    /// <summary>
    /// Gets the Seed for this world.
    /// </summary>
    /// <returns>This world's Seed.</returns>
    public long getSeed()
    {
        double[] info = GetWorldInfoSnapshot();
        return (long)info[3];
    }

    /// <summary>
    /// Gets the relative in-game time of this world.
    /// </summary>
    /// <returns>The current relative time.</returns>
    public long getTime()
    {
        double[] info = GetWorldInfoSnapshot();
        return (long)info[4];
    }

    /// <summary>
    /// Sets the relative in-game time on the server.
    /// </summary>
    /// <param name="time">The new relative time to set the in-game time to.</param>
    public void setTime(long time)
    {
        NativeBridge.SetWorldTime?.Invoke(_dimensionId, time);
    }

    /// <summary>
    /// Sets the in-game time on the server.
    /// </summary>
    /// <param name="time">The new absolute time to set this world to.</param>
    public void setFullTime(long time)
    {
        NativeBridge.SetWorldTime?.Invoke(_dimensionId, time);
    }

    /// <summary>
    /// Set whether there is a storm.
    /// </summary>
    /// <param name="hasStorm">Whether there is rain and snow.</param>
    public void setStorm(bool hasStorm)
    {
        NativeBridge.SetWeather?.Invoke(_dimensionId, hasStorm ? 1 : 0, -1, -1);
    }

    /// <summary>
    /// Set whether it is thundering.
    /// </summary>
    /// <param name="thundering">Whether it is thundering.</param>
    public void setThundering(bool thundering)
    {
        NativeBridge.SetWeather?.Invoke(_dimensionId, -1, thundering ? 1 : 0, -1);
    }

    /// <summary>
    /// Set the thundering duration.
    /// </summary>
    /// <param name="duration">Duration in ticks.</param>
    public void setThunderDuration(int duration)
    {
        NativeBridge.SetWeather?.Invoke(_dimensionId, -1, -1, duration);
    }

    /// <summary>
    /// Get a list of all players in this World.
    /// </summary>
    /// <returns>A list of all Players currently residing in this world.</returns>
    public List<Player> getPlayers()
    {
        var all = FourKit.getOnlinePlayers();
        var result = new List<Player>();
        foreach (var p in all)
        {
            var loc = p.getLocation();
            if (loc?.LocationWorld == this)
                result.Add(p);
        }
        return result;
    }

    /// <summary>
    /// Creates explosion at given coordinates with given power.
    /// </summary>
    /// <param name="x">X-coordinate.</param>
    /// <param name="y">Y-coordinate.</param>
    /// <param name="z">Z-coordinate.</param>
    /// <param name="power">The power of explosion, where 4F is TNT.</param>
    /// <returns>false if explosion was canceled, otherwise true.</returns>
    public bool createExplosion(double x, double y, double z, float power)
    {
        return createExplosion(x, y, z, power, false, true);
    }

    /// <summary>
    /// Creates explosion at given coordinates with given power and optionally
    /// setting blocks on fire.
    /// </summary>
    /// <param name="x">X-coordinate.</param>
    /// <param name="y">Y-coordinate.</param>
    /// <param name="z">Z-coordinate.</param>
    /// <param name="power">The power of explosion, where 4F is TNT.</param>
    /// <param name="setFire">Whether or not to set blocks on fire.</param>
    /// <returns>false if explosion was canceled, otherwise true.</returns>
    public bool createExplosion(double x, double y, double z, float power, bool setFire)
    {
        return createExplosion(x, y, z, power, setFire, true);
    }

    /// <summary>
    /// Creates explosion at given coordinates with given power and optionally
    /// setting blocks on fire or breaking blocks.
    /// </summary>
    /// <param name="x">X-coordinate.</param>
    /// <param name="y">Y-coordinate.</param>
    /// <param name="z">Z-coordinate.</param>
    /// <param name="power">The power of explosion, where 4F is TNT.</param>
    /// <param name="setFire">Whether or not to set blocks on fire.</param>
    /// <param name="breakBlocks">Whether or not to have blocks be destroyed.</param>
    /// <returns>false if explosion was canceled, otherwise true.</returns>
    public bool createExplosion(double x, double y, double z, float power, bool setFire, bool breakBlocks)
    {
        if (NativeBridge.CreateExplosion != null)
            return NativeBridge.CreateExplosion(_dimensionId, x, y, z, power, setFire ? 1 : 0, breakBlocks ? 1 : 0) != 0;
        return false;
    }

    /// <summary>
    /// Creates explosion at given coordinates with given power and optionally
    /// setting blocks on fire or breaking blocks.
    /// </summary>
    /// <param name="loc">Location to blow up.</param>
    /// <param name="power">The power of explosion, where 4F is TNT.</param>
    /// <param name="setFire">Whether or not to set blocks on fire.</param>
    /// <param name="breakBlocks">Whether or not to have blocks be destroyed.</param>
    /// <returns>false if explosion was canceled, otherwise true.</returns>
    public bool createExplosion(Location loc, float power, bool setFire, bool breakBlocks)
    {
        if (NativeBridge.CreateExplosion != null)
            return NativeBridge.CreateExplosion(_dimensionId, loc.X, loc.Y, loc.Z, power, setFire ? 1 : 0, breakBlocks ? 1 : 0) != 0;
        return false;
    }

    /// <summary>
    /// Creates explosion at given coordinates with given power.
    /// </summary>
    /// <param name="loc">Location to blow up.</param>
    /// <param name="power">The power of explosion, where 4F is TNT.</param>
    /// <returns>false if explosion was canceled, otherwise true.</returns>
    public bool createExplosion(Location loc, float power)
    {
        return createExplosion(loc.X, loc.Y, loc.Z, power);
    }

    /// <summary>
    /// Creates explosion at given coordinates with given power and optionally
    /// setting blocks on fire.
    /// </summary>
    /// <param name="loc">Location to blow up.</param>
    /// <param name="power">The power of explosion, where 4F is TNT.</param>
    /// <param name="setFire">Whether or not to set blocks on fire.</param>
    /// <returns>false if explosion was canceled, otherwise true.</returns>
    public bool createExplosion(Location loc, float power, bool setFire)
    {
        return createExplosion(loc.X, loc.Y, loc.Z, power, setFire);
    }

    /// <summary>
    /// Strikes lightning at the given Location.
    /// </summary>
    /// <param name="loc">The location to strike lightning.</param>
    /// <returns>true if lightning was successfully summoned.</returns>
    public bool strikeLightning(Location loc)
    {
        if (NativeBridge.StrikeLightning != null)
            return NativeBridge.StrikeLightning(_dimensionId, loc.X, loc.Y, loc.Z, 0) != 0;
        return false;
    }

    /// <summary>
    /// Strikes lightning at the given Location without doing damage.
    /// </summary>
    /// <param name="loc">The location to strike lightning.</param>
    /// <returns>true if lightning was successfully summoned.</returns>
    public bool strikeLightningEffect(Location loc)
    {
        if (NativeBridge.StrikeLightning != null)
            return NativeBridge.StrikeLightning(_dimensionId, loc.X, loc.Y, loc.Z, 1) != 0;
        return false;
    }

    /// <summary>
    /// Drops an item at the specified Location.
    /// </summary>
    /// <param name="location">Location to drop the item.</param>
    /// <param name="item">ItemStack to drop.</param>
    public void dropItem(Location location, ItemStack item)
    {
        NativeBridge.DropItem?.Invoke(_dimensionId, location.X, location.Y, location.Z, item.getTypeId(), item.getAmount(), item.getDurability(), 0);
    }

    /// <summary>
    /// Drops an item at the specified Location with a random offset.
    /// </summary>
    /// <param name="location">Location to drop the item.</param>
    /// <param name="item">ItemStack to drop.</param>
    public void dropItemNaturally(Location location, ItemStack item)
    {
        NativeBridge.DropItem?.Invoke(_dimensionId, location.X, location.Y, location.Z, item.getTypeId(), item.getAmount(), item.getDurability(), 1);
    }
}
