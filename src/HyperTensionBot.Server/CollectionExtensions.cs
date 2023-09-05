namespace HyperTensionBot.Server {
    public static class CollectionExtensions {
        public static T PickRandom<T>(this IEnumerable<T> collection) {
            var rnd = new Random();
            return collection.OrderBy(e => rnd.Next()).First();
        }
    }
}
