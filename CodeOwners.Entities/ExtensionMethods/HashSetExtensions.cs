namespace CodeOwners.Entities.ExtensionMethods
{
    public static class HashSetExtensions
    {
        public static void AddRange<TValue>(this HashSet<TValue> hashSet, IEnumerable<TValue> values) where TValue: notnull
        {
            foreach (TValue value in values)
            {
                hashSet.Add(value);
            }
        }
    }
}
