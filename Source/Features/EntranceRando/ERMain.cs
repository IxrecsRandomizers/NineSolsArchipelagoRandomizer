using HarmonyLib;
using Newtonsoft.Json.Linq;
using NineSolsAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using static ArchipelagoRandomizer.Features.EntranceRando.EntranceMapping;
using static ArchipelagoRandomizer.Features.EntranceRando.PortalData;

namespace ArchipelagoRandomizer.Features;

[HarmonyPatch]
internal class ERMain {
    static public bool entranceMappingActive = false;
    static public void ToggleMapping() {
        entranceMappingActive = !entranceMappingActive;
        ToastManager.Toast($"Set entranceMappingActive to {entranceMappingActive}. EntranceMap currently contains {EntranceMap.Count} mappings.");
    }

    static public void ApplyTestMapping() {
        ToastManager.Toast($"Swapping current EntranceMap ({EntranceMap.Count} mappings) with the test map ({TestEntranceMap.Count} mappings).");
        var oldMapping = EntranceMap;
        EntranceMap = TestEntranceMap;
        TestEntranceMap = oldMapping;
    }

    static public void CreateCircularTestMapping() {
        TestEntranceMap.Clear();
        var portals = Enum.GetValues(typeof(Portal)).Cast<Portal>().ToArray();
        var previousPortal = portals.Last();
        foreach (var portal in portals) {
            if (DepartureOnlyPortals.Contains(portal) || ArrivalOnlyPortals.Contains(portal))
                continue;
            Log.Warning($"mapping {previousPortal} to {portal}");
            TestEntranceMap.Add(previousPortal, portal);
            previousPortal = portal;
        }
        ToastManager.Toast($"Rewrote test map: all two-way portals are now linked in a circle.\nRemember it won't do anything until you click 'Apply Test Mapping'.");
    }

    static public void CreateRandomTestMapping() {
        TestEntranceMap.Clear();
        var portals = Enum.GetValues(typeof(Portal)).Cast<Portal>().ToArray();

        var unmappedDepartures = new List<Portal>();
        var unmappedArrivals = new List<Portal>();

        foreach (var portal in portals) {
            if (DepartureOnlyPortals.Contains(portal)) {
                unmappedDepartures.Add(portal);
            } else if (ArrivalOnlyPortals.Contains(portal)) {
                unmappedArrivals.Add(portal);
            } else {
                unmappedDepartures.Add(portal);
                unmappedArrivals.Add(portal);
            }
        }

        var rng = new Random();
        foreach (var departure in unmappedDepartures) {
            var i = rng.Next(0, unmappedArrivals.Count);

            var arrival = unmappedArrivals[i];
            unmappedArrivals.RemoveAt(i);

            Log.Warning($"mapping {departure} to {arrival}");
            TestEntranceMap.Add(departure, arrival);
        }

        ToastManager.Toast($"Rewrote test map: all portals are now randomly linked.\nRemember it won't do anything until you click 'Apply Test Mapping'.");
    }

    static public void ApplySlotData(object? erPairings) {
        //Log.Warning($"ER ApplySlotData() called with {erPairings}");
        if (erPairings == null) {
            entranceMappingActive = false;
        } else {
            if (erPairings is not JArray erPairingsArray) {
                Log.Error($"unable to apply ER pairings: {erPairings}");
                return;
            }

            EntranceMap.Clear();

            foreach (var pair in erPairingsArray) {
                if (pair is not JArray pairArray || pairArray.Count != 2) {
                    Log.Error($"unable to apply ER pairings: {erPairings}");
                    return;
                }
                var entranceString = (string)pairArray[0]!;
                var exitString = (string)pairArray[1]!;

                Enum.TryParse(entranceString, out Portal entrance);
                Enum.TryParse(exitString, out Portal exit);

                //Log.Warning($"mapping {entrance} to {exit}");
                EntranceMap.Add(entrance, exit);
            }

            Log.Info($"Successfully applied entrance mapping from slot data");
            entranceMappingActive = true;
        }
    }
}
