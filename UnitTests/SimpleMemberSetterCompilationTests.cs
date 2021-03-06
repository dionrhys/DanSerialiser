﻿using System;
using System.IO;
using System.Linq;
using DanSerialiser;
using DanSerialiser.CachedLookups;
using DanSerialiser.Reflection;
using Xunit;

namespace UnitTests
{
	public static class SimpleMemberSetterCompilationTests
	{
		public static class CanGenerate
		{
			[Fact]
			public static void ClassWithNoFieldsOrProperties() => AssertCanGenerateCorrectMemberSetter(new SomethingEmpty());

			/// <summary>
			/// Test a property that is a primitive type
			/// </summary>
			[Fact]
			public static void ClassWithOnlyKeyProperty() => AssertCanGenerateCorrectMemberSetter(new SomethingWithKey { Key = 123 });

			[Fact]
			public static void ClassWithOnlyKeyField() => AssertCanGenerateCorrectMemberSetter(new SomethingWithKeyField { Key = 123 });

			[Fact]
			public static void ClassWithOnlyReadOnlyKeyProperty() => AssertCanGenerateCorrectMemberSetter(new SomethingWithReadOnlyKey(key: 123));

			[Fact]
			public static void ClassWithOnlyPrivateKeyProperty() => AssertCanGenerateCorrectMemberSetter(new SomethingWithPrivateKeyField(key: 123));

			[Fact]
			public static void ClassWithOnlyPrivateKeyReadOnlyProperty() => AssertCanGenerateCorrectMemberSetter(new SomethingWithPrivateReadOnlyKeyField(key: 123));

			/// <summary>
			/// Test a property that is a string (not a primitive but treated like one in that it was an IWrite method and references of the class are never reused)
			/// </summary>
			[Fact]
			public static void ClassWithOnlyStringProperty() => AssertCanGenerateCorrectMemberSetter(new SomethingWithID { ID = "abc" });

			/// <summary>
			/// Test a property that is a string array - if a type has an IWrite method for it then a 1D array of that type is also supported (multi-dimensional arrays
			/// are not supported at the moment, see ClassWithOnlyStringArrayProperty)
			/// </summary>
			[Fact]
			public static void ClassWithOnlyStringArrayProperty() => AssertCanGenerateCorrectMemberSetter(new SomethingWithIDs { IDs = new[] { "abc", "def" } });

			/// <summary>
			/// Test a property that is a DateTime (also not a primitive but also given first class treatment by IWrite)
			/// </summary>
			[Fact]
			public static void ClassWithOnlyDateTimeProperty() => AssertCanGenerateCorrectMemberSetter(new SomethingWithModifiedDate { ModifiedAt = new DateTime(2018, 10, 9, 12, 10, 1) });

			/// <summary>
			/// This is a bit of a fat test! It is more of a final sanity test, rather than a small focussed unit test..
			/// </summary>
			[Fact]
			public static void ClassWithPropertiesCoveringEverySimpleTypeAndAsOneDimensionArraysShouldWork()
			{
				AssertCanGenerateCorrectMemberSetter(new SomethingWithAllSimpleTypesFields
				{
					Value1 = true,
					Value2 = 1,
					Value3 = 12,
					Value4 = 123,
					Value5 = 1234,
					Value6 = 12345,
					Value7 = 123,
					Value8 = 1234,
					Value9 = 12345,
					Value10 = 1.23f,
					Value11 = 12.34,
					Value12 = 123.45m,
					Value13 = 'a',
					Value14 = "abc",
					Value15 = new DateTime(2018, 10, 9, 12, 28, 53),
					Value16 = new TimeSpan(0, 12, 29, 1, 123),
					Value17 = new Guid("E1E06164-0477-4FF7-AD79-86772AE5EF7A"),

					Values1 = new[] { true },
					Values2 = new[] { (byte)1 },
					Values3 = new[] { (sbyte)12 },
					Values4 = new[] { (short)123 },
					Values5 = new[] { 1234 },
					Values6 = new[] { (long)12345 },
					Values7 = new[] { (ushort)123 },
					Values8 = new[] { (uint)1234 },
					Values9 = new[] { (ulong)12345 },
					Values10 = new[] { 1.23f },
					Values11 = new[] { 12.34 },
					Values12 = new[] { 123.45m },
					Values13 = new[] { 'a' },
					Values14 = new[] { "abc" },
					Values15 = new[] { new DateTime(2018, 10, 9, 12, 28, 53) },
					Values16 = new[] { new TimeSpan(0, 12, 29, 1, 123) },
					Values17 = new[] { new Guid("E1E06164-0477-4FF7-AD79-86772AE5EF7A") }
				});
			}
		}

		public static class CanNotGenerate
		{
			[Fact]
			public static void PropertyOfClassWithoutFirstClassSupportInIWrite() => Assert.Null(TryToGenerateMemberSetter(typeof(WrapperForSomethingEmpty)));

			/// <summary>
			/// Maybe support for multi-dimensional arrays will be added in the future but it is not supported currently (the test ClassWithOnlyStringArrayProperty
			/// illustrates that 1D arrays of simple types are supported)
			/// </summary>
			[Fact]
			public static void PropertiesThatAreMultiDimensionalArrays() => Assert.Null(TryToGenerateMemberSetter(typeof(SomethingWithMap)));

