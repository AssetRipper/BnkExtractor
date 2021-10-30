using System;
using System.IO;

namespace BnkExtractor.Ww2ogg;

internal static class EndianReadWriteMethods
{
	public static int ilog(uint v)
	{
		int ret = 0;
		while (v != 0)
		{
			ret++;
			v >>= 1;
		}
		return (ret);
	}

	public static uint read_32_le(byte[] b)
	{
		uint v = 0;
		for (int i = 3; i >= 0; i--)
		{
			v <<= 8;
			v |= b[i];
		}

		return v;
	}

	public static uint read_32_le(BinaryReader @is)
	{
		return read_32_le(@is.ReadBytes(4));
	}

	internal static void write_32_le(byte[] b, int offset, uint v)
	{
		if(b == null)
			throw new ArgumentNullException(nameof(b));
		if (offset < 0 || offset >= b.Length - 4)
			throw new ArgumentOutOfRangeException(nameof(offset));
		byte[] buffer = new byte[4];
		write_32_le(buffer, v);
		Array.Copy(buffer, 0, b, offset, 4);
	}

	public static void write_32_le(byte[] b, uint v)
	{
		for (int i = 0; i < 4; i++)
		{
			b[i] = (byte)(v & 0xFF);
			v >>= 8;
		}
	}

	public static void write_32_le(BinaryWriter os, uint v)
	{
		byte[] b = new byte[4];

		write_32_le(b, v);

		os.Write(b);
	}

	public static ushort read_16_le(byte[] b)
	{
		ushort v = 0;
		for (int i = 1; i >= 0; i--)
		{
			v <<= 8;
			v |= b[i];
		}

		return v;
	}

	public static ushort read_16_le(BinaryReader @is)
	{
		return read_16_le(@is.ReadBytes(2));
	}

	public static void write_16_le(byte[] b, ushort v)
	{
		for (int i = 0; i < 2; i++)
		{
			b[i] = (byte)(v & 0xFF);
			v >>= 8;
		}
	}

	public static void write_16_le(BinaryWriter os, ushort v)
	{
		byte[] b = new byte[2];

		write_16_le(b, v);

		os.Write(b);
	}

	public static uint read_32_be(byte[] b)
	{
		uint v = 0;
		for (int i = 0; i < 4; i++)
		{
			v <<= 8;
			v |= b[i];
		}

		return v;
	}

	public static uint read_32_be(BinaryReader @is)
	{
		return read_32_be(@is.ReadBytes(4));
	}

	public static void write_32_be(byte[] b, uint v)
	{
		for (int i = 3; i >= 0; i--)
		{
			b[i] = (byte)(v & 0xFF);
			v >>= 8;
		}
	}

	public static void write_32_be(BinaryWriter os, uint v)
	{
		byte[] b = new byte[4];

		write_32_be(b, v);

		os.Write(b);
	}

	public static ushort read_16_be(byte[] b)
	{
		ushort v = 0;
		for (int i = 0; i < 2; i++)
		{
			v <<= 8;
			v |= b[i];
		}

		return v;
	}

	public static ushort read_16_be(BinaryReader @is)
	{
		return read_16_be(@is.ReadBytes(2));
	}

	public static void write_16_be(byte[] b, ushort v)
	{
		for (int i = 1; i >= 0; i--)
		{
			b[i] = (byte)(v & 0xFF);
			v >>= 8;
		}
	}

	public static void write_16_be(BinaryWriter os, ushort v)
	{
		byte[] b = new byte[2];

		write_16_be(b, v);

		os.Write(b);
	}
}