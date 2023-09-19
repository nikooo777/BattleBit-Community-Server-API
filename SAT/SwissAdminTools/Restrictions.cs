using BattleBitAPI.Common;
using BattleBitAPI.Common.Threading;

namespace SAT.SwissAdminTools;

public static class Restrictions
{
    private static readonly ThreadSafe<Dictionary<Weapon, bool>> BlockedWeapons = new(new Dictionary<Weapon, bool>());
    private static readonly ThreadSafe<Dictionary<WeaponType, bool>> BlockedCategories = new(new Dictionary<WeaponType, bool>());
    private static readonly ThreadSafe<Dictionary<Gadget, bool>> BlockedGadgets = new(new Dictionary<Gadget, bool>());

    public static bool IsGadgetRestricted(Gadget gadget)
    {
        using (BlockedGadgets.GetReadHandle())
        {
            var blocked = BlockedGadgets.Value.ContainsKey(gadget);
            if (blocked)
                return true;
        }

        return false;
    }

    public static void AddGadgetRestriction(Gadget gadget)
    {
        using (BlockedGadgets.GetWriteHandle())
        {
            if (BlockedGadgets.Value.ContainsKey(gadget))
                return;
            BlockedGadgets.Value.Add(gadget, true);
        }
    }

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
            if (BlockedWeapons.Value.ContainsKey(weapon))
                return;
            BlockedWeapons.Value.Add(weapon, true);
        }
    }

    public static void AddCategoryRestriction(WeaponType weaponType)
    {
        Console.WriteLine("Adding category restriction for " + weaponType + "");
        using (BlockedCategories.GetWriteHandle())
        {
            if (BlockedCategories.Value.ContainsKey(weaponType))
                return;
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