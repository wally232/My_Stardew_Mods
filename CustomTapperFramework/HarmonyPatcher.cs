using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Internal;
using StardewValley.Objects;
using StardewValley.Tools;
using StardewValley.TerrainFeatures;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace CustomTapperFramework;

using SObject = StardewValley.Object;

public class HarmonyPatcher {
  public static void ApplyPatches(Harmony harmony) {
    // Patch interactions
    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.canBePlacedHere)),
        postfix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.SObject_canBePlacedHere_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.placementAction)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.SObject_placementAction_Prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.checkForAction)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.SObject_checkForAction_Prefix)),
        postfix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.SObject_checkForAction_Postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.performRemoveAction)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.SObject_performRemoveAction_Prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.actionOnPlayerEntry)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.SObject_actionOnPlayerEntry_Prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.updateWhenCurrentLocation)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.SObject_updateWhenCurrentLocation_Prefix)));

    // Patch tool actions
    harmony.Patch(
        original: AccessTools.Method(typeof(FruitTree),
          nameof(FruitTree.performToolAction)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.FruitTree_performToolAction_Prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(Tree),
          nameof(Tree.performToolAction)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.Tree_performToolAction_Prefix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(GiantCrop),
          nameof(GiantCrop.performToolAction)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.GiantCrop_performToolAction_Prefix)));

    // Misc patches
    harmony.Patch(
        original: AccessTools.Method(typeof(SObject),
          nameof(SObject.draw),
          new Type[] {typeof(SpriteBatch), typeof(int), typeof(int), typeof(float)}),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.SObject_draw_Prefix)),
        transpiler: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.SObject_draw_Transpiler)));

    harmony.Patch(
        original: AccessTools.Method(typeof(Tree),
          nameof(Tree.UpdateTapperProduct)),
        prefix: new HarmonyMethod(typeof(HarmonyPatcher),
          nameof(HarmonyPatcher.Tree_UpdateTapperProduct_Prefix)));
  }

	static void SObject_canBePlacedHere_Postfix(SObject __instance, ref bool __result, GameLocation l, Vector2 tile, CollisionMask collisionMask = CollisionMask.All, bool showError = false) {
    // Check crab pots
    if (Utils.IsCrabPot(__instance)) {
			__result = CrabPot.IsValidCrabPotLocationTile(l, (int)tile.X, (int)tile.Y);
      return;
    }
    // Check tappers - legacy API
    if (!__instance.IsTapper()) return;
    bool disallowBaseTapperRules = false;
    if (Utils.GetFeatureAt(l, tile, out var feature, out var centerPos)) {
        if (!l.objects.ContainsKey(centerPos) &&
            Utils.GetOutputRules(__instance, feature, TileFeature.WATER, out disallowBaseTapperRules) != null) {
          __result = true;
        }
        else if (disallowBaseTapperRules) {
          __result = false;
        }
    }
  }

  static bool SObject_placementAction_Prefix(SObject __instance, ref bool __result, GameLocation location, int x, int y, Farmer who = null) {
    Vector2 vector = new Vector2(x / 64, y / 64);
    //if (__instance.IsTapper() &&
    //    Utils.GetFeatureAt(location, vector, out var feature, out var centerPos) &&
    //    !location.objects.ContainsKey(centerPos) &&
    //    Utils.GetOutputRules(__instance, feature, TileFeature.REGULAR, out bool unused) is var outputRules &&
    //    outputRules != null) {
    //  // Place tapper if able
    //  SObject @object = (SObject)__instance.getOne();
    //  @object.heldObject.Value = null;
    //  @object.Location = location;
    //  @object.TileLocation = centerPos;
    //  location.objects.Add(centerPos, @object);
    //  Utils.UpdateTapperProduct(@object);
    //  location.playSound("axe");
    //  Utils.Shake(feature, centerPos);
    //  __result = true;
    //  return false;
    //}
    if (Utils.IsCrabPot(__instance) &&
        CrabPot.IsValidCrabPotLocationTile(location,
          (int)vector.X, (int)vector.Y)) {
      SObject @object = (SObject)__instance.getOne();
      __result = CustomCrabPotUtils.placementAction(@object, location, x, y, who);
      //Game1.player.reduceActiveItemByOne();
      @object.performDropDownAction(who);
      return false;
    }
    return true;
  }

  static void SObject_performRemoveAction_Prefix(SObject __instance) {
    if (Utils.IsCrabPot(__instance)) {
      CustomCrabPotUtils.performRemoveAction(__instance.Location, __instance.TileLocation);
    }
  }

  static void SObject_actionOnPlayerEntry_Prefix(SObject __instance) {
    if (Utils.IsCrabPot(__instance)) {
      CustomCrabPotUtils.actionOnPlayerEntry(__instance.Location, __instance.TileLocation);
    }
  }

  static void SObject_updateWhenCurrentLocation_Prefix(SObject __instance, GameTime time) {
    if (Utils.IsCrabPot(__instance)) {
      CustomCrabPotUtils.updateWhenCurrentLocation(__instance, time);
    }
  }
  

  // If a tapper is present, only shake and remove the tapper instead of damaging the tree.
  static bool FruitTree_performToolAction_Prefix(FruitTree __instance, ref bool __result, Tool t, int explosion, Vector2 tileLocation) {
    if (__instance.Location.objects.TryGetValue(tileLocation, out SObject obj) &&
        obj.IsTapper()) {
      __instance.shake(tileLocation, false);
      return false;
    }
    return true;
  }

  // If a tapper is present, only shake and remove the tapper instead of damaging the tree.
  static bool Tree_performToolAction_Prefix(Tree __instance, ref bool __result, Tool t, int explosion, Vector2 tileLocation) {
    if (__instance.Location.objects.TryGetValue(tileLocation, out SObject obj) &&
        obj.IsTapper() && !__instance.tapped.Value) {
      __instance.shake(tileLocation, false);
      return false;
    }
    return true;
  }

  // If a tapper is present, only shake and remove the tapper instead of damaging the tree.
  static bool GiantCrop_performToolAction_Prefix(GiantCrop __instance, ref bool __result, Tool t, int damage, Vector2 tileLocation) {
    Vector2 centerPos = __instance.Tile;
        centerPos.X = (int)centerPos.X + (int)__instance.width.Value / 2;
        centerPos.Y = (int)centerPos.Y + (int)__instance.height.Value - 1;
    if (__instance.Location.objects.TryGetValue(centerPos, out SObject obj) &&
        obj.IsTapper() && t.isHeavyHitter() && !(t is MeleeWeapon)) {
      // Has tapper, try to dislodge it
      // For some reason performToolAction on the object directly doesn't work
      obj.playNearbySoundAll("hammer");
      obj.performRemoveAction();
      __instance.Location.objects.Remove(centerPos);
      Game1.createItemDebris(obj, centerPos * 64f, -1);
      // Shake the crop
      __instance.shakeTimer = 100f;
      __instance.NeedsUpdate = true;
      return false;
    }
    return true;
  }

  // For tappers: Save the currently held item so the PreviousItemId rule can work, and regenerate the output if that is enabled
  static bool SObject_checkForAction_Prefix(SObject __instance, out Item __state, ref bool __result, Farmer who, bool justCheckingForActivity) {
    __state = null;
    // Crab pot code
    if (Utils.IsCrabPot(__instance) && CustomCrabPotUtils.checkForAction(__instance, who, justCheckingForActivity)) {
      __result = true;
      return false;
    }
    // Common code
    if ((!__instance.IsTapper() && !Utils.IsCrabPot(__instance)) || justCheckingForActivity || !__instance.readyForHarvest.Value) return true;
    __state = __instance.heldObject.Value;
    var rules = Utils.GetOutputRulesForPlacedTapper(__instance, out var unused, out var unused2, __instance.lastOutputRuleId.Value);
    if (rules != null && rules.Count > 0 && rules[0].RecalculateOnCollect) {
      Item newItem = ItemQueryResolver.TryResolveRandomItem(rules[0], new ItemQueryContext(__instance.Location, who, null),
          avoidRepeat: false, null, (string id) =>
          id.Replace("DROP_IN_ID", /*inputItem?.QualifiedItemId ??*/ "0")
          .Replace("NEARBY_FLOWER_ID", MachineDataUtility.GetNearbyFlowerItemId(__instance) ?? "-1"));
      if (newItem is SObject newObject)
      __instance.heldObject.Value = newObject;
    }
    return true;
  }

  // Update the tapper product after collection
  static void SObject_checkForAction_Postfix(SObject __instance, Item __state, bool __result, Farmer who, bool justCheckingForActivity) {
    if (__state == null || !__result) return;
    Utils.UpdateTapperProduct(__instance);
    if (Utils.IsCrabPot(__instance)) {
      CustomCrabPotUtils.resetRemovalTimer(__instance);
    }
  }

  static bool SObject_draw_Prefix(SObject __instance, SpriteBatch spriteBatch, int x, int y, float alpha = 1f) {
    // Tapper draw code
    // Draw an extra sprite on top of the fruit tree. Ugh...
    //if (__instance.IsTapper() && __instance.Location != null &&
    //    Utils.GetFeatureAt(__instance.Location, __instance.TileLocation, out var feature, out var unused) &&
    //    feature is FruitTree) {
		//	float layer = (float)((y + 1) * 64) / 10000f + __instance.TileLocation.X / 50000f;
	  //	layer += 1e-06f;
    //  __instance.draw(spriteBatch, x*64, (y-1)*64, layer, alpha);
    //  return true;
    //}

    // Crab pot draw code
    if (Utils.IsCrabPot(__instance) && __instance.Location != null) {
      CustomCrabPotUtils.draw(__instance, spriteBatch, x, y, alpha);
      return false;
    }

    return true;
  }

  // Patch the draw code to push the tapper draw layer up a tiny amount. ugh...
  public static IEnumerable<CodeInstruction> SObject_draw_Transpiler(IEnumerable<CodeInstruction> instructions) {
    var codes = new List<CodeInstruction>(instructions);
    bool afterIsTapperCall = false;
    for (var i = 0; i < codes.Count; i++) {
      if (codes[i].opcode == OpCodes.Callvirt &&
          codes[i].operand is MethodInfo method &&
          method == AccessTools.Method(typeof(SObject), nameof(SObject.IsTapper))) {
        afterIsTapperCall = true;
      }
      if (afterIsTapperCall &&
          codes[i].opcode == OpCodes.Call &&
          codes[i].operand is MethodInfo method2 &&
          method2 == AccessTools.Method(typeof(Math), nameof(Math.Max))) {
        afterIsTapperCall = false;
        // 0.001f seems to work...
        // TODO: calc this better
        yield return new CodeInstruction(OpCodes.Ldc_R4, 0.001f);
        yield return new CodeInstruction(OpCodes.Add);
      }
      yield return codes[i];
    }
  }


  static bool Tree_UpdateTapperProduct_Prefix(Tree __instance, SObject tapper, SObject previousOutput, bool onlyPerformRemovals) {
    var rules = Utils.GetOutputRules(tapper, __instance, TileFeature.REGULAR, out var disallowBaseTapperRules);
    if (rules != null || disallowBaseTapperRules) {
      return false;
    }
    return true;
  }
}
