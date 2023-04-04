using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.IO.Compression;
using dnlib.DotNet;
using dnlib.DotNet.Writer;

namespace jit_winform.Core
{
    public class FileContext
    {
        public MethodDef Initializer;
        public string OutPutPath { get; }

        public MethodDef Cctor { get; set; }
        public ModuleDefMD RT { get; set; }
        public bool Updated { get; set; }
        ModuleDefMD module;
        byte[] data;
        string filePath;
        byte[] outputBytes;
        public FileContext(string path)
        {
            module = ModuleDefMD.Load(path);
            //RT = ModuleDefMD.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BRRT.dll"));
            filePath = path;

            Cctor = module.GlobalType.FindOrCreateStaticConstructor();
        }

        public byte[] GetBytes()
        {
            var sec = new EmbeddedResource(CodeUtils.GenerateString(), GetCompressed());
            module.Resources.Add(sec);

            //var rtStream = new MemoryStream();
            //RT.Write(rtStream);

            //var rt = new EmbeddedResource("RT", rtStream.ToArray());

            //module.Resources.Add(rt);

            var stream = new MemoryStream();
            var options = new ModuleWriterOptions(module);
            options.MetadataLogger = DummyLogger.NoThrowInstance;

            module.Write(stream, options);

            return stream.ToArray();
        }

        public void SaveToDisk(string output)
        {
            if (outputBytes == null)
            {
                outputBytes = GetBytes();
            }
            File.WriteAllBytes(output, outputBytes);
        }

        public void SetOutPutBytes(byte[] bytes)
        {
            outputBytes = bytes;
        }

        public byte[] GetCompressed()
        {
            data = File.ReadAllBytes(filePath);
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        public ModuleDefMD Module => module;
        public string FPath => filePath;

        public byte[] Output => outputBytes;
    }
}
