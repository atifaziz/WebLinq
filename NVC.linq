<Query Kind="Program">
  <Namespace>System.Collections.Specialized</Namespace>
</Query>

void Main()
{
    var q =
        from c1  in Nvc.Collect()
        from foo in Nvc.Get("foo")
        from bar in Nvc.Get("bar")
        from qux in Nvc.Set("foo", bar)
        from __  in Nvc.SetAllMatching(".+", "X")
        from c2  in Nvc.Collect()
        from _   in Nvc.Clear()
        select new { foo, bar, qux, c1, c2 };

    var coll = new NameValueCollection
    {
        ["foo"] = "bar",
        ["bar"] = "baz",
    };

    q(coll).Dump();
}

static class Nvc
{
    public static Func<NameValueCollection, T> Return<T>(T value) =>
        _ => value;

    public static Func<NameValueCollection, IEnumerable<string>> Keys() =>
        coll => coll.Keys.Cast<string>();
    
    public static Func<NameValueCollection, string> Get(string key) =>
        coll => coll[key];

    public static Func<NameValueCollection, Unit> Set(string key, string value) =>
        Reader.Do<NameValueCollection>(coll => coll[key] = value);

    public static Func<NameValueCollection, Unit> Set(IEnumerable<string> keys, string value) =>
        from _ in Reader.For(keys, k => Set(k, value))
        select new Unit();

    static Func<NameValueCollection, string> TrySet(Func<IEnumerable<string>, string> matcher, string value) =>
        from ks in Keys()
        select matcher(ks) into k
        from r in k != null
                ? from _ in Set(k, value) select k
                : Return((string) null)
        select r;

    public static Func<NameValueCollection, string> SetOneMatching(Func<string, bool> matcher, string value) =>
        TrySet(ks => ks.Single(matcher), value);
        
    public static Func<NameValueCollection, string> SetOneMatching(string pattern, string value) =>
        SetOneMatching(k => Regex.IsMatch(k, pattern), value);

    public static Func<NameValueCollection, string> TrySetOneMatching(Func<string, bool> matcher, string value) =>
        TrySet(ks => ks.SingleOrDefault(matcher), value);

    public static Func<NameValueCollection, string> TrySetOneMatching(string pattern, string value) =>
        TrySetOneMatching(k => Regex.IsMatch(k, pattern), value);

    public static Func<NameValueCollection, string> SetFirstMatching(Func<string, bool> matcher, string value) =>
        TrySet(ks => ks.First(matcher), value);

    public static Func<NameValueCollection, string> TrySetFirstMatching(Func<string, bool> matcher, string value) =>
        TrySet(ks => ks.FirstOrDefault(matcher), value);

    public static Func<NameValueCollection, string> TrySetFirstMatching(string pattern, string value) =>
        TrySetFirstMatching(k => Regex.IsMatch(k, pattern), value);

    public static Func<NameValueCollection, IEnumerable<string>> SetAllMatching(Func<string, bool> matcher, string value) =>
        from ks in Keys()
        select ks.Where(matcher).ToArray() into ks
        from _ in Set(ks, value)
        select ks;

    public static Func<NameValueCollection, IEnumerable<string>> SetAllMatching(string pattern, string value) =>
        SetAllMatching(k => Regex.IsMatch(k, pattern), value);
    
    public static Func<NameValueCollection, Unit> Merge(NameValueCollection other) =>
        Reader.Do<NameValueCollection>(coll => coll.Add(other));

    public static Func<NameValueCollection, NameValueCollection> Collect() =>
        coll => new NameValueCollection(coll);

    public static Func<NameValueCollection, Unit> Clear() =>
        coll => { coll.Clear(); return new Unit(); };
}

static class Reader
{
    public static Func<E, T> Return<E, T>(T value) => _ => value;

    public static Func<E, U> Bind<E, T, U>(this Func<E, T> m, Func<T, Func<E, U>> f) =>
        env => f(m(env))(env);

    public static Func<E, U> Select<E, T, U>(this Func<E, T> m, Func<T, U> f) =>
        env => f(m(env));

    public static Func<E, TResult> SelectMany<E, TFirst, TSecond, TResult>(
        this Func<E, TFirst> m,
        Func<TFirst, Func<E, TSecond>> f,
        Func<TFirst, TSecond, TResult> r) => env =>
        {
            var t = m(env);
            return r(t, f(t)(env));
        };

    /*
    public static Func<E, IEnumerable<TResult>> For<E, T, TResult>(IEnumerable<T> source,
        Func<T, Func<E, TResult>> f) =>
        env => from e in source
               select f(e)(env);
    */
    
    public static Func<E, IEnumerable<TResult>> For<E, T, TResult>(IEnumerable<T> source,
        Func<T, Func<E, TResult>> f) =>
        coll =>
        {
            var list = new List<TResult>();
            foreach (var e in from e in source select f(e))
                list.Add(e(coll));
            return list;
        };

    public static Func<E, T> Do<E, T>(this Func<E, T> m, Action<E> action) =>
        m.Bind<E, T, T>(x => env => { action(env); return x; });

    public static Func<E, Unit> Do<E>(Action<E> action) =>
        env => { action(env); return new Unit(); };
}

struct Unit { }