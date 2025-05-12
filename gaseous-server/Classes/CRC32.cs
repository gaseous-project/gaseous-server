using System;
using System.IO;

public static class CRC32
{
    private static readonly uint[] Table;

    // Static constructor to initialize the CRC32 table
    static CRC32()
    {
        const uint polynomial = 0xedb88320;
        Table = new uint[256];

        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (int j = 8; j > 0; j--)
            {
                if ((crc & 1) == 1)
                {
                    crc = (crc >> 1) ^ polynomial;
                }
                else
                {
                    crc >>= 1;
                }
            }
            Table[i] = crc;
        }
    }

    // Compute the CRC32 hash for a byte array
    public static uint Compute(byte[] data)
    {
        uint crc = 0xffffffff;

        foreach (byte b in data)
        {
            byte tableIndex = (byte)((crc & 0xff) ^ b);
            crc = (crc >> 8) ^ Table[tableIndex];
        }

        return ~crc;
    }

    // Compute the CRC32 hash for a file
    public static uint ComputeFile(string filePath)
    {
        uint crc = 0xffffffff;

        using (FileStream fs = File.OpenRead(filePath))
        {
            int byteRead;
            while ((byteRead = fs.ReadByte()) != -1)
            {
                byte tableIndex = (byte)((crc & 0xff) ^ (byte)byteRead);
                crc = (crc >> 8) ^ Table[tableIndex];
            }
        }

        return ~crc;
    }
}