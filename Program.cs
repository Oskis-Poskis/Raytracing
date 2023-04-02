namespace Raytracing
{
    class program
    {
        static void Main()
        {
            using Renderer program = new Renderer(1280, 786, "Game Engine");
            program.Run();
        }
    }
}