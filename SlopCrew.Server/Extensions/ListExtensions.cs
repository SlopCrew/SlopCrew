namespace SlopCrew.Server.Extensions {
    public static class ListExtensions {
        private static readonly Random Rng = new Random();

        public static void Shuffle<T>(this List<T> list) {
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = Rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
