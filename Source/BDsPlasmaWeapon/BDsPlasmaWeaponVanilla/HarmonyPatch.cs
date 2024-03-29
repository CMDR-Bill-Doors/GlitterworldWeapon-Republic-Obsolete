﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDsPlasmaWeaponVanilla
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        private static readonly Type patchType = typeof(HarmonyPatches);
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("BDsPlasmaWeapon");

            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "PostProcessGeneratedGear"), postfix: new HarmonyMethod(patchType, nameof(PostProcessGeneratedGear_Postfix)));

            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.DrawEquipmentAiming))]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();
            MethodInfo drawMeshMI = typeof(Graphics).GetMethod(nameof(Graphics.DrawMesh),
                new Type[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(int) });
            int index = codes.FindIndex((x) => x.Calls(drawMeshMI));
            MethodInfo myDrawingMethod = typeof(HarmonyPatches).GetMethod(nameof(HarmonyPatches.DrawEquipmentAiming_postfix));
            codes.InsertRange(index + 1, new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldloc_1),
            new CodeInstruction(OpCodes.Call, myDrawingMethod)
            });

            return codes;
        }

        public static void PostProcessGeneratedGear_Postfix(Thing gear)
        {
            CompReloadableFromFiller comp = gear.TryGetComp<CompReloadableFromFiller>();
            if (comp != null)
            {
                comp.remainingCharges = comp.MaxCharges;
            }
        }

        public static void DrawEquipmentAiming_postfix(Thing eq, Vector3 drawLoc, Mesh mesh, float num)
        {
            DefModExtension_WeaponGlowRender renderExtension = eq.def.GetModExtension<DefModExtension_WeaponGlowRender>();
            if (renderExtension != null)
            {
                Graphics.DrawMesh(material: renderExtension.graphicData.Graphic.MatSingle, mesh: mesh, position: drawLoc, rotation: Quaternion.AngleAxis(num, Vector3.up), layer: 0);
            }
        }
    }

    [HarmonyPatch]
    internal class Harmony_PlaceWorker_ShowTurretRadius
    {
        private const string className = "<>c";

        private const string methodName = "<AllowsPlacing>";

        public static MethodBase TargetMethod()
        {
            IEnumerable<Type> source = from x in typeof(PlaceWorker_ShowTurretRadius).GetNestedTypes(AccessTools.all)
                                       where x.Name.Contains("<>c")
                                       select x;
            MethodInfo methodInfo = source.SelectMany((Type x) => x.GetMethods(AccessTools.all)).FirstOrDefault((MethodInfo x) => x.Name.Contains("<AllowsPlacing>"));
            return methodInfo;
        }

        [HarmonyPostfix]
        public static void PostFix(VerbProperties v, ref bool __result)
        {
            __result = __result || v.verbClass == typeof(Verb_ShootOverchargeDamage);
        }
    }

    public class DefModExtension_WeaponGlowRender : DefModExtension
    {
        public GraphicData graphicData;
    }
}
