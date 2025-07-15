using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using System.Linq;namespace NoMaxBillsRedux
{
    public class NoMaxBillsReduxMod : Mod
    {
        public NoMaxBillsReduxMod (ModContentPack contentPack) : base(contentPack)
        {
            new Harmony("com.just-harry.no-max-bills-redux").PatchAll();
        }
    }[Serializable]
internal class TranspilerFailedException : Exception
{
    public TranspilerFailedException ()
    {}

    public TranspilerFailedException (string message) : base(message)
    {}

    public TranspilerFailedException (string message, Exception innerException) : base (message, innerException)
    {}
}
[HarmonyPatch]
public static class RaiseBillCountLimitForBillStackingListing
{
    [HarmonyTargetMethods]
    static public IEnumerable<MethodBase> BillStackMaxCountRespecters ()
    {
        yield return typeof(BillStack).GetMethod(nameof(BillStack.DoListing));
        yield return typeof(ITab_Bills).GetMethod("FillTab", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    [HarmonyTranspiler]
    static public IEnumerable<CodeInstruction> RaiseBillCountLimit (
        IEnumerable<CodeInstruction> theInstructions, MethodBase original
    )
    {
        /* Here we're looking for a piece of code that looks like:
                ... BillStack.MaxCount ...
           and replacing it with some code that looks like this:
                ... int.MaxValue ...
        */

        var instructions = theInstructions.ToList();

        int patchStage = 0;

        for (int i = 0; i < instructions.Count; i++)
        {
            if (instructions[i].LoadsIntegerValue(BillStack.MaxCount))
            {
                instructions[i] = new CodeInstruction(OpCodes.Ldc_I4, int.MaxValue);
                patchStage++;
            }
        }

        if (patchStage == 0)
        {
            throw new TranspilerFailedException($"The transpiler patch for `{original.Name}` failed to apply.");
        }

        return instructions;
    }
}
internal static class CodeInstructionExtensions
{
    internal static bool LoadsIntegerValue (this CodeInstruction instruction)
    {
        return instruction.LoadsIntegerValue(out long actualValue);
    }

    internal static bool LoadsIntegerValue (this CodeInstruction instruction, long value)
    {
        return instruction.LoadsIntegerValue(out long actualValue) ? value == actualValue : false;
    }

    internal static bool LoadsIntegerValue (this CodeInstruction instruction, out long value)
    {
        int opcode = (int) (uint) (ushort) instruction.opcode.Value;

        if (opcode == 0x001F) /* Ldc_I4_S */
        {
            value = (sbyte) instruction.operand;
            return true;
        }
        else if (opcode >= 0x0015) /* Ldc_I4_M1 */
        {
            if (opcode <= 0x001E) /* Ldc_I4_8 */
            {
                value = (long) (opcode - 0x0016);
                return true;
            }

            if (opcode == 0x0020) /* Ldc_I4 */
            {
                value = (int) instruction.operand;
                return true;
            }

            if (opcode == 0x0021) /* Ldc_I8 */
            {
                value = (long) instruction.operand;
                return true;
            }
        }

        value = -1;
        return false;
    }
}}

