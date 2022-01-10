namespace ConcordiumSdk.Types;

public class ContractAddress : Address
{
   public ulong Index { get; }
   public ulong SubIndex { get; }

   public ContractAddress(ulong index, ulong subIndex)
   {
      Index = index;
      SubIndex = subIndex;
   }

   public override bool Equals(object? obj)
   {
      if (ReferenceEquals(null, obj)) return false;
      if (ReferenceEquals(this, obj)) return true;
      if (obj.GetType() != GetType()) return false;
      var other = (ContractAddress)obj;
      return Index == other.Index && SubIndex == other.SubIndex;
   }

   public override int GetHashCode()
   {
      unchecked
      {
         return (Index.GetHashCode() * 397) ^ SubIndex.GetHashCode();
      }
   }

   public static bool operator ==(ContractAddress? left, ContractAddress? right)
   {
      return Equals(left, right);
   }

   public static bool operator !=(ContractAddress? left, ContractAddress? right)
   {
      return !Equals(left, right);
   }
}