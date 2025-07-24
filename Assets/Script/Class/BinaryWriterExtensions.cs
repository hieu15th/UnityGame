using System;
using System.IO;

public static class BinaryWriterExtensions
{
    public static void WriteInt32BigEndian(this BinaryWriter writer, int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        writer.Write(bytes);
    }
}
