using System;
using System.IO;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

using jit_winform.Core;
using jit_winform.Core.Injection;
using System.Text;
using System.Linq;



namespace jit_winform
{
    public class Protection1
    {
        static Random rnd = new Random();
        string resName = CodeUtils.GenerateString();

        private Context context;
        private List<EncryptedMethod> encryptedMethods;

        public Protection1(Context ctx)
        {
            context = ctx;
            encryptedMethods = new List<EncryptedMethod>();
        }

        public byte[] Protect()
        {
            InjectRuntime();
            SearchMethods();
            return EncryptModule();
        }

        void InjectRuntime()
        {
            var cctor = context.Module.GlobalType.FindOrCreateStaticConstructor();

            var injector = new Injector(context.Module, typeof(Runtime), false);

            injector.InjectType();

            var initMethod = injector.FindMember("Initialize") as MethodDef;
            var count = cctor.Body.Instructions.Count;
            cctor.Body.Instructions.Insert(0, OpCodes.Ldstr.ToInstruction(resName));
            cctor.Body.Instructions.Insert(1, OpCodes.Call.ToInstruction(initMethod));

            foreach (var member in injector.Members) {
                if (member is MethodDef target)
                {
                    if (target.HasBody && target.Body.HasInstructions)
                    {
                        //aaaa.Phaserdblk(target, context.Module);


                        //ControlFlow.PhaseControlFlow(target, context.Module);

                    }

                    //if (RenamerProtection.CanRename(target.DeclaringType, target))
                    //{
                    //    target.Name = CodeUtils.GenerateString();
                    //}
                }
                //else if (member is FieldDef field) {
                //    if (RenamerProtection.CanRename(field)) {
                //        field.Name = CodeUtils.GenerateString();
                //    }
                
            }
        }

        void SearchMethods()
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            foreach (TypeDef typeDef in context.Module.GetTypes())
            {
                //if (!typeDef.Namespace.Contains("Main"))
                //    continue;
                if (typeDef.IsGlobalModuleType)
                    continue;
                foreach (MethodDef methodDef in typeDef.Methods)
                {
                    //if (methodDef == context.Module.EntryPoint)
                    //    continue;
                    if (methodDef.HasGenericParameters ||
                        (methodDef.HasReturnType && methodDef.ReturnType.IsGenericParameter))
                        continue;
                    bool isConstructor = methodDef.IsStaticConstructor;
                    if (!isConstructor)
                    {
                        bool hasBody = methodDef.HasBody;
                        if (hasBody && methodDef.Body.HasInstructions)
                        {
                            //methodDef.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Box, methodDef.Module.Import(typeof(Math))));

                            //methodDef.Body.Instructions.Clear();
                            methodDef.ImplAttributes |= MethodImplAttributes.NoInlining;

                            var exceptionRef = context.Module.CorLibTypes.GetTypeRef("System", "Exception");

                            var objectCtor = new MemberRefUser(context.Module, ".ctor",
                            MethodSig.CreateInstance(context.Module.CorLibTypes.Void, context.Module.CorLibTypes.String),
                               exceptionRef);



                            methodDef.Body.Instructions.Add(OpCodes.Ldstr.ToInstruction("Error, DNGuard Runtime library not loaded!"));
                            methodDef.Body.Instructions.Add(OpCodes.Newobj.ToInstruction(objectCtor));
                            methodDef.Body.Instructions.Add(OpCodes.Throw.ToInstruction());




                            encryptedMethods.Add(new EncryptedMethod
                            {
                                Method = methodDef,
                                OriginalBytes = context.Module.GetOriginalRawILBytes(methodDef),
                                IsEncrypted = false
                            });
                        }
                    }
                }
            }

            writer.Write(encryptedMethods.Count);

            foreach (var method in encryptedMethods)
            {
                writer.Write(method.Method.MDToken.ToInt32());
                writer.Write(EncryptOrDecrypt(Convert.ToBase64String(method.OriginalBytes), "PROT44"));
            }

            var res = new EmbeddedResource(resName, stream.ToArray(), ManifestResourceAttributes.Private);

            context.Module.Resources.Add(res);
        }

        string EncryptOrDecrypt(string text, string key)
        {
            var result = new StringBuilder();

            for (int c = 0; c < text.Length; c++)
                result.Append((char)((uint)text[c] ^ (uint)key[c % key.Length]));

            return result.ToString();
        }

        byte[] EncryptModule() {
            var writerOptions = new ModuleWriterOptions(context.Module);

            writerOptions.Logger = DummyLogger.NoThrowInstance;
            writerOptions.MetadataOptions.Flags = MetadataFlags.PreserveAll;
            writerOptions.WriterEvent += OnWriterEvent;

            var result = new MemoryStream();

            context.Module.Write(result, writerOptions);

            var bytes = result.ToArray();

            foreach (var method in encryptedMethods)
            {
                if (!method.IsEncrypted) {
                    var end = (int)method.FileOffset + method.CodeSize;

                    for (int i = (int)method.FileOffset; i < end; i++)
                    {
                        //bytes[i] = (byte)rnd.Next(1, 255);
                        bytes[i] = 0x0;
                    }
                }
            }

            return bytes;
        }

        static void PrintByteArray(byte[] bytes)
        {
            var sb = new StringBuilder("new byte[] { ");
            foreach (var b in bytes)
            {
                sb.Append(b + ", ");
            }
            sb.Append("}");
            Console.WriteLine(sb.ToString());
        }

        void OnWriterEvent(object sender, ModuleWriterEventArgs e)
        {
            if (e.Event != ModuleWriterEvent.EndWriteChunks)
                return;

            var writer = e.Writer;

            foreach (var method in encryptedMethods)
            {
                var methodBody = writer.Metadata.GetMethodBody(method.Method);
                var index = CodeUtils.Locate(methodBody.Code, method.OriginalBytes);

                method.FileOffset = (uint)(((uint)methodBody.FileOffset) + index);
                method.CodeSize = method.OriginalBytes.Length;
            }
        }

        public class EncryptedMethod {
            public MethodDef Method;
            public uint FileOffset;
            public int CodeSize;
            public byte[] OriginalBytes;
            public bool IsEncrypted;
        }
    }
}