			/// <summary>
			/// Similar to PropertiesThatAreMultiDimensionalArraysWillNotWork - currently jagged arrays of basic types are not supported
			/// </summary>
			[Fact]
			public static void PropertiesThatAreJaggedArrays() => Assert.Null(TryToGenerateMemberSetter(typeof(SomethingWithJaggedMap)));
		}

		private static MemberSetterDetails TryToGenerateMemberSetter(Type type)
		{
			return
				BinarySerialisationCompiledMemberSetters.GetMemberSetterAvailability(
					type,
					DefaultTypeAnalyser.Instance,
					valueWriterRetriever: t =>
					{
						// These tests are only for "simple" member setters - ones where properties are of types that may be serialised using IWrite methods (such as
						// Boolean, String and DateTime) and not for when nested member setters are required for fields or properties of more complex types, which is
						// when non-null values would need to be returned from a valueWriterRetriever delegate
						return null;
					}
				)
				.MemberSetterDetailsIfSuccessful;
		}

		private static void AssertCanGenerateCorrectMemberSetter(object source)
		{
			var memberSetterDetails = TryToGenerateMemberSetter(source.GetType());
			Assert.NotNull(memberSetterDetails);

			byte[] serialised;
			using (var stream = new MemoryStream())
			{
				// In an ideal world, this wouldn't be so tied to implementation details of the Serialiser and BinarySerialisationWriter classes but I can't
				// immediately think of a way to fully test this otherwise - one option would be to skip the writing of the FieldNamePreLoad data and to skip
				// the ObjectStart and ObjectEnd calls and to then read the data back out via a BinarySerialisationReader and manually compare the field and
				// property names to expected values but I wanted to offload that sort of comparison work to a library like CompareNetObjects!
				// - "Optimisied member setters" are not usually generated until after an instance of a type has been serialised without, in which case all
				//   field names will have appeared in the serialised data at least once, which is important because the member setters will write out Name
				//   Reference IDs for fields and the reader needs to know what strings those IDs map on to. Since this code won't be serialising an instance
				//   of each type before using the member setter, FieldNamePreLoad data can be injected into the start of the serialised data and then the
				//   reader will be able to refer to use that to map IDs to strings.
				foreach (var fieldName in memberSetterDetails.FieldsSet)
				{
					var fieldNameBytes = new[] { (byte)BinarySerialisationDataType.FieldNamePreLoad }.Concat(fieldName.AsStringAndReferenceID).ToArray();
					stream.Write(fieldNameBytes, 0, fieldNameBytes.Length);
				}
				var writer = new BinarySerialisationWriter(stream);
				writer.ObjectStart(source.GetType());
				memberSetterDetails.GetCompiledMemberSetter()(source, writer);
				writer.ObjectEnd();
				serialised = stream.ToArray();
			}
			var clone = BinarySerialisation.Deserialise<object>(serialised);
			if (!ObjectComparer.AreEqual(source, clone, out var differenceSummaryIfNotEqual))
				throw new Exception("Clone failed: " + differenceSummaryIfNotEqual);
		}

		private class SomethingEmpty { }

		private class SomethingWithKey
		{
			public int Key { get; set; }
		}

		private class SomethingWithReadOnlyKey
		{
			public SomethingWithReadOnlyKey(int key)
			{
				Key = key;
			}
			public int Key { get; }
		}

		private class SomethingWithPrivateKeyField
		{
#pragma warning disable IDE0044 // Don't suggest changing this field, I want it how it is
			private int _key;
#pragma warning restore IDE0044
			public SomethingWithPrivateKeyField(int key)
			{
				_key = key;
			}
		}

		private class SomethingWithPrivateReadOnlyKeyField
		{
			private readonly int _key;
			public SomethingWithPrivateReadOnlyKeyField(int key)
			{
				_key = key;
			}
		}

		private class SomethingWithKeyField
		{
			public int Key;
		}

		private class SomethingWithID
		{
			public string ID { get; set; }
		}

		private class SomethingWithIDs
		{
			public string[] IDs { get; set; }
		}

		private class SomethingWithMap
		{
			public bool[,] Items { get; set; }
		}

		public class SomethingWithJaggedMap
		{
			public bool[][] Items { get; set; }
		}

		private class SomethingWithModifiedDate
		{
			public DateTime ModifiedAt { get; set; }
		}

		private class WrapperForSomethingEmpty
		{
			public SomethingEmpty Value { get; set; }
		}

		private class SomethingWithAllSimpleTypesFields
		{
			public bool Value1;
			public byte Value2;
			public sbyte Value3;
			public short Value4;
			public int Value5;
			public long Value6;
			public ushort Value7;
			public uint Value8;
			public ulong Value9;
			public float Value10;
			public double Value11;
			public decimal Value12;
			public char Value13;
			public string Value14;
			public DateTime Value15;
			public TimeSpan Value16;
			public Guid Value17;

			public bool[] Values1;
			public byte[] Values2;
			public sbyte[] Values3;
			public short[] Values4;
			public int[] Values5;
			public long[] Values6;
			public ushort[] Values7;
			public uint[] Values8;
			public ulong[] Values9;
			public float[] Values10;
			public double[] Values11;
			public decimal[] Values12;
			public char[] Values13;
			public string[] Values14;
			public DateTime[] Values15;
			public TimeSpan[] Values16;
			public Guid[] Values17;
		}
	}
}