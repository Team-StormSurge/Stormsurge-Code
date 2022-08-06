using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace StormSurge.Utils
{
    public static class ILHelper
    {
        public static MethodReference GenerateReference(this MethodInfo method)
        {
            TypeReference tRef = AssemblyDefinition.ReadAssembly(method.ReturnType.Assembly.Location).MainModule.ImportReference(method.ReturnType).Resolve();
            var mRef = new MethodReference(method.Name, tRef);
            foreach (System.Reflection.ParameterInfo param in method.GetParameters())
            {
                Type pType = param.ParameterType;
                TypeDefinition pTypeDef = AssemblyDefinition.ReadAssembly(pType.Assembly.Location).MainModule.ImportReference(pType).Resolve();
                mRef.Parameters.Add(new ParameterDefinition(pTypeDef));
            }
            Type dType = method.DeclaringType;
            mRef.DeclaringType = AssemblyDefinition.ReadAssembly(dType.Assembly.Location).MainModule.ImportReference(dType);

            return mRef;
        }
    }
}
