using ManyConsole;

namespace ActivityDiagram.Application.Sample;

internal class Program
{
    private static int Main(string[] args) => ConsoleCommandDispatcher.DispatchCommand(GetCommands(), args, Console.Out);

    public static IEnumerable<ConsoleCommand> GetCommands() => ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Program));
}
