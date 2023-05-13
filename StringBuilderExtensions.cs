using System.Text;

namespace SiteMapGenerator
{
    public static class StringBuilderExtensions
    {
        public static void Log(this StringBuilder stringBuilder, string message, bool writeToConsole = true)
        {
            if (writeToConsole)
            {
                Console.WriteLine(message);
            }
            stringBuilder.AppendLine(message);
        }

        public static void Log(this StringBuilder stringBuilder, string message, ConsoleColor consoleColor, bool writeToConsole = true)
        {
            if (writeToConsole)
            {
                Console.ForegroundColor = consoleColor;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            stringBuilder.AppendLine(message);
        }

        public static void LogError(this StringBuilder stringBuilder, string message, bool writeToConsole = true)
        {
            if (writeToConsole)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(message);
                Console.ResetColor();
                Console.WriteLine();
            }
            stringBuilder.AppendLine(message);
        }
    }
}
