# EntityFramework.TypedOriginalValues
Get typed access to the `OriginalValue`s of your entity properties. Simple and complex properties are supported, navigation/collections are not.

## Usage
```csharp
using (var context = new Context()) {
	var me = context.People.Single(x => x.Name == "Nick");
	me.NumericValue = 42;

	// old and busted
	var ogNumVal = (int) context.Entry(me).Property(nameof(Numberic)).OriginalValue;

	// new hotness
	var originalNumericValue = context.GetOriginal(me).NumericValue; // compile-time type-checked

	context.SaveChanges();
}
```

## How it works
A type is emitted at run-time which wraps the change tracking object of your `DbContext`. This type inherits from your entity type, so that it acts like a read-only snapshot of your entities original state.