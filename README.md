# EntityFramework.TypedOriginalValues
Get typed access to the `OriginalValue`s of your entity properties. Simple and complex properties are supported, navigation/collections are not.

[![](https://img.shields.io/nuget/v/EntityFramework.TypedOriginalValues.svg)](https://www.nuget.org/packages/EntityFramework.TypedOriginalValues)
[![](https://img.shields.io/nuget/vpre/EntityFramework.TypedOriginalValues.svg)](https://www.nuget.org/packages/EntityFramework.TypedOriginalValues)

## Usage
```csharp
using (var context = new Context()) {
	var me = context.People.Single(x => x.Name == "Nick");
	me.EmployeeNumber = 42;

	// old and busted
	var og = (int) context.Entry(me).Property(nameof(EmployeeNumber)).OriginalValue;

	// new hotness
	var og = context.GetOriginal(me).EmployeeNumber;
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
