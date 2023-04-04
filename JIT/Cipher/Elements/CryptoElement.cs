using System;

using jit_winform.Cipher.Generation;

namespace jit_winform.Cipher.Elements {
	internal abstract class CryptoElement {
		public CryptoElement(int count) {
			DataCount = count;
			DataIndexes = new int[count];
		}

		public int DataCount { get; private set; }
		public int[] DataIndexes { get; private set; }

		public abstract void Initialize(RandomGenerator random);
		public abstract void Emit(CipherGenContext context);
		public abstract void EmitInverse(CipherGenContext context);
	}
}