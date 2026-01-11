using System.Reflection;
using LookupEngine.Abstractions.Configuration;
using LookupEngine.Abstractions.Decomposition;
using LookupEngine.Descriptors;
using LookupEngine.Options;

namespace LookupEngine.Tests.Unit;

/// <summary>
/// Tests for <see cref="IDescriptorResolver"/> functionality and custom type resolution.
/// </summary>
public sealed class ResolverTests
{
    [Test]
    public async Task Decompose_IncludingUnresolvedData_ResolvedData()
    {
        //Arrange
        var data = new ResolvableObject();
        var options = new DecomposeOptions
        {
            TypeResolver = (obj, _) =>
            {
                return obj switch
                {
                    ResolvableObject => new ResolverDescriptor(),
                    _ => new ObjectDescriptor(obj)
                };
            }
        };

        //Act
        var defaultResult = LookupComposer.Decompose(data);
        var comparableResult = LookupComposer.Decompose(data, options);

        //Assert
        using (Assert.Multiple())
        {
            await Assert.That(defaultResult.Members).IsEmpty();
            await Assert.That(comparableResult.Members).IsNotEmpty();
        }
    }

    [Test]
    public async Task Decompose_IncludingUnresolvedContextData_ResolvedData()
    {
        //Arrange
        var data = new ResolvableObject();
        var context = new EngineTestContext();
        var options = new DecomposeOptions
        {
            TypeResolver = (obj, _) =>
            {
                return obj switch
                {
                    ResolvableObject => new ResolverDescriptor(),
                    _ => new ObjectDescriptor(obj)
                };
            }
        };

        var contextOptions = new DecomposeOptions<EngineTestContext>
        {
            Context = context,
            TypeResolver = (obj, _) =>
            {
                return obj switch
                {
                    ResolvableObject => new ResolverDescriptor(),
                    _ => new ObjectDescriptor(obj)
                };
            }
        };

        //Act
        var defaultResult = LookupComposer.Decompose(data);
        var comparableResult = LookupComposer.Decompose(data, options);
        var comparableContextResult = LookupComposer.Decompose(data, contextOptions);

        //Assert
        using (Assert.Multiple())
        {
            await Assert.That(defaultResult.Members).IsEmpty();
            await Assert.That(comparableResult.Members).IsNotEmpty();
            await Assert.That(comparableContextResult.Members).IsNotEmpty();
            await Assert.That(comparableContextResult.Members.Count).IsGreaterThan(comparableResult.Members.Count);
        }
    }
}

file sealed class ResolvableObject
{
    public string UnsupportedMethod(int parameter)
    {
        return parameter.ToString();
    }

    public string UnsupportedDescribedMethod(int parameter)
    {
        return parameter.ToString();
    }

    public string UnsupportedMultiMethod(int parameter)
    {
        return parameter.ToString();
    }
}

file sealed class EngineTestContext
{
    public int Version { get; } = 1;
    public string Metadata { get; } = "Test context";
}

file sealed class ResolverDescriptor : Descriptor, IDescriptorResolver, IDescriptorResolver<EngineTestContext>
{
    public ResolverDescriptor()
    {
        Name = "Redirection";
    }

    public Func<IVariant>? Resolve(string target, ParameterInfo[] parameters)
    {
        return target switch
        {
            nameof(ResolvableObject.UnsupportedMethod) => ResolveUnsupportedMethod,
            nameof(ResolvableObject.UnsupportedDescribedMethod) => ResolveUnsupportedDescribedMethod,
            _ => null
        };

        IVariant ResolveUnsupportedMethod()
        {
            return Variants.Value("Resolved");
        }

        IVariant ResolveUnsupportedDescribedMethod()
        {
            return Variants.Value("Resolved", "Value description");
        }
    }

    Func<EngineTestContext, IVariant>? IDescriptorResolver<EngineTestContext>.Resolve(string target, ParameterInfo[] parameters)
    {
        return target switch
        {
            nameof(ResolvableObject.UnsupportedMultiMethod) => ResolveUnsupportedMultiMethod,
            _ => null
        };

        IVariant ResolveUnsupportedMultiMethod(EngineTestContext context)
        {
            return Variants.Values<string>(2)
                .Add("Resolved 1")
                .Add("Resolved 2", "Value description")
                .Consume();
        }
    }
}