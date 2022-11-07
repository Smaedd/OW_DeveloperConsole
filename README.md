# Developer Console
This mod adds a general-purpose developer console in the game to be used by other mods. 

![Ingame Screenshot](https://raw.githubusercontent.com/Smaedd/OW_DeveloperConsole/main/docs/ConsoleIngame.png)

To open the console, press the `~` key ingame.

To try out the console with some premade commands, download the [Console Cheats](https://github.com/Smaedd/OW_ConsoleCheats) mod.

### Native commands

Currently, the console has four 'native' commands (commands that do not require another mod). These are:
`help`: Prints out every console variable and command
`find (string search)`: Prints out every console variable and command that contains the given argument in its name
`clear`: Clears the console log
`bind (string key) (string command)`: Binds a key to a command (names are given by those in the UnityEngine.KeyCode enum)

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
	[ConsoleData("test_convar")]
	public static float TestConvar;
```

This can be set ingame by running, for example `test_convar 3.2` in the console. Additionally, running `test_convar` alone will print the value of `TestConvar` in the console.

A property can also be used as a console variable:
```cs
	private static float _TestConvarProp 

	[ConsoleData("test_convar_property")]
	public static float TestConvarProp 
	{
		get => _TestConvarProp % 5;
		set
		{
			_TestConvarProp = value % 10;
		}
	}
```

To create a new console command, write something similar to the following inside your container class:
```cs
	[ConsoleData("test_concommand")]
	public static void TestConCommand(float value, float second = 1f) 
	{
		TestConvar = value * 2f + second;
	}
```

This command can be run ingame by running, for example, `test_concommand 1.3` in the console. If the wrong number of arguments are given, an error occurs, but there is support for default parameters in console commands.

If a console command should output to the console, the ConsoleWrapper variable created earlier can be used. Assuming this value is stored in the static variable `DevConsole`:
```cs
	[ConsoleData("test_printcommand")]
	public static void TestPrintCommand(int number)
	{
		DevConsole.Log($"You ran TestPrintCommand with {number}!");
	}
```

**WARNING:** All fields, properties, and methods that use the `ConsoleData` attribute MUST be static to function properly.
