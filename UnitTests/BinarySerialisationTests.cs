﻿using DanSerialiser;
using Xunit;

namespace UnitTests
{
	public static class BinarySerialisationTests
	{
		[Fact]
		public static void Int32()
		{
			AssertCloneMatchesOriginal(32);
		}

		[Fact]
		public static void NullString()
		{
			AssertCloneMatchesOriginal((string)null);
		}

		[Fact]
		public static void BlankString()
		{
			AssertCloneMatchesOriginal("");
		}

		[Fact]
		public static void String()
		{
			AssertCloneMatchesOriginal("Café");
		}

		[Fact]
		public static void PrivateSealedClassWithNoMembers()
		{
			var clone = Clone(new ClassWithNoMembersAndNoInheritance());
			Assert.NotNull(clone);
			Assert.Equal(typeof(ClassWithNoMembersAndNoInheritance), clone.GetType());
		}

		[Fact]
		public static void PrivateNullSealedClassWithNoMembers()
		{
			var clone = Clone((ClassWithNoMembersAndNoInheritance)null);
			Assert.Null(clone);
		}

		[Fact]
		public static void PrivateSealedClassWithSinglePublicField()
		{
			var clone = Clone(new ClassWithSinglePublicFieldAndNoInheritance { Name = "abc" });
			Assert.NotNull(clone);
			Assert.Equal(typeof(ClassWithSinglePublicFieldAndNoInheritance), clone.GetType());
			Assert.Equal("abc", clone.Name);
		}

		[Fact]
		public static void PrivateSealedClassWithSinglePublicAutoProperty()
		{
			var clone = Clone(new ClassWithSinglePublicAutoPropertyAndNoInheritance { Name = "abc" });
			Assert.NotNull(clone);
			Assert.Equal(typeof(ClassWithSinglePublicAutoPropertyAndNoInheritance), clone.GetType());
			Assert.Equal("abc", clone.Name);
		}

		[Fact]
		public static void PrivateSealedClassWithSinglePublicReadonlyAutoProperty()
		{
			var clone = Clone(new ClassWithSinglePublicReadonlyAutoPropertyAndNoInheritance("abc"));
			Assert.NotNull(clone);
			Assert.Equal(typeof(ClassWithSinglePublicReadonlyAutoPropertyAndNoInheritance), clone.GetType());
			Assert.Equal("abc", clone.Name);
		}

		[Fact]
		public static void PrivateStructWithNoMembers()
		{
			var clone = Clone(new StructWithNoMembers());
			Assert.Equal(typeof(StructWithNoMembers), clone.GetType());
		}

		[Fact]
		public static void PrivateStructWithSinglePublicField()
		{
			var clone = Clone(new StructWithSinglePublicField { Name = "abc" });
			Assert.Equal(typeof(StructWithSinglePublicField), clone.GetType());
			Assert.Equal("abc", clone.Name);
		}

		[Fact]
		public static void PrivateStructWithSinglePublicAutoProperty()
		{
			var clone = Clone(new StructWithSinglePublicAutoProperty { Name = "abc" });
			Assert.Equal(typeof(StructWithSinglePublicAutoProperty), clone.GetType());
			Assert.Equal("abc", clone.Name);
		}

		private static void AssertCloneMatchesOriginal<T>(T value)
		{
			var clone = Clone(value);
			Assert.Equal(value, clone);
		}

		private static T Clone<T>(T value)
		{
			var writer = new BinaryWriter();
			Serialiser.Instance.Serialise(value, writer);
			var reader = new BinaryReader(writer.GetData());
			return reader.Read<T>();
		}

		private sealed class ClassWithNoMembersAndNoInheritance { }

		private sealed class ClassWithSinglePublicFieldAndNoInheritance
		{
			public string Name;
		}

		private sealed class ClassWithSinglePublicAutoPropertyAndNoInheritance
		{
			public string Name { get; set; }
		}

		private sealed class ClassWithSinglePublicReadonlyAutoPropertyAndNoInheritance
		{
			public ClassWithSinglePublicReadonlyAutoPropertyAndNoInheritance(string name)
			{
				Name = name;
			}

			public string Name { get; }
		}

		private struct StructWithNoMembers { }

		private struct StructWithSinglePublicField
		{
			public string Name;
		}

		private struct StructWithSinglePublicAutoProperty
		{
			public string Name { get; set; }
		}
	}
}