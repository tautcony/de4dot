using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace de4dot.code.deobfuscators.dotNET_Reactor.v4
{
	class CflowConstantsInliner {
		public TypeDef Type;

		ModuleDefMD module;
		ISimpleDeobfuscator simpleDeobfuscator;
		Dictionary<IField, int> dictionary = new Dictionary<IField, int>();

		public CflowConstantsInliner(ModuleDefMD module, ISimpleDeobfuscator simpleDeobfuscator) {
			this.module = module;
			this.simpleDeobfuscator = simpleDeobfuscator;
			Find();
		}

		void Find() {
			foreach (var type in module.GetTypes()) {
				if (type.IsSealed && type.HasFields) {
					if (type.Fields.Count < 100)
						continue;
					foreach (var method in type.Methods) {
						if (!method.IsStatic)
							continue;
						if (!method.IsAssembly)
							continue;

						simpleDeobfuscator.Deobfuscate(method);

						var instrs = method.Body.Instructions;

						for (var i = 0; i < instrs.Count; i += 2) {
							var intValue = instrs[i];
							var fieldValue = i + 1 < instrs.Count ? instrs[i + 1] : null;
							if (intValue.IsLdcI4() && fieldValue?.OpCode == OpCodes.Stsfld) {
								var key = (IField)fieldValue.Operand;
								var value = intValue.GetLdcI4Value();
								if (!dictionary.ContainsKey(key))
									dictionary.Add(key, value);
								else
									dictionary[key] = value;
							}
						}

						Type = type;
						return;
					}
				}
			}
		}

		public void InlineAllConstants() {
			if (dictionary.Count == 0)
				return;

			foreach (var type in module.GetTypes()) {
				foreach (var method in type.Methods) {
					if (!method.HasBody) continue;

					var instrs = method.Body.Instructions;

					for (var i = 0; i < instrs.Count; i++) {
						var ldsfld = instrs[i];
						if (ldsfld.OpCode != OpCodes.Ldsfld)
							continue;
						if (!instrs[i + 1].IsConditionalBranch())
							continue;
						var popOrBr = instrs[i + 2];
						if (popOrBr.OpCode != OpCodes.Pop && !popOrBr.IsBr())
							continue;
						var ldsfldValue = ldsfld.Operand as IField;
						if (ldsfldValue == null)
							continue;
						if (dictionary.TryGetValue(ldsfldValue, out var value))
							instrs[i] = Instruction.CreateLdcI4(value);
					}
				}
			}
		}
	}
}
