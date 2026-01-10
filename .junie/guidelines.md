# LookupEngine Guidelines

Guidelines for developing the LookupEngine project. These guidelines enforce a strict production quality standard, ensuring maintainability, scalability, and consistency across the codebase.

## 1. Project Structure

The solution follows a clean architecture with a clear separation between abstractions and implementation.

### 1.1. Solution Organization

*   **`/Automation`**: Build automation and CI/CD.
    *   `build`: Nuke build system for automated builds, testing, and publishing.
*   **`/source`**: Core library projects.
    *   `LookupEngine.Abstractions`: Public interfaces, contracts, and data models. **Pure C#**, framework-agnostic.
    *   `LookupEngine`: Main library implementation with reflection-based analysis engine.
*   **`/tests`**: Testing projects.
    *   `LookupEngine.Tests.Unit`: Unit tests for library functionality.
    *   `LookupEngine.Tests.Performance`: Performance benchmarks using BenchmarkDotNet.
*   **Root Level**:
    *   Configuration files: `Directory.Build.props`, `Directory.Packages.props`
    *   Documentation: `Readme.md`, `Changelog.md`, `Contributing.md`
    *   CI/CD: `.github/workflows`

## 2. Architecture Principles

LookupEngine is a high-performance reflection library designed for runtime object analysis.

### 2.1. Core Design Goals
*   **Performance:** Minimize allocations, track memory and time for each operation.
*   **Extensibility:** Descriptor-based system allows custom type handling.
*   **Safety:** Graceful error handling, no crashes during reflection.
*   **Thread-Safety:** Stateless static API with per-call instance isolation.

### 2.2. Key Components

**Abstractions Layer:**
*   `Descriptor`: Base class for type-specific behavior.
*   `IDescriptorResolver`: Resolves parametric methods/properties.
*   `IDescriptorExtension`: Adds synthetic members to types.
*   `IDescriptorRedirector`: Redirects object evaluation to another object.
*   `IDescriptorCollector`: Marker interface for UI integration.
*   `IVariant`: Represents evaluated values (single or multiple variants).

**Implementation Layer:**
*   `LookupComposer`: Main engine (static API, thread-safe).
*   `DecomposeOptions`: Configuration for decomposition behavior.
*   `TimeDiagnoser` / `MemoryDiagnoser`: Performance tracking.
*   Built-in descriptors: `ObjectDescriptor`, `StringDescriptor`, `EnumerableDescriptor`, etc.

## 3. Strict C# Production Style

All code must adhere to enterprise-grade standards. "It works" is not enough; it must be clean, readable, and robust.

