using System.Text;

namespace ClothoSharedItems.Common
{
    public sealed partial class ByteStream
    {
        public static bool Equals(ByteStream dsLeft, ByteStream dsRight)
        {
            if (dsLeft == null)
                return (dsRight == null);
            else
            {
                if (dsRight != null && dsLeft.dtBytes.Length == dsRight.dtBytes.Length)
                {
                    for (int i = 0; i < dsLeft.dtBytes.Length; i++)
                        if (dsLeft.dtBytes[i] != dsRight.dtBytes[i]) return false;
                    return true;
                }
                return false;
            }
        }

        public static ByteStream Clone(ByteStream target)
        {
            return (target == null ? null : new ByteStream((byte[])target.dtBytes.Clone()));
        }

        public static string ToText(byte[] bytes)
        {
            return ByteStream.ToText(bytes, bytes.Length);
        }

        public static string ToText(byte[] bytes, int length)
        {
            var strBuilder = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                if (strBuilder.Length > 0) strBuilder.Append(" ");
                strBuilder.Append(bytes[i].ToString("X2"));
            }
            return strBuilder.ToString();
        }
    }
}