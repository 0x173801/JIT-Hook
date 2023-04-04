using dnlib.DotNet;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace jit_winform.Core
{
    public class Context
    {
        public ModuleDefMD Module;
        public Context(byte[] bytes)
        {
            Module = ModuleDefMD.Load(bytes);

        }
    }
}
