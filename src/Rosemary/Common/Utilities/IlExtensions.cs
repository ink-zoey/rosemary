using MonoMod.Cil;

namespace Rosemary.Common;

public static class IlExtensions
{
    extension(ILCursor c)
    {
        public ILCursor EmitLdloc(VariableIndex index)
        {
            return c.EmitLdloc((int)index);
        }

        public ILCursor EmitLdloca(VariableIndex index)
        {
            return c.EmitLdloca((int)index);
        }

        public ILCursor EmitStloc(VariableIndex index)
        {
            return c.EmitStloc((int)index);
        }

        public ILCursor EmitLdarg(ParameterIndex index)
        {
            return c.EmitLdarg((int)index);
        }

        public ILCursor EmitLdarga(ParameterIndex index)
        {
            return c.EmitLdarga((int)index);
        }

        public ILCursor EmitStarg(ParameterIndex index)
        {
            return c.EmitStarg((int)index);
        }
    }
}
