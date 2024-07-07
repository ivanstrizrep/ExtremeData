namespace NQueens
{
    class Program
    {
        static int count = 0;

        static void Main()
        {
            Console.WriteLine("Number of queens (N):");
            var inputN = int.TryParse(Console.ReadLine(), out var N);
            while (inputN == false)
            {
                Console.WriteLine("N must be an integer! Number of queens (N): ");
                inputN = int.TryParse(Console.ReadLine(), out N);
            }

            int[] board = new int[N];
            PlaceQueen(board, 0, N);

            Console.WriteLine($"Number of combinations for {N} queens is: {count}");
            Console.ReadKey();
        }

        static void PlaceQueen(int[] board, int row, int N)
        {
            if (row == N)
            {
                // We have passed all rows so it means we have found new solution
                count++;
                return;
            }

            // Try placing queen on each column in this row
            for (int column = 0; column < N; column++)
            {
                // Check if current setup is safe
                if (IsSafePlace(board, row, column))
                {
                    board[row] = column;
                    // Check all possibilites on next row
                    PlaceQueen(board, row + 1, N);
                }
            }
        }

        static bool IsSafePlace(int[] board, int row, int column)
        {
            for (int i = 0; i < row; i++)
            {
                // We don't check if the queens are in the same row since we are always placing a queen in the new row

                // Check if queeen is in the same column as previously set queen
                if (board[i] == column)
                    return false;

                var columnDifference = Math.Abs(board[i] - column);
                var rowDifference = Math.Abs(i - row);

                // Check if Queeen is on the same diagonal - row and column difference is the same
                if (columnDifference == rowDifference)
                    return false;
            }
            return true;
        }
    }
}