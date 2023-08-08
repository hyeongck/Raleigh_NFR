using System;

namespace ClothoSharedItems.Common
{
    [Serializable]
    public class WFValue
    {
        public int Value;
        public ulong Flag;

        public WFValue(int value, ulong flag)
        {
            Value = value; Flag = flag;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as WFValue, true);
        }

        public bool Equals(WFValue other, bool typeCheck)
        {
            if (other == null) return false;
            if (typeCheck && this.GetType() != other.GetType()) return false;
            return (Value == other.Value && Flag == other.Flag);
        }

        public override int GetHashCode()
        {
            return Value;
        }
    }
}