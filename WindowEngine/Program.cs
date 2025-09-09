using System;           
using WindowEngine;     

namespace WindowEngine
{
    class Program
    {
        // Main method
        static void Main(string[] args)
        {
            using (Game game = new Game())
            {
                game.Run();
            } 
        }
    }
}