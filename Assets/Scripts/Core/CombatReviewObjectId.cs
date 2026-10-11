using System.Globalization;
using UnityEngine;
namespace Emberfall
{
    // Opaque, session-local identity only: never persist in saves or compare across runs.
    // Decimal strings retain every bit and avoid JSON consumers rounding 64-bit numbers.
    // No registry/cache retains Unity objects; null/destroyed objects use the same "0" sentinel.
    public static class CombatReviewObjectId
    {
        public static string Get(Object value)
        {
            if (value == null) return "0";
#if UNITY_6000_4_OR_NEWER || UNITY_6000_6_OR_NEWER
            return EntityId.ToULong(value.GetEntityId()).ToString(CultureInfo.InvariantCulture);
#else
            return value.GetInstanceID().ToString(CultureInfo.InvariantCulture);
#endif
        }
    }
}
