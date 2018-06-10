﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace DanSerialiser
{
	public sealed class BinaryWriter : IWrite
	{
		private readonly List<byte> _data;
		public BinaryWriter()
		{
			_data = new List<byte>();
		}

		public void Byte(byte value)
		{
			_data.Add((byte)DataType.Byte);
			_data.Add(value);
		}

		public void Int32(int value)
		{
			_data.Add((byte)DataType.Int);
			IntWithoutDataType(value);
		}

		public void String(string value)
		{
			_data.Add((byte)DataType.String);
			StringWithoutDataType(value);
		}

		public void ListStart<T>(T value)
		{
			_data.Add((byte)DataType.ListStart);
			StringWithoutDataType(value?.GetType()?.AssemblyQualifiedName);
			if (value == null)
				return;
			if (!(value is IEnumerable enumerableValue))
				throw new ArgumentException("Unable to process list as value does not implement IEnumerable");
			var count = 0;
			foreach (var item in enumerableValue)
				count++;
			IntWithoutDataType(count);
		}

		public void ListEnd()
		{
			_data.Add((byte)DataType.ListEnd);
		}

		public void ObjectStart<T>(T value)
		{
			_data.Add((byte)DataType.ObjectStart);
			StringWithoutDataType(value?.GetType()?.AssemblyQualifiedName);
		}

		public const string FieldTypeNamePrefix = "#type#";
		public void FieldName(string name, string typeNameIfRequired)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			_data.Add((byte)DataType.FieldName);
			if (typeNameIfRequired != null)
				StringWithoutDataType(FieldTypeNamePrefix + typeNameIfRequired);
			StringWithoutDataType(name);
		}

		public void ObjectEnd()
		{
			_data.Add((byte)DataType.ObjectEnd);
		}

		public byte[] GetData()
		{
			return _data.ToArray();
		}

		private void IntWithoutDataType(int value)
		{
			_data.AddRange(BitConverter.GetBytes(value));
		}

		private void StringWithoutDataType(string value)
		{
			if (value == null)
			{
				_data.AddRange(BitConverter.GetBytes(-1));
				return;
			}
			var bytes = Encoding.UTF8.GetBytes(value);
			_data.AddRange(BitConverter.GetBytes(bytes.Length));
			_data.AddRange(bytes);
		}
	}
}