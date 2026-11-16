// using System;

// /// <summary>
// /// Put on a bool. The bool becomes the checkbox of a foldout, and every field after it
// /// belongs to that foldout until a field marked <see cref="KHEndFoldoutToggleAttribute"/>
// /// or the next <see cref="KHFoldoutToggleAttribute"/>. Needs <c>KHFoldoutToggleEditor</c> on the type.
// /// If no title is given, the field's own name is used.
// /// </summary>
// [AttributeUsage(AttributeTargets.Field)]
// public class KHFoldoutToggleAttribute : Attribute
// {
//     /// <summary>Null means "use the field's own name".</summary>
//     public readonly string Title;

//     public KHFoldoutToggleAttribute() { }
//     public KHFoldoutToggleAttribute(string title) => Title = title;
// }

// /// <summary>Put on the last field that belongs to a <see cref="KHFoldoutToggleAttribute"/> group.</summary>
// [AttributeUsage(AttributeTargets.Field)]
// public class KHEndFoldoutToggleAttribute : Attribute { }