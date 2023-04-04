using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jit_winform.Core
{
    public static class Utils
    {
        public static Random rnd = new Random();
  

        public static byte[] RandomByteArr(int size)
        {
            var result = new byte[size];
            rnd.NextBytes(result);
            return result;
        }
        private static Random random = new Random();


        public static Code GetCode(bool supported = false)
        {
            var codes = new Code[] { Code.Add, Code.And, Code.Xor, Code.Sub, Code.Or, Code.Rem, Code.Shl, Code.Shr, Code.Mul };
            if (supported)
                codes = new Code[] { Code.Add, Code.Sub, Code.Xor };
            return codes[rnd.Next(0, codes.Length)];
        }
        public static Code GetCode1(bool supported = false)
        {
            var codes = new Code[] { Code.Add, Code.And, Code.Xor, Code.Sub, Code.Or, Code.Rem, Code.Shl, Code.Shr, Code.Mul };
            if (supported)
                codes = new Code[] { Code.Add, Code.Sub, Code.Xor };
            return codes[rnd.Next(0, codes.Length)];
        }
        public static FieldDefUser CreateField(FieldSig sig)
        {
           return new FieldDefUser(GenerateString(), sig, FieldAttributes.Public | FieldAttributes.Static);
        }

        public static MethodDefUser CreateMethod(ModuleDef mod)
        {
            var method = new MethodDefUser(GenerateString(), MethodSig.CreateStatic(mod.CorLibTypes.Void),
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Public | MethodAttributes.Static);
            method.Body = new CilBody();
            method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
            mod.GlobalType.Methods.Add(method);
            return method;
        }

        public static int RandomSmallInt32() => rnd.Next(15, 40);
        public static int RandomInt32() => rnd.Next(100, 500);
        public static int RandomBigInt32() => rnd.Next();
        public static bool RandomBoolean() => Convert.ToBoolean(rnd.Next(0, 2));
        public static string GenerateString()
        {
            int seed = rnd.Next();
            return (seed * 0x19660D + 0x3C6EF35).ToString("X");
        }
        internal static byte[] GetOriginalRawILBytes(this ModuleDefMD module, MethodDef methodDef)
        {
            var reader = module.Metadata.PEImage.CreateReader(methodDef.RVA);

            byte b = reader.ReadByte();

            uint codeSize = 0;
            switch (b & 7)
            {
                case 2:
                case 6:
                    codeSize = (uint)(b >> 2);
                    break;
                case 3:
                    var flags = (ushort)((reader.ReadByte() << 8) | b);
                    var headerSize = (byte)(flags >> 12);
                    reader.ReadUInt16();
                    codeSize = reader.ReadUInt32();
                    reader.ReadUInt32();

                    reader.Position = reader.Position - 12 + headerSize * 4U;
                    break;
            }

            byte[] ilBytes = new byte[codeSize];
            reader.ReadBytes(ilBytes, 0, ilBytes.Length);
            return ilBytes;
        }


        static readonly int[] Empty = new int[0];

        internal static int Locate(this byte[] self, byte[] candidate)
        {
            if (IsEmptyLocate(self, candidate))
                return -1;

            for (int i = 0; i < self.Length; i++)
            {
                if (!IsMatch(self, i, candidate))
                    continue;
                return i;
            }

            return -1;
        }

        static bool IsMatch(byte[] array, int position, byte[] candidate)
        {
            if (candidate.Length > (array.Length - position))
                return false;

            for (int i = 0; i < candidate.Length; i++)
                if (array[position + i] != candidate[i])
                    return false;

            return true;
        }

        static bool IsEmptyLocate(byte[] array, byte[] candidate)
        {
            return array == null
                || candidate == null
                || array.Length == 0
                || candidate.Length == 0
                || candidate.Length > array.Length;
        }

    }
}
