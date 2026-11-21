using ArchipelagoRandomizer.Locations;
using HarmonyLib;
using NineSolsAPI;

namespace ArchipelagoRandomizer.ScriptedEventEdits;

// In the vanilla game, after the Sky Rending Claw fight and Yanlao's death, the Boundless Repository "gauntlet"
// makes several small changes so you can revisit it and pick up any chests or vault locks you missed before.
// Many of those changes are removing boxes that you may have previously needed Yanlao to shoot for you.
// Unfortunately, some of those box removals massively impact randomizer logic, to the point that any single logic
// we write would either make it easy to softlock in ER, or way too easy to go out of logic without realizing it.
// I believe the least bad solution is to revive Yanlao for gauntlet revisits in the randomizer.

[HarmonyPatch]
internal class BRGauntlet {
    private static string yanlaoKilledFlagId = "fac7bf4a1b1f89d47873e1ec23d1c3c8ScriptableDataBool"; // A4_S5_BossKilled

    // Instead of listing every flag, this patch makes every FlagBoolCondition for Yanlao's death in the BR Gauntlet pretend he's alive.
    [HarmonyPrefix, HarmonyPatch(typeof(FlagBoolCondition), "isValid", MethodType.Getter)]
    static bool FlagBoolCondition_get_isValid(FlagBoolCondition __instance, ref bool __result) {
        var goPath = LocationTriggers.GetFullDisambiguatedPath(__instance.gameObject);

        if (__instance.flagBool?.boolFlag?.FinalSaveID != yanlaoKilledFlagId)
            return true;

        // BR itself is "A4_S3", and the "5th container" room is A4_SG6. We're intentionally not changing the 5th container.
        var levelName = SingletonBehaviour<GameCore>.Instance.gameLevel.name;
        if (levelName != "A4_S4") // BR Gauntlet
            return true;

        // Because this is a prefix patch, we don't want an "if __result already false do nothing" block.
        // It's entirely possible for the vanilla code to change __result from false to true if we let it run.
        // Instead we directly check if Yanlao is still alive.
        var yanlaoDead = ((ScriptableDataBool)SingletonBehaviour<SaveManager>.Instance.allFlags.FlagDict[yanlaoKilledFlagId]).CurrentValue;
        if (!yanlaoDead)
            return true;

        ShowToast();
        __result = false;
        return false;
    }

    // Show this once per session, because that's the simplest thing to implement that's neither spam not silence.
    // Hopefully we don't need anything fancier than that.
    private static bool toastShown = false;
    private static void ShowToast() {
        if (toastShown)
            return;

        toastShown = true;
        ToastManager.Toast($"Forcing the BR gauntlet to pretend Yanlao is still alive.");
    }
}
