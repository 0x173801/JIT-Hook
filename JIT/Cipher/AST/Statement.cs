using System;

namespace jit_winform.Cipher.AST {
	public abstract class Statement {
		public object Tag { get; set; }
		public abstract override string ToString();
	}
}