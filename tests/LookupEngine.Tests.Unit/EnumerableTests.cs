namespace LookupEngine.Tests.Unit;

/// <summary>
///     Tests for IEnumerable decomposition
/// </summary>
public sealed class EnumerableTests
{
    [Test]
    public async Task Decompose_List_IncludesItemsAsMembers()
    {
        // Arrange
        var list = new List<int> {1, 2, 3, 4, 5};

        // Act
        var result = LookupComposer.Decompose(list);

        // Assert
        var enumerableMembers = result.Members.Where(member => member.Name.Contains(nameof(Int32))).ToList();
        using (Assert.Multiple())
        {
            await Assert.That(result.Members).IsNotEmpty();
            await Assert.That(enumerableMembers).Count().IsEqualTo(5);
        }
    }

    [Test]
    public async Task Decompose_Array_IncludesArrayElements()
    {
        // Arrange
        var array = new[] {"First", "Second", "Third"};

        // Act
        var result = LookupComposer.Decompose(array);

        // Assert
        await Assert.That(result.Members).IsNotEmpty();
        var arrayMembers = result.Members.Where(member => member.Name.Contains(nameof(String))).ToList();
        
        await Assert.That(arrayMembers).Count().IsEqualTo(3);
    }

    [Test]
    public async Task Decompose_EmptyList_HasNoEnumerableMembers()
    {
        // Arrange
        var list = new List<int>();

        // Act
        var result = LookupComposer.Decompose(list);

        // Assert
        var enumerableMembers = result.Members.Where(member => member.Name.Contains(nameof(Int32))).ToList();
        using (Assert.Multiple())
        {
            await Assert.That(result.Members).IsNotNull();
            await Assert.That(enumerableMembers).IsEmpty();
        }
    }

    [Test]
    public async Task Decompose_Dictionary_IncludesKeyValuePairs()
    {
        // Arrange
        var dictionary = new Dictionary<string, int>
        {
            ["one"] = 1,
            ["two"] = 2
        };

        // Act
        var result = LookupComposer.Decompose(dictionary);

        // Assert
        var pairMembers = result.Members
            .Where(member => member.Name.Contains($"{nameof(Dictionary<,>)}<{nameof(String)}, {nameof(Int32)}>"))
            .ToList();

        using (Assert.Multiple())
        {
            await Assert.That(result.Members).IsNotEmpty();
            await Assert.That(pairMembers).Count().IsEqualTo(2);
        }
    }

    [Test]
    public async Task Decompose_CustomEnumerable_HandlesCorrectly()
    {
        // Arrange
        var customEnumerable = new CustomEnumerable();

        // Act
        var result = LookupComposer.Decompose(customEnumerable);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result).IsNotNull();
            await Assert.That(result.Members).IsNotEmpty();
        }
    }

    [Test]
    public async Task Decompose_EnumerableWithNullElements_HandlesNulls()
    {
        // Arrange
        var list = new List<string?> {"First", null, "Third"};

        // Act
        var result = LookupComposer.Decompose(list);

        // Assert
        var enumerableMembers = result.Members.Where(member => member.Name.Contains(nameof(String))).ToList();
        using (Assert.Multiple())
        {
            await Assert.That(result.Members).IsNotEmpty();
            await Assert.That(enumerableMembers).Count().IsEqualTo(3);
        }
    }

    [Test]
    public async Task Decompose_NestedEnumerable_HandlesRecursion()
    {
        // Arrange
        var nestedList = new List<List<int>>
        {
            new() {1, 2},
            new() {3, 4}
        };

        // Act
        var result = LookupComposer.Decompose(nestedList);

        // Assert
        await Assert.That(result.Members).IsNotEmpty();
    }

    [Test]
    public async Task Decompose_String_TreatedAsEnumerable()
    {
        // Arrange
        var text = "Test";

        // Act
        var result = LookupComposer.Decompose(text);

        // Assert
        var charMembers = result.Members.Where(member => member.Name.StartsWith($"{nameof(String)}[")).ToList();
        using (Assert.Multiple())
        {
            await Assert.That(result.Members).IsNotEmpty();
            await Assert.That(charMembers).Count().IsEqualTo(4);
        }
    }

    [Test]
    public async Task Decompose_EnumerableIndexing_IsSequential()
    {
        // Arrange
        var list = new List<string> {"A", "B", "C"};

        // Act
        var result = LookupComposer.Decompose(list);

        // Assert
        var enumerableMembers = result.Members.Where(member => member.Name.Contains(nameof(String))).ToList();
        using (Assert.Multiple())
        {
            await Assert.That(enumerableMembers[0].Name).IsEqualTo($"{nameof(List<>)}<{nameof(String)}>[0]");
            await Assert.That(enumerableMembers[1].Name).IsEqualTo($"{nameof(List<>)}<{nameof(String)}>[1]");
            await Assert.That(enumerableMembers[2].Name).IsEqualTo($"{nameof(List<>)}<{nameof(String)}>[2]");
        }
    }

    [Test]
    public async Task Decompose_EnumeratorDisposed_NoMemoryLeak()
    {
        // Arrange
        var disposableEnumerable = new DisposableEnumerable();

        // Act
        var result = LookupComposer.Decompose(disposableEnumerable);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result).IsNotNull();
            await Assert.That(disposableEnumerable.IsDisposed).IsTrue();
        }
    }
}

// Test helper classes
file sealed class CustomEnumerable : IEnumerable<int>
{
    public IEnumerator<int> GetEnumerator()
    {
        yield return 1;
        yield return 2;
        yield return 3;
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

file sealed class DisposableEnumerable : IEnumerable<int>
{
    public bool IsDisposed { get; private set; }

    public IEnumerator<int> GetEnumerator()
    {
        return new DisposableEnumerator(this);
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class DisposableEnumerator(DisposableEnumerable parent) : IEnumerator<int>
    {
        private int _current;

        public int Current => _current;
        object System.Collections.IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_current >= 3) return false;
            _current++;
            return true;
        }

        public void Reset() => _current = 0;

        public void Dispose()
        {
            parent.IsDisposed = true;
        }
    }
}