### 3.1. General Principles
*   **SOLID:** strictly follow Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion.
*   **DRY (Don't Repeat Yourself):** Extract common logic to `Kinship.Common` or `Kinship.Abstractions`.
*   **Explicit over Implicit:** Code should be self-explanatory. Avoid "magic" behavior.

### 3.2. Naming Conventions
*   **Clarity is King:** Names must be descriptive.
*   **No Abbreviations:**
    *   ❌ `repo`, `config`, `ctx`, `svc`
    *   ✅ `repository`, `configuration`, `context`, `service`
*   **No Single-Letter Variables:**
    *   ❌ `p`, `i`, `e` (except in very short lambdas or for loops).
    *   ✅ `property`, `element`, `exception`.
*   **Async Suffix:** Methods returning `Task` or `Task<T>` must end with `Async`.
    *   ✅ `GetDataAsync()`

### 3.3. Formatting & Layout
*   **File-Scoped Namespaces:** Always use `namespace Kinship.Revit;` (no braces).
*   **Nullable Reference Types:** Enabled project-wide. Treat warnings as errors.
*   **Organization:**
    1.  Private Fields (if strictly necessary)
    2.  Primary Constructor
    3.  Public Properties
    4.  Public Methods
    5.  Private Methods

### 3.4. Error Handling
*   **Graceful Degradation:** LookupEngine must never crash during object analysis. Exceptions are captured and returned as `DecomposedValue` objects.
*   **Custom Exceptions:** Use `EngineException` for internal engine errors only.
*   **Validation:** Validate inputs at public API boundaries (`LookupComposer.Decompose`).
*   **Exception Propagation:** In decomposition methods (Fields, Properties, Methods), catch all exceptions and convert them to values:
    ```csharp
    catch (TargetInvocationException exception)
    {
        value = exception.InnerException;
    }
    catch (Exception exception)
    {
        value = exception;
    }
    ```

### 3.5. No Async/Await
*   LookupEngine is a **synchronous library** by design.
*   Reflection operations (`GetValue`, `Invoke`) are inherently synchronous.
*   Do **NOT** introduce `async/await` unless there's a clear performance benefit with proof.

### 3.6. Data Objects
*   **Sealed Classes:** Prefer `sealed` for data models (`DecomposedObject`, `DecomposedMember`, `DecomposedValue`).
*   **Required Properties:** Use `required` keyword for mandatory initialization.
*   **Immutability:** Use `{ get; init; }` for immutable properties.
*   **Records:** Use `record` sparingly, only for true value-based equality scenarios.

### 3.7. Thread Safety
*   LookupEngine is **thread-safe by design**.
*   Each call to `LookupComposer.Decompose()` creates a new isolated instance.
*   No shared mutable state between decomposition operations.
*   Users can call `Decompose()` concurrently from multiple threads safely.

## 4. Descriptor Development

Descriptors are the extensibility mechanism for custom type handling.

### 4.1. Descriptor Interfaces

*   **`IDescriptorResolver`**: Resolve parametric methods/properties that require context or special handling.
    ```csharp
    public Func<IVariant>? Resolve(string target, ParameterInfo[] parameters)
    {
        return target switch
        {
            nameof(MyType.MyMethod) when parameters.Length == 0 => ResolveMyMethod,
            _ => null
        };
    }
    ```

*   **`IDescriptorExtension`**: Add synthetic members (methods/properties that don't exist on the original type).
    ```csharp
    public void RegisterExtensions(IExtensionManager manager)
    {
        manager.Register("CustomProperty", () => Variants.Value(CalculateCustomValue()));
    }
    ```

*   **`IDescriptorRedirector`**: Redirect evaluation to another object (e.g., resolve ID to entity).
    ```csharp
    public bool TryRedirect(string target, out object result)
    {
        result = _database.GetById(_id);
        return result != null;
    }
    ```

### 4.2. Descriptor Guidelines

*   **Immutability:** Descriptors should be immutable after construction.
*   **Primary Constructors:** Always use C# 12 primary constructors.
*   **Context:** Use generic versions (`IDescriptorResolver<TContext>`) when you need execution context.
*   **Circular Redirects:** **AVOID** creating circular redirect chains (A→B→A). There's currently no protection.
*   **Performance:** Descriptors are called for every member evaluation. Keep them fast.

## 5. Performance Guidelines

### 5.1. Memory Allocation
*   **Pre-allocate collections:** Use capacity hints for `List<T>` when size is known.
    ```csharp
    _decomposedMembers = new List<DecomposedMember>(32); // DefaultMembersCapacity
    ```
*   **Avoid boxing:** Be mindful of value type boxing in reflection operations.

### 5.2. Reflection Optimization
*   **Cache binding flags:** Define once, reuse throughout decomposition.
*   **Type hierarchy:** Build hierarchy once, iterate backwards for inheritance chain.
*   **Enumerators:** Always dispose `IEnumerator` (use `using` or try-finally).

## 6. Testing Strategy

### 6.1. Unit Tests
*   **Framework:** TUnit.
*   **Location:** `LookupEngine.Tests.Unit`.
*   **Coverage:** Focus on descriptor behavior, edge cases, null handling.

### 6.2. Performance Tests
*   **Framework:** BenchmarkDotNet.
*   **Location:** `LookupEngine.Tests.Performance`.
*   **Purpose:** Measure reflection overhead, memory allocations, time complexity.

## 7. Package Management

*   **Centralized:** All versions are defined in `Directory.Packages.props`.
*   **Clean csproj:** No `<Version>` tags in individual project files.
*   **Multi-targeting:** Projects target `net48;net8.0;net10.0` for broad compatibility.
*   **Conditional Compilation:** Use `#if NET` for .NET-specific features (e.g., `JsonIgnore` attribute).