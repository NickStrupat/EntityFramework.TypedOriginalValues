# EntityFramework.TypedOriginalValues
Get typed access to the `OriginalValue`s of your entity properties. Simple and complex properties are supported, navigation/collections are not.

| EF version | .NET support                                    | NuGet package                                                                                                                                                                    |
|:-----------|:------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 6.1.3      | >= Framework 4.6.1                              | [![NuGet Status](http://img.shields.io/nuget/v/EntityFramework.TypedOriginalValues.svg?style=flat)](https://www.nuget.org/packages/EntityFramework.TypedOriginalValues/)         |
| Core 2.0   | >= Framework 4.6.1 &#124;&#124; >= Standard 2.0 | [![NuGet Status](http://img.shields.io/nuget/v/EntityFrameworkCore.TypedOriginalValues.svg?style=flat)](https://www.nuget.org/packages/EntityFrameworkCore.TypedOriginalValues/) |

## Usage
```csharp
using (var context = new Context()) {
	var me = await context.People.SingleAsync(x => x.Name == "Nick");
	me.EmployeeNumber = 42; // change the value

	// but wait! maybe we want to see what the value was inside some other mechanism, after we changed it (i.e. logging, auditing, etc.)

	// old and busted
	var og = (int) context.Entry(me).Property(nameof(EmployeeNumber)).OriginalValue;

	// new hotness
	var og = context.GetOriginal(me).EmployeeNumber;

	await context.SaveChangesAsync(); // save that new value
}
```

## How it works

A type is emitted at run-time which wraps the change tracking object of your `DbContext`. This type inherits from your entity type, so you get typed access to its properties. It behaves like a read-only snapshot of your entities original state.

## Contributing

1. [Create an issue](https://github.com/NickStrupat/EntityFramework.TypedOriginalValues/issues/new)
2. Let's find some point of agreement on your suggestion.
3. Fork it!
4. Create your feature branch: `git checkout -b my-new-feature`
5. Commit your changes: `git commit -am 'Add some feature'`
6. Push to the branch: `git push origin my-new-feature`
7. Submit a pull request :D

## History

[Commit history](https://github.com/NickStrupat/EntityFramework.TypedOriginalValues/commits/master)

## License

[MIT License](https://github.com/NickStrupat/EntityFramework.TypedOriginalValues/blob/master/README.md)
