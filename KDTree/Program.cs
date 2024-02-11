using System;
using System.Collections.Generic;

namespace KDTree
{
    internal class Program
    {
        static void Main(string[] args)
        {
            List<List<int>> collect = [];

            Random rnd = new();
            for (int i = 0; i < 10; i++)
            {
                collect.Add([rnd.Next(100), rnd.Next(100), rnd.Next(100)]);
            }

            KDTree<int> kDTree = new(collect.ToArray(), 3, 0);
        }
    }
}
