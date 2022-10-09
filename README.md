# Developer Console
This mod adds a general-purpose developer console in the game to be used by other mods. 

![Ingame Screenshot](https://raw.githubusercontent.com/Smaedd/OW_DeveloperConsole/main/docs/ConsoleIngame.png)

To open the console, press the `~` key ingame.

To try out the console with some premade commands, download the [Console Cheats](https://github.com/Smaedd/OW_ConsoleCheats) mod.

### Using the console in your mod

To use the console in your mod, copy `DeveloperConsole/ConsoleUtil.cs` to your mod, and run: 
```cs
var consoleInterface = ModHelper.Interaction.TryGetModApi<IConsoleManager>("Smaed.DeveloperConsole");
var consoleManager = new ConsoleWrapper(consoleInterface);

consoleManager.Link(Assembly.GetExecutingAssembly());
```

This will link up your mod to the developer console, and do some extra boilerplate processing to make your life a little bit easier. 

Before creating a new console variable or console command, a container class must be made to encapsulate your variables/commands:
```cs
[ConsoleContainer]
internal static class ContainerClass
{
	// ...
}
```

To create a new console variable, write something similar to the following inside your container class:
```cs
	[Console("test_convar")]
	public static float TestConvar;
```

This can be set ingame by running, for example `test_convar 3.2` in the console. Additionally, running `test_convar` alone will print the value of `TestConvar` in the console.

A property can also be used as a console variable:
```cs
	[Console("test_convar_property")]
	public static float TestConvarProp 
	{
		get => TestConvarProp % 5;
		set
		{
			TestConvarProp = value % 10;
		}
	}
```

To create a new console command, write something similar to the following inside your container class:
```cs
	[Console("test_concommand")]
	public static void TestConCommand(float value, float second = 1f) 
	{
		TestConvar = value * 2f + second;
	}
```

This command can be run ingame by running, for example, `test_concommand 1.3` in the console. If the wrong number of arguments are given, an error occurs, but there is support for default parameters in console commands.

If a console command should output to the console, the ConsoleWrapper variable created earlier can be used. Assuming this value is stored in the static variable `DevConsole`:
```cs
	[Console("test_printcommand")]
	public static void TestPrintCommand(int number)
	{
		DevConsole.Log($"You ran TestPrintCommand with {number}!");
	}
```

**WARNING:** All fields, properties, and methods that use the `Console` attribute MUST be static to function properly.
