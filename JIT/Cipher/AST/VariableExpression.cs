using System;

namespace jit_winform.Cipher.AST {
	public class VariableExpression : Expression {
		public Variable Variable { get; set; }

		public override string ToString() {
			return Variable.Name;
		}
	}
}