using System.Numerics;
using HotChocolate.Language;
using HotChocolate.Types;

namespace Application.Api.GraphQL.Extensions.ScalarTypes
{
    public class BigIntegerScalarType : ScalarType
    {
        public BigIntegerScalarType() : base("BigInteger")
        {
        }

        public override Type RuntimeType => typeof(BigInteger);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is StringValueNode;
        }

        public override object? ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is StringValueNode stringLiteral)
            {
                BigInteger ret;
                if (BigInteger.TryParse(stringLiteral.Value, out ret))
                {
                    return ret;
                }
                else
                {
                    throw new ArgumentException("Invalid numeric string.", nameof(literal));
                }
            }

            throw new ArgumentException("The BigInteger type can only parse string literals.", nameof(literal));
        }

        public override IValueNode ParseResult(object? value)
        {
            if (value is BigInteger s)
            {
                return new StringValueNode(s.ToString());
            }

            throw new ArgumentException(
                "The specified value has to be a string in order to be parsed by the BigInteger type.");
        }

        public override IValueNode ParseValue(object? value)
        {
            if (value is BigInteger s)
            {
                return new StringValueNode(s.ToString());
            }

            throw new ArgumentException(
                "The specified value has to be a string in order to be parsed by the BigInteger type.");
        }

        public override bool TryDeserialize(object? value, out object? runtimeValue)
        {
            if (value is String s)
            {
                runtimeValue = BigInteger.Parse(s);
                return true;
            }

            runtimeValue = null;
            return false;
        }

        public override bool TrySerialize(object? value, out object? resultValue)
        {
            if (value is BigInteger s)
            {
                resultValue = s.ToString();
                return true;
            }

            resultValue = null;
            return false;
        }
    }
}
