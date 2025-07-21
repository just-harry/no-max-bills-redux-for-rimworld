
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;


namespace NoMaxBillsRedux
{
	public class NoMaxBillsReduxMod : Mod
	{
		public NoMaxBillsReduxMod (ModContentPack contentPack) : base(contentPack)
		{
			new Harmony("com.just-harry.no-max-bills-redux").PatchAll();
		}
	}


	[Serializable]
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
	public static class RaiseBillCountLimitForVanillaBillStackMethods
	{
		[HarmonyTargetMethods]
		static public IEnumerable<MethodBase> BillStackMaxCountRespecters ()
		{
			yield return typeof(BillStack).GetMethod(nameof(BillStack.DoListing));
			yield return typeof(ITab_Bills).GetMethod("FillTab", BindingFlags.NonPublic | BindingFlags.Instance);
		}

		[HarmonyTranspiler]
		static public IEnumerable<CodeInstruction> RaiseBillCountLimit (
			IEnumerable<CodeInstruction> theInstructions,
			MethodBase method
		)
		{
			/* Here we're looking for a piece of code that looks like:
					... BillStack.MaxCount ...
			   and replacing it with some code that looks like this:
					... 0x7FFFFFFF ...
			*/

			using IEnumerator<CodeInstruction> instructions = theInstructions.GetEnumerator();

			uint patchStage = 0;

			CodeInstruction instruction;
		findLoadOfMaxCount:
			if (!instructions.MoveNext()) goto noMoreInstructions;
			instruction = instructions.Current;

			if (!instruction.LoadsIntegerValue(BillStack.MaxCount))
			{
				yield return instruction;

				goto findLoadOfMaxCount;
			}

			instruction.opcode = OpCodes.Ldc_I4;
			instruction.operand = 0x7FFFFFFF;

			yield return instruction;

			++patchStage;
		yieldRestOfCode:
			if (!instructions.MoveNext()) goto noMoreInstructions;
			instruction = instructions.Current;

			yield return instruction;

			goto yieldRestOfCode;
		noMoreInstructions:
			if (patchStage == 1)
			{
				yield break;
			}

			throw new TranspilerFailedException($"The transpiler patch for `{method.DeclaringType?.FullName}.{method.Name}` failed to apply.");
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
	}
}

