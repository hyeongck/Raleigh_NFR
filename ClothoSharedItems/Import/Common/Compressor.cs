using System.IO;
using System.IO.Compression;

namespace ClothoSharedItems.Common
{
    public static class Compressor
    {
        private static ByteStream _bridgeStream = new ByteStream(100000);

        public static byte[] CompressByRFC1950(byte[] bytes)
        {
            using (MemoryStream byteStream = new MemoryStream(bytes.Length + 10))
            {
                byteStream.WriteByte(0x78);
                byteStream.WriteByte(0x9c);
                using (DeflateStream compStream = new DeflateStream(byteStream, CompressionMode.Compress, true))
                {
                    uint s1 = 1 & 0xffff;
                    uint s2 = (1 >> 16) & 0xffff;
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        byte bWrite = bytes[i];
                        s1 = (s1 + bWrite) % 65521;
                        s2 = (s2 + s1) % 65521;
                        compStream.WriteByte(bWrite);
                    }
                    compStream.Dispose();

                    byteStream.WriteByte((byte)(s2 >> 8));
                    byteStream.WriteByte((byte)s2);
                    byteStream.WriteByte((byte)(s1 >> 8));
                    byteStream.WriteByte((byte)s1);

                    return byteStream.GetBuffer().SubArray(0, (int)byteStream.Length);
                }
            }
        }

        public static byte[] DecompressByRFC1950(byte[] bytes)
        {
            if (bytes.Length > 6 && (bytes[0] == 0x78 && bytes[1] == 0x9c))
            {
                using (MemoryStream byteStream = new MemoryStream(bytes, 2, bytes.Length - 6))
                {
                    using (DeflateStream decompStream = new DeflateStream(byteStream, CompressionMode.Decompress))
                    {
                        _bridgeStream.Reset(0);

                        int iRead;
                        uint s1 = 1 & 0xffff;
                        uint s2 = (1 >> 16) & 0xffff;
                        while ((iRead = decompStream.ReadByte()) != -1)
                        {
                            byte bRead = (byte)iRead;
                            s1 = (s1 + bRead) % 65521;
                            s2 = (s2 + s1) % 65521;
                            _bridgeStream.WriteByte(bRead);
                        }
                        if (bytes[bytes.Length - 4] == (byte)(s2 >> 8) &&
                            bytes[bytes.Length - 3] == (byte)(s2) &&
                            bytes[bytes.Length - 2] == (byte)(s1 >> 8) &&
                            bytes[bytes.Length - 1] == (byte)(s1))
                        {
                            return _bridgeStream.Root.SubArray(0, _bridgeStream.Length);
                        }
                    }
                }
            }
            return null;
        }
    }
}