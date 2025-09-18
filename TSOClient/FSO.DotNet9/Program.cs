namespace FSO.DotNet9
{
    public static class Program
    {
        static void Main(string[] args)
        {
            using var game = new FSO.DotNet9.Game1();
            game.Run();
            //if ((new FSOProgram()).InitWithArguments(args))
            //    (new GameStartProxy()).Start(UseDX);
        }
    }
}