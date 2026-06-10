using System.Reflection;

string version = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev";

Console.WriteLine($"obd-free-tool {version}");
Console.WriteLine("An open-source, always-free OBD-II tool.");
Console.WriteLine();
Console.WriteLine("This is an early scaffold — commands are not implemented yet.");
Console.WriteLine("See docs/ARCHITECTURE.md for the planned design.");

return 0;
