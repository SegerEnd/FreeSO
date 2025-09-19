namespace FSO.DotNet9
{
    public static class Program
    {
        public static bool UseDX = false;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        static void Main(string[] args)
        {
            using var game = new FSO.DotNet9.Game1();
            game.Run();
            //if ((new FSOProgram()).InitWithArguments(args))
            //    (new GameStartProxy()).Start(UseDX);
        }
    }
}