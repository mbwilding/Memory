using System;

namespace MemoryManipulation
{
    class Program
    {
        static void Main()
        {
            MemoryManage memory = new();

            //memory.Write(memory.MapVisibility, 1);
            while (true)
            {
                Console.WriteLine(memory.Read(memory.MapVisibility));
            }
        }
    }
}
