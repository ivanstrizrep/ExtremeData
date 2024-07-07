namespace Trees
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Minimun meters of wood (K): ");
            var inputK = int.TryParse(Console.ReadLine(), out var K);
            while (inputK == false)
            {
                Console.WriteLine("K must be an integer! Minimun meters of wood (K): ");
                inputK = int.TryParse(Console.ReadLine(), out K);
            }

            Console.WriteLine("Number of trees (N): ");
            var inputN = int.TryParse(Console.ReadLine(), out var N);
            while (inputN == false)
            {
                Console.WriteLine("N must be an integer! Number of trees (N): ");
                inputN = int.TryParse(Console.ReadLine(), out N);
            }

            var trees = new List<int>();
            for (int i = 0; i < N; i++)
            {
                Console.WriteLine($"Height of {i + 1}. tree: ");
                var inputI = int.TryParse(Console.ReadLine(), out var h);
                while (inputI == false)
                {
                    Console.WriteLine($"Height must be an integer! Height of {i + 1}. tree: ");
                    inputI = int.TryParse(Console.ReadLine(), out h);
                }
                trees.Add(h);
            }

            int maxHeight = FindMaxHeight(trees, K);

            // Check result to see if it is even possible
            int woodCollected = CalculateWood(maxHeight, trees);
            if (woodCollected >= K)
                Console.WriteLine($"Max height to collect min {K} meters of wood is: {maxHeight}. {woodCollected} meters of wood will be collected.");
            else
                Console.WriteLine($"No solution for {K} meters of wood");

            Console.ReadKey();
        }

        static int CalculateWood(int height, List<int> trees)
        {
            int woodCollected = 0;
            foreach (var tree in trees)
            {
                if (tree > height)
                {
                    woodCollected += tree - height;
                }
            }
            return woodCollected;
        }

        static int FindMaxHeight(List<int> trees, int K)
        {
            int low = 0;
            int high = trees.Max();
            int result = 0;

            // Do binary search between 0 and max tree height
            while (low <= high)
            {
                int mid = (low + high) / 2;
                int woodCollected = CalculateWood(mid, trees);

                if (woodCollected >= K)
                {
                    result = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return result;
        }
    }
}