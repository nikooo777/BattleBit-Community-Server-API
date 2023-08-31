using BattleBitAPI.Common;
using BattleBitAPI.Common.Threading;

namespace SAT.SwissAdminTools;

public class Restrictions
{
    private static readonly ThreadSafe<Dictionary<Weapon, bool>> BlockedWeapons = new(new Dictionary<Weapon, bool>());
    private static readonly ThreadSafe<Dictionary<WeaponType, bool>> BlockedCategories = new(new Dictionary<WeaponType, bool>());

    public static bool IsWeaponRestricted(Weapon weapon)
    {
        using (BlockedWeapons.GetReadHandle())
        {
            var blocked = BlockedWeapons.Value.ContainsKey(weapon);
            if (blocked)
                return true;
        }

        using (BlockedCategories.GetReadHandle())
        {
            return BlockedCategories.Value.ContainsKey(weapon.WeaponType);
        }
    }

    public static void AddWeaponRestriction(Weapon weapon)
    {
        using (BlockedWeapons.GetWriteHandle())
        {
            BlockedWeapons.Value.Add(weapon, true);
        }
    }

    public static void AddCategoryRestriction(WeaponType weaponType)
    {
        Console.WriteLine("Adding category restriction for " + weaponType + "");
        using (BlockedCategories.GetWriteHandle())
        {
            BlockedCategories.Value.Add(weaponType, true);
        }
    }

    public static void RemoveCategoryRestriction(WeaponType weaponType)
    {
        using (BlockedCategories.GetWriteHandle())
        {
            BlockedCategories.Value.Remove(weaponType);
        }
    }

    public static void RemoveWeaponRestriction(Weapon weapon)
    {
        using (BlockedWeapons.GetWriteHandle())
        {
            BlockedWeapons.Value.Remove(weapon);
        }
    }
}