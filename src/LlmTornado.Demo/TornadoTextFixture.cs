using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace LlmTornado.Demo;

public class AssertionException : Exception
{
    public AssertionException(string? message) : base(message) { }
}

public abstract class TornadoTextFixture
{
    public static class Is
    {
        public static TestPredicate<T> EqualTo<T>(T expected) => new TestPredicate<T>(actual =>
            EqualityComparer<T>.Default.Equals(expected, actual),
            $"Is.EqualTo({expected})");

        public static TestPredicate<object?> Null => new TestPredicate<object?>(obj => obj is null, "Is.Null");
        public static TestPredicate<object?> NotNull => new TestPredicate<object?>(obj => obj is not null, "Is.Not.Null");
        public static TestPredicate<bool> True => new TestPredicate<bool>(b => b, "Is.True");
        public static TestPredicate<bool> False => new TestPredicate<bool>(b => !b, "Is.False");
        public static TestPredicate<object?> Not(TestPredicate<object?> pred) => new TestPredicate<object?>(x => !pred.Predicate(x), $"Is.Not({pred.Description})");
        public static TestPredicate<T> Not<T>(TestPredicate<T> pred) => new TestPredicate<T>(x => !pred.Predicate(x), $"Is.Not({pred.Description})");
        public static TestPredicate<IEnumerable<T>> Empty<T>() => new TestPredicate<IEnumerable<T>>(col => col != null && !col.Any(), "Is.Empty");
        public static TestPredicate<IEnumerable<T>> NotEmpty<T>() => new TestPredicate<IEnumerable<T>>(col => col != null && col.Any(), "Is.NotEmpty");
        public static TestPredicate<IEnumerable<T>> Unique<T>() => new TestPredicate<IEnumerable<T>>(col =>
        {
            if (col == null) return false;
            HashSet<T> set = new HashSet<T>();
            foreach (T item in col)
                if (!set.Add(item)) return false;
            return true;
        }, "Is.Unique");
        public static TestPredicate<IEnumerable<T>> Ordered<T>() where T : IComparable<T> => new TestPredicate<IEnumerable<T>>(col =>
        {
            if (col == null) return false;
            using IEnumerator<T> e = col.GetEnumerator();
            if (!e.MoveNext()) return true;
            T prev = e.Current;
            while (e.MoveNext())
            {
                if (prev.CompareTo(e.Current) > 0) return false;
                prev = e.Current;
            }
            return true;
        }, "Is.Ordered");
        public static TestPredicate<IEnumerable<T>> EquivalentTo<T>(IEnumerable<T> expected) => new TestPredicate<IEnumerable<T>>(actual =>
        {
            if (actual == null && expected == null) return true;
            if (actual == null || expected == null) return false;
            List<T> expectedList = new List<T>(expected);
            List<T> actualList = new List<T>(actual);
            if (expectedList.Count != actualList.Count) return false;
            Dictionary<T, int> expectedCounts = new Dictionary<T, int>();
            foreach (T item in expectedList.Where(item => !expectedCounts.TryAdd(item, 1))) expectedCounts[item]++;
            foreach (T item in actualList)
            {
                if (!expectedCounts.TryGetValue(item, out int count) || count == 0)
                    return false;
                expectedCounts[item]--;
            }
            return true;
        }, $"Is.EquivalentTo({expected})");
        public static TestPredicate<IEnumerable<T>> SubsetOf<T>(IEnumerable<T> superset) => new TestPredicate<IEnumerable<T>>(subset =>
        {
            if (subset == null) return false;
            HashSet<T> superSet = new HashSet<T>(superset);
            foreach (T item in subset)
                if (!superSet.Contains(item)) return false;
            return true;
        }, $"Is.SubsetOf({superset})");
        public static TestPredicate<IEnumerable<T>> SupersetOf<T>(IEnumerable<T> subset) => new TestPredicate<IEnumerable<T>>(superset =>
        {
            if (superset == null) return false;
            HashSet<T> subSet = new HashSet<T>(subset);
            foreach (T item in subSet)
                if (!superset.Contains(item)) return false;
            return true;
        }, $"Is.SupersetOf({subset})");
        public static TestPredicate<IEnumerable<T>> Length<T>(int expectedLength) =>
            new TestPredicate<IEnumerable<T>>(col => col != null && col.Count() == expectedLength, $"Is.Length({expectedLength})");
        public static TestPredicate<IEnumerable<T>> Count<T>(int expectedCount) =>
            new TestPredicate<IEnumerable<T>>(col => col != null && col.Count() == expectedCount, $"Is.Count({expectedCount})");
    }

    public class TestPredicate<T>
    {
        public Func<T, bool> Predicate { get; }
        public string Description { get; }
        public TestPredicate(Func<T, bool> predicate, string description)
        {
            Predicate = predicate;
            Description = description;
        }
    }

    protected static void AssertThat<T>(T actual, TestPredicate<T> constraint, string? message = null)
    {
        if (!constraint.Predicate(actual))
        {
            throw new AssertionException(message ?? $"AssertThat failed: Actual value '{actual}' does not match constraint: {constraint.Description}");
        }
    }
    
    protected static void AssertTrue(bool condition, string? message = null)
    {
        if (!condition)
            throw new AssertionException(message ?? "AssertTrue failed: condition was false.");
    }

    protected static void AssertFalse(bool condition, string? message = null)
    {
        if (condition)
            throw new AssertionException(message ?? "AssertFalse failed: condition was true.");
    }

    protected static void AssertEqual<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new AssertionException(message ?? $"AssertEqual failed: Expected {expected}, got {actual}.");
    }

    protected static void AssertNotNull(object? obj, string? message = null)
    {
        if (obj is null)
            throw new AssertionException(message ?? "AssertNotNull failed: object was null.");
    }

    protected static void AssertNull(object? obj, string? message = null)
    {
        if (obj is not null)
            throw new AssertionException(message ?? "AssertNull failed: object was not null.");
    }

    protected static void AssertFail(string? message = null)
    {
        throw new AssertionException(message ?? "AssertFail called.");
    }

    protected static void AssertThrows<TException>(Action code, string? message = null) where TException : Exception
    {
        try
        {
            code();
        }
        catch (Exception ex)
        {
            if (ex is TException)
                return;
            throw new AssertionException(message ?? $"AssertThrows failed: Expected exception of type {typeof(TException)}, got {ex.GetType()}.");
        }
        throw new AssertionException(message ?? $"AssertThrows failed: No exception thrown, expected {typeof(TException)}.");
    }

    public static class Assert
    {
        public static void That<T>(T actual, TornadoTextFixture.TestPredicate<T> constraint, string? message = null)
        {
            TornadoTextFixture.AssertThat(actual, constraint, message);
        }
    }
}