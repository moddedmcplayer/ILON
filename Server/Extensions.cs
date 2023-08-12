namespace Server;

using System.Reflection;
using System.Reflection.Emit;
using Server.Models;

public static class Extensions
{
    public static InlinedReferences EmitTo(this ILMessage ilMessage, ILGenerator? ilGenerator, Module? metadataResolver)
    {
        var inlinedTypes = new List<Type>();
        var inlinedMethods = new List<MethodBase>();
        var inlinedStrings = new List<string>();

        var resolvedTypes = new Queue<Type>();
        var resolvedMethods = new Queue<MethodBase>();
        var resolvedStrings = new Queue<string>();

        var methodGenericArguments = ilMessage.MethodGenericArguments?.ResolveTypes();
        var classGenericArguments = ilMessage.ClassGenericArguments?.ResolveTypes();

        bool manualResolve = ilGenerator != null && ilMessage.InlinedReferences != null;
        if (manualResolve)
        {
            foreach (var serializedMethodInfo in ilMessage.InlinedReferences!.InlinedMethods)
            {
                resolvedMethods.Enqueue(serializedMethodInfo.ResolveMethod());
            }

            foreach (var inlinedString in ilMessage.InlinedReferences!.InlinedStrings)
            {
                resolvedStrings.Enqueue(inlinedString);
            }

            foreach (var inlinedType in ilMessage.InlinedReferences!.InlinedTypes)
            {
                resolvedTypes.Enqueue(inlinedType.ResolveType());
            }
        }

        for (int i = 0; i < ilMessage.Instructions.Length; i++)
        {
            var opCodeResolver = new OpCodeResolver(ilMessage.Instructions[i]);
            if (opCodeResolver.OpCode == null)
                continue;
            var opcode = opCodeResolver.OpCode.Value;
            switch (opcode.OperandType)
            {
                case OperandType.InlineI:
                    ilGenerator?.Emit(opcode, BitConverter.ToInt32(ilMessage.Instructions, i + 1));
                    i += 4;
                    break;
                case OperandType.InlineBrTarget:
                    ilGenerator?.Emit(opcode, BitConverter.ToInt32(ilMessage.Instructions, i + 1));
                    i += 4;
                    break;
                case OperandType.ShortInlineBrTarget:
                    ilGenerator?.Emit(opcode, ilMessage.Instructions[++i]);
                    break;
                case OperandType.InlineType:
                    if (metadataResolver != null)
                    {
                        var type = manualResolve ? resolvedTypes.Dequeue() :
                                metadataResolver.ResolveType(
                                    BitConverter.ToInt32(ilMessage.Instructions, i + 1),
                                    classGenericArguments,
                                    methodGenericArguments
                                );
                        inlinedTypes.Add(type);
                        ilGenerator?.Emit(opcode, type);
                    }
                    i += 4;
                    break;
                case OperandType.InlineString:
                    if (metadataResolver != null)
                    {
                        var @string = manualResolve ? resolvedStrings.Dequeue() :
                            metadataResolver.ResolveString(
                                BitConverter.ToInt32(ilMessage.Instructions, i + 1)
                                );
                        inlinedStrings.Add(@string);
                        ilGenerator?.Emit(opcode, @string);
                    }
                    i += 4;
                    break;
                default:
                    switch (opcode.FlowControl)
                    {
                        case FlowControl.Call:
                            if (metadataResolver != null)
                            {
                                var callee = manualResolve ? resolvedMethods.Dequeue() :
                                    metadataResolver.ResolveMethod(
                                            BitConverter.ToInt32(ilMessage.Instructions, i + 1), 
                                            classGenericArguments,
                                            methodGenericArguments)
                                    ?? throw new ArgumentException("Method not found");
                                inlinedMethods.Add(callee);
                                switch (callee)
                                {
                                    case MethodInfo methodInfo:
                                        ilGenerator?.Emit(opcode, methodInfo);
                                        break;
                                    case ConstructorInfo constructorInfo:
                                        ilGenerator?.Emit(opcode, constructorInfo);
                                        break;
                                    default:
                                        throw new Exception("Unknown method type");
                                }
                            }
                            i += 4;
                            break;
                        default:
                            ilGenerator?.Emit(opcode);
                            break;
                    }
                    break;
            }
        }

        return new InlinedReferences(
            inlinedTypes.Select(x => new SerializedTypeInfo(x)).ToArray(),
            inlinedMethods.Select(x => new SerializedMethodInfo(x)).ToArray(),
            inlinedStrings.ToArray());
    }

    public static MethodBase ResolveMethod(this SerializedMethodInfo serializedMethodInfo)
    {
        var declaringType = serializedMethodInfo.DeclaringType.ResolveType();
        if (serializedMethodInfo.GenericArguments.Any())
        {
            var methods = declaringType
                .GetMethods()
                .Where(x => x.Name == serializedMethodInfo.MethodName
                                     && x.GetGenericArguments().Length == serializedMethodInfo.GenericArguments.Length
                                     && x.GetParameters().Length == serializedMethodInfo.Arguments.Length)
                .ToArray();
            var existingMethod = methods.FirstOrDefault(x => x.GetGenericArguments()
                .SequenceEqual(serializedMethodInfo.GenericArguments.ResolveTypes()));
            if (existingMethod != null)
                return existingMethod;
            var genericDefinition = methods.FirstOrDefault(x => x.IsGenericMethodDefinition);
            if (genericDefinition == null)
                throw new Exception("Generic method definiton not found");
            return genericDefinition.MakeGenericMethod(serializedMethodInfo.GenericArguments.ResolveTypes());
        }
        if (serializedMethodInfo.MethodName == ".ctor")
            return declaringType.GetConstructor(serializedMethodInfo.Arguments.ResolveTypes())
                   ?? throw new ArgumentException("Constructor not found");
        return declaringType.GetMethod(serializedMethodInfo.MethodName, serializedMethodInfo.Arguments.ResolveTypes())
            ?? throw new ArgumentException("Method not found");
    }

    public static Type[] ResolveTypes(this string[] typeNames) => typeNames.Select(ResolveType).ToArray();
    public static Type ResolveType(this string typeName)
    {
        var type = Type.GetType(typeName);
        if (type != null)
            return type;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null)
                return type;
        }

        throw new ArgumentException($"Type {typeName} not found");
    }

    public static Type ResolveType(this SerializedTypeInfo serializedTypeInfo)
    {
        if (serializedTypeInfo.GenericArguments.Any())
        {
            var type = ResolveType(serializedTypeInfo.TypeName);
            var genericDefinition = type.IsGenericTypeDefinition ? type : type.GetGenericTypeDefinition();
            return genericDefinition.MakeGenericType(serializedTypeInfo.GenericArguments.ResolveTypes());
        }

        return ResolveType(serializedTypeInfo.TypeName);
    }

    public static SerializedLocalVariable[] SerializeLocalVars(this IEnumerable<LocalVariableInfo> vars)
        => vars.Select(x => new SerializedLocalVariable
        {
            Type = x.LocalType.FullName ?? throw new ArgumentException("LocalType.FullName is null"),
            Index = x.LocalIndex,
            IsPinned = x.IsPinned
        }).ToArray();
}