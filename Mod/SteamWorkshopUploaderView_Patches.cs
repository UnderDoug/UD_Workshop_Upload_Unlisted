using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;

using XRL;
using UnityEngine.UI;
using Steamworks;

namespace UD_Workshop_Upload_Unlisted.Mod.Harmony
{
    [HarmonyPatch(typeof(SteamWorkshopUploaderView))]
    public static class SteamWorkshopUploaderView_Patches
    {
        public static ModInfo ThisMod => ModManager.GetMod("UD_Workshop_Upload_Unlisted");

        public const string Unlisted = "Unlisted";

        [HarmonyPatch(
            declaringType: typeof(SteamWorkshopUploaderView),
            methodName: nameof(SteamWorkshopUploaderView.Show))]
        [HarmonyPostfix]
        public static void Show_AddDropDownOption_Postfix(ref SteamWorkshopUploaderView __instance)
        {
            string patchMethodName = $"{nameof(SteamWorkshopUploaderView_Patches)}.{nameof(SteamWorkshopUploaderView.Show)}";

            var rootObject = AccessTools.PropertyGetter(typeof(SteamWorkshopUploaderView), "rootObject").Invoke(__instance, new object[0] { }) as UnityEngine.GameObject;

            if (rootObject == null)
            {
                ThisMod.Warn($"Failed to postfix {patchMethodName}: {nameof(rootObject)} null");
                return;
            }

            if (rootObject?.transform?.Find("DetailsPanel/Scroll View/Viewport/Content/Visibility")?.GetComponent<Dropdown>() is Dropdown dropdown
                && dropdown?.options?.Any(o => o.text == Unlisted) is false)
            {
                dropdown.AddOptions(new List<Dropdown.OptionData> { new(Unlisted) });
            }
        }

        [HarmonyPatch(
            declaringType: typeof(SteamWorkshopUploaderView),
            methodName: nameof(SteamWorkshopUploaderView.SubmitCurrentMod))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SubmitCurrentMod_ParseDropDownOption_Transpiler(
            IEnumerable<CodeInstruction> Instructions,
            ILGenerator Generator,
            MethodBase OriginalMethod
            )
        {
            string patchMethodName = $"{nameof(SteamWorkshopUploaderView_Patches)}.{nameof(SteamWorkshopUploaderView.SubmitCurrentMod)}";

            var codeMatcher = new CodeMatcher(Instructions, Generator);

            int metricsCheckSteps = 0;

            var match_value_equals_2 = new CodeMatch[]
            {
                new(OpCodes.Ldloc_0),
                new(OpCodes.Ldc_I4_2),
                new(OpCodes.Bne_Un_S),
            };

            var steamUGC_SetVisibility = AccessTools.Method(
                type: typeof(SteamUGC),
                name: nameof(SteamUGC.SetItemVisibility),
                parameters: new Type[]
                {
                    typeof(UGCUpdateHandle_t),
                    typeof(ERemoteStoragePublishedFileVisibility),
                });

            var match_SteamUGC_SetItemVisibility_Public = new CodeMatch[]
            {
                new(OpCodes.Ldloc_3),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Call, steamUGC_SetVisibility),
                new(OpCodes.Pop),
            };

            if (codeMatcher.Start().MatchEndForward(match_value_equals_2).IsInvalid)
            {
                ThisMod.Error($"Failed to transpile {patchMethodName} at {nameof(metricsCheckSteps)} {metricsCheckSteps}: {nameof(match_value_equals_2)}");
                return Instructions;
            }

            int pos_value_not_2_label = codeMatcher.Pos;
            var label_value_not_2 = (Label)codeMatcher.Operand;

            if (codeMatcher.MatchEndForward(match_SteamUGC_SetItemVisibility_Public).IsInvalid)
            {
                ThisMod.Error($"Failed to transpile {patchMethodName} at {nameof(metricsCheckSteps)} {metricsCheckSteps}: {nameof(match_SteamUGC_SetItemVisibility_Public)}");
                return Instructions;
            }

            codeMatcher
                .Insert(new CodeInstruction[]
                {
                    new(OpCodes.Ldloc_0),
                    new(OpCodes.Ldc_I4_3),
                    new(OpCodes.Bne_Un_S, label_value_not_2),


                    new(OpCodes.Ldloc_3),
                    new(OpCodes.Ldc_I4_3),
                    new(OpCodes.Call, steamUGC_SetVisibility),
                    new(OpCodes.Pop),
                })
                .CreateLabel(out var label_new_value_not_2)
                .Start()
                .Advance(pos_value_not_2_label)
                .SetOperandAndAdvance(label_new_value_not_2);

            return codeMatcher.InstructionEnumeration();
        }

        [HarmonyCleanup]
        public static Exception RenderDynamicBook_AddTitle_Cleanup(MethodBase OriginalMethod, Exception Exception)
        {
            if (OriginalMethod == null)
                return Exception;

            string patchMethodName = $"{nameof(SteamWorkshopUploaderView_Patches)}.{nameof(SteamWorkshopUploaderView.SubmitCurrentMod)}";

            if (Exception != null)
            {
                ThisMod.Warn($"Failed to transpile {patchMethodName}: {Exception}");
                return null;
            }

            MetricsManager.LogModInfo(ThisMod, $"Successfully transpiled {patchMethodName}");
            return null;
        }
    }
}
